using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using Jellyfin.Api.ModelBinders;
using Jellyfin.Api.Models.UserViewDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities.Libraries;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Library;
using MediaBrowser.Model.Querying;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// User views controller.
/// </summary>
[Route("")]
[Authorize]
public class UserViewsController : BaseJellyfinApiController
{
    private readonly IUserManager _userManager;
    private readonly IUserViewManager _userViewManager;
    private readonly IDtoService _dtoService;
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryController> _logger;
    private readonly string[] _englishLangaugeCodes = { "en", "eng", "en-us", "en-gb", "en-ca", "en-au", "en-in", "english" };

    /// <summary>
    /// Initializes a new instance of the <see cref="UserViewsController"/> class.
    /// </summary>
    /// <param name="userManager">Instance of the <see cref="IUserManager"/> interface.</param>
    /// <param name="userViewManager">Instance of the <see cref="IUserViewManager"/> interface.</param>
    /// <param name="dtoService">Instance of the <see cref="IDtoService"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LibraryController}"/> interface.</param>
    public UserViewsController(
        IUserManager userManager,
        IUserViewManager userViewManager,
        IDtoService dtoService,
        ILibraryManager libraryManager,
        ILogger<LibraryController> logger)
    {
        _userManager = userManager;
        _userViewManager = userViewManager;
        _dtoService = dtoService;
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Get user views.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="includeExternalContent">Whether or not to include external views such as channels or live tv.</param>
    /// <param name="presetViews">Preset views.</param>
    /// <param name="includeHidden">Whether or not to include hidden content.</param>
    /// <response code="200">User views returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the user views.</returns>
    [HttpGet("UserViews")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public QueryResult<BaseItemDto> GetUserViews(
        [FromQuery] Guid? userId,
        [FromQuery] bool? includeExternalContent,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] CollectionType?[] presetViews,
        [FromQuery] bool includeHidden = false)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        var query = new UserViewQuery { User = user, IncludeHidden = includeHidden };

        if (includeExternalContent.HasValue)
        {
            query.IncludeExternalContent = includeExternalContent.Value;
        }

        if (presetViews.Length != 0)
        {
            query.PresetViews = presetViews;
        }

        var folders = _userViewManager.GetUserViews(query);

        var dtoOptions = new DtoOptions().AddClientFields(User);
        dtoOptions.Fields = [..dtoOptions.Fields, ItemFields.PrimaryImageAspectRatio, ItemFields.DisplayPreferencesId];

        var dtos = Array.ConvertAll(folders, i => _dtoService.GetBaseItemDto(i, dtoOptions, user));

        return new QueryResult<BaseItemDto>(dtos);
    }

    private bool IsDubbed(BaseItem item)
    {
        var audioLanguages = item.GetMediaStreams()
            .Where(m => m.Type == MediaBrowser.Model.Entities.MediaStreamType.Audio && !string.IsNullOrEmpty(m.Language))
            .Select(m => m.Language)
            .Distinct()
            .ToArray();

        return audioLanguages.Length > 0 && audioLanguages.Any(lang => _englishLangaugeCodes.Contains(lang, StringComparer.OrdinalIgnoreCase));
    }

