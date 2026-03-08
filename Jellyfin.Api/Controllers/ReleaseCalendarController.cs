using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.ReleaseCalendar;
using MediaBrowser.Model.ReleaseCalendar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Release calendar controller for Radarr/Sonarr integration.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ReleaseCalendarController"/> class.
/// </remarks>
/// <param name="releaseCalendarService">Instance of the <see cref="IReleaseCalendarService"/>.</param>
/// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
[Route("ReleaseCalendar")]
[Authorize]
public class ReleaseCalendarController(
    IReleaseCalendarService releaseCalendarService,
    IServerConfigurationManager configurationManager) : BaseJellyfinApiController
{
    private readonly IReleaseCalendarService _releaseCalendarService = releaseCalendarService;
    private readonly IServerConfigurationManager _configurationManager = configurationManager;

    /// <summary>
    /// Gets the combined release calendar from Radarr and Sonarr.
    /// </summary>
    /// <param name="start">Start date (UTC) of the calendar window.</param>
    /// <param name="end">End date (UTC) of the calendar window.</param>
    /// <param name="type">Optional filter by release type (Movie or Episode).</param>
    /// <response code="200">Calendar items returned.</response>
    /// <returns>A list of release calendar items sorted by air date.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ReleaseCalendarItem[]>> GetCalendar(
        [FromQuery, Required] DateTime start,
        [FromQuery, Required] DateTime end,
        [FromQuery] ReleaseCalendarType? type)
    {
        var items = await _releaseCalendarService.GetCalendarAsync(
            start.ToUniversalTime(),
            end.ToUniversalTime(),
            type,
            CancellationToken.None).ConfigureAwait(false);

        return Ok(items.ToArray());
    }

    /// <summary>
    /// Gets the release calendar configuration.
    /// </summary>
    /// <response code="200">Configuration returned.</response>
    /// <returns>The current release calendar configuration.</returns>
    [HttpGet("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ReleaseCalendarConfiguration> GetConfiguration()
    {
        return _configurationManager.Configuration.ReleaseCalendar;
    }

    /// <summary>
    /// Updates the release calendar configuration.
    /// </summary>
    /// <param name="configuration">The updated configuration.</param>
    /// <response code="204">Configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateConfiguration([FromBody, Required] ReleaseCalendarConfiguration configuration)
    {
        var serverConfig = _configurationManager.Configuration;
        serverConfig.ReleaseCalendar = configuration;
        _configurationManager.ReplaceConfiguration(serverConfig);
        _releaseCalendarService.InvalidateCache();
        return NoContent();
    }
}
