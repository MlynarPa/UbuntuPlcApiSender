using System.Text;
using System.Text.Json;
using UbuntuPlcApiSender.Models;

namespace UbuntuPlcApiSender.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public ApiClient(string baseUrl, string apiKey)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _apiKey = apiKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "*/*");
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _apiKey);
    }

    /// <summary>
    /// Odešle bulk data pro všechny stroje na API endpoint
    /// </summary>
    public async Task<bool> SendBulkMachineDataAsync(List<Machine> machines)
    {
        try
        {
            var bulkRequest = new BulkMachinesRequest
            {
                Machines = machines
            };

            var json = JsonSerializer.Serialize(bulkRequest, new JsonSerializerOptions 
            { 
                WriteIndented = false 
            });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_baseUrl}/api/MachinesApi/Bulk";
            
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Chyba při odesílání bulk dat: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Detaily chyby: {errorContent}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Výjimka při odesílání bulk dat: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Vytvoří fallback data pro stroje když PLC není připojeno
    /// -300W signalizuje problém s PLC připojením
    /// </summary>
    public List<Machine> CreateFallbackMachines(bool plcConnected)
    {
        var machineIds = new[] { "DRST_0001", "DRST_0002", "DRST_0003", "DRST_0004" };

        return machineIds.Select(id => new Machine
        {
            ExternalId = id,
            ElectricityConsumption = -300, // -300W = indikátor problému s PLC
            PlcConnected = plcConnected,
            IsRunning = false,
            Stav1 = false,
            Stav2 = false,
            Stav3 = false,
            Stav4 = false,
            Stav5 = false,
            Stav6 = false,
            Timestamp = DateTime.UtcNow
        }).ToList();
    }

    public async Task<bool> SendMachineDataAsync(string abbreviation, Machine machine)
    {
        try
        {
            var updateData = new
            {
                electricityConsumption = machine.PowerConsumption,
                stav1 = machine.DI1,
                stav2 = machine.DI2
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{_baseUrl}/api/MachinesApi/{abbreviation}";
            
            // Méně verbose pro častější odesílání - jen při chybách zobrazíme detaily
            var response = await _httpClient.PutAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                // Úspěch - už se loguje v Program.cs
                return true;
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Chyba při odesílání dat pro {abbreviation}: {response.StatusCode}");
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Detaily chyby: {errorContent}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Odeslaná data: {json}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✗ Výjimka při odesílání dat pro {abbreviation}: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
