using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Friends;

namespace MediaBrowser.Controller.Friends;

/// <summary>
/// Provides friend management functionality.
/// </summary>
public interface IFriendService
{
    /// <summary>
    /// Sends a friend request from <paramref name="requesterId"/> to the user identified by <paramref name="addresseeUsername"/>.
    /// </summary>
    /// <param name="requesterId">The id of the user sending the request.</param>
    /// <param name="addresseeUsername">The username of the intended recipient.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created <see cref="FriendRequestDto"/>.</returns>
    Task<FriendRequestDto> SendFriendRequestAsync(Guid requesterId, string addresseeUsername, CancellationToken cancellationToken);

    /// <summary>
    /// Accepts the incoming friend request identified by <paramref name="requestId"/>.
    /// </summary>
    /// <param name="requestId">The id of the friend request to accept.</param>
    /// <param name="addresseeId">The id of the user accepting the request; used to verify ownership.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AcceptFriendRequestAsync(Guid requestId, Guid addresseeId, CancellationToken cancellationToken);

    /// <summary>
    /// Declines or cancels a friend request.
    /// The caller may be either the requester (cancelling) or the addressee (declining).
    /// </summary>
    /// <param name="requestId">The id of the friend request.</param>
    /// <param name="userId">The id of the user performing the action.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task DeclineFriendRequestAsync(Guid requestId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Removes an existing friendship between <paramref name="userId"/> and <paramref name="friendId"/>.
    /// </summary>
    /// <param name="userId">The id of the user removing the friend.</param>
    /// <param name="friendId">The id of the friend to remove.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task RemoveFriendAsync(Guid userId, Guid friendId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all accepted friends for the specified user.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="FriendDto"/> entries.</returns>
    Task<IReadOnlyList<FriendDto>> GetFriendsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all pending incoming friend requests for the specified user.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="FriendRequestDto"/> entries.</returns>
    Task<IReadOnlyList<FriendRequestDto>> GetPendingRequestsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the home-page widget data for the specified user, including friends sorted by
    /// online status and a count of pending incoming requests.
    /// </summary>
    /// <param name="userId">The user's id.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="FriendWidgetDto"/>.</returns>
    Task<FriendWidgetDto> GetFriendWidgetAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Directly creates an accepted friendship between two users, bypassing the request flow.
    /// Intended for administrator use only.
    /// </summary>
    /// <param name="userId">The id of the first user.</param>
    /// <param name="friendId">The id of the second user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task AddFriendshipAsync(Guid userId, Guid friendId, CancellationToken cancellationToken);
}
