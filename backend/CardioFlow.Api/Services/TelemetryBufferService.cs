using System.Collections.Concurrent;
using CardioFlow.Api.Models;
namespace CardioFlow.Api.Services;

/// <summary>
/// Thread-safe in-memory buffer for storing ECG telemetry messages.
/// Uses FIFO (First-In-First-Out) strategy when buffer is full.
/// </summary>
public class TelemetryBufferService : ITelemetryBufferService
{
    private readonly ConcurrentQueue<TelemetryMessage> _buffer;
    private readonly int _maxSize;
    private readonly ILogger<TelemetryBufferService> _logger;
    private int _totalMessagesReceived;

    /// <summary>
    /// Initializes a new instance of the TelemetryBufferService.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Configuration to read max buffer size.</param>
    public TelemetryBufferService(
        ILogger<TelemetryBufferService> logger,
        IConfiguration configuration)
    {
        _buffer = new ConcurrentQueue<TelemetryMessage>();
        _maxSize = configuration.GetValue<int>("TelemetryBuffer:MaxBufferSize", 1000);
        _logger = logger;
        _totalMessagesReceived = 0;

        _logger.LogInformation("TelemetryBufferService initialized with max size: {MaxSize}", _maxSize);
    }

    /// <summary>
    /// Adds a telemetry message to the buffer.
    /// If the buffer exceeds max size, removes the oldest message (FIFO).
    /// </summary>
    /// <param name="message">The telemetry message to add.</param>
    public void Add(TelemetryMessage message)
    {
        if (message == null)
        {
            _logger.LogWarning("Attempted to add null message to buffer");
            return;
        }

        _buffer.Enqueue(message);
        _totalMessagesReceived++;
        var currentCount = _buffer.Count;

        // Remove oldest messages if buffer exceeds max size
        while (currentCount > _maxSize)
        {
            if (_buffer.TryDequeue(out var removedMessage))
            {
                currentCount--;
                _logger.LogDebug(
                    "Buffer full, removed oldest message: sampleIndex={SampleIndex}",
                    removedMessage.SampleIndex);
            }
            else
            {
                break;
            }
        }

        // Log every 100 messages
        if (_totalMessagesReceived % 100 == 0)
        {
            _logger.LogInformation(
                "Buffer status: {CurrentCount}/{MaxSize} messages, total received: {TotalReceived}",
                currentCount,
                _maxSize,
                _totalMessagesReceived);
        }
    }

    /// <summary>
    /// Gets the latest N messages from the buffer (most recent first).
    /// </summary>
    /// <param name="count">Number of messages to retrieve.</param>
    /// <returns>Read-only list of telemetry messages.</returns>
    public IReadOnlyList<TelemetryMessage> GetLatest(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<TelemetryMessage>();
        }

        var allMessages = _buffer.ToArray();
        var latestMessages = allMessages
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.SampleIndex)
            .Take(count)
            .ToList();

        return latestMessages.AsReadOnly();
    }

    /// <summary>
    /// Gets all messages currently in the buffer.
    /// </summary>
    /// <returns>Read-only list of all telemetry messages.</returns>
    public IReadOnlyList<TelemetryMessage> GetAll()
    {
        return _buffer.ToArray().AsReadOnly();
    }

    /// <summary>
    /// Clears all messages from the buffer.
    /// </summary>
    public void Clear()
    {
        while (_buffer.TryDequeue(out _))
        {
            // Dequeue all items
        }

        _logger.LogInformation("Buffer cleared");
    }

    /// <summary>
    /// Gets the current number of messages in the buffer.
    /// </summary>
    /// <returns>Number of messages in the buffer.</returns>
    public int GetCount()
    {
        return _buffer.Count;
    }

    /// <summary>
    /// Gets timestamp of the latest buffered message.
    /// </summary>
    public DateTime? GetLastMessageAt()
    {
        return _buffer
            .ToArray()
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.SampleIndex)
            .Select(m => (DateTime?)m.Timestamp)
            .FirstOrDefault();
    }
}
