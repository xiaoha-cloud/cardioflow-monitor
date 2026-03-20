using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/ecg")]
public class EcgController : ControllerBase
{
    private const int DefaultCount = 500;
    private const int MaxCount = 1000;
    private readonly ITelemetryBufferService _bufferService;
    private readonly ILogger<EcgController> _logger;

    public EcgController(ITelemetryBufferService bufferService, ILogger<EcgController> logger)
    {
        _bufferService = bufferService;
        _logger = logger;
    }

    [HttpGet("latest")]
    public ActionResult<IReadOnlyList<TelemetryMessage>> GetLatest([FromQuery] int count = DefaultCount)
    {
        if (count < 1 || count > MaxCount)
        {
            _logger.LogWarning("Invalid count parameter for /api/ecg/latest: {Count}", count);
            return BadRequest("count must be between 1 and 1000");
        }

        var sorted = _bufferService
            .GetLatest(count)
            .OrderBy(m => m.SampleIndex)
            .ToList()
            .AsReadOnly();

        return Ok(sorted);
    }
}
