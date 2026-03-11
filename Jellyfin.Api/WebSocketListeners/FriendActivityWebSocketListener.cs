using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Friends;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Friends;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.WebSocketListeners;

/// <summary>
/// Pushes friend activity updates (online status, now-playing, hours watched) over WebSocket.
/// Each connected client only receives data for their own friends.
/// </summary>
public class FriendActivityWebSocketListener : BasePeriodicWebSocketListener<FriendWidgetDto, WebSocketListenerState>
{
    private readonly IFriendService _friendService;
    private readonly ISessionManager _sessionManager;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="FriendActivityWebSocketListener"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{FriendActivityWebSocketListener}"/> interface.</param>
    /// <param name="friendService">Instance of the <see cref="IFriendService"/> interface.</param>
    /// <param name="sessionManager">Instance of the <see cref="ISessionManager"/> interface.</param>
    public FriendActivityWebSocketListener(
        ILogger<FriendActivityWebSocketListener> logger,
        IFriendService friendService,
        ISessionManager sessionManager)
        : base(logger)
    {
        _friendService = friendService;
        _sessionManager = sessionManager;

        _sessionManager.SessionStarted += OnSessionEvent;
        _sessionManager.SessionEnded += OnSessionEvent;
        _sessionManager.PlaybackStart += OnPlaybackEvent;
        _sessionManager.PlaybackProgress += OnPlaybackProgressEvent;
        _sessionManager.PlaybackStopped += OnPlaybackStoppedEvent;
    }

    /// <inheritdoc />
    protected override SessionMessageType Type => SessionMessageType.FriendActivity;

    /// <inheritdoc />
    protected override SessionMessageType StartType => SessionMessageType.FriendActivityStart;

    /// <inheritdoc />
    protected override SessionMessageType StopType => SessionMessageType.FriendActivityStop;

    /// <inheritdoc />
    protected override Task<FriendWidgetDto> GetDataToSend()
    {
        // Not used — GetDataToSendForConnection is always called instead.
        return Task.FromResult(new FriendWidgetDto());
    }

    /// <inheritdoc />
    protected override async Task<FriendWidgetDto> GetDataToSendForConnection(IWebSocketConnection connection)
    {
        var user = connection.AuthorizationInfo?.User;
        if (user is null)
        {
            return new FriendWidgetDto();
        }

        return await _friendService.GetFriendWidgetAsync(user.Id, CancellationToken.None).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void Start(WebSocketMessageInfo message)
    {
        if (message.Connection.AuthorizationInfo.User is null && !message.Connection.AuthorizationInfo.IsApiKey)
        {
            throw new AuthenticationException("User must be authenticated to subscribe to friend activity.");
        }

        base.Start(message);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_disposed)
        {
            _sessionManager.SessionStarted -= OnSessionEvent;
            _sessionManager.SessionEnded -= OnSessionEvent;
            _sessionManager.PlaybackStart -= OnPlaybackEvent;
            _sessionManager.PlaybackProgress -= OnPlaybackProgressEvent;
            _sessionManager.PlaybackStopped -= OnPlaybackStoppedEvent;
            _disposed = true;
        }

        await base.DisposeAsyncCore().ConfigureAwait(false);
    }

    private void OnSessionEvent(object? sender, SessionEventArgs e)
    {
        SendData(true);
    }

    private void OnPlaybackEvent(object? sender, PlaybackProgressEventArgs e)
    {
        SendData(true);
    }

    private void OnPlaybackProgressEvent(object? sender, PlaybackProgressEventArgs e)
    {
        SendData(!e.IsAutomated);
    }

    private void OnPlaybackStoppedEvent(object? sender, PlaybackStopEventArgs e)
    {
        SendData(true);
    }
}
