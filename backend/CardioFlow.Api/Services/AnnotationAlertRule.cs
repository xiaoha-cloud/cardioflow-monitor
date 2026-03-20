using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public sealed class AnnotationAlertRule : IAlertRule
{
    public string Name => "annotation_rule";

    public AlertCandidate? Evaluate(TelemetryMessage telemetryMessage, DetectionContext context)
    {
        var annotation = telemetryMessage.Annotation?.Trim().ToUpperInvariant() ?? string.Empty;
        return annotation switch
        {
            "V" => new AlertCandidate { Severity = "warning", Message = "PVC detected", SourceRule = Name },
            "A" => new AlertCandidate { Severity = "warning", Message = "Atrial premature beat detected", SourceRule = Name },
            "E" => new AlertCandidate { Severity = "warning", Message = "Ventricular escape beat detected", SourceRule = Name },
            "F" => new AlertCandidate { Severity = "warning", Message = "Fusion beat detected", SourceRule = Name },
            "Q" => new AlertCandidate { Severity = "warning", Message = "Unclassifiable beat detected", SourceRule = Name },
            _ => null
        };
    }
}
