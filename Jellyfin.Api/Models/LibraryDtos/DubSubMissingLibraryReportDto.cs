using System;
using System.Collections.Generic;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Report payload for one library.
/// </summary>
public class DubSubMissingLibraryReportDto
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
    /// Gets or sets the count of episodes missing dub in the library.
    /// </summary>
    public int MissingDubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing subtitles in the library.
    /// </summary>
    public int MissingSubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the series rows in this report.
    /// </summary>
    public IReadOnlyList<DubSubMissingSeriesReportDto> Series { get; set; } = Array.Empty<DubSubMissingSeriesReportDto>();
}
