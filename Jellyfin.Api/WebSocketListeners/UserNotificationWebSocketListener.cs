using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// Pushes user notification updates over WebSocket.
/// Each connected client only receives their own notifications.
/// </summary>
public class UserNotificationWebSocketListener : BasePeriodicWebSocketListener<IReadOnlyList<UserNotificationDto>, WebSocketListenerState>
{
    private readonly IUserNotificationService _notificationService;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserNotificationWebSocketListener"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{UserNotificationWebSocketListener}"/> interface.</param>
    /// <param name="notificationService">Instance of the <see cref="IUserNotificationService"/> interface.</param>
    public UserNotificationWebSocketListener(
        ILogger<UserNotificationWebSocketListener> logger,
        IUserNotificationService notificationService)
        : base(logger)
    {
        _notificationService = notificationService;
        _notificationService.NotificationCreated += OnNotificationCreated;
    }

    /// <inheritdoc />
    protected override SessionMessageType Type => SessionMessageType.UserNotification;

    /// <inheritdoc />
    protected override SessionMessageType StartType => SessionMessageType.UserNotificationStart;

    /// <inheritdoc />
    protected override SessionMessageType StopType => SessionMessageType.UserNotificationStop;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<UserNotificationDto>> GetDataToSend()
    {
        // Not used — GetDataToSendForConnection is always called instead.
        return Task.FromResult<IReadOnlyList<UserNotificationDto>>(Array.Empty<UserNotificationDto>());
    }

    /// <inheritdoc />
    protected override async Task<IReadOnlyList<UserNotificationDto>> GetDataToSendForConnection(IWebSocketConnection connection)
    {
        var user = connection.AuthorizationInfo?.User;
        if (user is null)
        {
            return Array.Empty<UserNotificationDto>();
        }

        return await _notificationService.GetNotificationsAsync(user.Id, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _notificationService.NotificationCreated -= OnNotificationCreated;
            _disposed = true;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private void OnNotificationCreated(object? sender, UserNotificationCreatedEventArgs e)
    {
        SendData(true);
    }
}
