using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Friends;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Friends;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Friends;

/// <summary>
/// Provides friend management functionality backed by the Jellyfin database.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FriendService"/> class.
/// </remarks>
/// <param name="dbProvider">The database context factory.</param>
/// <param name="userManager">The user manager.</param>
/// <param name="sessionManager">The session manager.</param>
/// <param name="notificationService">The user notification service.</param>
/// <param name="logger">The logger.</param>
public class FriendService(
    IDbContextFactory<JellyfinDbContext> dbProvider,
    IUserManager userManager,
    ISessionManager sessionManager,
    IUserNotificationService notificationService,
    ILogger<FriendService> logger) : IFriendService
{
    private const long TicksPerHour = 36_000_000_000L;

    private readonly IDbContextFactory<JellyfinDbContext> _dbProvider = dbProvider;
    private readonly IUserManager _userManager = userManager;
    private readonly ISessionManager _sessionManager = sessionManager;
    private readonly IUserNotificationService _notificationService = notificationService;
    private readonly ILogger<FriendService> _logger = logger;

    /// <inheritdoc />
    public async Task<FriendRequestDto> SendFriendRequestAsync(Guid requesterId, string addresseeUsername, CancellationToken cancellationToken)
    {
        var addressee = _userManager.GetUserByName(addresseeUsername)
            ?? throw new InvalidOperationException($"User '{addresseeUsername}' not found.");

        if (addressee.Id.Equals(requesterId))
        {
            throw new InvalidOperationException("You cannot send a friend request to yourself.");
        }

        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        // Check if any relationship already exists between these two users (in either direction)
        var addresseeId = addressee.Id;
        var existing = await dbContext.FriendRequests
            .FirstOrDefaultAsync(
                fr => (fr.RequesterId.Equals(requesterId) && fr.AddresseeId.Equals(addresseeId))
                   || (fr.RequesterId.Equals(addresseeId) && fr.AddresseeId.Equals(requesterId)),
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            throw new InvalidOperationException("A friend request or friendship already exists with this user.");
        }

        var requester = _userManager.GetUserById(requesterId)
            ?? throw new InvalidOperationException("Requesting user not found.");

        var now = DateTime.UtcNow;
        var request = new FriendRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = requesterId,
            AddresseeId = addresseeId,
            Status = FriendRequestStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.FriendRequests.Add(request);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {RequesterId} sent a friend request to {AddresseeId}", requesterId, addresseeId);

        var requestData = System.Text.Json.JsonSerializer.Serialize(new { userId = requesterId, username = requester.Username });
        await _notificationService.CreateNotificationAsync(addresseeId, Jellyfin.Database.Implementations.Enums.NotificationType.FriendRequestReceived, requestData, cancellationToken).ConfigureAwait(false);

        return new FriendRequestDto
        {
            RequestId = request.Id,
            RequesterId = requesterId,
            RequesterName = requester.Username,
            CreatedAt = request.CreatedAt
        };
    }

    /// <inheritdoc />
    public async Task AcceptFriendRequestAsync(Guid requestId, Guid addresseeId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var request = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id.Equals(requestId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Friend request not found.");

        if (!request.AddresseeId.Equals(addresseeId))
        {
            throw new UnauthorizedAccessException("You are not the recipient of this friend request.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            throw new InvalidOperationException("This friend request is no longer pending.");
        }

        request.Status = FriendRequestStatus.Accepted;
        request.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {AddresseeId} accepted friend request {RequestId}", addresseeId, requestId);

        // Notify both parties that they are now friends.
        var requesterId = request.RequesterId;
        var requester = _userManager.GetUserById(requesterId);
        var addressee = _userManager.GetUserById(addresseeId);

        if (requester is not null && addressee is not null)
        {
            var requesterData = System.Text.Json.JsonSerializer.Serialize(new { userId = addresseeId, username = addressee.Username });
            var addresseeData = System.Text.Json.JsonSerializer.Serialize(new { userId = requesterId, username = requester.Username });

            await _notificationService.CreateNotificationAsync(requesterId, Jellyfin.Database.Implementations.Enums.NotificationType.FriendRequestAccepted, requesterData, cancellationToken).ConfigureAwait(false);
            await _notificationService.CreateNotificationAsync(addresseeId, Jellyfin.Database.Implementations.Enums.NotificationType.FriendRequestAccepted, addresseeData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task DeclineFriendRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var request = await dbContext.FriendRequests
            .FirstOrDefaultAsync(fr => fr.Id.Equals(requestId), cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Friend request not found.");

        if (!request.RequesterId.Equals(userId) && !request.AddresseeId.Equals(userId))
        {
            throw new UnauthorizedAccessException("You are not a party to this friend request.");
        }

        if (request.Status != FriendRequestStatus.Pending)
        {
            throw new InvalidOperationException("This friend request is no longer pending.");
        }

        dbContext.FriendRequests.Remove(request);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Friend request {RequestId} declined/cancelled by user {UserId}", requestId, userId);
    }

    /// <inheritdoc />
    public async Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var friendship = await dbContext.FriendRequests
            .FirstOrDefaultAsync(
                fr => fr.Status == FriendRequestStatus.Accepted
                   && ((fr.RequesterId.Equals(userId) && fr.AddresseeId.Equals(friendId))
                    || (fr.RequesterId.Equals(friendId) && fr.AddresseeId.Equals(userId))),
                cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("Friendship not found.");

        dbContext.FriendRequests.Remove(friendship);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("User {UserId} removed friend {FriendId}", userId, friendId);

        var remover = _userManager.GetUserById(userId);
        if (remover is not null)
        {
            var removeData = System.Text.Json.JsonSerializer.Serialize(new { userId, username = remover.Username });
            await _notificationService.CreateNotificationAsync(friendId, Jellyfin.Database.Implementations.Enums.NotificationType.FriendRemoved, removeData, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FriendDto>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var friendships = await dbContext.FriendRequests
            .Where(fr => fr.Status == FriendRequestStatus.Accepted
                      && (fr.RequesterId.Equals(userId) || fr.AddresseeId.Equals(userId)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var activeSessions = _sessionManager.Sessions.ToList();
        var results = new List<FriendDto>(friendships.Count);

        foreach (var friendship in friendships)
        {
            var friendId = friendship.RequesterId.Equals(userId) ? friendship.AddresseeId : friendship.RequesterId;
            var friendUser = _userManager.GetUserById(friendId);
            if (friendUser is null)
            {
                continue;
            }

            var friendDto = await BuildFriendDtoAsync(friendUser, activeSessions, dbContext, cancellationToken).ConfigureAwait(false);
            results.Add(friendDto);
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FriendRequestDto>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var requests = await dbContext.FriendRequests
            .Where(fr => fr.AddresseeId.Equals(userId) && fr.Status == FriendRequestStatus.Pending)
            .OrderBy(fr => fr.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var results = new List<FriendRequestDto>(requests.Count);
        foreach (var req in requests)
        {
            var requester = _userManager.GetUserById(req.RequesterId);
            results.Add(new FriendRequestDto
            {
                RequestId = req.Id,
                RequesterId = req.RequesterId,
                RequesterName = requester?.Username ?? string.Empty,
                CreatedAt = req.CreatedAt
            });
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<FriendWidgetDto> GetFriendWidgetAsync(Guid userId, CancellationToken cancellationToken)
    {
        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var friendships = await dbContext.FriendRequests
            .Where(fr => fr.Status == FriendRequestStatus.Accepted
                      && (fr.RequesterId.Equals(userId) || fr.AddresseeId.Equals(userId)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var pendingCount = await dbContext.FriendRequests
            .CountAsync(fr => fr.AddresseeId.Equals(userId) && fr.Status == FriendRequestStatus.Pending, cancellationToken)
            .ConfigureAwait(false);

        var activeSessions = _sessionManager.Sessions.ToList();
        var friends = new List<FriendDto>(friendships.Count);

        foreach (var friendship in friendships)
        {
            var friendId = friendship.RequesterId.Equals(userId) ? friendship.AddresseeId : friendship.RequesterId;
            var friendUser = _userManager.GetUserById(friendId);
            if (friendUser is null)
            {
                continue;
            }

            var friendDto = await BuildFriendDtoAsync(friendUser, activeSessions, dbContext, cancellationToken).ConfigureAwait(false);
            friends.Add(friendDto);
        }

        // Online friends first, then sorted by most recently online
        var sorted = friends
            .OrderByDescending(f => f.IsOnline)
            .ThenByDescending(f => f.LastOnline)
            .ToArray();

        return new FriendWidgetDto
        {
            Friends = sorted,
            PendingRequestCount = pendingCount
        };
    }

    /// <inheritdoc />
    public async Task AddFriendshipAsync(Guid userId, Guid friendId, CancellationToken cancellationToken)
    {
        if (userId.Equals(friendId))
        {
            throw new InvalidOperationException("A user cannot be friends with themselves.");
        }

        await using var dbContext = await _dbProvider.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var existing = await dbContext.FriendRequests
            .FirstOrDefaultAsync(
                fr => (fr.RequesterId.Equals(userId) && fr.AddresseeId.Equals(friendId))
                   || (fr.RequesterId.Equals(friendId) && fr.AddresseeId.Equals(userId)),
                cancellationToken)
            .ConfigureAwait(false);

        if (existing is not null)
        {
            if (existing.Status == FriendRequestStatus.Accepted)
            {
                throw new InvalidOperationException("These users are already friends.");
            }

            // Upgrade a pending request to accepted
            existing.Status = FriendRequestStatus.Accepted;
            existing.UpdatedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Admin upgraded pending request to accepted friendship between {UserId} and {FriendId}", userId, friendId);
            return;
        }

        var now = DateTime.UtcNow;
        var friendship = new FriendRequest
        {
            Id = Guid.NewGuid(),
            RequesterId = userId,
            AddresseeId = friendId,
            Status = FriendRequestStatus.Accepted,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.FriendRequests.Add(friendship);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Admin created friendship between {UserId} and {FriendId}", userId, friendId);
    }

    private async Task<FriendDto> BuildFriendDtoAsync(
        User friendUser,
        List<SessionInfo> activeSessions,
        JellyfinDbContext dbContext,
        CancellationToken cancellationToken)
    {
        // Find the most recent active session for this friend
        var session = activeSessions
            .Where(s => s.UserId.Equals(friendUser.Id))
            .OrderByDescending(s => s.LastActivityDate)
            .FirstOrDefault();

        NowPlayingFriendDto? nowPlaying = null;
        if (session?.NowPlayingItem is not null)
        {
            nowPlaying = new NowPlayingFriendDto
            {
                ItemId = session.NowPlayingItem.Id,
                Name = session.NowPlayingItem.Name ?? string.Empty,
                SeriesName = session.NowPlayingItem.SeriesName,
                SeriesId = session.NowPlayingItem.SeriesId,
                PositionTicks = session.PlayState?.PositionTicks,
                RunTimeTicks = session.NowPlayingItem.RunTimeTicks
            };
        }

        var hoursWatched = await GetHoursWatchedLastMonthAsync(friendUser.Id, dbContext, cancellationToken)
            .ConfigureAwait(false);

        return new FriendDto
        {
            UserId = friendUser.Id,
            Username = friendUser.Username,
            LastOnline = friendUser.LastActivityDate,
            IsOnline = session is not null,
            NowPlaying = nowPlaying,
            HoursWatchedLastMonth = hoursWatched,
            HasProfileImage = friendUser.ProfileImage is not null
        };
    }

    private static async Task<double> GetHoursWatchedLastMonthAsync(
        Guid userId,
        JellyfinDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var cutoff = DateTime.UtcNow.AddDays(-28);

        var totalTicks = await dbContext.UserData
            .Where(ud => ud.UserId.Equals(userId)
                      && ud.Played
                      && ud.LastPlayedDate >= cutoff
                      && ud.Item != null
                      && ud.Item.RunTimeTicks != null)
            .SumAsync(ud => ud.Item!.RunTimeTicks!.Value, cancellationToken)
            .ConfigureAwait(false);

        return totalTicks / (double)TicksPerHour;
    }
}
