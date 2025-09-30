using System.Text.Json.Serialization;

namespace UbuntuPlcApiSender.Models;

public class BulkMachinesRequest
{
    [JsonPropertyName("machines")]
    public List<Machine> Machines { get; set; } = new List<Machine>();
}
