using Cinema.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cinema.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeatsController : ControllerBase
{
    private readonly ISeatMapService _seatMapService;
    private readonly ILogger<SeatsController> _logger;

    public SeatsController(ISeatMapService seatMapService, ILogger<SeatsController> logger)
    {
        _seatMapService = seatMapService;
        _logger = logger;
    }

    /// <summary>
    /// Get the complete seat plan for the cinema
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetSeatPlan(CancellationToken cancellationToken)
    {
        var seatPlan = await _seatMapService.GetSeatPlanAsync(cancellationToken);
        
        if (seatPlan == null)
        {
            _logger.LogWarning("Unable to retrieve seat plan from upstream");
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Service temporarily unavailable" });
        }

        return Ok(seatPlan);
    }

    /// <summary>
    /// Check availability of a specific seat
    /// </summary>
    /// <param name="row">The row letter (e.g., A, B, C)</param>
    /// <param name="seatNumber">The seat number within the row</param>
    /// <param name="cancellationToken"></param>
    [HttpGet("{row}/{seatNumber}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CheckSeatAvailability(string row, int seatNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(row) || seatNumber < 1)
        {
            return BadRequest(new { error = "Invalid row or seat number" });
        }

        var availability = await _seatMapService.CheckSeatAvailabilityAsync(row.ToUpperInvariant(), seatNumber, cancellationToken);
        
        if (availability == null)
        {
            return NotFound(new { error = "Seat not found or service unavailable" });
        }

        return Ok(availability);
    }
}
