using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/alerts")]
public class AlertsController : ControllerBase
{
    private static readonly HashSet<string> AllowedRecordIds = ["100", "101", "103"];
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
    public ActionResult<IReadOnlyList<AlertMessage>> GetLatest(
        [FromQuery] int count = DefaultCount,
        [FromQuery] string? recordId = null)
    {
        if (count < 1 || count > MaxCount)
        {
            _logger.LogWarning("Invalid count parameter for /api/alerts: {Count}", count);
            return BadRequest("count must be between 1 and 100");
        }

        if (!string.IsNullOrWhiteSpace(recordId) && !AllowedRecordIds.Contains(recordId.Trim()))
        {
            return BadRequest("recordId must be one of: 100, 101, 103");
        }

        var alerts = string.IsNullOrWhiteSpace(recordId)
            ? _alertService.GetLatest(count)
            : _alertService.GetLatestByRecord(count, recordId);

        return Ok(alerts);
    }
}
