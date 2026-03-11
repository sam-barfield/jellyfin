using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Notifications;
using MediaBrowser.Model.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// User notification management controller.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="NotificationsController"/> class.
/// </remarks>
/// <param name="notificationService">Instance of the <see cref="IUserNotificationService"/>.</param>
[Route("Notifications")]
[Authorize]
public class NotificationsController(IUserNotificationService notificationService) : BaseJellyfinApiController
{
    private readonly IUserNotificationService _notificationService = notificationService;

    /// <summary>
    /// Gets all notifications for the current user, unread first then by most recent.
    /// </summary>
    /// <response code="200">Notifications returned.</response>
    /// <returns>A list of notifications.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserNotificationDto>>> GetNotifications()
    {
        var userId = User.GetUserId();
        var notifications = await _notificationService.GetNotificationsAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(notifications);
    }

    /// <summary>
    /// Gets the unread notification status for the current user.
    /// </summary>
    /// <response code="200">Unread status returned.</response>
    /// <returns>The unread notification status.</returns>
    [HttpGet("Unread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<UserNotificationUnreadStatus>> GetUnreadStatus()
    {
        var userId = User.GetUserId();
        var status = await _notificationService.GetUnreadStatusAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return Ok(status);
    }

    /// <summary>
    /// Marks a specific notification as read.
    /// </summary>
    /// <param name="notificationId">The id of the notification to mark as read.</param>
    /// <response code="204">Notification marked as read.</response>
    /// <response code="403">Notification does not belong to current user.</response>
    /// <response code="404">Notification not found.</response>
    /// <returns>No content.</returns>
    [HttpPost("{notificationId:guid}/Read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAsRead([FromRoute, Required] Guid notificationId)
    {
        var userId = User.GetUserId();
        try
        {
            await _notificationService.MarkAsReadAsync(notificationId, userId, CancellationToken.None).ConfigureAwait(false);
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
    }

    /// <summary>
    /// Marks all notifications for the current user as read.
    /// </summary>
    /// <response code="204">All notifications marked as read.</response>
    /// <returns>No content.</returns>
    [HttpPost("ReadAll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = User.GetUserId();
        await _notificationService.MarkAllAsReadAsync(userId, CancellationToken.None).ConfigureAwait(false);
        return NoContent();
    }
}
