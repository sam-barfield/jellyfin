using System;

namespace MediaBrowser.Model.Friends;

/// <summary>
/// Represents a pending friend request.
/// </summary>
public class FriendRequestDto
{
    /// <summary>
    /// Gets or sets the id of the friend request.
    /// </summary>
    public Guid RequestId { get; set; }

    /// <summary>
    /// Gets or sets the id of the user who sent the request.
    /// </summary>
    public Guid RequesterId { get; set; }

    /// <summary>
    /// Gets or sets the username of the user who sent the request.
    /// </summary>
    public string RequesterName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the id of the user receiving the request.
    /// </summary>
    public Guid AddresseeId { get; set; }

    /// <summary>
    /// Gets or sets the username of the user receiving the request.
    /// </summary>
    public string AddresseeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time the request was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
