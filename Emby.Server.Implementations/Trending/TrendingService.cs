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
/// Provides access to trending movies and series from Trakt.
/// </summary>
public class TrendingService : ITrendingService
{
    private const string CacheKeyPrefix = "Trending_";
    private const string TraktApiUrl = "https://api.trakt.tv";

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
        if (string.IsNullOrWhiteSpace(config.TraktClientId))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{TraktApiUrl}/movies/trending?limit=100";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("trakt-api-version", "2");
            request.Headers.Add("trakt-api-key", config.TraktClientId);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var traktMovies = await response.Content.ReadFromJsonAsync<List<TraktTrendingMovieResponse>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (traktMovies is null)
            {
                return [];
            }

            var results = new List<TrendingItem>();
            int rank = 1;
            foreach (var m in traktMovies)
            {
                if (rank > 10)
                {
                    break;
                }

                var movie = m.Movie;
                if (movie?.Ids is null)
                {
                    continue;
                }

                var localItem = FindLocalItem(movie.Ids, new[] { BaseItemKind.Movie });
                if (localItem is not null)
                {
                    results.Add(new TrendingItem
                    {
                        Rank = rank,
                        Title = localItem.Name ?? movie.Title ?? string.Empty,
                        Overview = localItem.Overview ?? movie.Overview ?? string.Empty,
                        Year = localItem.ProductionYear ?? movie.Year,
                        TmdbId = movie.Ids.Tmdb?.ToString(CultureInfo.InvariantCulture),
                        ImdbId = movie.Ids.Imdb,
                        JellyfinItemId = localItem.Id
                    });
                    rank++;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch trending movies from Trakt");
            return [];
        }
    }

    private async Task<IReadOnlyList<TrendingItem>> FetchTrendingShowsAsync(TrendingConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TraktClientId))
        {
            return [];
        }

        try
        {
            var client = _httpClientFactory.CreateClient(NamedClient.Default);
            var url = $"{TraktApiUrl}/shows/trending?limit=100";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("trakt-api-version", "2");
            request.Headers.Add("trakt-api-key", config.TraktClientId);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var traktShows = await response.Content.ReadFromJsonAsync<List<TraktTrendingShowResponse>>(_jsonOptions, cancellationToken).ConfigureAwait(false);
            if (traktShows is null)
            {
                return [];
            }

            var results = new List<TrendingItem>();
            int rank = 1;
            foreach (var s in traktShows)
            {
                if (rank > 10)
                {
                    break;
                }

                var show = s.Show;
                if (show?.Ids is null)
                {
                    continue;
                }

                var localItem = FindLocalItem(show.Ids, new[] { BaseItemKind.Series });
                if (localItem is not null)
                {
                    results.Add(new TrendingItem
                    {
                        Rank = rank,
                        Title = localItem.Name ?? show.Title ?? string.Empty,
                        Overview = localItem.Overview ?? show.Overview ?? string.Empty,
                        Year = localItem.ProductionYear ?? show.Year,
                        TmdbId = show.Ids.Tmdb?.ToString(CultureInfo.InvariantCulture),
                        ImdbId = show.Ids.Imdb,
                        JellyfinItemId = localItem.Id
                    });
                    rank++;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch trending shows from Trakt");
            return [];
        }
    }

    private BaseItem? FindLocalItem(TraktIds ids, BaseItemKind[] itemTypes)
    {
        var providerIds = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(ids.Imdb))
        {
            providerIds["Imdb"] = ids.Imdb;
        }

        if (ids.Tmdb.HasValue)
        {
            providerIds["Tmdb"] = ids.Tmdb.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (ids.Tvdb.HasValue)
        {
            providerIds["Tvdb"] = ids.Tvdb.Value.ToString(CultureInfo.InvariantCulture);
        }

        if (providerIds.Count == 0)
        {
            return null;
        }

        var query = new InternalItemsQuery
        {
            IncludeItemTypes = itemTypes,
            HasAnyProviderId = providerIds,
            Limit = 1
        };

        var items = _libraryManager.GetItemList(query);
        return items.FirstOrDefault();
    }

    internal sealed class TraktTrendingMovieResponse
    {
        public TraktMovie? Movie { get; set; }
    }

    internal sealed class TraktTrendingShowResponse
    {
        public TraktShow? Show { get; set; }
    }

    internal sealed class TraktMovie
    {
        public string? Title { get; set; }

        public int Year { get; set; }

        public string? Overview { get; set; }

        public TraktIds? Ids { get; set; }
    }

    internal sealed class TraktShow
    {
        public string? Title { get; set; }

        public int Year { get; set; }

        public string? Overview { get; set; }

        public TraktIds? Ids { get; set; }
    }

    internal sealed class TraktIds
    {
        public string? Imdb { get; set; }

        public int? Tmdb { get; set; }

        public int? Tvdb { get; set; }
    }
}
