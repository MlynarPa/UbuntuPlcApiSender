using System.Text.Json.Serialization;

namespace UbuntuPlcApiSender.Models;

public class Machine
{
    [JsonPropertyName("externalId")]
    public string ExternalId { get; set; } = string.Empty;

    [JsonPropertyName("electricityConsumption")]
    public int ElectricityConsumption { get; set; }

    [JsonPropertyName("plcConnected")]
    public bool PlcConnected { get; set; }

    [JsonPropertyName("stav1")]
    public bool Stav1 { get; set; }

    [JsonPropertyName("stav2")]
    public bool Stav2 { get; set; }

    [JsonPropertyName("stav3")]
    public bool Stav3 { get; set; }

    [JsonPropertyName("stav4")]
    public bool Stav4 { get; set; }

    [JsonPropertyName("stav5")]
    public bool Stav5 { get; set; }

    [JsonPropertyName("stav6")]
    public bool Stav6 { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    // Pomocné vlastnosti pro zpětnou kompatibilitu s PlcReader
    [JsonIgnore]
    public string Abbreviation 
    { 
        get => ExternalId; 
        set => ExternalId = value; 
    }

    [JsonIgnore]
    public int PowerConsumption 
    { 
        get => ElectricityConsumption; 
        set => ElectricityConsumption = value; 
    }

    [JsonIgnore]
    public bool IsRunning { get; set; }

    [JsonIgnore]
    public bool DI1 
    { 
        get => Stav1; 
        set => Stav1 = value; 
    }

    [JsonIgnore]
    public bool DI2 
    { 
        get => Stav2; 
        set => Stav2 = value; 
    }
}
