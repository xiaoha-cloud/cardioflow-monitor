using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

/// <summary>
/// Controller for accessing ECG telemetry data from the buffer.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryBufferService _bufferService;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(
        ITelemetryBufferService bufferService,
        ILogger<TelemetryController> logger)
    {
        _bufferService = bufferService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the latest N telemetry messages from the buffer.
    /// </summary>
    /// <param name="count">Number of messages to retrieve (default: 100, max: 1000).</param>
    /// <returns>List of telemetry messages.</returns>
    [HttpGet("latest")]
    public ActionResult<IReadOnlyList<TelemetryMessage>> GetLatest([FromQuery] int count = 100)
    {
        if (count <= 0 || count > 1000)
        {
            return BadRequest("Count must be between 1 and 1000");
        }

        var messages = _bufferService.GetLatest(count);
        _logger.LogInformation("Retrieved {Count} latest messages from buffer", messages.Count);

        return Ok(messages);
    }

    /// <summary>
    /// Gets all telemetry messages currently in the buffer.
    /// </summary>
    /// <returns>List of all telemetry messages.</returns>
    [HttpGet("all")]
    public ActionResult<IReadOnlyList<TelemetryMessage>> GetAll()
    {
        var messages = _bufferService.GetAll();
        _logger.LogInformation("Retrieved all {Count} messages from buffer", messages.Count);

        return Ok(messages);
    }

    /// <summary>
    /// Gets the current buffer status.
    /// </summary>
    /// <returns>Buffer status information.</returns>
    [HttpGet("status")]
    public ActionResult<object> GetStatus()
    {
        var count = _bufferService.GetCount();
        var latestMessages = _bufferService.GetLatest(1);

        return Ok(new
        {
            count = count,
            hasMessages = count > 0,
            latestMessage = latestMessages.FirstOrDefault()
        });
    }

    /// <summary>
    /// Clears all messages from the buffer.
    /// </summary>
    [HttpPost("clear")]
    public IActionResult Clear()
    {
        _bufferService.Clear();
        _logger.LogWarning("Buffer cleared by API request");
        return Ok(new { message = "Buffer cleared successfully" });
    }
}
