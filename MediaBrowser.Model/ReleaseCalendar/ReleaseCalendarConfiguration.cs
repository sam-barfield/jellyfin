namespace MediaBrowser.Model.ReleaseCalendar;

/// <summary>
/// Configuration for the Radarr/Sonarr release calendar integration.
/// </summary>
public class ReleaseCalendarConfiguration
{
    /// <summary>
    /// Gets or sets the base URL of the Radarr instance (e.g. http://localhost:7878).
    /// </summary>
    public string RadarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key used to authenticate with Radarr.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the base URL of the Sonarr instance (e.g. http://localhost:8989).
    /// </summary>
    public string SonarrUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the API key used to authenticate with Sonarr.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time-to-live in minutes for the in-memory calendar cache.
    /// </summary>
    public int CacheTimeToLiveMinutes { get; set; } = 30;
}
