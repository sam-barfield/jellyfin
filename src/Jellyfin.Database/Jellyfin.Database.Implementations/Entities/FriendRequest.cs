using System;
using Jellyfin.Database.Implementations.Enums;

namespace Jellyfin.Database.Implementations.Entities;

/// <summary>
/// An entity representing a friend request between two users.
/// A row with <see cref="FriendRequestStatus.Accepted"/> also serves as the friendship record.
/// </summary>
public class FriendRequest
{
    /// <summary>
    /// Gets or sets the unique identifier for this friend request.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who sent the request.
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who received the request.
    /// </summary>
    public Guid AddresseeId { get; set; }

    /// <summary>
    /// Gets or sets the current status of the friend request.
    /// </summary>
    public FriendRequestStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the date and time the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time the request status was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the user who sent the request.
    /// </summary>
    public User? Requester { get; set; }

    /// <summary>
    /// Gets or sets the user who received the request.
    /// </summary>
    public User? Addressee { get; set; }
}
