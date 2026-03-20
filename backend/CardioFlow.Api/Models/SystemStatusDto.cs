using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class SystemStatusDto
{
    /// <summary>
    /// Health score for telemetry stream quality.
    /// Values: healthy, degraded, stale, down.
    /// </summary>
    [JsonPropertyName("streamHealth")]
    public string StreamHealth { get; set; } = "down";

    /// <summary>
    /// Stream state computed from telemetry freshness and consumer health.
    /// Values: running, idle, stopped, degraded.
    /// </summary>
    [JsonPropertyName("streamStatus")]
    public string StreamStatus { get; set; } = "stopped";

    /// <summary>
    /// Sampling rate in Hz.
    /// </summary>
    [JsonPropertyName("samplingRate")]
    public int SamplingRate { get; set; } = 360;

    /// <summary>
    /// Kafka topic name used for telemetry ingestion.
    /// </summary>
    [JsonPropertyName("topic")]
    public string Topic { get; set; } = "ecg.telemetry";

    /// <summary>
    /// Active patient id inferred from the latest buffered telemetry.
    /// Null when no telemetry has been consumed yet.
    /// </summary>
    [JsonPropertyName("activePatient")]
    public string? ActivePatient { get; set; }

    /// <summary>
    /// Active record id (legacy alias). Prefer activeRecordId.
    /// </summary>
    [JsonPropertyName("activeRecord")]
    public string? ActiveRecord { get; set; }

    /// <summary>
    /// Active record id inferred from latest telemetry.
    /// Null when no telemetry has been consumed yet.
    /// </summary>
    [JsonPropertyName("activeRecordId")]
    public string? ActiveRecordId { get; set; }

    /// <summary>
    /// Last telemetry timestamp in UTC.
    /// </summary>
    [JsonPropertyName("lastMessageTimestamp")]
    public DateTime? LastMessageTimestamp { get; set; }

    /// <summary>
    /// Current active record inferred from latest telemetry.
    /// </summary>
    [JsonPropertyName("currentRecord")]
    public string? CurrentRecord { get; set; }

    /// <summary>
    /// Active device id inferred from latest telemetry.
    /// </summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    /// <summary>
    /// Message of the latest generated alert.
    /// </summary>
    [JsonPropertyName("lastAlert")]
    public string? LastAlert { get; set; }

    /// <summary>
    /// Number of telemetry messages currently retained in buffer.
    /// </summary>
    [JsonPropertyName("bufferCount")]
    public int BufferCount { get; set; }

    /// <summary>
    /// Current telemetry item count retained in buffer.
    /// </summary>
    [JsonPropertyName("telemetryCount")]
    public int TelemetryCount { get; set; }

    /// <summary>
    /// Number of alerts currently retained in alert store.
    /// </summary>
    [JsonPropertyName("alertCount")]
    public int AlertCount { get; set; }

    /// <summary>
    /// Timestamp of latest telemetry event.
    /// Null when no telemetry has been consumed yet.
    /// </summary>
    [JsonPropertyName("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Timestamp of latest alert event.
    /// Null when no alert has been generated yet.
    /// </summary>
    [JsonPropertyName("lastAlertAt")]
    public DateTime? LastAlertAt { get; set; }

    /// <summary>
    /// Optional consumer lag approximation (messages). Reserved for future use.
    /// </summary>
    [JsonPropertyName("consumerLagApprox")]
    public long? ConsumerLagApprox { get; set; }

    /// <summary>
    /// API process uptime in seconds.
    /// </summary>
    [JsonPropertyName("uptimeSeconds")]
    public long UptimeSeconds { get; set; }

    /// <summary>
    /// Kafka consumer state.
    /// Values: connected, reconnecting, error.
    /// </summary>
    [JsonPropertyName("consumerState")]
    public string ConsumerState { get; set; } = "reconnecting";
}
