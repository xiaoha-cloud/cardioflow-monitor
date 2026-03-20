using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class CurrentPatientDto
{
    [JsonPropertyName("patientId")]
    public string? PatientId { get; set; }

    [JsonPropertyName("recordId")]
    public string? RecordId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("battery")]
    public int? Battery { get; set; }

    [JsonPropertyName("signalQuality")]
    public string? SignalQuality { get; set; }

    [JsonPropertyName("lastSeenAt")]
    public DateTime? LastSeenAt { get; set; }

    [JsonPropertyName("streamStatus")]
    public string StreamStatus { get; set; } = "stopped";
}
