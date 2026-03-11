using System;

namespace MediaBrowser.Model.Friends;

/// <summary>
/// Represents a friend and their current activity.
/// </summary>
public class FriendDto
{
    /// <summary>
    /// Gets or sets the user id of the friend.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the username of the friend.
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time the friend was last online, or <c>null</c> if never.
    /// </summary>
    public DateTime? LastOnline { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the friend currently has an active session.
    /// </summary>
    public bool IsOnline { get; set; }

    /// <summary>
    /// Gets or sets what the friend is currently watching, or <c>null</c> if not playing anything.
    /// </summary>
    public NowPlayingFriendDto? NowPlaying { get; set; }

    /// <summary>
    /// Gets or sets the total hours of media watched in the last 28 days.
    /// </summary>
    public double HoursWatchedLastMonth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the friend has a profile image set.
    /// When <c>true</c>, the image can be fetched from <c>/Users/{UserId}/Images/Primary</c>.
    /// </summary>
    public bool HasProfileImage { get; set; }
}
