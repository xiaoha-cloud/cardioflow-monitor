using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class AlertMessage
{
    /// <summary>
    /// Patient identifier for the alert event.
    /// </summary>
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Record identifier for the alert event.
    /// </summary>
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Device identifier for the alert event.
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Event timestamp (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Backend receive timestamp (UTC), used for diagnostics.
    /// </summary>
    [JsonPropertyName("receivedAt")]
    public DateTime? ReceivedAt { get; set; }

    /// <summary>
    /// Sample index in the source ECG record.
    /// </summary>
    [JsonPropertyName("sampleIndex")]
    public int SampleIndex { get; set; }

    /// <summary>
    /// Optional beat annotation symbol.
    /// </summary>
    [JsonPropertyName("annotation")]
    public string? Annotation { get; set; }

    /// <summary>
    /// Alert severity level: normal, warning, critical.
    /// </summary>
    [JsonPropertyName("severity")]
    public string Severity { get; set; } = "warning";

    /// <summary>
    /// Human-readable alert message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Rule identifier/source that produced this alert.
    /// </summary>
    [JsonPropertyName("sourceRule")]
    public string SourceRule { get; set; } = string.Empty;

    /// <summary>
    /// Heart rate in bpm when alert was produced.
    /// Nullable when not available.
    /// </summary>
    [JsonPropertyName("heartRate")]
    public int? HeartRate { get; set; }

    /// <summary>
    /// RR interval in milliseconds.
    /// Nullable when not available.
    /// </summary>
    [JsonPropertyName("rrIntervalMs")]
    public double? RrIntervalMs { get; set; }

    /// <summary>
    /// Additional extensible metadata for alert context.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }

    /// <summary>
    /// Short dashboard summary from the explainer service (optional).
    /// </summary>
    [JsonPropertyName("explanationSummary")]
    public string? ExplanationSummary { get; set; }

    /// <summary>
    /// Longer non-diagnostic explanation from the explainer service (optional).
    /// </summary>
    [JsonPropertyName("explanationDetails")]
    public string? ExplanationDetails { get; set; }

    /// <summary>
    /// Monitoring-oriented suggested follow-up from the explainer service (optional).
    /// </summary>
    [JsonPropertyName("recommendedAction")]
    public string? RecommendedAction { get; set; }
}
