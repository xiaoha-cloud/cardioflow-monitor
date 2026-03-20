using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

/// <summary>
/// Derived ECG metrics computed from raw telemetry.
/// All fields are nullable because metrics may not be available for every sample.
/// </summary>
public class DerivedMetricsDto
{
    /// <summary>
    /// RR interval in milliseconds.
    /// </summary>
    [JsonPropertyName("rrIntervalMs")]
    public double? RrIntervalMs { get; set; }

    /// <summary>
    /// HRV RMSSD metric in milliseconds.
    /// </summary>
    [JsonPropertyName("hrvRmssd")]
    public double? HrvRmssd { get; set; }

    /// <summary>
    /// QRS complex width in milliseconds.
    /// </summary>
    [JsonPropertyName("qrsWidthMs")]
    public double? QrsWidthMs { get; set; }
}
