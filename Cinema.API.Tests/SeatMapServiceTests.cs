using System.Net;
using System.Text;
using System.Text.Json;
using Cinema.API.Models;
using Cinema.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace Cinema.API.Tests;

public class SeatMapServiceTests
{
    private readonly Mock<ILogger<SeatMapService>> _loggerMock;
    private readonly IMemoryCache _memoryCache;
    private readonly IOptions<CinemaApiSettings> _settings;

    public SeatMapServiceTests()
    {
        _loggerMock = new Mock<ILogger<SeatMapService>>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _settings = Options.Create(new CinemaApiSettings
        {
            SeatMapUrl = "https://example.com/seatmap.json",
            CacheDurationSeconds = 5,
            HttpTimeoutSeconds = 10
        });
    }

    [Fact]
    public async Task GetSeatPlanAsync_ReturnsTransformedData_WhenUpstreamSucceeds()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string>
                {
                    { "A", "110" },
                    { "B", "001" }
                }
            }
        };

        var httpClient = CreateMockHttpClient(seatMapData);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        var result = await service.GetSeatPlanAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Main-Hall", result.Auditorium);
        Assert.Equal("Test Movie", result.FilmTitle);
        Assert.Equal(6, result.Seats.Count);
        
        // Verify seat transformation
        var firstSeat = result.Seats[0];
        Assert.Equal("A", firstSeat.Row);
        Assert.Equal(1, firstSeat.SeatNumber);
        Assert.Equal("booked", firstSeat.Status);
        
        var thirdSeat = result.Seats[2];
        Assert.Equal("A", thirdSeat.Row);
        Assert.Equal(3, thirdSeat.SeatNumber);
        Assert.Equal("available", thirdSeat.Status);
    }

    [Fact]
    public async Task CheckSeatAvailabilityAsync_ReturnsTrue_WhenSeatIsAvailable()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string>
                {
                    { "A", "101" }
                }
            }
        };

        var httpClient = CreateMockHttpClient(seatMapData);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        var result = await service.CheckSeatAvailabilityAsync("A", 2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Available);
    }

    [Fact]
    public async Task CheckSeatAvailabilityAsync_ReturnsFalse_WhenSeatIsBooked()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string>
                {
                    { "A", "101" }
                }
            }
        };

        var httpClient = CreateMockHttpClient(seatMapData);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        var result = await service.CheckSeatAvailabilityAsync("A", 1);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Available);
    }

    [Fact]
    public async Task CheckSeatAvailabilityAsync_ReturnsNull_WhenRowDoesNotExist()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string>
                {
                    { "A", "101" }
                }
            }
        };

        var httpClient = CreateMockHttpClient(seatMapData);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        var result = await service.CheckSeatAvailabilityAsync("Z", 1);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CheckSeatAvailabilityAsync_ReturnsNull_WhenSeatNumberIsOutOfRange()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string>
                {
                    { "A", "101" }
                }
            }
        };

        var httpClient = CreateMockHttpClient(seatMapData);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        var result = await service.CheckSeatAvailabilityAsync("A", 10);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeatPlanAsync_UsesCachedData_OnSecondCall()
    {
        // Arrange
        var seatMapData = new List<SeatMapResponse>
        {
            new()
            {
                Auditorium = "Main-Hall",
                FilmTitle = "Test Movie",
                StartTime = "19:00",
                SeatRows = new Dictionary<string, string> { { "A", "110" } }
            }
        };

        var callCount = 0;
        var httpClient = CreateMockHttpClient(seatMapData, () => callCount++);
        var service = new SeatMapService(httpClient, _memoryCache, _loggerMock.Object, _settings);

        // Act
        await service.GetSeatPlanAsync();
        await service.GetSeatPlanAsync();

        // Assert
        Assert.Equal(1, callCount); // HTTP client should only be called once
    }

    private HttpClient CreateMockHttpClient(List<SeatMapResponse> data, Action? onSend = null)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                onSend?.Invoke();
                var json = JsonSerializer.Serialize(data);
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        return new HttpClient(mockHandler.Object);
    }
}
