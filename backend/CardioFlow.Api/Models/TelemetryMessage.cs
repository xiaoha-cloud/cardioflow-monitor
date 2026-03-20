using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

/// <summary>
/// Represents an ECG telemetry message received from Kafka.
/// This model corresponds to the JSON message format sent by the simulator.
/// </summary>
public class TelemetryMessage
{
    /// <summary>
    /// Patient identifier (e.g., "mitdb-100").
    /// </summary>
    [JsonPropertyName("patientId")]
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// MIT-BIH record identifier (e.g., "100").
    /// </summary>
    [JsonPropertyName("recordId")]
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Device identifier (e.g., "ecg-sim-01").
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the sample was recorded (ISO 8601 format).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Timestamp when backend received this message.
    /// Nullable for backward compatibility with existing payloads.
    /// </summary>
    [JsonPropertyName("receivedAt")]
    public DateTime? ReceivedAt { get; set; }

    /// <summary>
    /// Sample index position in the ECG record.
    /// </summary>
    [JsonPropertyName("sampleIndex")]
    public int SampleIndex { get; set; }

    /// <summary>
    /// ECG lead1 signal value (in millivolts).
    /// </summary>
    [JsonPropertyName("lead1")]
    public double Lead1 { get; set; }

    /// <summary>
    /// Beat annotation symbol (N=Normal, V=PVC, A=Atrial Premature, etc.).
    /// </summary>
    [JsonPropertyName("annotation")]
    public string Annotation { get; set; } = string.Empty;

    /// <summary>
    /// Heart rate in beats per minute (optional).
    /// </summary>
    [JsonPropertyName("heartRate")]
    public int? HeartRate { get; set; }

    /// <summary>
    /// Overall status: "normal" or "abnormal".
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Signal quality indicator (e.g., "good", "fair", "poor").
    /// </summary>
    [JsonPropertyName("signalQuality")]
    public string? SignalQuality { get; set; } = "unknown";

    /// <summary>
    /// Battery level percentage (0-100).
    /// </summary>
    [JsonPropertyName("battery")]
    public int? Battery { get; set; }

    /// <summary>
    /// RR interval in milliseconds. Nullable when not available.
    /// </summary>
    [JsonPropertyName("rrIntervalMs")]
    public double? RrIntervalMs { get; set; }

    /// <summary>
    /// True when this telemetry sample is derived rather than directly measured.
    /// Nullable for backward compatibility.
    /// </summary>
    [JsonPropertyName("isDerived")]
    public bool? IsDerived { get; set; }

    /// <summary>
    /// Optional derived metrics container.
    /// </summary>
    [JsonPropertyName("derivedMetrics")]
    public DerivedMetricsDto? DerivedMetrics { get; set; }
}
