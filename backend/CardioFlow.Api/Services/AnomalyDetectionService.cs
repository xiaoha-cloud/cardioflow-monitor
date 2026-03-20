using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    private readonly int _hrHighThreshold;
    private readonly int _hrLowThreshold;

    public AnomalyDetectionService(IConfiguration configuration)
    {
        _hrHighThreshold = configuration.GetValue<int>("Alerts:HrHighThreshold", 120);
        _hrLowThreshold = configuration.GetValue<int>("Alerts:HrLowThreshold", 45);
    }

    public IReadOnlyList<AlertMessage> DetectAlerts(TelemetryMessage telemetryMessage)
    {
        if (telemetryMessage == null)
        {
            return Array.Empty<AlertMessage>();
        }

        var annotation = telemetryMessage.Annotation?.Trim().ToUpperInvariant() ?? string.Empty;
        var reasons = new List<string>();
        var severity = "normal";

        if (telemetryMessage.HeartRate.HasValue && telemetryMessage.HeartRate.Value > _hrHighThreshold)
        {
            severity = "critical";
            reasons.Add("Tachycardia threshold exceeded");
        }
        else if (telemetryMessage.HeartRate.HasValue && telemetryMessage.HeartRate.Value < _hrLowThreshold)
        {
            severity = "critical";
            reasons.Add("Bradycardia threshold exceeded");
        }

        var annotationRule = annotation switch
        {
            "V" => ("warning", "PVC detected"),
            "A" => ("warning", "Atrial premature beat detected"),
            "E" => ("warning", "Ventricular escape beat detected"),
            "F" => ("warning", "Fusion beat detected"),
            "Q" => ("warning", "Unclassifiable beat detected"),
            _ => ((string?)null, (string?)null)
        };

        if (annotationRule.Item1 is not null)
        {
            severity = MaxSeverity(severity, annotationRule.Item1);
            reasons.Add(annotationRule.Item2!);
        }

        if (severity == "normal" &&
            string.Equals(telemetryMessage.Status, "abnormal", StringComparison.OrdinalIgnoreCase))
        {
            severity = "warning";
            reasons.Add("Abnormal telemetry status detected");
        }

        if (severity == "normal")
        {
            return Array.Empty<AlertMessage>();
        }

        var message = string.Join("; ", reasons.Distinct(StringComparer.Ordinal));
        return new[] { BuildAlert(telemetryMessage, severity, message) };
    }

    private static string MaxSeverity(string a, string b)
    {
        var rankA = SeverityRank(a);
        var rankB = SeverityRank(b);
        return rankA >= rankB ? a : b;
    }

    private static int SeverityRank(string value)
    {
        return value switch
        {
            "critical" => 3,
            "warning" => 2,
            "info" => 1,
            _ => 0
        };
    }

    private static AlertMessage BuildAlert(TelemetryMessage telemetryMessage, string severity, string message)
    {
        return new AlertMessage
        {
            PatientId = telemetryMessage.PatientId,
            DeviceId = telemetryMessage.DeviceId,
            Timestamp = telemetryMessage.Timestamp,
            SampleIndex = telemetryMessage.SampleIndex,
            Annotation = telemetryMessage.Annotation,
            Severity = severity,
            Message = message,
            HeartRate = telemetryMessage.HeartRate
        };
    }
}
