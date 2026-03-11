namespace MediaBrowser.Model.Notifications;

/// <summary>
/// Represents the unread notification status for the current user.
/// </summary>
public class UserNotificationUnreadStatus
{
    /// <summary>
    /// Gets or sets the number of unread notifications.
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has any unread notifications.
    /// </summary>
    public bool HasUnread { get; set; }
}
