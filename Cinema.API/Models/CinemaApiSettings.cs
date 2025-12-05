namespace Cinema.API.Models;

public class CinemaApiSettings
{
    public string SeatMapUrl { get; set; } = string.Empty;
    public int CacheDurationSeconds { get; set; } = 5;
    public int HttpTimeoutSeconds { get; set; } = 10;
}
