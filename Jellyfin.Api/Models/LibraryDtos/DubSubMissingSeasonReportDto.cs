using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Report payload for one season.
/// </summary>
public class DubSubMissingSeasonReportDto
{
    /// <summary>
    /// Gets or sets the season id.
    /// </summary>
    public Guid SeasonId { get; set; }

    /// <summary>
    /// Gets or sets the season name.
    /// </summary>
    public string SeasonName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the rolled-up dub availability.
    /// </summary>
    public DubAvailability? DubAvailable { get; set; }

    /// <summary>
    /// Gets or sets the rolled-up subtitle availability.
    /// </summary>
    public DubAvailability? SubAvailable { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing dub in this season.
    /// </summary>
    public int MissingDubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the count of episodes missing subtitles in this season.
    /// </summary>
    public int MissingSubEpisodeCount { get; set; }

    /// <summary>
    /// Gets or sets the missing episode rows in this season.
    /// </summary>
    public IReadOnlyList<DubSubMissingEpisodeReportDto> Episodes { get; set; } = Array.Empty<DubSubMissingEpisodeReportDto>();
}
