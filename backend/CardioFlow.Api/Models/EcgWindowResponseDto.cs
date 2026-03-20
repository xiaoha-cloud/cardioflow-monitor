using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

public class EcgWindowResponseDto
{
    [JsonPropertyName("meta")]
    public EcgWindowMetaDto Meta { get; set; } = new();

    [JsonPropertyName("items")]
    public IReadOnlyList<TelemetryMessage> Items { get; set; } = Array.Empty<TelemetryMessage>();
}

public class EcgWindowMetaDto
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("samplingRate")]
    public int SamplingRate { get; set; } = 360;

    [JsonPropertyName("windowSeconds")]
    public int? WindowSeconds { get; set; }

    [JsonPropertyName("downsample")]
    public int Downsample { get; set; } = 1;
}
