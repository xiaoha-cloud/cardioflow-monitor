using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private const int DefaultCount = 20;
    private const int MaxCount = 100;
    private readonly IAlertService _alertService;
    private readonly ILogger<AlertsController> _logger;

    public AlertsController(IAlertService alertService, ILogger<AlertsController> logger)
    {
        _alertService = alertService;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<AlertMessage>> GetLatest([FromQuery] int count = DefaultCount)
    {
        if (count < 1 || count > MaxCount)
        {
            _logger.LogWarning("Invalid count parameter for /api/alerts: {Count}", count);
            return BadRequest("count must be between 1 and 100");
        }

        return Ok(_alertService.GetLatest(count));
    }
}
