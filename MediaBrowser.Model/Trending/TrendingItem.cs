using System;

namespace MediaBrowser.Model.Trending;

/// <summary>
/// A trending item from Trakt, mapped to a local Jellyfin item if available.
/// </summary>
public class TrendingItem
{
    /// <summary>
    /// Gets or sets the Trakt ranking.
    /// </summary>
    public int Rank { get; set; }

    /// <summary>
    /// Gets or sets the title of the item.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overview/description of the item.
    /// </summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the year the item was released.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the TMDB identifier.
    /// </summary>
    public string? TmdbId { get; set; }

    /// <summary>
    /// Gets or sets the IMDB identifier.
    /// </summary>
    public string? ImdbId { get; set; }

    /// <summary>
    /// Gets or sets the internal base item id if present on the server.
    /// </summary>
    public Guid? JellyfinItemId { get; set; }
}
