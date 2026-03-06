using System;

namespace MediaBrowser.Model.ReleaseCalendar;

/// <summary>
/// Represents a single item in the unified release calendar.
/// </summary>
public class ReleaseCalendarItem
{
    /// <summary>
    /// Gets or sets the title of the movie or episode.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the sort title.
    /// </summary>
    public string SortTitle { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the overview or description.
    /// </summary>
    public string Overview { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC air or release date.
    /// </summary>
    public DateTime AirDateUtc { get; set; }

    /// <summary>
    /// Gets or sets the type of calendar item.
    /// </summary>
    public ReleaseCalendarType Type { get; set; }

    /// <summary>
    /// Gets or sets the release year.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the series title. Only populated for episodes.
    /// </summary>
    public string? SeriesTitle { get; set; }

    /// <summary>
    /// Gets or sets the season number. Only populated for episodes.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number. Only populated for episodes.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the poster or thumbnail image URL.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the release status (e.g. "announced", "inCinemas", "released", "unaired", "aired").
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the media file has been downloaded.
    /// </summary>
    public bool HasFile { get; set; }
}
