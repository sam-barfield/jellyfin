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
using Jellyfin.Data.Enums;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Trending;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Trending;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Trending;

/// <summary>
/// Provides access to trending movies and series from TMDB.
/// </summary>
public class TrendingService : ITrendingService
{
    private const string CacheKeyPrefix = "Trending_";
    private const string TMDBApiUrl = "https://api.themoviedb.org/3/";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IMemoryCache _memoryCache;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<TrendingService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrendingService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/>.</param>
    /// <param name="configurationManager">The <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="memoryCache">The <see cref="IMemoryCache"/>.</param>
    /// <param name="libraryManager">The <see cref="ILibraryManager"/>.</param>
    /// <param name="logger">The logger.</param>
    public TrendingService(
        IHttpClientFactory httpClientFactory,
        IServerConfigurationManager configurationManager,
        IMemoryCache memoryCache,
        ILibraryManager libraryManager,
        ILogger<TrendingService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configurationManager = configurationManager;
        _memoryCache = memoryCache;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrendingItem>> GetTrendingMoviesAsync(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}Movies";
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<TrendingItem>? cached) && cached is not null)
        {
            return cached;
        }

        var config = _configurationManager.Configuration.Trending;
        var items = await FetchTrendingMoviesAsync(config, cancellationToken).ConfigureAwait(false);

        var ttl = config.CacheTimeToLiveMinutes > 0
            ? TimeSpan.FromMinutes(config.CacheTimeToLiveMinutes)
            : TimeSpan.FromMinutes(30);

        _memoryCache.Set(cacheKey, items, ttl);

        return items;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TrendingItem>> GetTrendingShowsAsync(CancellationToken cancellationToken)
    {
        var cacheKey = $"{CacheKeyPrefix}Shows";
        if (_memoryCache.TryGetValue(cacheKey, out IReadOnlyList<TrendingItem>? cached) && cached is not null)
        {
            return cached;
        }

        var config = _configurationManager.Configuration.Trending;
        var items = await FetchTrendingShowsAsync(config, cancellationToken).ConfigureAwait(false);

        var ttl = config.CacheTimeToLiveMinutes > 0
            ? TimeSpan.FromMinutes(config.CacheTimeToLiveMinutes)
            : TimeSpan.FromMinutes(30);

        _memoryCache.Set(cacheKey, items, ttl);

        return items;
    }

    /// <inheritdoc />
    public void InvalidateCache()
    {
        _memoryCache.Remove($"{CacheKeyPrefix}Movies");
        _memoryCache.Remove($"{CacheKeyPrefix}Shows");
        _logger.LogInformation("Trending cache invalidated");
    }

    private async Task<IReadOnlyList<TrendingItem>> FetchTrendingMoviesAsync(TrendingConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TMDBAccessToken))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{TMDBApiUrl}trending/movie/week?page=";
            List<TMDBMovie> tmdbMovies = [];

            for (int i = 1; i <= 5; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url + i.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("User-Agent", "Jellyfin");
                request.Headers.Add("Authorization", "Bearer " + config.TMDBAccessToken);

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<TMDBTrendingMovieResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (jsonResponse is not null)
                {
                    tmdbMovies.AddRange(jsonResponse.Results ?? []);
                }
            }

            if (tmdbMovies is null)
            {
                return [];
            }

            var results = new List<TrendingItem>();
            int rank = 1;
            foreach (var movie in tmdbMovies)
            {
                if (rank > 10)
                {
                    break;
                }

                var localItem = FindLocalItem(movie.Id, new[] { BaseItemKind.Movie });
                if (localItem is not null)
                {
                    results.Add(new TrendingItem
                    {
                        Rank = rank,
                        Title = localItem.Name ?? movie.Title ?? string.Empty,
                        Overview = localItem.Overview ?? movie.Overview ?? string.Empty,
                        Year = localItem.ProductionYear ?? DateTime.ParseExact(movie.Release_Date ?? "1970-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture).Year,
                        TmdbId = movie.Id.ToString(CultureInfo.InvariantCulture),
                        JellyfinItemId = localItem.Id
                    });
                    rank++;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch trending movies from TMDB");
            return [];
        }
    }

    private async Task<IReadOnlyList<TrendingItem>> FetchTrendingShowsAsync(TrendingConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TMDBAccessToken))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{TMDBApiUrl}trending/tv/week?page=";
            List<TMDBShow> tmdbShows = [];

            for (var i = 1; i <= 5; i++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url + i.ToString(CultureInfo.InvariantCulture));
                request.Headers.Add("accept", "application/json");
                request.Headers.Add("User-Agent", "Jellyfin");
                request.Headers.Add("Authorization", "Bearer " + config.TMDBAccessToken);

                using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadFromJsonAsync<TMDBTrendingShowResponse>(_jsonOptions, cancellationToken).ConfigureAwait(false);
                if (jsonResponse is not null)
                {
                    tmdbShows.AddRange(jsonResponse.Results ?? []);
                }
            }

            if (tmdbShows.Count == 0)
            {
                return [];
            }

            var results = new List<TrendingItem>();
            int rank = 1;
            foreach (var show in tmdbShows)
            {
                if (rank > 10)
                {
                    break;
                }

                var localItem = FindLocalItem(show.Id, new[] { BaseItemKind.Series });
                if (localItem is not null)
                {
                    results.Add(new TrendingItem
                    {
                        Rank = rank,
                        Title = localItem.Name ?? show.Title ?? string.Empty,
                        Overview = localItem.Overview ?? show.Overview ?? string.Empty,
                        Year = localItem.ProductionYear ?? DateTime.ParseExact(show.Release_Date ?? "1970-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture).Year,
                        TmdbId = show.Id.ToString(CultureInfo.InvariantCulture),
                        JellyfinItemId = localItem.Id
                    });
                    rank++;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch trending shows from TMDB");
            return [];
        }
    }

    private BaseItem? FindLocalItem(int id, BaseItemKind[] itemTypes)
    {
        var providerIds = new Dictionary<string, string>();
        providerIds["Tmdb"] = id.ToString(CultureInfo.InvariantCulture);

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = itemTypes,
            HasAnyProviderId = providerIds,
            Limit = 1
        };

        var items = _libraryManager.GetItemList(query);
        return items.FirstOrDefault();
    }

    internal sealed class TMDBTrendingMovieResponse
    {
        public int Page { get; set; }

        public List<TMDBMovie>? Results { get; set; }
    }

    internal sealed class TMDBTrendingShowResponse
    {
        public int Page { get; set; }

        public List<TMDBShow>? Results { get; set; }
    }

    internal sealed class TMDBMovie
    {
        public string? Title { get; set; }

        public string? Release_Date { get; set; }

        public string? Overview { get; set; }

        public int Id { get; set; }
    }

    internal sealed class TMDBShow
    {
        public string? Title { get; set; }

        public string? Release_Date { get; set; }

        public string? Overview { get; set; }

        public int Id { get; set; }
    }
}
