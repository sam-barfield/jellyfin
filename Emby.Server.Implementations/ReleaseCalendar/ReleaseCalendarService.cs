using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.ReleaseCalendar;
using MediaBrowser.Model.ReleaseCalendar;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.ReleaseCalendar;

/// <summary>
/// Provides access to release calendar data from Radarr and Sonarr.
/// </summary>
public class ReleaseCalendarService : IReleaseCalendarService
{
    private const string CacheKeyPrefix = "ReleaseCalendar_";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ReleaseCalendarService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseCalendarService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="memoryCache">The <see cref="IMemoryCache"/>.</param>
    /// <param name="logger">The logger.</param>
    public ReleaseCalendarService(
        IHttpClientFactory httpClientFactory,
        IServerConfigurationManager configurationManager,
        IMemoryCache memoryCache,
        ILogger<ReleaseCalendarService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configurationManager = configurationManager;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReleaseCalendarItem>> GetCalendarAsync(
        DateTime start,
        DateTime end,
        ReleaseCalendarType? type,
        CancellationToken cancellationToken)
    {
        var cacheKey = string.Create(
            CultureInfo.InvariantCulture,
            $"{CacheKeyPrefix}{start:yyyy-MM-dd}_{end:yyyy-MM-dd}_{type}");

        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<ReleaseCalendarItem>? cached) && cached is not null)
        {
            return cached;
        }

        var config = _configurationManager.Configuration.ReleaseCalendar;
        var items = new List<ReleaseCalendarItem>();

        if (type is null or ReleaseCalendarType.Movie)
        {
            items.AddRange(await GetRadarrCalendarAsync(config, start, end, cancellationToken).ConfigureAwait(false));
        }

        if (type is null or ReleaseCalendarType.Episode)
        {
            items.AddRange(await GetSonarrCalendarAsync(config, start, end, cancellationToken).ConfigureAwait(false));
        }

        var sorted = items.OrderBy(i => i.AirDateUtc).ToList().AsReadOnly();

        var ttl = config.CacheTimeToLiveMinutes > 0
            ? TimeSpan.FromMinutes(config.CacheTimeToLiveMinutes)
            : TimeSpan.FromMinutes(30);

        _memoryCache.Set(cacheKey, sorted, ttl);

        return sorted;
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        // IMemoryCache does not expose enumeration of keys.
        // We use a CancellationTokenSource approach to evict all calendar entries.
        if (_memoryCache is MemoryCache mc)
        {
            mc.Compact(1.0);
            _logger.LogInformation("Release calendar cache invalidated");
        }
    }

    private async Task<IReadOnlyList<ReleaseCalendarItem>> GetRadarrCalendarAsync(
        ReleaseCalendarConfiguration config,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.RadarrUrl) || string.IsNullOrWhiteSpace(config.RadarrApiKey))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{config.RadarrUrl.TrimEnd('/')}/api/v3/calendar?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", config.RadarrApiKey);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var movies = await response.Content.ReadFromJsonAsync<List<RadarrMovie>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (movies is null)
            {
                return [];
            }

            return movies.Select(movie => new ReleaseCalendarItem
            {
                Title = movie.Title ?? string.Empty,
                SortTitle = movie.SortTitle ?? string.Empty,
                Overview = movie.Overview ?? string.Empty,
                AirDateUtc = movie.InCinemas ?? movie.PhysicalRelease ?? movie.DigitalRelease ?? DateTime.UtcNow,
                Type = ReleaseCalendarType.Movie,
                Year = movie.Year,
                ImageUrl = GetRadarrImageUrl(config.RadarrUrl, movie),
                Status = movie.Status ?? string.Empty,
                HasFile = movie.HasFile
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Radarr calendar from {Url}", config.RadarrUrl);
            return [];
        }
    }

    private async Task<IReadOnlyList<ReleaseCalendarItem>> GetSonarrCalendarAsync(
        ReleaseCalendarConfiguration config,
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.SonarrUrl) || string.IsNullOrWhiteSpace(config.SonarrApiKey))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{config.SonarrUrl.TrimEnd('/')}/api/v3/calendar?start={start:yyyy-MM-dd}&end={end:yyyy-MM-dd}&includeSeries=true";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("X-Api-Key", config.SonarrApiKey);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var episodes = await response.Content.ReadFromJsonAsync<List<SonarrEpisode>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (episodes is null)
            {
                return [];
            }

            return episodes.Select(episode => new ReleaseCalendarItem
            {
                Title = episode.Title ?? string.Empty,
                SortTitle = episode.Series?.SortTitle ?? string.Empty,
                Overview = episode.Overview ?? string.Empty,
                AirDateUtc = episode.AirDateUtc ?? DateTime.UtcNow,
                Type = ReleaseCalendarType.Episode,
                Year = episode.Series?.Year ?? 0,
                SeriesTitle = episode.Series?.Title,
                SeasonNumber = episode.SeasonNumber,
                EpisodeNumber = episode.EpisodeNumber,
                ImageUrl = GetSonarrImageUrl(config.SonarrUrl, episode),
                Status = episode.HasFile ? "aired" : "unaired",
                HasFile = episode.HasFile
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Sonarr calendar from {Url}", config.SonarrUrl);
            return [];
        }
    }

    private static string? GetRadarrImageUrl(string baseUrl, RadarrMovie movie)
    {
        var poster = movie.Images?.FirstOrDefault(i =>
            string.Equals(i.CoverType, "poster", StringComparison.OrdinalIgnoreCase));
        if (poster?.RemoteUrl is not null)
        {
            return poster.RemoteUrl;
        }

        if (poster?.Url is not null)
        {
            return $"{baseUrl.TrimEnd('/')}{poster.Url}";
        }

        return null;
    }

    private static string? GetSonarrImageUrl(string baseUrl, SonarrEpisode episode)
    {
        var poster = episode.Series?.Images?.FirstOrDefault(i =>
            string.Equals(i.CoverType, "poster", StringComparison.OrdinalIgnoreCase));
        if (poster?.RemoteUrl is not null)
        {
            return poster.RemoteUrl;
        }

        if (poster?.Url is not null)
        {
            return $"{baseUrl.TrimEnd('/')}{poster.Url}";
        }

        return null;
    }

    // Internal models for deserializing Radarr/Sonarr API responses.
    // These are intentionally kept internal — only ReleaseCalendarItem is public.

    internal sealed class RadarrMovie
    {
        public string? Title { get; set; }

        public string? SortTitle { get; set; }

        public string? Overview { get; set; }

        public int Year { get; set; }

        public DateTime? InCinemas { get; set; }

        public DateTime? PhysicalRelease { get; set; }

        public DateTime? DigitalRelease { get; set; }

        public string? Status { get; set; }

        public bool HasFile { get; set; }

        public List<ArrImage>? Images { get; set; }
    }

    internal sealed class SonarrEpisode
    {
        public string? Title { get; set; }

        public string? Overview { get; set; }

        public int SeasonNumber { get; set; }

        public int EpisodeNumber { get; set; }

        public DateTime? AirDateUtc { get; set; }

        public bool HasFile { get; set; }

        public SonarrSeries? Series { get; set; }
    }

    internal sealed class SonarrSeries
    {
        public string? Title { get; set; }

        public string? SortTitle { get; set; }

        public int Year { get; set; }

        public List<ArrImage>? Images { get; set; }
    }

    internal sealed class ArrImage
    {
        public string? CoverType { get; set; }

        public string? Url { get; set; }

        public string? RemoteUrl { get; set; }
    }
}
