using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public interface IAnomalyDetectionService
{
    IReadOnlyList<AlertMessage> DetectAlerts(TelemetryMessage telemetryMessage);
}
