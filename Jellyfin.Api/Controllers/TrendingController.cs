using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Trending;
using MediaBrowser.Model.Trending;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Trending controller for Trakt integration.
/// </summary>
[Route("Trending")]
[Authorize]
public class TrendingController : BaseJellyfinApiController
{
    private readonly ITrendingService _trendingService;
    private readonly IServerConfigurationManager _configurationManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrendingController"/> class.
    /// </summary>
    /// <param name="trendingService">Instance of the <see cref="ITrendingService"/>.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    public TrendingController(
        ITrendingService trendingService,
        IServerConfigurationManager configurationManager)
    {
        _trendingService = trendingService;
        _configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets trending movies.
    /// </summary>
    /// <response code="200">Trending movies returned.</response>
    /// <returns>A list of trending movies.</returns>
    [HttpGet("Movies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrendingItem[]>> GetTrendingMovies()
    {
        var items = await _trendingService.GetTrendingMoviesAsync(CancellationToken.None).ConfigureAwait(false);
        return Ok(items.ToArray());
    }

    /// <summary>
    /// Gets trending shows.
    /// </summary>
    /// <response code="200">Trending shows returned.</response>
    /// <returns>A list of trending shows.</returns>
    [HttpGet("Shows")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<TrendingItem[]>> GetTrendingShows()
    {
        var items = await _trendingService.GetTrendingShowsAsync(CancellationToken.None).ConfigureAwait(false);
        return Ok(items.ToArray());
    }

    /// <summary>
    /// Gets the trending configuration.
    /// </summary>
    /// <response code="200">Configuration returned.</response>
    /// <returns>The current trending configuration.</returns>
    [HttpGet("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<TrendingConfiguration> GetConfiguration()
    {
        return _configurationManager.Configuration.Trending;
    }

    /// <summary>
    /// Updates the trending configuration.
    /// </summary>
    /// <param name="configuration">The updated configuration.</param>
    /// <response code="204">Configuration updated.</response>
    /// <returns>Update status.</returns>
    [HttpPost("Configuration")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateConfiguration([FromBody, Required] TrendingConfiguration configuration)
    {
        var serverConfig = _configurationManager.Configuration;
        serverConfig.Trending = configuration;
        _configurationManager.ReplaceConfiguration(serverConfig);
        _trendingService.InvalidateCache();
        return NoContent();
    }
}
