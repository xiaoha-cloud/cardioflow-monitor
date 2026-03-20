using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class StatusAggregationService : IStatusAggregationService
{
    private readonly ITelemetryBufferService _telemetryBufferService;
    private readonly IAlertService _alertService;
    private readonly IConfiguration _configuration;
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private DateTime? _lastConsumerErrorAtUtc;
    private DateTime? _lastMessageTimestampUtc;
    private string? _currentRecord;
    private string _consumerState = "reconnecting";

    public StatusAggregationService(
        ITelemetryBufferService telemetryBufferService,
        IAlertService alertService,
        IConfiguration configuration)
    {
        _telemetryBufferService = telemetryBufferService;
        _alertService = alertService;
        _configuration = configuration;
    }

    public void UpdateWithTelemetry(TelemetryMessage telemetryMessage)
    {
        _lastMessageTimestampUtc = telemetryMessage.Timestamp;
        _currentRecord = telemetryMessage.RecordId;
        _lastConsumerErrorAtUtc = null;
    }

    public void ReportConsumerConnected()
    {
        _consumerState = "connected";
    }

    public void ReportConsumerReconnecting()
    {
        _consumerState = "reconnecting";
    }

    public void ReportConsumerError()
    {
        _consumerState = "error";
        _lastConsumerErrorAtUtc = DateTime.UtcNow;
    }

    public SystemStatusDto GetCurrentStatus()
    {
        var latestTelemetry = _telemetryBufferService.GetLatest(1).FirstOrDefault();
        var activeRecordId = _currentRecord ?? _telemetryBufferService.GetLatestRecordId();
        var telemetryCount = _telemetryBufferService.GetCount();
        var lastMessageAt = _lastMessageTimestampUtc ?? _telemetryBufferService.GetLastMessageAt();
        var lastAlert = _alertService.GetLastAlert();
        var now = DateTime.UtcNow;

        var streamStatus = "stopped";
        if (telemetryCount > 0)
        {
            streamStatus = lastMessageAt.HasValue && now - lastMessageAt.Value <= TimeSpan.FromSeconds(30)
                ? "running"
                : "idle";
        }

        // Health rule:
        // healthy   <= 10s silence and consumer connected
        // degraded  10-30s silence or recent consumer error
        // stale     30-60s silence
        // down      > 60s silence or consumer disconnected
        var streamHealth = "down";
        if (_consumerState != "connected")
        {
            streamHealth = "down";
        }
        else if (!lastMessageAt.HasValue)
        {
            streamHealth = "down";
        }
        else
        {
            var silence = now - lastMessageAt.Value;
            if (silence <= TimeSpan.FromSeconds(10))
            {
                streamHealth = "healthy";
            }
            else if (silence <= TimeSpan.FromSeconds(30))
            {
                streamHealth = "degraded";
            }
            else if (silence <= TimeSpan.FromSeconds(60))
            {
                streamHealth = "stale";
            }
            else
            {
                streamHealth = "down";
            }
        }

        if (_lastConsumerErrorAtUtc.HasValue && now - _lastConsumerErrorAtUtc.Value <= TimeSpan.FromSeconds(30))
        {
            streamHealth = "degraded";
        }

        return new SystemStatusDto
        {
            StreamHealth = streamHealth,
            StreamStatus = streamStatus,
            SamplingRate = _configuration.GetValue<int>("Telemetry:SamplingRate", 360),
            Topic = _configuration["Kafka:TelemetryTopic"] ?? "ecg.telemetry",
            ActivePatient = latestTelemetry?.PatientId,
            ActiveRecord = activeRecordId,
            ActiveRecordId = activeRecordId,
            CurrentRecord = activeRecordId,
            DeviceId = latestTelemetry?.DeviceId,
            LastAlert = lastAlert?.Message,
            BufferCount = telemetryCount,
            TelemetryCount = telemetryCount,
            AlertCount = _alertService.GetCount(),
            LastMessageTimestamp = lastMessageAt,
            LastMessageAt = lastMessageAt,
            LastAlertAt = lastAlert?.Timestamp,
            ConsumerLagApprox = null,
            UptimeSeconds = Math.Max(0, Convert.ToInt64((now - _startedAtUtc).TotalSeconds)),
            ConsumerState = _consumerState
        };
    }
}
