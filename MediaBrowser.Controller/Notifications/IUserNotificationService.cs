using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Notifications;

namespace MediaBrowser.Controller.Notifications;

/// <summary>
/// Provides user-specific notification management.
/// </summary>
public interface IUserNotificationService
{
    /// <summary>
    /// Raised when a new notification is created, enabling real-time WebSocket delivery.
    /// </summary>
    event EventHandler<UserNotificationCreatedEventArgs> NotificationCreated;

    /// <summary>
    /// Creates and persists a new notification for the specified user.
    /// </summary>
    /// <param name="userId">The recipient user id.</param>
    /// <param name="type">The notification type.</param>
    /// <param name="data">Optional JSON payload with type-specific context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created <see cref="UserNotificationDto"/>.</returns>
    Task<UserNotificationDto> CreateNotificationAsync(Guid userId, Jellyfin.Database.Implementations.Enums.NotificationType type, string? data, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all notifications for the specified user, unread first then by most recent.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="UserNotificationDto"/>.</returns>
    Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the unread notification count for the specified user.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The unread status.</returns>
    Task<UserNotificationUnreadStatus> GetUnreadStatusAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks a single notification as read.
    /// </summary>
    /// <param name="notificationId">The notification id.</param>
    /// <param name="userId">The user's id; used to verify ownership.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Marks all notifications for the specified user as read.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken);
}
