using UbuntuPlcApiSender.Services;
using System.Collections.Concurrent;

Console.WriteLine("=== Ubuntu PLC API Sender ===");
Console.WriteLine("Tato aplikace čte reálná data z PLC pro stroj DRST_0001");
Console.WriteLine("a odesílá je na drevostroj.app API pomocí PUT požadavku.");
Console.WriteLine();

// Konfigurace
var plcIpAddress = "192.168.0.10";  // IP adresa PLC - ZMĚŇ PODLE POTŘEBY
var plcRack = (short)0;
var plcSlot = (short)1;
var apiBaseUrl = "https://drevostroj.app";
var apiKey = "drevostrojapi2024";
var plcReadInterval = 5000; // 5 sekund pro čtení PLC
var apiSendInterval = 5000; // 5 sekund pro odesílání API
var apiSendOffset = 1000;   // API odesílá o 1 sekundu později než PLC

// Thread-safe queue pro data mezi PLC čtením a API odesíláním
var dataQueue = new ConcurrentQueue<UbuntuPlcApiSender.Models.Machine>();
var latestData = new ConcurrentDictionary<string, UbuntuPlcApiSender.Models.Machine>();

Console.WriteLine("=== KONFIGURACE ===");
Console.WriteLine($"PLC IP: {plcIpAddress}");
Console.WriteLine($"PLC Rack: {plcRack}, Slot: {plcSlot}");
Console.WriteLine($"API URL: {apiBaseUrl}/api/MachinesApi/DRST_0001");
Console.WriteLine($"API Key: {apiKey} (přes X-API-Key header)");
Console.WriteLine($"Interval čtení PLC: {plcReadInterval}ms");
Console.WriteLine($"Interval odesílání API: {apiSendInterval}ms (se zpožděním {apiSendOffset}ms)");
Console.WriteLine("Režim: Paralelní úkoly (PLC čtení a API odesílání nezávisle)");
Console.WriteLine();

var plcReader = new PlcReader(plcIpAddress, plcRack, plcSlot);
var apiClient = new ApiClient(apiBaseUrl, apiKey);

Console.WriteLine("Pro ukončení stiskněte Ctrl+C");
Console.WriteLine();
Console.WriteLine("--- Začínají paralelní úkoly: čtení PLC a odesílání API ---");

// CancellationToken pro elegantní ukončení
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\n--- Ukončování aplikace... ---");
};

// Task 1: Čtení z PLC každou sekundu
var plcReadTask = Task.Run(async () =>
{
    var iteration = 0;
    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;
        
        if (plcReader.TryReadDRST0001(out var machine, out var errorMessage))
        {
            if (machine != null)
            {
                // Uložit nejnovější data
                latestData.AddOrUpdate("DRST_0001", machine, (key, oldValue) => machine);
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📊 PLC #{iteration} - Data načtena:");
                Console.WriteLine($"  • Spotřeba: {machine.PowerConsumption} W | DI1: {machine.DI1} | DI2: {machine.DI2} | Běží: {machine.IsRunning}");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  PLC #{iteration} - Nepodařilo se načíst data");
            }
        }
        else
        {
            var message = string.IsNullOrWhiteSpace(errorMessage)
                ? "PLC data se nepodařilo načíst."
                : errorMessage;

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ PLC #{iteration} - {message}");
        }

        try
        {
            await Task.Delay(plcReadInterval, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}, cts.Token);

// Task 2: Odesílání na API každou sekundu
var apiSendTask = Task.Run(async () =>
{
    var iteration = 0;
    try
    {
        await Task.Delay(apiSendOffset, cts.Token); // Zpoždění, aby API odesílalo o 1s později než PLC čte
    }
    catch (OperationCanceledException)
    {
        return;
    }

    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;
        
        if (latestData.TryGetValue("DRST_0001", out var machine))
        {
            var success = await apiClient.SendMachineDataAsync("DRST_0001", machine);
            
            if (success)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ API #{iteration} - Data úspěšně odeslána");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ API #{iteration} - Chyba při odesílání");
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  API #{iteration} - Žádná data k odeslání");
        }

        try
        {
            await Task.Delay(apiSendInterval, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}, cts.Token);

// Čekání na dokončení obou úkolů
await Task.WhenAll(plcReadTask, apiSendTask);

// Cleanup (nedosažitelné kvůli nekonečné smyčce, ale dobré mít)
apiClient.Dispose();
plcReader.Close();
