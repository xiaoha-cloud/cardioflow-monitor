using CardioFlow.Api.Models;

namespace CardioFlow.Api.Services;

/// <summary>
/// Interface for telemetry message buffer service.
/// Provides thread-safe storage and retrieval of ECG telemetry messages.
/// </summary>
public interface ITelemetryBufferService
{
    /// <summary>
    /// Adds a telemetry message to the buffer.
    /// If the buffer is full, the oldest message is removed (FIFO).
    /// </summary>
    /// <param name="message">The telemetry message to add.</param>
    void Add(TelemetryMessage message);

    /// <summary>
    /// Gets the latest N messages from the buffer.
    /// </summary>
    /// <param name="count">Number of messages to retrieve.</param>
    /// <returns>Read-only list of telemetry messages (most recent first).</returns>
    IReadOnlyList<TelemetryMessage> GetLatest(int count);

    /// <summary>
    /// Gets the latest N messages from the buffer for a specific MIT-BIH record.
    /// </summary>
    /// <param name="count">Number of messages to retrieve.</param>
    /// <param name="recordId">Record identifier filter (e.g., "100").</param>
    /// <returns>Read-only list of telemetry messages (most recent first).</returns>
    IReadOnlyList<TelemetryMessage> GetLatestByRecord(int count, string recordId);

    /// <summary>
    /// Gets all messages currently in the buffer.
    /// </summary>
    /// <returns>Read-only list of all telemetry messages.</returns>
    IReadOnlyList<TelemetryMessage> GetAll();

    /// <summary>
    /// Clears all messages from the buffer.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the current number of messages in the buffer.
    /// </summary>
    /// <returns>Number of messages in the buffer.</returns>
    int GetCount();

    /// <summary>
    /// Gets the timestamp of the latest message currently in the buffer.
    /// </summary>
    /// <returns>Latest message timestamp, or null when buffer is empty.</returns>
    DateTime? GetLastMessageAt();

    /// <summary>
    /// Gets the latest active record identifier in the buffer.
    /// </summary>
    /// <returns>Record identifier, or null when buffer is empty.</returns>
    string? GetLatestRecordId();
}
