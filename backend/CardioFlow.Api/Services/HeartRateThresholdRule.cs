using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public sealed class HeartRateThresholdRule : IAlertRule
{
    public string Name => "hr_threshold_rule";

    public AlertCandidate? Evaluate(TelemetryMessage telemetryMessage, DetectionContext context)
    {
        if (!telemetryMessage.HeartRate.HasValue)
        {
            return null;
        }

        if (telemetryMessage.HeartRate.Value > context.HrHighThreshold)
        {
            return new AlertCandidate
            {
                Severity = "critical",
                Message = "Tachycardia threshold exceeded",
                SourceRule = Name
            };
        }

        if (telemetryMessage.HeartRate.Value < context.HrLowThreshold)
        {
            return new AlertCandidate
            {
                Severity = "critical",
                Message = "Bradycardia threshold exceeded",
                SourceRule = Name
            };
        }

        return null;
    }
}
