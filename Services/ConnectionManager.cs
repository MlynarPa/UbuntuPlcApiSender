using UbuntuPlcApiSender.Models;
using System.Collections.Concurrent;
using System.Text.Json;

namespace UbuntuPlcApiSender.Services;

public class ConnectionManager
{
    private readonly ConcurrentDictionary<string, Machine> _lastKnownStates = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSuccessfulRead = new();
    private readonly string _persistentDataFile = "last_machine_states.json";
    private readonly TimeSpan _fallbackThreshold = TimeSpan.FromSeconds(10);
    private DateTime _lastPlcConnectionAttempt = DateTime.MinValue;
    private bool _isPlcConnected = false;
    private int _consecutiveFailures = 0;

    public bool IsPlcConnected => _isPlcConnected;
    public int ConsecutiveFailures => _consecutiveFailures;
    public TimeSpan TimeSinceLastConnection { get; private set; } = TimeSpan.Zero;

    public ConnectionManager()
    {
        LoadPersistedStates();
    }

    public void UpdateConnectionStatus(bool isConnected)
    {
        var wasConnected = _isPlcConnected;
        _isPlcConnected = isConnected;
        
        if (isConnected)
        {
            if (!wasConnected)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ✅ PLC spojení obnoveno po {_consecutiveFailures} neúspěšných pokusech");
            }
            _consecutiveFailures = 0;
            TimeSinceLastConnection = TimeSpan.Zero;
        }
        else
        {
            _consecutiveFailures++;
            TimeSinceLastConnection = DateTime.UtcNow - _lastPlcConnectionAttempt;
            
            if (wasConnected)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ❌ PLC spojení ztraceno - přepínám na fallback režim");
            }
        }
        
        _lastPlcConnectionAttempt = DateTime.UtcNow;
    }

    public void UpdateMachineState(Machine machine)
    {
        if (machine == null) return;

        _lastKnownStates.AddOrUpdate(machine.Abbreviation, machine, (key, oldValue) => machine);
        _lastSuccessfulRead.AddOrUpdate(machine.Abbreviation, DateTime.UtcNow, (key, oldValue) => DateTime.UtcNow);
        
        // Periodicky ukládáme stav na disk (každých 30 sekund)
        if (DateTime.UtcNow.Second % 30 == 0)
        {
            _ = Task.Run(PersistStatesAsync);
        }
    }

    public Machine? GetFallbackData(string machineAbbreviation)
    {
        if (!_lastKnownStates.TryGetValue(machineAbbreviation, out var lastState))
        {
            // Pokud nemáme žádný předchozí stav, vytvoříme výchozí
            return new Machine
            {
                Abbreviation = machineAbbreviation,
                IsRunning = false,
                PowerConsumption = -999, // Indikuje výpadek spojení
                DI1 = false,
                DI2 = false,
                Timestamp = DateTime.UtcNow
            };
        }

        if (!_lastSuccessfulRead.TryGetValue(machineAbbreviation, out var lastReadTime))
        {
            lastReadTime = DateTime.MinValue;
        }

        var timeSinceLastRead = DateTime.UtcNow - lastReadTime;

        // Pokud je spojení ztraceno více než 10 sekund, vrátíme fallback data
        if (timeSinceLastRead > _fallbackThreshold)
        {
            return new Machine
            {
                Abbreviation = machineAbbreviation,
                IsRunning = lastState.IsRunning, // Zachováváme poslední známý stav
                PowerConsumption = lastState.IsRunning ? -999 : 0, // -999 pokud běžel, 0 pokud byl vypnutý
                DI1 = lastState.DI1, // Zachováváme poslední známé stavy
                DI2 = lastState.DI2,
                Timestamp = DateTime.UtcNow
            };
        }

        return lastState;
    }

    public bool ShouldUseFallbackData(string machineAbbreviation)
    {
        if (_isPlcConnected) return false;

        if (!_lastSuccessfulRead.TryGetValue(machineAbbreviation, out var lastReadTime))
        {
            return true; // Nikdy jsme nečetli data
        }

        return (DateTime.UtcNow - lastReadTime) > _fallbackThreshold;
    }

    public Dictionary<string, object> GetConnectionHealth()
    {
        return new Dictionary<string, object>
        {
            ["IsConnected"] = _isPlcConnected,
            ["ConsecutiveFailures"] = _consecutiveFailures,
            ["TimeSinceLastConnection"] = TimeSinceLastConnection.ToString(@"hh\:mm\:ss"),
            ["MachinesWithData"] = _lastKnownStates.Count,
            ["LastConnectionAttempt"] = _lastPlcConnectionAttempt.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private async Task PersistStatesAsync()
    {
        try
        {
            var dataToSave = new
            {
                LastKnownStates = _lastKnownStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LastSuccessfulRead = _lastSuccessfulRead.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                SavedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(dataToSave, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_persistentDataFile, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  Chyba při ukládání stavů: {ex.Message}");
        }
    }

    private void LoadPersistedStates()
    {
        try
        {
            if (!File.Exists(_persistentDataFile)) return;

            var json = File.ReadAllText(_persistentDataFile);
            using var document = JsonDocument.Parse(json);
            
            if (document.RootElement.TryGetProperty("LastKnownStates", out var statesElement))
            {
                foreach (var property in statesElement.EnumerateObject())
                {
                    try
                    {
                        var machine = JsonSerializer.Deserialize<Machine>(property.Value.GetRawText());
                        if (machine != null)
                        {
                            _lastKnownStates.TryAdd(property.Name, machine);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  Chyba při načítání stavu {property.Name}: {ex.Message}");
                    }
                }
            }

            if (document.RootElement.TryGetProperty("LastSuccessfulRead", out var readsElement))
            {
                foreach (var property in readsElement.EnumerateObject())
                {
                    if (DateTime.TryParse(property.Value.GetString(), out var dateTime))
                    {
                        _lastSuccessfulRead.TryAdd(property.Name, dateTime);
                    }
                }
            }

            if (document.RootElement.TryGetProperty("SavedAt", out var savedAtElement))
            {
                if (DateTime.TryParse(savedAtElement.GetString(), out var savedAt))
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 📁 Načteny předchozí stavy strojů (uloženo: {savedAt:yyyy-MM-dd HH:mm:ss})");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  Chyba při načítání předchozích stavů: {ex.Message}");
        }
    }

    public void SaveStatesOnExit()
    {
        try
        {
            PersistStatesAsync().Wait(TimeSpan.FromSeconds(5));
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 💾 Stavy strojů uloženy");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] ⚠️  Chyba při finálním uložení: {ex.Message}");
        }
    }
}
