using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("health")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ITelemetryBufferService _bufferService;
    private readonly IAlertService _alertService;

    public HealthController(
        IConfiguration configuration,
        ITelemetryBufferService bufferService,
        IAlertService alertService)
    {
        _configuration = configuration;
        _bufferService = bufferService;
        _alertService = alertService;
    }

    [HttpGet]
    public ActionResult<object> Get()
    {
        var kafkaConfigured =
            !string.IsNullOrWhiteSpace(_configuration["Kafka:BootstrapServers"]) &&
            !string.IsNullOrWhiteSpace(_configuration["Kafka:TelemetryTopic"]);

        return Ok(new
        {
            status = "ok",
            kafkaConfigured,
            bufferCount = _bufferService.GetCount(),
            alertsCount = _alertService.GetCount()
        });
    }
}
