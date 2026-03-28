using System.Text.Json.Serialization;

namespace CardioFlow.Api.Models;

/// <summary>
/// Response body from the explainer service POST /explain.
/// </summary>
public class ExplanationResponseDto
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;

    [JsonPropertyName("recommendedAction")]
    public string RecommendedAction { get; set; } = string.Empty;
}