    private bool IsSubbed(BaseItem item)
    {
        var subtitleLanguages = item.GetMediaStreams()
            .Where(m => m.Type == MediaBrowser.Model.Entities.MediaStreamType.Subtitle && !string.IsNullOrEmpty(m.Language))
            .Select(m => m.Language)
            .Distinct()
            .ToArray();

        return subtitleLanguages.Length > 0 && subtitleLanguages.Any(lang => _englishLangaugeCodes.Contains(lang, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Starts a library scan to record dubbed / subbed counts for series and seasons.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <returns>A <see cref="OkResult"/>.</returns>
    [HttpPost("Library/DubSubScan")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<string>> DubSubScanLibrary([FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value) ?? throw new ResourceNotFoundException();

        var query = new UserViewQuery { User = user, IncludeHidden = false };
        query.IncludeExternalContent = false;

        _logger.LogInformation("Starting dub sub scan.");

        // Fetch folders
        var folders = _userViewManager.GetUserViews(query);
        var dtoOptions = new DtoOptions().AddClientFields(User);
        dtoOptions.Fields = [.. dtoOptions.Fields, ItemFields.PrimaryImageAspectRatio, ItemFields.DisplayPreferencesId];
        var dtos = Array.ConvertAll(folders, i => _dtoService.GetBaseItemDto(i, dtoOptions, user));

        _logger.LogInformation("Found {Folders} folders to scan", dtos.Length);

        var totalSeriesCount = 0;
        var totalSeasonCount = 0;
        var totalEpisodeCount = 0;

        // For each folder, loop through each series, season and episode and extract total dub/sub counts
        foreach (var folder in dtos)
        {
            var folderItem = _libraryManager.GetItemById(folder.Id);
            if (folderItem is not Folder folderInstance)
            {
                continue;
            }

            if (folder.Name != "Anime") // TODO Don't hardcode this, make it a user changeable setting.
            {
                continue;
            }

            var itemsToUpdate = new List<BaseItem>();

            foreach (var series in folderInstance.Children.OfType<MediaBrowser.Controller.Entities.TV.Series>())
            {
                _logger.LogInformation("Scanning series: {SeriesName}", series.Name);

                var dubbedSeasons = new List<int>();
                var subbedSeasons = new List<int>();
                var seasonsToUpdate = new List<BaseItem>();

                foreach (var season in series.Children.OfType<MediaBrowser.Controller.Entities.TV.Season>())
                {
                    _logger.LogInformation("[{SeriesName}] Scanning season: {SeasonName}", series.Name, season.Name);

                    int episodesWithDub = 0;
                    int episodesWithSub = 0;
                    int totalEpisodes = 0;

                    var episodesToUpdate = new List<BaseItem>();

                    foreach (var episode in season.Children.OfType<MediaBrowser.Controller.Entities.TV.Episode>())
                    {
                        _logger.LogInformation("[{SeriesName}] > [{SeasonName}] Scanning episode: {EpisodeName}", series.Name, season.Name, episode.Name);

                        var episodeDto = _dtoService.GetBaseItemDto(episode, dtoOptions, user);
                        var audioLanguages = episodeDto.AudioLanguages ?? Array.Empty<string>();
                        var subtitleLanguages = episodeDto.SubtitleLanguages ?? Array.Empty<string>();

                        var previousEpDubbedCount = episode.ItemDubbedCount;
                        var previousEpSubbedCount = episode.ItemSubbedCount;

                        if (IsDubbed(episode))
                        {
                            episodesWithDub++;
                            episode.ItemDubbedCount = 2;
                        }
                        else
                        {
                            episode.ItemDubbedCount = 0;
                        }

                        if (IsSubbed(episode))
                        {
                            episodesWithSub++;
                            episode.ItemSubbedCount = 2;
                        }
                        else
                        {
                            episode.ItemSubbedCount = 0;
                        }

                        totalEpisodes++;
                        totalEpisodeCount++;

                        // Mark episode for update after modifying its properties
                        if (episode.ItemDubbedCount != previousEpDubbedCount || episode.ItemSubbedCount != previousEpSubbedCount)
                        {
                            episodesToUpdate.Add(episode);
                        }
                    }

                    var previousDubbedCount = season.ItemDubbedCount;
                    var previousSubbedCount = season.ItemSubbedCount;

                    // todo add explanation of 0, 1, 2.
                    if (episodesWithDub == totalEpisodes)
                    {
                        season.ItemDubbedCount = 2;
                        dubbedSeasons.Add(2);
                    }
                    else if (episodesWithDub >= 1)
                    {
                        season.ItemDubbedCount = 1;
                        dubbedSeasons.Add(1);
                    }
                    else
                    {
                        season.ItemDubbedCount = 0;
                        dubbedSeasons.Add(0);
                    }

                    if (episodesWithSub == totalEpisodes)
                    {
                        season.ItemSubbedCount = 2;
                        subbedSeasons.Add(2);
                    }
                    else if (episodesWithSub >= 1)
                    {
                        season.ItemSubbedCount = 1;
                        subbedSeasons.Add(1);
                    }
                    else
                    {
                        season.ItemSubbedCount = 0;
                        subbedSeasons.Add(0);
                    }

                    // Mark season for update after modifying its properties
                    if (season.ItemDubbedCount != previousDubbedCount || season.ItemSubbedCount != previousSubbedCount)
                    {
                        seasonsToUpdate.Add(season);
                    }

                    if (episodesToUpdate.Count > 0)
                    {
                        await _libraryManager.UpdateItemsAsync(episodesToUpdate, season, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                    }

                    totalSeasonCount++;
                }

                // Update series properties as before
                var previousSeriesDubbedCount = series.ItemDubbedCount;
                var previousSeriesSubbedCount = series.ItemSubbedCount;

                if (dubbedSeasons.Count > 0 && dubbedSeasons.All(n => n == 2))
                {
                    series.ItemDubbedCount = 2;
                }
                else if (dubbedSeasons.Count > 0 && dubbedSeasons.All(n => n == 0))
                {
                    series.ItemDubbedCount = 0;
                }
                else
                {
                    series.ItemDubbedCount = 1;
                }

                if (subbedSeasons.Count > 0 && subbedSeasons.All(n => n == 2))
                {
                    series.ItemSubbedCount = 2;
                }
                else if (subbedSeasons.Count > 0 && subbedSeasons.All(n => n == 0))
                {
                    series.ItemSubbedCount = 0;
                }
                else
                {
                    series.ItemSubbedCount = 1;
                }

                // If series properties changed, mark for update
                if (series.ItemDubbedCount != previousSeriesDubbedCount || series.ItemSubbedCount != previousSeriesSubbedCount)
                {
                    itemsToUpdate.Add(series);
                }

                // Update all modified seasons
                if (seasonsToUpdate.Count > 0)
                {
                    await _libraryManager.UpdateItemsAsync(seasonsToUpdate, series, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
                }

                totalSeriesCount++;
            }

            // Update all modified series
            if (itemsToUpdate.Count > 0)
            {
                await _libraryManager.UpdateItemsAsync(itemsToUpdate, folderInstance, ItemUpdateType.MetadataEdit, CancellationToken.None).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Dub sub scan complete.");

        return Ok($"Scan complete. Processed {totalSeriesCount} series, {totalSeasonCount} seasons, and {totalEpisodeCount} episodes.");
    }

    /// <summary>
    /// Get user views.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <param name="includeExternalContent">Whether or not to include external views such as channels or live tv.</param>
    /// <param name="presetViews">Preset views.</param>
    /// <param name="includeHidden">Whether or not to include hidden content.</param>
    /// <response code="200">User views returned.</response>
    /// <returns>An <see cref="OkResult"/> containing the user views.</returns>
    [HttpGet("Users/{userId}/Views")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public QueryResult<BaseItemDto> GetUserViewsLegacy(
        [FromRoute, Required] Guid userId,
        [FromQuery] bool? includeExternalContent,
        [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] CollectionType?[] presetViews,
        [FromQuery] bool includeHidden = false)
        => GetUserViews(userId, includeExternalContent, presetViews, includeHidden);

    /// <summary>
    /// Get user view grouping options.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <response code="200">User view grouping options returned.</response>
    /// <response code="404">User not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the user view grouping options
    /// or a <see cref="NotFoundResult"/> if user not found.
    /// </returns>
    [HttpGet("UserViews/GroupingOptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<SpecialViewOptionDto>> GetGroupingOptions([FromQuery] Guid? userId)
    {
        userId = RequestHelpers.GetUserId(User, userId);
        var user = _userManager.GetUserById(userId.Value);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(_libraryManager.GetUserRootFolder()
            .GetChildren(user, true)
            .OfType<Folder>()
            .Where(UserView.IsEligibleForGrouping)
            .Select(i => new SpecialViewOptionDto
            {
                Name = i.Name,
                Id = i.Id.ToString("N", CultureInfo.InvariantCulture)
            })
            .OrderBy(i => i.Name)
            .AsEnumerable());
    }

    /// <summary>
    /// Get user view grouping options.
    /// </summary>
    /// <param name="userId">User id.</param>
    /// <response code="200">User view grouping options returned.</response>
    /// <response code="404">User not found.</response>
    /// <returns>
    /// An <see cref="OkResult"/> containing the user view grouping options
    /// or a <see cref="NotFoundResult"/> if user not found.
    /// </returns>
    [HttpGet("Users/{userId}/GroupingOptions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Obsolete("Kept for backwards compatibility")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<IEnumerable<SpecialViewOptionDto>> GetGroupingOptionsLegacy(
        [FromRoute, Required] Guid userId)
        => GetGroupingOptions(userId);
}
