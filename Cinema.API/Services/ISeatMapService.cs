using Cinema.API.Models;

namespace Cinema.API.Services;

public interface ISeatMapService
{
    Task<SeatPlanResponse?> GetSeatPlanAsync(CancellationToken cancellationToken = default);
    Task<SeatAvailabilityResponse?> CheckSeatAvailabilityAsync(string row, int seatNumber, CancellationToken cancellationToken = default);
}
