namespace MediaBrowser.Model.Trending;

/// <summary>
/// Configuration for the TMDB trending integration.
/// </summary>
public class TrendingConfiguration
{
    /// <summary>
    /// Gets or sets the API access token used to authenticate with TMDB.
    /// </summary>
    public string TMDBAccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time-to-live in minutes for the in-memory trending cache.
    /// </summary>
    public int CacheTimeToLiveMinutes { get; set; } = 30;
}
