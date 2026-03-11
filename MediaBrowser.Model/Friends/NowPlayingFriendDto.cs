using System;

namespace MediaBrowser.Model.Friends;

/// <summary>
/// Represents what a friend is currently watching.
/// </summary>
public class NowPlayingFriendDto
{
    /// <summary>
    /// Gets or sets the Jellyfin item id.
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Gets or sets the title of the item being watched.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the series name when the item is an episode.
    /// </summary>
    public string? SeriesName { get; set; }

    /// <summary>
    /// Gets or sets the series item id when the item is an episode.
    /// </summary>
    public Guid? SeriesId { get; set; }

    /// <summary>
    /// Gets or sets the current playback position in ticks.
    /// </summary>
    public long? PositionTicks { get; set; }

    /// <summary>
    /// Gets or sets the total runtime of the item in ticks.
    /// </summary>
    public long? RunTimeTicks { get; set; }
}
