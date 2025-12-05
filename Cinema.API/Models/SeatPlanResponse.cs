namespace Cinema.API.Models;

public class SeatPlanResponse
{
    public string Auditorium { get; set; } = string.Empty;
    public string FilmTitle { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public List<Seat> Seats { get; set; } = new();
}

public class Seat
{
    public string Row { get; set; } = string.Empty;
    public int SeatNumber { get; set; }
    public string Status { get; set; } = string.Empty;
}
