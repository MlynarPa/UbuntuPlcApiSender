using System.Text.Json.Serialization;

namespace UbuntuPlcApiSender.Models;

public class Machine
{
    [JsonPropertyName("abbreviation")]
    public string Abbreviation { get; set; } = string.Empty;

    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    [JsonPropertyName("powerConsumption")]
    public int PowerConsumption { get; set; }

    [JsonPropertyName("dI1")]
    public bool DI1 { get; set; }           // nový vstup

    [JsonPropertyName("dI2")]
    public bool DI2 { get; set; }           // nový vstup

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}
