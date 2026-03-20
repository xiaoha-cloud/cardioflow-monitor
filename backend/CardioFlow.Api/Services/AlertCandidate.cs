namespace CardioFlow.Api.Services;

public sealed class AlertCandidate
{
    public string Severity { get; init; } = "warning";
    public string Message { get; init; } = string.Empty;
    public string SourceRule { get; init; } = string.Empty;
}
