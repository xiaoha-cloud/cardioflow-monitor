using System.Collections.Concurrent;
using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

public class AlertService : IAlertService
{
    private readonly ConcurrentQueue<AlertMessage> _alerts = new();
    private readonly int _maxSize;

    public AlertService(IConfiguration configuration)
    {
        _maxSize = configuration.GetValue<int>("Alerts:MaxBufferSize", 100);
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
        var normalizedRecordId = recordId.Trim();

        return _alerts
            .ToArray()
            .Where(a => string.Equals(a.RecordId, normalizedRecordId, StringComparison.OrdinalIgnoreCase))
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
