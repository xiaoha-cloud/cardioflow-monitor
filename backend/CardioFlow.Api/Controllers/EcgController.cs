using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/ecg")]
public class EcgController : ControllerBase
{
    private static readonly HashSet<string> AllowedRecordIds = ["100", "101", "103"];
    private static readonly HashSet<int> AllowedWindowSeconds = [5, 10];
    private static readonly HashSet<int> AllowedDownsample = [1, 2, 4];
    private const int DefaultCount = 800;
    private const int MaxCount = 1000;
    private const int EventsDefaultCount = 30;
    private const int EventsMaxCount = 50;
    private readonly ITelemetryBufferService _bufferService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EcgController> _logger;

    public EcgController(
        ITelemetryBufferService bufferService,
        IConfiguration configuration,
        ILogger<EcgController> logger)
    {
        _bufferService = bufferService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(IReadOnlyList<TelemetryMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TelemetryMessage>> GetLatest(
        [FromQuery] int count = DefaultCount,
        [FromQuery] int? windowSeconds = null,
        [FromQuery] string? recordId = null,
        [FromQuery] int downsample = 1)
    {
        var validationError = ValidateQuery(count, windowSeconds, downsample);
        if (validationError is not null)
        {
            _logger.LogWarning("Invalid query parameters for /api/ecg/latest: {Error}", validationError);
            return BadRequest(validationError);
        }

        if (!IsValidRecordId(recordId))
        {
            return BadRequest("recordId must be one of: 100, 101, 103");
        }

        var samplingRate = _configuration.GetValue<int>("Telemetry:SamplingRate", 360);
        var messages = ResolveMessages(count, windowSeconds, recordId, downsample, samplingRate);
        return Ok(messages);
    }

    [HttpGet("window")]
    [ProducesResponseType(typeof(EcgWindowResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<EcgWindowResponseDto> GetWindow(
        [FromQuery] int count = DefaultCount,
        [FromQuery] int? windowSeconds = null,
        [FromQuery] string? recordId = null,
        [FromQuery] int downsample = 1)
    {
        var validationError = ValidateQuery(count, windowSeconds, downsample);
        if (validationError is not null)
        {
            _logger.LogWarning("Invalid query parameters for /api/ecg/window: {Error}", validationError);
            return BadRequest(validationError);
        }

        if (!IsValidRecordId(recordId))
        {
            return BadRequest("recordId must be one of: 100, 101, 103");
        }

        var samplingRate = _configuration.GetValue<int>("Telemetry:SamplingRate", 360);
        var messages = ResolveMessages(count, windowSeconds, recordId, downsample, samplingRate);

        var dto = new EcgWindowResponseDto
        {
            Meta = new EcgWindowMetaDto
            {
                Count = messages.Count,
                SamplingRate = samplingRate,
                WindowSeconds = windowSeconds,
                Downsample = downsample
            },
            Items = messages
        };
        return Ok(dto);
    }

    [HttpGet("events")]
    [ProducesResponseType(typeof(IReadOnlyList<TelemetryMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<TelemetryMessage>> GetEvents(
        [FromQuery] int count = EventsDefaultCount,
        [FromQuery] string? recordId = null)
    {
        if (count < 1 || count > EventsMaxCount)
        {
            return BadRequest("count must be between 1 and 50");
        }

        if (!IsValidRecordId(recordId))
        {
            return BadRequest("recordId must be one of: 100, 101, 103");
        }

        var effectiveRecordId = string.IsNullOrWhiteSpace(recordId)
            ? _bufferService.GetLatest(1).FirstOrDefault()?.RecordId
            : recordId.Trim();

        var latestMessages = string.IsNullOrWhiteSpace(effectiveRecordId)
            ? _bufferService.GetLatest(count)
            : _bufferService.GetLatestByRecord(count, effectiveRecordId);

        var latestFirst = latestMessages
            .OrderByDescending(m => m.Timestamp)
            .ThenByDescending(m => m.SampleIndex)
            .ToList()
            .AsReadOnly();

        return Ok(latestFirst);
    }

    private static bool IsValidRecordId(string? recordId)
    {
        return string.IsNullOrWhiteSpace(recordId) || AllowedRecordIds.Contains(recordId.Trim());
    }

    private IReadOnlyList<TelemetryMessage> ResolveMessages(
        int count,
        int? windowSeconds,
        string? recordId,
        int downsample,
        int samplingRate)
    {
        var effectiveCount = count;
        if (windowSeconds.HasValue)
        {
            effectiveCount = Math.Min(count, windowSeconds.Value * samplingRate);
        }

        var effectiveRecordId = string.IsNullOrWhiteSpace(recordId)
            ? _bufferService.GetLatest(1).FirstOrDefault()?.RecordId
            : recordId.Trim();

        var latestMessages = string.IsNullOrWhiteSpace(effectiveRecordId)
            ? _bufferService.GetLatest(effectiveCount)
            : _bufferService.GetLatestByRecord(effectiveCount, effectiveRecordId);

        // Keep order deterministic for front-end rolling chart.
        var sorted = latestMessages
            .OrderBy(m => m.SampleIndex)
            .ToList();

        if (downsample > 1 && sorted.Count > 0)
        {
            sorted = sorted
                .Where((_, index) => index % downsample == 0)
                .ToList();
        }

        return sorted.AsReadOnly();
    }

    private static string? ValidateQuery(int count, int? windowSeconds, int downsample)
    {
        if (count < 1 || count > MaxCount)
        {
            return "count must be between 1 and 1000";
        }

        if (windowSeconds.HasValue && !AllowedWindowSeconds.Contains(windowSeconds.Value))
        {
            return "windowSeconds must be one of: 5, 10";
        }

        if (!AllowedDownsample.Contains(downsample))
        {
            return "downsample must be one of: 1, 2, 4";
        }

        return null;
    }
}
