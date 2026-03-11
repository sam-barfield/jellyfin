namespace MediaBrowser.Model.Friends;

/// <summary>
/// Data for the friends home-page widget.
/// Contains the friend list ordered by online status and a count of pending requests.
/// </summary>
public class FriendWidgetDto
{
    /// <summary>
    /// Gets or sets the list of friends, sorted so online friends appear first.
    /// </summary>
    public FriendDto[] Friends { get; set; } = [];

    /// <summary>
    /// Gets or sets the number of incoming friend requests that are still pending.
    /// </summary>
    public int PendingRequestCount { get; set; }
}
