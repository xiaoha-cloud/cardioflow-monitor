using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public sealed class RrIntervalRule : IAlertRule
{
    public string Name => "rr_interval_rule";

    public AlertCandidate? Evaluate(TelemetryMessage telemetryMessage, DetectionContext context)
    {
        if (!context.EnableRrRule || !telemetryMessage.RrIntervalMs.HasValue)
        {
            return null;
        }

        if (telemetryMessage.RrIntervalMs.Value < context.RrLowMs)
        {
            return new AlertCandidate
            {
                Severity = "warning",
                Message = "Short RR interval detected",
                SourceRule = Name
            };
        }

        if (telemetryMessage.RrIntervalMs.Value > context.RrHighMs)
        {
            return new AlertCandidate
            {
                Severity = "warning",
                Message = "Long RR interval detected",
                SourceRule = Name
            };
        }

        return null;
    }
}
