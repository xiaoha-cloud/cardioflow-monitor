namespace CardioFlow.Api.Services;

public sealed class DetectionContext
{
    public int HrHighThreshold { get; init; } = 120;
    public int HrLowThreshold { get; init; } = 45;
    public int RrLowMs { get; init; } = 400;
    public int RrHighMs { get; init; } = 1200;
    public bool EnableRrRule { get; init; } = true;
}
