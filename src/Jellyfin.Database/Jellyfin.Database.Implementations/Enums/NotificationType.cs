namespace Jellyfin.Database.Implementations.Enums;

/// <summary>
/// The type of a user notification.
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// A friend request was accepted; both users receive this notification.
    /// </summary>
    FriendRequestAccepted = 0,

    /// <summary>
    /// A friend request was received from another user.
    /// </summary>
    FriendRequestReceived = 1,

    /// <summary>
    /// A friend removed the current user from their friend list.
    /// </summary>
    FriendRemoved = 2
}
