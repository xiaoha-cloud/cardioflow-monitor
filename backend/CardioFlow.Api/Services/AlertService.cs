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
