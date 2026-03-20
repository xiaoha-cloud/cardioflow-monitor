using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AnomalyDetectionService : IAnomalyDetectionService
{
    public IReadOnlyList<AlertMessage> DetectAlerts(TelemetryMessage telemetryMessage)
    {
        if (telemetryMessage == null)
        {
            return Array.Empty<AlertMessage>();
        }

        var alerts = new List<AlertMessage>();

        if (string.Equals(telemetryMessage.Annotation, "V", StringComparison.OrdinalIgnoreCase))
        {
            alerts.Add(BuildAlert(telemetryMessage, "PVC detected"));
        }

        if (telemetryMessage.HeartRate.HasValue && telemetryMessage.HeartRate.Value > 100)
        {
            alerts.Add(BuildAlert(telemetryMessage, "Heart rate above threshold"));
        }

        if (telemetryMessage.HeartRate.HasValue && telemetryMessage.HeartRate.Value < 50)
        {
            alerts.Add(BuildAlert(telemetryMessage, "Heart rate below threshold"));
        }

        return alerts.AsReadOnly();
    }

    private static AlertMessage BuildAlert(TelemetryMessage telemetryMessage, string message)
    {
        return new AlertMessage
        {
            PatientId = telemetryMessage.PatientId,
            DeviceId = telemetryMessage.DeviceId,
            Timestamp = telemetryMessage.Timestamp,
            SampleIndex = telemetryMessage.SampleIndex,
            Annotation = telemetryMessage.Annotation,
            Severity = "warning",
            Message = message,
            HeartRate = telemetryMessage.HeartRate
        };
    }
}
