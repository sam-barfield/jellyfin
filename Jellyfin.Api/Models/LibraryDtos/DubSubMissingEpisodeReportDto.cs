using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Api.Models.LibraryDtos;

/// <summary>
/// Report payload for one episode.
/// </summary>
public class DubSubMissingEpisodeReportDto
{
    /// <summary>
    /// Gets or sets the episode id.
    /// </summary>
    public Guid EpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the episode name.
    /// </summary>
    public string EpisodeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the season number.
    /// </summary>
    public int? SeasonNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode number.
    /// </summary>
    public int? EpisodeNumber { get; set; }

    /// <summary>
    /// Gets or sets the episode dub availability.
    /// </summary>
    public DubAvailability? DubAvailable { get; set; }

    /// <summary>
    /// Gets or sets the episode subtitle availability.
    /// </summary>
    public DubAvailability? SubAvailable { get; set; }
}
