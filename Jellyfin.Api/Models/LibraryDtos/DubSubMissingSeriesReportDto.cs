using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Report payload for one series.
/// </summary>
public class DubSubMissingSeriesReportDto
{
    /// <summary>
    /// Gets or sets the series id.
    /// </summary>
    public Guid SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the series name.
    /// </summary>
    public string SeriesName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the rolled-up dub availability.
    /// </summary>
    public DubAvailability? DubAvailable { get; set; }

    /// <summary>
    /// Gets or sets the rolled-up subtitle availability.
    /// </summary>
    public DubAvailability? SubAvailable { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing dub in this series.
    /// </summary>
    public int MissingDubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing subtitles in this series.
    /// </summary>
    public int MissingSubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the season rows in this report.
    /// </summary>
    public IReadOnlyList<DubSubMissingSeasonReportDto> Seasons { get; set; } = Array.Empty<DubSubMissingSeasonReportDto>();
}
