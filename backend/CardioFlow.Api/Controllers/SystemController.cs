using CardioFlow.Api.Models;
using CardioFlow.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CardioFlow.Api.Controllers;

[ApiController]
[Route("api/system")]
public class SystemController : ControllerBase
{
    private readonly IStatusAggregationService _statusAggregationService;

    public SystemController(
        IStatusAggregationService statusAggregationService)
    {
        _statusAggregationService = statusAggregationService;
    }

    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusDto), StatusCodes.Status200OK)]
    public ActionResult<SystemStatusDto> GetStatus()
    {
        return Ok(_statusAggregationService.GetCurrentStatus());
    }
}
