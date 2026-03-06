namespace MediaBrowser.Model.Trending;

/// <summary>
/// Configuration for the Trakt trending integration.
/// </summary>
public class TrendingConfiguration
{
    /// <summary>
    /// Gets or sets the API key (Client ID) used to authenticate with Trakt.
    /// </summary>
    public string TraktClientId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time-to-live in minutes for the in-memory trending cache.
    /// </summary>
    public int CacheTimeToLiveMinutes { get; set; } = 30;
}
