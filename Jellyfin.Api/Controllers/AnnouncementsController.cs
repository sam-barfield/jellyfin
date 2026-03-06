using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Announcements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Announcements controller.
/// </summary>
[Route("Announcements")]
[Authorize]
public class AnnouncementsController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _configurationManager;
    private readonly IUserManager _userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="AnnouncementsController"/> class.
    /// </summary>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    public AnnouncementsController(
        IServerConfigurationManager configurationManager,
        IUserManager userManager)
    {
        _configurationManager = configurationManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Gets active announcements.
    /// </summary>
    /// <response code="200">Announcements returned.</response>
    /// <returns>Active announcements sorted by most recent creation date.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AnnouncementInfoDto[]> GetAnnouncements()
    {
        var user = GetRequestUser();
        if (user is null)
        {
            return Unauthorized();
        }

        var nowUtc = DateTime.UtcNow;
        var readIds = user.GetPreferenceValues<Guid>(PreferenceKind.ReadAnnouncements);
        var readIdSet = new HashSet<Guid>(readIds);

        return Ok(OrderAnnouncements(GetAnnouncementsInternal()
            .Where(a => a.IsActive)
            .Where(a => !a.StartDateUtc.HasValue || a.StartDateUtc.Value <= nowUtc)
            .Where(a => !a.EndDateUtc.HasValue || a.EndDateUtc.Value >= nowUtc))
            .Select(a => ToAnnouncementInfoDto(a, readIdSet))
            .ToArray());
    }

    /// <summary>
    /// Gets all announcements including disabled and scheduled entries.
    /// </summary>
    /// <response code="200">Announcements returned.</response>
    /// <returns>All announcements sorted by most recent creation date.</returns>
    [HttpGet("Admin")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AnnouncementInfo[]> GetAnnouncementsAdmin()
    {
        return Ok(OrderAnnouncements(GetAnnouncementsInternal()).ToArray());
    }

    /// <summary>
    /// Creates a new announcement.
    /// </summary>
    /// <param name="request">Announcement data.</param>
    /// <response code="200">Announcement created.</response>
    /// <response code="400">Invalid payload.</response>
    /// <returns>The created announcement.</returns>
    [HttpPost]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AnnouncementInfo> CreateAnnouncement([FromBody, Required] AnnouncementRequest request)
    {
        if (!TryValidateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var announcements = GetAnnouncementsInternal().ToList();
        var nowUtc = DateTime.UtcNow;
        var announcement = new AnnouncementInfo
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Text = request.Text.Trim(),
            IsPinned = request.IsPinned,
            Priority = request.Priority,
            DisplayOrder = request.DisplayOrder,
            DateCreatedUtc = nowUtc,
            StartDateUtc = request.StartDateUtc?.ToUniversalTime(),
            EndDateUtc = request.EndDateUtc?.ToUniversalTime(),
            IsActive = request.IsActive
        };

        announcements.Add(announcement);
        SaveAnnouncements(announcements);
        return Ok(announcement);
    }

    /// <summary>
    /// Updates an existing announcement.
    /// </summary>
    /// <param name="announcementId">The announcement id.</param>
    /// <param name="request">Announcement data.</param>
    /// <response code="200">Announcement updated.</response>
    /// <response code="400">Invalid payload.</response>
    /// <response code="404">Announcement not found.</response>
    /// <returns>The updated announcement.</returns>
    [HttpPost("{announcementId:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AnnouncementInfo> UpdateAnnouncement(
        [FromRoute, Required] Guid announcementId,
        [FromBody, Required] AnnouncementRequest request)
    {
        if (!TryValidateRequest(request, out var validationError))
        {
            return BadRequest(validationError);
        }

        var announcements = GetAnnouncementsInternal().ToList();
        var index = announcements.FindIndex(a => a.Id.Equals(announcementId));
        if (index < 0)
        {
            return NotFound();
        }

        var existing = announcements[index];
        var updated = new AnnouncementInfo
        {
            Id = existing.Id,
            Title = request.Title.Trim(),
            Text = request.Text.Trim(),
            IsPinned = request.IsPinned,
            Priority = request.Priority,
            DisplayOrder = request.DisplayOrder,
            DateCreatedUtc = existing.DateCreatedUtc,
            StartDateUtc = request.StartDateUtc?.ToUniversalTime(),
            EndDateUtc = request.EndDateUtc?.ToUniversalTime(),
            IsActive = request.IsActive
        };

        announcements[index] = updated;
        SaveAnnouncements(announcements);
        return Ok(updated);
    }

    /// <summary>
    /// Deletes an announcement.
    /// </summary>
    /// <param name="announcementId">The announcement id.</param>
    /// <response code="204">Announcement deleted.</response>
    /// <response code="404">Announcement not found.</response>
    /// <returns>Delete status.</returns>
    [HttpDelete("{announcementId:guid}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult DeleteAnnouncement([FromRoute, Required] Guid announcementId)
    {
        var announcements = GetAnnouncementsInternal().ToList();
        var removed = announcements.RemoveAll(a => a.Id.Equals(announcementId));
        if (removed == 0)
        {
            return NotFound();
        }

        SaveAnnouncements(announcements);
        return NoContent();
    }

    /// <summary>
    /// Gets unread announcement state for the current user.
    /// </summary>
    /// <response code="200">Unread status returned.</response>
    /// <response code="401">User context is required.</response>
    /// <returns>Unread announcement status.</returns>
    [HttpGet("Unread")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<AnnouncementUnreadStatus> GetUnreadStatus()
    {
        var user = GetRequestUser();
        if (user is null)
        {
            return Unauthorized();
        }

        var activeAnnouncementIds = GetActiveAnnouncements(DateTime.UtcNow)
            .Select(a => a.Id)
            .ToHashSet();

        var readIds = user.GetPreferenceValues<Guid>(PreferenceKind.ReadAnnouncements);
        var unreadCount = activeAnnouncementIds.Count(id => !readIds.Contains(id));

        return Ok(new AnnouncementUnreadStatus
        {
            UnreadCount = unreadCount,
            HasUnread = unreadCount > 0
        });
    }

    /// <summary>
    /// Marks one announcement as read for the current user.
    /// </summary>
    /// <param name="announcementId">The announcement id.</param>
    /// <response code="204">Read receipt recorded.</response>
    /// <response code="401">User context is required.</response>
    /// <response code="404">Announcement not found.</response>
    /// <returns>Update status.</returns>
    [HttpPost("{announcementId:guid}/Read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> MarkAnnouncementRead([FromRoute, Required] Guid announcementId)
    {
        var user = GetRequestUser();
        if (user is null)
        {
            return Unauthorized();
        }

        var exists = GetAnnouncementsInternal().Any(a => a.Id.Equals(announcementId));
        if (!exists)
        {
            return NotFound();
        }

        var readIds = user.GetPreferenceValues<Guid>(PreferenceKind.ReadAnnouncements).ToHashSet();
        readIds.Add(announcementId);
        user.SetPreference(PreferenceKind.ReadAnnouncements, readIds.ToArray());
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

        return NoContent();
    }

    /// <summary>
    /// Marks all currently active announcements as read for the current user.
    /// </summary>
    /// <response code="204">Read receipts recorded.</response>
    /// <response code="401">User context is required.</response>
    /// <returns>Update status.</returns>
    [HttpPost("ReadAll")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> MarkAllAnnouncementsRead()
    {
        var user = GetRequestUser();
        if (user is null)
        {
            return Unauthorized();
        }

        var readIds = user.GetPreferenceValues<Guid>(PreferenceKind.ReadAnnouncements).ToHashSet();
        foreach (var announcementId in GetActiveAnnouncements(DateTime.UtcNow).Select(a => a.Id))
        {
            readIds.Add(announcementId);
        }

        user.SetPreference(PreferenceKind.ReadAnnouncements, readIds.ToArray());
        await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

        return NoContent();
    }

    private AnnouncementInfo[] GetAnnouncementsInternal()
        => _configurationManager.Configuration.Announcements ?? Array.Empty<AnnouncementInfo>();

    private IEnumerable<AnnouncementInfo> GetActiveAnnouncements(DateTime nowUtc)
        => GetAnnouncementsInternal()
            .Where(a => a.IsActive)
            .Where(a => !a.StartDateUtc.HasValue || a.StartDateUtc.Value <= nowUtc)
            .Where(a => !a.EndDateUtc.HasValue || a.EndDateUtc.Value >= nowUtc);

    private void SaveAnnouncements(IReadOnlyList<AnnouncementInfo> announcements)
    {
        var configuration = _configurationManager.Configuration;
        configuration.Announcements = announcements.ToArray();
        _configurationManager.ReplaceConfiguration(configuration);
    }

    private Jellyfin.Database.Implementations.Entities.User? GetRequestUser()
    {
        var userId = User.GetUserId();
        if (userId.Equals(Guid.Empty))
        {
            return null;
        }

        return _userManager.GetUserById(userId);
    }

    private static IEnumerable<AnnouncementInfo> OrderAnnouncements(IEnumerable<AnnouncementInfo> announcements)
        => announcements
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.Priority)
            .ThenBy(a => a.DisplayOrder)
            .ThenByDescending(a => a.DateCreatedUtc);

    private static AnnouncementInfoDto ToAnnouncementInfoDto(AnnouncementInfo announcement, HashSet<Guid> readIds)
        => new AnnouncementInfoDto
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Text = announcement.Text,
            IsPinned = announcement.IsPinned,
            Priority = announcement.Priority,
            DisplayOrder = announcement.DisplayOrder,
            DateCreatedUtc = announcement.DateCreatedUtc,
            StartDateUtc = announcement.StartDateUtc,
            EndDateUtc = announcement.EndDateUtc,
            IsActive = announcement.IsActive,
            IsRead = readIds.Contains(announcement.Id)
        };

    private static bool TryValidateRequest(AnnouncementRequest request, out string error)
    {
        var title = request.Title?.Trim();
        var text = request.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            error = "Title is required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Text is required.";
            return false;
        }

        var startUtc = request.StartDateUtc?.ToUniversalTime();
        var endUtc = request.EndDateUtc?.ToUniversalTime();

        if (startUtc.HasValue && endUtc.HasValue && startUtc.Value > endUtc.Value)
        {
            error = "StartDateUtc must be less than or equal to EndDateUtc.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
