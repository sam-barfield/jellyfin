using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Notifications;

/// <summary>
/// Provides user-specific notification management backed by the Jellyfin database.
/// </summary>
public class UserNotificationService(
    IDbContextFactory<JellyfinDbContext> dbProvider,
    ILogger<UserNotificationService> logger) : IUserNotificationService
{
    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;
    private readonly ILogger<UserNotificationService> _logger = logger;

    /// <inheritdoc />
    public event EventHandler<UserNotificationCreatedEventArgs>? NotificationCreated;

    /// <inheritdoc />
    public async Task<UserNotificationDto> CreateNotificationAsync(Guid userId, Jellyfin.Database.Implementations.Enums.NotificationType type, string? data, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var notification = new UserNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            Data = data
        };

        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Created notification {Type} for user {UserId}", type, userId);

        var dto = ToDto(notification);

        NotificationCreated?.Invoke(this, new UserNotificationCreatedEventArgs { UserId = userId });

        return dto;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserNotificationDto>> GetNotificationsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var notifications = await dbContext.UserNotifications
            .Where(n => n.UserId.Equals(userId))
            .OrderBy(n => n.IsRead)
            .ThenByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. notifications.Select(ToDto)];
    }

    /// <inheritdoc />
    public async Task<UserNotificationUnreadStatus> GetUnreadStatusAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var unreadCount = await dbContext.UserNotifications
            .CountAsync(n => n.UserId.Equals(userId) && !n.IsRead, cancellationToken)
            .ConfigureAwait(false);

        return new UserNotificationUnreadStatus
        {
            UnreadCount = unreadCount,
            HasUnread = unreadCount > 0
        };
    }

    /// <inheritdoc />
    public async Task MarkAsReadAsync(Guid notificationId, Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var notification = await dbContext.UserNotifications
            .FirstOrDefaultAsync(n => n.Id.Equals(notificationId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Notification not found.");

        if (!notification.UserId.Equals(userId))
        {
            throw new UnauthorizedAccessException("This notification does not belong to you.");
        }

        if (notification.IsRead)
        {
            return;
        }

        notification.IsRead = true;
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        await dbContext.UserNotifications
            .Where(n => n.UserId.Equals(userId) && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken)
            .ConfigureAwait(false);
    }

    private static UserNotificationDto ToDto(UserNotification notification) => new()
    {
        Id = notification.Id,
        Type = notification.Type,
        IsRead = notification.IsRead,
        CreatedAt = notification.CreatedAt,
        Data = notification.Data
    };
}
