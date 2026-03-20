using System.Collections.Concurrent;
using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AlertService : IAlertService
{
    private readonly ConcurrentQueue<AlertMessage> _alerts = new();
    private readonly int _maxSize;
    private readonly ITelemetryBufferService _bufferService;

    public AlertService(IConfiguration configuration, ITelemetryBufferService bufferService)
    {
        _maxSize = configuration.GetValue<int>("Alerts:MaxBufferSize", 100);
        _bufferService = bufferService;
    }

    public void AddAlert(AlertMessage alert)
    {
        if (alert == null)
        {
            return;
        }

        var incomingKey = $"{alert.Timestamp:O}|{alert.SampleIndex}|{alert.Annotation}|{alert.Message}";
        var exists = _alerts
            .ToArray()
            .Any(a => $"{a.Timestamp:O}|{a.SampleIndex}|{a.Annotation}|{a.Message}" == incomingKey);
        if (exists)
        {
            return;
        }

        _alerts.Enqueue(alert);

        while (_alerts.Count > _maxSize)
        {
            _alerts.TryDequeue(out _);
        }
    }

    public IReadOnlyList<AlertMessage> GetLatest(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<AlertMessage>();
        }

        return _alerts
            .ToArray()
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.SampleIndex)
            .Take(count)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<AlertMessage> GetLatestByRecord(int count, string recordId)
    {
        if (count <= 0 || string.IsNullOrWhiteSpace(recordId))
        {
            return Array.Empty<AlertMessage>();
        }

        var recordTelemetry = _bufferService.GetLatestByRecord(1000, recordId);
        var patientIds = recordTelemetry
            .Select(m => m.PatientId)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var samplesByRecord = recordTelemetry
            .Select(m => (m.Timestamp, m.SampleIndex, m.PatientId))
            .ToHashSet();

        if (samplesByRecord.Count == 0 && patientIds.Count == 0)
        {
            return Array.Empty<AlertMessage>();
        }

        return _alerts
            .ToArray()
            .Where(a =>
                samplesByRecord.Contains((a.Timestamp, a.SampleIndex, a.PatientId)) ||
                patientIds.Contains(a.PatientId))
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.SampleIndex)
            .Take(count)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<AlertMessage> GetAll()
    {
        return _alerts.ToArray().AsReadOnly();
    }

    public AlertMessage? GetLastAlert()
    {
        return _alerts
            .ToArray()
            .OrderByDescending(a => a.Timestamp)
            .ThenByDescending(a => a.SampleIndex)
            .FirstOrDefault();
    }

    public int GetCount()
    {
        return _alerts.Count;
    }
}
