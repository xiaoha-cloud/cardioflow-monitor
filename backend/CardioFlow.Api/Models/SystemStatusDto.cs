using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class SystemStatusDto
{
    [JsonPropertyName("streamStatus")]
    public string StreamStatus { get; set; } = "stopped";

    [JsonPropertyName("samplingRate")]
    public int SamplingRate { get; set; } = 360;

    [JsonPropertyName("topic")]
    public string Topic { get; set; } = "ecg.telemetry";

    [JsonPropertyName("activePatient")]
    public string? ActivePatient { get; set; }

    [JsonPropertyName("activeRecord")]
    public string? ActiveRecord { get; set; }

    [JsonPropertyName("activeRecordId")]
    public string? ActiveRecordId { get; set; }

    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; set; }

    [JsonPropertyName("lastAlert")]
    public string? LastAlert { get; set; }

    [JsonPropertyName("bufferCount")]
    public int BufferCount { get; set; }

    [JsonPropertyName("lastMessageAt")]
    public DateTime? LastMessageAt { get; set; }
}
