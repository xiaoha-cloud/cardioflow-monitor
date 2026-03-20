using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class AlertMessage
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    [JsonPropertyName("sampleIndex")]
    public int SampleIndex { get; set; }

    [JsonPropertyName("annotation")]
    public string Annotation { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("heartRate")]
    public int? HeartRate { get; set; }
}
