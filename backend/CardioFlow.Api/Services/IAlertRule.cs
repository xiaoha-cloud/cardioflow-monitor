using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public interface IAlertRule
{
    string Name { get; }
    AlertCandidate? Evaluate(TelemetryMessage telemetryMessage, DetectionContext context);
}
