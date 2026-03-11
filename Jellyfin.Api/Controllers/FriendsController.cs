using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Friends;
using MediaBrowser.Model.Friends;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Friend management controller.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FriendsController"/> class.
/// </remarks>
/// <param name="friendService">Instance of the <see cref="IFriendService"/>.</param>
/// <param name="logger">Instance of the <see cref="ILogger{FriendsController}"/>.</param>
[Route("Friends")]
[Authorize]
public class FriendsController(IFriendService friendService, ILogger<FriendsController> logger) : BaseJellyfinApiController
{
    private readonly IFriendService _friendService = friendService;
    private readonly ILogger<FriendsController> _logger = logger;

    /// <summary>
    /// Gets the current user's friend list with online status and activity.
    /// </summary>
    /// <response code="200">Friend list returned.</response>
    /// <returns>A list of friends.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> GetFriends()
    {
        var userId = User.GetUserId();
        var friends = await _friendService.GetFriendsAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(friends);
    }

    /// <summary>
    /// Gets all pending incoming friend requests for the current user.
    /// </summary>
    /// <response code="200">Pending requests returned.</response>
    /// <returns>A list of pending friend requests.</returns>
    [HttpGet("Requests")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendRequestDto>>> GetPendingRequests()
    {
        var userId = User.GetUserId();
        var requests = await _friendService.GetPendingRequestsAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(requests);
    }

    /// <summary>
    /// Gets the home-page widget data: friends sorted by online status and a pending request count.
    /// </summary>
    /// <response code="200">Widget data returned.</response>
    /// <returns>The friend widget data.</returns>
    [HttpGet("Widget")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<FriendWidgetDto>> GetFriendWidget()
    {
        var userId = User.GetUserId();
        var widget = await _friendService.GetFriendWidgetAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(widget);
    }

    /// <summary>
    /// Sends a friend request to the user with the specified username.
    /// </summary>
    /// <param name="username">The username of the user to send a request to.</param>
    /// <response code="201">Friend request sent.</response>
    /// <response code="400">Invalid request (e.g. user not found, duplicate request).</response>
    /// <returns>The created friend request.</returns>
    [HttpPost("Requests/{username}")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FriendRequestDto>> SendFriendRequest([FromRoute, Required] string username)
    {
        var userId = User.GetUserId();
        try
        {
            var request = await _friendService.SendFriendRequestAsync(userId, username, CancellationToken.None).ConfigureAwait(false);
            return CreatedAtAction(nameof(GetPendingRequests), request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Accepts an incoming friend request.
    /// </summary>
    /// <param name="requestId">The id of the friend request to accept.</param>
    /// <response code="204">Friend request accepted.</response>
    /// <response code="400">Request is not pending or invalid.</response>
    /// <response code="403">Current user is not the addressee of this request.</response>
    /// <response code="404">Friend request not found.</response>
    /// <returns>No content.</returns>
    [HttpPost("Requests/{requestId:guid}/Accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AcceptFriendRequest([FromRoute, Required] Guid requestId)
    {
        var userId = User.GetUserId();
        try
        {
            await _friendService.AcceptFriendRequestAsync(requestId, userId, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Declines an incoming friend request, or cancels an outgoing one.
    /// </summary>
    /// <param name="requestId">The id of the friend request.</param>
    /// <response code="204">Friend request declined or cancelled.</response>
    /// <response code="403">Current user is not a party to this request.</response>
    /// <response code="404">Friend request not found.</response>
    /// <returns>No content.</returns>
    [HttpDelete("Requests/{requestId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeclineFriendRequest([FromRoute, Required] Guid requestId)
    {
        var userId = User.GetUserId();
        try
        {
            await _friendService.DeclineFriendRequestAsync(requestId, userId, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Removes a friend from the current user's friend list.
    /// </summary>
    /// <param name="friendId">The user id of the friend to remove.</param>
    /// <response code="204">Friend removed.</response>
    /// <response code="404">Friendship not found.</response>
    /// <returns>No content.</returns>
    [HttpDelete("{friendId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFriend([FromRoute, Required] Guid friendId)
    {
        var userId = User.GetUserId();
        try
        {
            await _friendService.RemoveFriendAsync(userId, friendId, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }

    // ── Admin endpoints ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the friend list for any user. Administrator only.
    /// </summary>
    /// <param name="userId">The id of the target user.</param>
    /// <response code="200">Friend list returned.</response>
    /// <returns>A list of friends.</returns>
    [HttpGet("Admin/{userId:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FriendDto>>> AdminGetUserFriends([FromRoute, Required] Guid userId)
    {
        var friends = await _friendService.GetFriendsAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(friends);
    }

    /// <summary>
    /// Directly adds a friendship between two users. Administrator only.
    /// </summary>
    /// <param name="userId">The id of the first user.</param>
    /// <param name="friendId">The id of the second user.</param>
    /// <response code="204">Friendship created.</response>
    /// <response code="400">Users are already friends or the request is invalid.</response>
    /// <returns>No content.</returns>
    [HttpPost("Admin/{userId:guid}/{friendId:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> AdminAddFriendship(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid friendId)
    {
        try
        {
            await _friendService.AddFriendshipAsync(userId, friendId, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Removes a friendship between two users. Administrator only.
    /// </summary>
    /// <param name="userId">The id of the first user.</param>
    /// <param name="friendId">The id of the second user (the friend to remove).</param>
    /// <response code="204">Friendship removed.</response>
    /// <response code="404">Friendship not found.</response>
    /// <returns>No content.</returns>
    [HttpDelete("Admin/{userId:guid}/{friendId:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AdminRemoveFriendship(
        [FromRoute, Required] Guid userId,
        [FromRoute, Required] Guid friendId)
    {
        try
        {
            await _friendService.RemoveFriendAsync(userId, friendId, CancellationToken.None).ConfigureAwait(false);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
