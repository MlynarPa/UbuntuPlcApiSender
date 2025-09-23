using System.Net.Sockets;
using System.Threading;
using S7.Net;
using UbuntuPlcApiSender.Models;
using UbuntuPlcApiSender.Services;

Console.WriteLine("=== Ubuntu PLC API Sender ===");
Console.WriteLine("Tato aplikace čte reálná data z PLC pro stroj DRST_0001");
Console.WriteLine("a odesílá je na drevostroj.app API pomocí PUT požadavku.");
Console.WriteLine();

string plcIpAddress = "192.168.0.10";
short plcRack = 0;
short plcSlot = 1;
string apiBaseUrl = "https://drevostroj.app";
string apiKey = "drevostrojapi2024";
int plcReadInterval = 5000;
int apiSendInterval = 5000;
int apiSendOffset = 1000;

string GetEnv(string key, string @default) => Environment.GetEnvironmentVariable(key) ?? @default;

plcIpAddress = GetEnv("PLC_IP", plcIpAddress);
plcRack = short.Parse(GetEnv("PLC_RACK", plcRack.ToString()));
plcSlot = short.Parse(GetEnv("PLC_SLOT", plcSlot.ToString()));
plcReadInterval = int.Parse(GetEnv("READ_INTERVAL_MS", plcReadInterval.ToString()));
apiSendInterval = int.Parse(GetEnv("SEND_INTERVAL_MS", apiSendInterval.ToString()));
apiSendOffset = int.Parse(GetEnv("SEND_OFFSET_MS", apiSendOffset.ToString()));

Console.WriteLine("=== KONFIGURACE ===");
Console.WriteLine($"PLC IP: {plcIpAddress}");
Console.WriteLine($"PLC Rack/Slot: {plcRack}/{plcSlot}");
Console.WriteLine($"API URL: {apiBaseUrl}/api/MachinesApi/DRST_0001");
Console.WriteLine($"API Key: {apiKey} (přes X-API-Key header)");
Console.WriteLine($"Interval čtení PLC: {plcReadInterval} ms");
Console.WriteLine($"Interval odesílání API: {apiSendInterval} ms (offset {apiSendOffset} ms)");
Console.WriteLine("Režim: Rychlý reconnect po chybě (max 5 s)");
Console.WriteLine();
Console.WriteLine("Pro ukončení stiskněte Ctrl+C");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    if (!cts.IsCancellationRequested)
    {
        Console.WriteLine("\n--- Ukončování aplikace... ---");
        cts.Cancel();
    }
};

using var apiClient = new ApiClient(apiBaseUrl, apiKey);

Plc? plc = null;
int backoffMs = 1000;
const int backoffMax = 5000;
DateTime nextSendTime = DateTime.UtcNow.AddMilliseconds(apiSendOffset);

bool WaitWithCancellation(int milliseconds)
{
    if (cts.IsCancellationRequested)
    {
        return true;
    }

    if (milliseconds <= 0)
    {
        return cts.IsCancellationRequested;
    }

    return cts.Token.WaitHandle.WaitOne(milliseconds);
}

bool Connect()
{
    SafeClose();

    try
    {
        plc = new Plc(CpuType.S71200, plcIpAddress, plcRack, plcSlot)
        {
            ReadTimeout = 1000,
            WriteTimeout = 1000
        };

        plc.Open();

        try
        {
            if (plc?.TcpClient?.Client is Socket socket)
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            }
        }
        catch
        {
            // Best-effort keepalive, ignorujeme případnou chybu
        }

        nextSendTime = DateTime.UtcNow.AddMilliseconds(apiSendOffset);
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔌 PLC připojeno");
        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ Connect fail: {ex.Message}");
        SafeClose();
        return false;
    }
}

void SafeClose()
{
    try
    {
        plc?.Close();
    }
    catch
    {
        // ignored
    }

    try
    {
        plc?.Dispose();
    }
    catch
    {
        // ignored
    }

    plc = null;
}

bool TryReconnect()
{
    while (!cts.IsCancellationRequested)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔁 Reconnect za {backoffMs} ms…");
        if (WaitWithCancellation(backoffMs))
        {
            return false;
        }

        if (Connect())
        {
            return true;
        }

        backoffMs = Math.Min(backoffMs * 2, backoffMax);
    }

    return false;
}

Machine ReadMachineData(Plc plcInstance)
{
    bool ReadBool(int byteOffset, int bitOffset)
    {
        var value = plcInstance.Read(DataType.DataBlock, 1, byteOffset, VarType.Bit, 1, (byte)bitOffset);
        if (value is bool b)
        {
            return b;
        }

        throw new InvalidOperationException("PLC returned unexpected boolean value");
    }

    short ReadInt(int offset)
    {
        var rawObj = plcInstance.ReadBytes(DataType.DataBlock, 1, offset, 2);
        if (rawObj is byte[] raw && raw.Length >= 2)
        {
            return BitConverter.ToInt16(new[] { raw[1], raw[0] }, 0);
        }

        throw new InvalidOperationException("PLC returned unexpected byte array for integer value");
    }

    return new Machine
    {
        Abbreviation = "DRST_0001",
        IsRunning = ReadBool(0, 0),
        PowerConsumption = ReadInt(2),
        DI1 = ReadBool(16, 0),
        DI2 = ReadBool(16, 1),
        Timestamp = DateTime.UtcNow
    };
}

bool DoOneCycle()
{
    if (plc == null)
    {
        return false;
    }

    var machine = ReadMachineData(plc);

    var now = DateTime.UtcNow;
    if (now < nextSendTime)
    {
        var waitMs = (int)Math.Clamp((nextSendTime - now).TotalMilliseconds, 0, int.MaxValue);
        if (waitMs > 0 && WaitWithCancellation(waitMs))
        {
            throw new OperationCanceledException();
        }
    }

    var success = apiClient.SendMachineDataAsync(machine.Abbreviation, machine).GetAwaiter().GetResult();
    if (!success)
    {
        throw new InvalidOperationException("API send failed");
    }

    nextSendTime = DateTime.UtcNow.AddMilliseconds(apiSendInterval);
    return true;
}

if (!Connect())
{
    if (!TryReconnect())
    {
        SafeClose();
        return;
    }
}

while (!cts.IsCancellationRequested)
{
    try
    {
        if (plc != null && plc.IsConnected && DoOneCycle())
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📊 PLC OK; posílám na API…");
            backoffMs = 1000;

            if (WaitWithCancellation(plcReadInterval))
            {
                break;
            }

            continue;
        }

        throw new InvalidOperationException("PLC cycle failed");
    }
    catch (OperationCanceledException)
    {
        break;
    }
    catch (Exception ex) when (!cts.IsCancellationRequested)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ PLC read error: {ex.Message}");
        SafeClose();

        if (!TryReconnect())
        {
            break;
        }
    }
}

SafeClose();
