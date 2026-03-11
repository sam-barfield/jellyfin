using System;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// A notification addressed to a specific user.
/// </summary>
public class UserNotification
{
    /// <summary>
    /// Gets or sets the unique identifier for this notification.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who receives this notification.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the notification type.
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user has read this notification.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets the date and time the notification was created (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets optional JSON payload providing type-specific context.
    /// The shape depends on <see cref="Type"/>; clients should deserialize based on that value.
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// Gets or sets the user who owns this notification.
    /// </summary>
    public User? User { get; set; }
}
