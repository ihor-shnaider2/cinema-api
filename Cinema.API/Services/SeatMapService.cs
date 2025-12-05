using System.Text.Json;
using Cinema.API.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cinema.API.Services;

public class SeatMapService : ISeatMapService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<SeatMapService> _logger;
    private readonly CinemaApiSettings _settings;
    private static readonly SemaphoreSlim _fetchSemaphore = new(1, 1); // Static to prevent cache stampede across all instances
    private const string CacheKey = "SeatMapData";

    public SeatMapService(
        HttpClient httpClient,
        IMemoryCache memoryCache,
        ILogger<SeatMapService> logger,
        IOptions<CinemaApiSettings> settings)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<SeatPlanResponse?> GetSeatPlanAsync(CancellationToken cancellationToken = default)
    {
        var seatMapData = await GetSeatMapDataAsync(cancellationToken);
        if (seatMapData == null)
        {
            return null;
        }

        return TransformToSeatPlan(seatMapData);
    }

    public async Task<SeatAvailabilityResponse?> CheckSeatAvailabilityAsync(string row, int seatNumber, CancellationToken cancellationToken = default)
    {
        var seatMapData = await GetSeatMapDataAsync(cancellationToken);
        if (seatMapData == null)
        {
            return null;
        }

        // Validate row exists
        if (!seatMapData.SeatRows.TryGetValue(row, out var rowData))
        {
            return null;
        }

        // Validate seat number is within range (1-indexed)
        if (seatNumber < 1 || seatNumber > rowData.Length)
        {
            return null;
        }

        // Check availability (0 = available, 1 = booked)
        var isAvailable = rowData[seatNumber - 1] == '0';
        
        return new SeatAvailabilityResponse { Available = isAvailable };
    }

    private async Task<SeatMapResponse?> GetSeatMapDataAsync(CancellationToken cancellationToken)
    {
        // Try to get from cache first (fast path without lock)
        if (_memoryCache.TryGetValue(CacheKey, out SeatMapResponse? cachedData))
        {
            _logger.LogDebug("Returning seat map from cache");
            return cachedData;
        }

        // Use semaphore to prevent cache stampede (multiple concurrent requests to upstream)
        await _fetchSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check cache after acquiring lock (another request may have populated it)
            if (_memoryCache.TryGetValue(CacheKey, out cachedData))
            {
                _logger.LogDebug("Returning seat map from cache (after lock)");
                return cachedData;
            }

            _logger.LogInformation("Fetching seat map from upstream API");
            
            // Fetch from upstream with cancellation support
            using var response = await _httpClient.GetAsync(_settings.SeatMapUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            // Use streaming to deserialize directly from response stream
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            // Deserialize as array since the JSON is wrapped in array
            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true // Handle malformed JSON with trailing commas
            };
            var seatMaps = await JsonSerializer.DeserializeAsync<List<SeatMapResponse>>(
                stream, 
                jsonOptions,
                cancellationToken);

            var seatMapData = seatMaps?.FirstOrDefault();
            
            if (seatMapData != null)
            {
                // Cache with sliding expiration
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_settings.CacheDurationSeconds),
                    SlidingExpiration = TimeSpan.FromSeconds(_settings.CacheDurationSeconds / 2)
                };
                
                _memoryCache.Set(CacheKey, seatMapData, cacheOptions);
                _logger.LogInformation("Seat map cached successfully");
            }

            return seatMapData;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching seat map from upstream");
            
            // Try to return stale cache if available
            if (_memoryCache.TryGetValue(CacheKey, out SeatMapResponse? staleData))
            {
                _logger.LogWarning("Returning stale cached data due to upstream failure");
                return staleData;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching seat map");
            return null;
        }
        finally
        {
            _fetchSemaphore.Release();
        }
    }

    private static SeatPlanResponse TransformToSeatPlan(SeatMapResponse seatMapData)
    {
        var seats = new List<Seat>();
        
        // Pre-allocate capacity if possible
        var estimatedCapacity = seatMapData.SeatRows.Sum(r => r.Value.Length);
        seats.Capacity = estimatedCapacity;

        foreach (var row in seatMapData.SeatRows.OrderBy(r => r.Key))
        {
            var rowLetter = row.Key;
            var rowSeats = row.Value.AsSpan(); // Use Span for efficient iteration without allocation
            
            for (int i = 0; i < rowSeats.Length; i++)
            {
                seats.Add(new Seat
                {
                    Row = rowLetter,
                    SeatNumber = i + 1, // 1-indexed
                    Status = rowSeats[i] == '0' ? "available" : "booked"
                });
            }
        }

        return new SeatPlanResponse
        {
            Auditorium = seatMapData.Auditorium,
            FilmTitle = seatMapData.FilmTitle,
            StartTime = seatMapData.StartTime,
            Seats = seats
        };
    }
}
