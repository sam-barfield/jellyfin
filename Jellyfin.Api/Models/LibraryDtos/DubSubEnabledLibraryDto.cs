using System;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Summary for a dub/sub-enabled library.
/// </summary>
public class DubSubEnabledLibraryDto
{
    /// <summary>
    /// Gets or sets the library id.
    /// </summary>
    public Guid LibraryId { get; set; }

    /// <summary>
    /// Gets or sets the library name.
    /// </summary>
    public string LibraryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the count of episodes missing dub.
    /// </summary>
    public int MissingDubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing subtitles.
    /// </summary>
    public int MissingSubEpisodeCount { get; set; }
}
