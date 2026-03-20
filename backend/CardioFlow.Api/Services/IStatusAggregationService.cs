using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public interface IStatusAggregationService
{
    void UpdateWithTelemetry(TelemetryMessage telemetryMessage);
    void ReportConsumerConnected();
    void ReportConsumerReconnecting();
    void ReportConsumerError();
    SystemStatusDto GetCurrentStatus();
}
