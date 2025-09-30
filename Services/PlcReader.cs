using UbuntuPlcApiSender.Models;
using S7.Net;

namespace UbuntuPlcApiSender.Services;

public class PlcReader
{
    private Plc? _plc;
    private readonly string _ipAddress;
    private readonly short _rack;
    private readonly short _slot;
    private bool _isConnected = false;
    private readonly object _connectionLock = new object();

    private readonly (string Abbr, int RunByte, int RunBit, int PowerOffset, int DI1Byte, int DI1Bit, int DI2Byte, int DI2Bit)[] _machineMap =
    {
        ("DRST_0001", 0, 0, 2, 16, 0, 16, 1),
        ("DRST_0002", 4, 0, 6, 16, 2, 16, 3),
        ("DRST_0003", 8, 0, 10, 16, 4, 16, 5),
        ("DRST_0004", 12, 0, 14, 16, 6, 16, 7),
    };

    // Mapování aliasů - různé názvy pro stejné stroje
    private readonly Dictionary<string, string> _machineAliases = new()
    {
        // DRS -> DRST mapování
        {"DRS_0001", "DRST_0001"},
        {"DRS_0002", "DRST_0002"},
        {"DRS_0003", "DRST_0003"},
        {"DRS_0004", "DRST_0004"},
        
        // Další možné varianty (pro flexibilitu)
        {"DRS1", "DRST_0001"},
        {"DRS2", "DRST_0002"},
        {"DRS3", "DRST_0003"},
        {"DRS4", "DRST_0004"},
        
        {"DRST1", "DRST_0001"},
        {"DRST2", "DRST_0002"},
        {"DRST3", "DRST_0003"},
        {"DRST4", "DRST_0004"},
        
        // Přímé mapování (pro případy kdy už je název správný)
        {"DRST_0001", "DRST_0001"},
        {"DRST_0002", "DRST_0002"},
        {"DRST_0003", "DRST_0003"},
        {"DRST_0004", "DRST_0004"}
    };

    public PlcReader(string ip, short rack, short slot)
    {
        _ipAddress = ip;
        _rack = rack;
        _slot = slot;
    }

    public bool IsConnected => _isConnected;

    /// <summary>
    /// Asynchronně se pokusí připojit k PLC s retry logikou
    /// </summary>
    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        lock (_connectionLock)
        {
            if (_isConnected && _plc?.IsConnected == true)
            {
                return true;
            }

            // Zavřít existující spojení pokud existuje
            try
            {
                _plc?.Close();
            }
            catch { }

            _plc = new Plc(CpuType.S71500, _ipAddress, _rack, _slot);
        }

        var maxRetries = 3;
        var baseDelay = TimeSpan.FromMilliseconds(1000);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            if (cancellationToken.IsCancellationRequested)
                return false;

