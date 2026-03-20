using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/patients")]
public class PatientsController : ControllerBase
{
    private readonly ITelemetryBufferService _bufferService;
    private readonly IStatusAggregationService _statusAggregationService;
    private readonly ILogger<PatientsController> _logger;

    public PatientsController(
        ITelemetryBufferService bufferService,
        IStatusAggregationService statusAggregationService,
        ILogger<PatientsController> logger)
    {
        _bufferService = bufferService;
        _statusAggregationService = statusAggregationService;
        _logger = logger;
    }

    [HttpGet("current")]
    [ProducesResponseType(typeof(CurrentPatientDto), StatusCodes.Status200OK)]
    public ActionResult<CurrentPatientDto> GetCurrent()
    {
        var latestTelemetry = _bufferService.GetLatest(1).FirstOrDefault();
        var streamStatus = _statusAggregationService.GetCurrentStatus().StreamStatus;

        var dto = new CurrentPatientDto
        {
            PatientId = latestTelemetry?.PatientId,
            RecordId = latestTelemetry?.RecordId,
            DeviceId = latestTelemetry?.DeviceId,
            Battery = latestTelemetry?.Battery,
            SignalQuality = latestTelemetry?.SignalQuality,
            LastSeenAt = latestTelemetry?.Timestamp,
            StreamStatus = streamStatus
        };

        return Ok(dto);
    }

    [HttpGet("current/history")]
    public IActionResult GetCurrentHistory([FromQuery] int minutes = 5)
    {
        if (minutes < 1 || minutes > 60)
        {
            return BadRequest("minutes must be between 1 and 60");
        }

        _logger.LogInformation(
            "Patient history endpoint is reserved for future implementation. Requested minutes={Minutes}",
            minutes);
        return StatusCode(StatusCodes.Status501NotImplemented, new
        {
            message = "Not implemented yet. This endpoint is reserved for future patient history support."
        });
    }
}
