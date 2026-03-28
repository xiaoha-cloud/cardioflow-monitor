using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

/// <summary>
/// Request body for the explainer service POST /explain (camelCase JSON).
/// </summary>
public class AlertExplanationRequestDto
{
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    [JsonPropertyName("annotation")]
    public string? Annotation { get; set; }

    [JsonPropertyName("heartRate")]
    public double? HeartRate { get; set; }

    [JsonPropertyName("rrInterval")]
    public double? RrInterval { get; set; }

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