            try
            {
                _plc.Open();

                if (_plc.IsConnected)
                {
                    lock (_connectionLock)
                    {
                        _isConnected = true;
                    }
                    
                    if (attempt > 1)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ PLC připojeno na pokus #{attempt}");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔄 Pokus #{attempt} připojení k PLC {_ipAddress}: {ex.Message}");
                
                if (attempt < maxRetries)
                {
                    var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        lock (_connectionLock)
        {
            _isConnected = false;
        }
        
        return false;
    }

    /// <summary>
    /// Přečte data pro všechny 4 stroje
    /// </summary>
    public async Task<(List<Machine> machines, bool success, string? errorMessage)> ReadAllMachinesAsync()
    {
        var machines = new List<Machine>();

        // Základní kontrola spojení
        if (!_isConnected || _plc == null)
        {
            return (machines, false, "PLC není připojeno");
        }

        // Test živosti spojení - pokus o čtení jednoho bytu
        // Toto detekuje dead connections které IsConnected nezachytí
        try
        {
            // Kontrola pomocí IsConnected property
            if (!_plc.IsConnected)
            {
                lock (_connectionLock)
                {
                    _isConnected = false;
                }

                try
                {
                    _plc.Close();
                }
                catch { }

                return (machines, false, "PLC spojení ztraceno (IsConnected = false)");
            }

            // Aktivní test - zkusíme přečíst první byte z DB1
            // Pokud spojení je dead, toto vyhodí exception
            await Task.Run(() => _plc.ReadBytes(DataType.DataBlock, 1, 0, 1));
        }
        catch (Exception ex)
        {
            lock (_connectionLock)
            {
                _isConnected = false;
            }

            try
            {
                _plc.Close();
            }
            catch { }

            return (machines, false, $"PLC spojení test selhal: {ex.Message}");
        }

        try
        {
            foreach (var (abbr, runByte, runBit, powerOffset, di1Byte, di1Bit, di2Byte, di2Bit) in _machineMap)
            {
                var machine = new Machine
                {
                    ExternalId = abbr,
                    ElectricityConsumption = ReadInt(powerOffset),
                    PlcConnected = true, // Pokud čteme data, PLC je připojeno
                    Stav1 = ReadBool(runByte, runBit),
                    Stav2 = ReadBool(di1Byte, di1Bit),
                    Stav3 = ReadBool(di2Byte, di2Bit),
                    Stav4 = false, // Zatím nemáme mapování pro stav4-6
                    Stav5 = false,
                    Stav6 = false,
                    Timestamp = DateTime.UtcNow
                };
                
                machines.Add(machine);
            }

            return (machines, true, null);
        }
        catch (Exception ex)
        {
            lock (_connectionLock)
            {
                _isConnected = false;
            }
            
            try
            {
                _plc?.Close();
            }
            catch { }

            return (machines, false, $"Chyba při čtení dat z PLC: {ex.Message}");
        }
    }

    public bool TryReadDRST0001(out Machine? machine, out string? errorMessage)
    {
        return TryReadMachine("DRST_0001", out machine, out errorMessage);
    }

    public bool TryReadAllMachines(out List<Machine> machines, out string? errorMessage)
    {
        machines = new List<Machine>();
        errorMessage = null;

        if (!EnsureConnected(out errorMessage))
        {
            return false;
        }

        try
        {
            foreach (var (abbr, runByte, runBit, powerOffset, di1Byte, di1Bit, di2Byte, di2Bit) in _machineMap)
            {
                var machine = new Machine
                {
                    Abbreviation = abbr,
                    IsRunning = ReadBool(runByte, runBit),
                    PowerConsumption = ReadInt(powerOffset),
                    DI1 = ReadBool(di1Byte, di1Bit),
                    DI2 = ReadBool(di2Byte, di2Bit),
                    Timestamp = DateTime.UtcNow
                };
                
                machines.Add(machine);
            }

            return true;
        }
        catch (Exception ex)
        {
            _plc.Close();
            errorMessage = $"PLC data se nenačetla. Problém ve spojení: {ex.Message}";
            return false;
        }
    }

    public bool TryReadMachinesByAliases(string[] requestedAliases, out List<Machine> machines, out string? errorMessage)
    {
        machines = new List<Machine>();
        errorMessage = null;

        if (!EnsureConnected(out errorMessage))
        {
            return false;
        }

        try
        {
            foreach (var alias in requestedAliases)
            {
                var resolvedName = ResolveAlias(alias);
                if (resolvedName == null)
                {
                    errorMessage = $"Neznámý alias: {alias}. Podporované: {string.Join(", ", _machineAliases.Keys)}";
                    return false;
                }

                var machineConfig = _machineMap.FirstOrDefault(m => m.Abbr == resolvedName);
                if (machineConfig.Abbr != null)
                {
                    var (abbr, runByte, runBit, powerOffset, di1Byte, di1Bit, di2Byte, di2Bit) = machineConfig;

                    var machine = new Machine
                    {
                        Abbreviation = alias, // Používáme původní alias
                        IsRunning = ReadBool(runByte, runBit),
                        PowerConsumption = ReadInt(powerOffset),
                        DI1 = ReadBool(di1Byte, di1Bit),
                        DI2 = ReadBool(di2Byte, di2Bit),
                        Timestamp = DateTime.UtcNow
                    };
                    
                    machines.Add(machine);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _plc.Close();
            errorMessage = $"PLC data se nenačetla. Problém ve spojení: {ex.Message}";
            return false;
        }
    }

    public bool TryReadMachine(string abbreviation, out Machine? machine, out string? errorMessage)
    {
        machine = null;
        errorMessage = null;

        if (!EnsureConnected(out errorMessage))
        {
            return false;
        }

        // Převést alias na skutečný název stroje
        var resolvedName = ResolveAlias(abbreviation);
        if (resolvedName == null)
        {
            errorMessage = $"Stroj {abbreviation} není podporován. Podporované názvy: {string.Join(", ", _machineAliases.Keys)}";
            return false;
        }

        var machineConfig = _machineMap.FirstOrDefault(m => m.Abbr == resolvedName);
        if (machineConfig.Abbr == null)
        {
            errorMessage = $"Stroj {resolvedName} (alias pro {abbreviation}) není definován v mapě.";
            return false;
        }

        try
        {
            var (abbr, runByte, runBit, powerOffset, di1Byte, di1Bit, di2Byte, di2Bit) = machineConfig;

            machine = new Machine
            {
                Abbreviation = abbreviation, // Používáme původní název (ne alias)
                IsRunning = ReadBool(runByte, runBit),
                PowerConsumption = ReadInt(powerOffset),
                DI1 = ReadBool(di1Byte, di1Bit),
                DI2 = ReadBool(di2Byte, di2Bit),
                Timestamp = DateTime.UtcNow
            };

            return true;
        }
        catch (Exception ex)
        {
            _plc.Close();
            errorMessage = $"PLC data se nenačetla pro {abbreviation}. Problém ve spojení: {ex.Message}";
            return false;
        }
    }

    /// <summary>
    /// Převede alias na skutečný název stroje
    /// </summary>
    private string? ResolveAlias(string abbreviation)
    {
        return _machineAliases.TryGetValue(abbreviation, out var resolved) ? resolved : null;
    }

    /// <summary>
    /// Získá všechny podporované aliasy
    /// </summary>
    public string[] GetSupportedMachineNames()
    {
        return _machineAliases.Keys.ToArray();
    }

    public void Close()
    {
        lock (_connectionLock)
        {
            _isConnected = false;
            try
            {
                _plc?.Close();
            }
            catch { }
        }
    }

    private bool EnsureConnected(out string? errorMessage)
    {
        errorMessage = null;

        // Zkontroluj současné spojení
        try
        {
            if (_plc.IsConnected)
            {
                return true;
            }
        }
        catch (Exception)
        {
            // Spojení může být v neplatném stavu
            try
            {
                _plc.Close();
            }
            catch { }
        }

        // Pokus o připojení s retry logikou
        var maxRetries = 3;
        var retryDelay = TimeSpan.FromMilliseconds(500);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _plc.Open();

                if (_plc.IsConnected)
                {
                    if (attempt > 1)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ PLC připojeno na pokus #{attempt}");
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Pokus #{attempt} připojení k PLC {_ipAddress}: {ex.Message}";
                
                if (attempt < maxRetries)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔄 {errorMessage} - zkusím znovu za {retryDelay.TotalMilliseconds}ms");
                    Thread.Sleep(retryDelay);
                    retryDelay = TimeSpan.FromMilliseconds(retryDelay.TotalMilliseconds * 1.5); // Exponential backoff
                }
            }
        }

        errorMessage = $"Nepodařilo se připojit k PLC {_ipAddress} po {maxRetries} pokusech";
        return false;
    }

    private bool ReadBool(int byteOffset, int bitOffset)
    {
        var value = _plc.Read(DataType.DataBlock, 1, byteOffset, VarType.Bit, 1, (byte)bitOffset);
        if (value is bool b)
        {
            return b;
        }

        throw new Exception("ReadBool failed or returned unexpected type");
    }

    private short ReadInt(int offset)
    {
        var rawObj = _plc.ReadBytes(DataType.DataBlock, 1, offset, 2);
        if (rawObj is not byte[] raw || raw.Length < 2)
        {
            throw new Exception("ReadBytes failed or returned insufficient data");
        }

        return BitConverter.ToInt16(new byte[] { raw[1], raw[0] }, 0);
    }
}
