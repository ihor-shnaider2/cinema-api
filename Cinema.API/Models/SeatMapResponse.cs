namespace Cinema.API.Models;

public class SeatMapResponse
{
    public string Auditorium { get; set; } = string.Empty;
    public string FilmTitle { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public Dictionary<string, string> SeatRows { get; set; } = new();
}
