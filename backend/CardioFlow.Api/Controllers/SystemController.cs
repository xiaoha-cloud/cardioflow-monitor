using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly ITelemetryBufferService _bufferService;
    private readonly IAlertService _alertService;
    private readonly IConfiguration _configuration;

    public SystemController(
        ITelemetryBufferService bufferService,
        IAlertService alertService,
        IConfiguration configuration)
    {
        _bufferService = bufferService;
        _alertService = alertService;
        _configuration = configuration;
    }

    [HttpGet("status")]
    public ActionResult<SystemStatusDto> GetStatus()
    {
        var latestTelemetry = _bufferService.GetLatest(1).FirstOrDefault();
        var bufferCount = _bufferService.GetCount();
        var lastMessageAt = _bufferService.GetLastMessageAt();
        var now = DateTime.UtcNow;

        var streamStatus = "stopped";
        if (bufferCount > 0)
        {
            streamStatus = lastMessageAt.HasValue && now - lastMessageAt.Value <= TimeSpan.FromSeconds(30)
                ? "running"
                : "idle";
        }

        var dto = new SystemStatusDto
        {
            StreamStatus = streamStatus,
            SamplingRate = _configuration.GetValue<int>("Telemetry:SamplingRate", 360),
            Topic = _configuration["Kafka:TelemetryTopic"] ?? "ecg.telemetry",
            ActivePatient = latestTelemetry?.PatientId,
            LastAlert = _alertService.GetLastAlert()?.Message,
            BufferCount = bufferCount,
            LastMessageAt = lastMessageAt
        };

        return Ok(dto);
    }
}
