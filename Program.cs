using UbuntuPlcApiSender.Services;
using UbuntuPlcApiSender.Models;

Console.WriteLine("=== Ubuntu PLC API Sender ===");
Console.WriteLine("Aplikace čte data z PLC datablocku pro 4 stroje (DRST_0001-0004)");
Console.WriteLine("a odesílá je na drevostroj.app API pomocí bulk PUT požadavku.");
Console.WriteLine();

// Konfigurace
var plcIpAddress = "192.168.0.10";  // IP adresa PLC - ZMĚŇ PODLE POTŘEBY
var plcRack = (short)0;
var plcSlot = (short)1;
var apiBaseUrl = "https://drevostroj.app";
var apiKey = "drevostrojapi2024";
var readInterval = 1000; // 1 sekunda pro čtení PLC i odesílání API

Console.WriteLine("=== KONFIGURACE ===");
Console.WriteLine($"PLC IP: {plcIpAddress}");
Console.WriteLine($"PLC Rack: {plcRack}, Slot: {plcSlot}");
Console.WriteLine($"API URL: {apiBaseUrl}/api/MachinesApi/Bulk");
Console.WriteLine($"API Key: {apiKey} (přes X-API-Key header)");
Console.WriteLine($"Interval čtení a odesílání: {readInterval}ms");
Console.WriteLine("Režim: Dva asynchronní procesy - PLC připojení/čtení a API odesílání");
Console.WriteLine();

var plcReader = new PlcReader(plcIpAddress, plcRack, plcSlot);
var apiClient = new ApiClient(apiBaseUrl, apiKey);

// Sdílená data mezi procesy
var latestMachines = new List<Machine>();
var machinesLock = new object();

Console.WriteLine("Pro ukončení stiskněte Ctrl+C");
Console.WriteLine();
Console.WriteLine("--- Spouštím dva asynchronní procesy ---");

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

// Task 1: PLC připojení a čtení dat každou sekundu
var plcTask = Task.Run(async () =>
{
    var iteration = 0;
    var connectionAttempts = 0;
    
    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;
        
        // Pokus o připojení pokud nejsme připojeni
        if (!plcReader.IsConnected)
        {
            connectionAttempts++;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 🔄 Pokus o připojení k PLC #{connectionAttempts}...");
            
            var connected = await plcReader.TryConnectAsync(cts.Token);
            if (connected)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ PLC úspěšně připojeno po {connectionAttempts} pokusech");
                connectionAttempts = 0;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ PLC připojení selhalo, zkusím znovu za {readInterval}ms");
                
                // Vytvoříme fallback data s plcConnected = false
                var fallbackMachines = apiClient.CreateFallbackMachines(false);
                lock (machinesLock)
                {
                    latestMachines.Clear();
                    latestMachines.AddRange(fallbackMachines);
                }
                
                try
                {
                    await Task.Delay(readInterval, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                continue;
            }
        }
        
        // Čtení dat z PLC
        var (machines, success, errorMessage) = await plcReader.ReadAllMachinesAsync();
        
        if (success && machines.Count > 0)
        {
            lock (machinesLock)
            {
                latestMachines.Clear();
                latestMachines.AddRange(machines);
            }
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📊 PLC #{iteration} - Data načtena pro {machines.Count} strojů");
            foreach (var machine in machines)
            {
                Console.WriteLine($"  • {machine.ExternalId}: {machine.ElectricityConsumption}W | Stavy: {machine.Stav1},{machine.Stav2},{machine.Stav3}");
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ PLC #{iteration} - {errorMessage ?? "Chyba při čtení dat"}");
            
            // Vytvoříme fallback data s plcConnected = false
            var fallbackMachines = apiClient.CreateFallbackMachines(false);
            lock (machinesLock)
            {
                latestMachines.Clear();
                latestMachines.AddRange(fallbackMachines);
            }
        }

        try
        {
            await Task.Delay(readInterval, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}, cts.Token);

// Task 2: Odesílání bulk dat na API každou sekundu
var apiTask = Task.Run(async () =>
{
    var iteration = 0;
    await Task.Delay(500); // Malé zpoždění aby PLC stihlo načíst první data
    
    while (!cts.Token.IsCancellationRequested)
    {
        iteration++;
        
        List<Machine> machinesToSend;
        lock (machinesLock)
        {
            machinesToSend = new List<Machine>(latestMachines);
        }
        
        if (machinesToSend.Count > 0)
        {
            var success = await apiClient.SendBulkMachineDataAsync(machinesToSend);
            
            if (success)
            {
                var plcStatus = machinesToSend.First().PlcConnected ? "PLC ✅" : "PLC ❌";
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✓ API #{iteration} - Bulk data odeslána ({machinesToSend.Count} strojů, {plcStatus})");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ API #{iteration} - Chyba při odesílání bulk dat");
            }
        }
        else
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  API #{iteration} - Žádná data k odeslání");
        }

        try
        {
            await Task.Delay(readInterval, cts.Token);
        }
        catch (OperationCanceledException)
        {
            break;
        }
    }
}, cts.Token);

// Čekání na dokončení obou úkolů
await Task.WhenAll(plcTask, apiTask);

// Cleanup
Console.WriteLine("--- Ukončuji aplikaci ---");
apiClient.Dispose();
plcReader.Close();
Console.WriteLine("--- Aplikace ukončena ---");