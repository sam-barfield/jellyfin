using System;

namespace MediaBrowser.Controller.Notifications;

/// <summary>
/// Event arguments raised when a new user notification is created.
/// </summary>
public class UserNotificationCreatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the id of the user who received the notification.
    /// </summary>
    public Guid UserId { get; set; }
}
