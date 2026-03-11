using System;

namespace MediaBrowser.Model.Notifications;

/// <summary>
/// Represents a notification for a specific user.
/// </summary>
public class UserNotificationDto
{
    /// <summary>
    /// Gets or sets the notification id.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public Jellyfin.Database.Implementations.Enums.NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the date and time the notification was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the optional JSON payload providing type-specific context.
    /// Deserialize based on <see cref="Type"/>.
    /// </summary>
    public string? Data { get; set; }
}
