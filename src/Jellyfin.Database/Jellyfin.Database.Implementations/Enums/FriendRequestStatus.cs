namespace Jellyfin.Database.Implementations.Enums;

/// <summary>
/// The status of a friend request.
/// </summary>
public enum FriendRequestStatus
{
    /// <summary>
    /// The request has been sent and is awaiting a response.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The request has been accepted; the two users are now friends.
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// The request was declined by the addressee.
    /// </summary>
    Declined = 2,

    /// <summary>
    /// The addressee has blocked the requester.
    /// </summary>
    Blocked = 3
}
