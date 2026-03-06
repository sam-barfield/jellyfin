namespace MediaBrowser.Model.Announcements;

/// <summary>
/// Represents unread announcement state for a user.
/// </summary>
public class AnnouncementUnreadStatus
{
    /// <summary>
    /// Gets or sets the unread announcement count.
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has unread announcements.
    /// </summary>
    public bool HasUnread { get; set; }
}
