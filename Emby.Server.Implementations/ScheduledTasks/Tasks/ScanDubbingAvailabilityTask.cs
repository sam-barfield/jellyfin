using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Episode = MediaBrowser.Controller.Entities.TV.Episode;
using Season = MediaBrowser.Controller.Entities.TV.Season;
using Series = MediaBrowser.Controller.Entities.TV.Series;

namespace Emby.Server.Implementations.ScheduledTasks.Tasks;

/// <summary>
/// This task scans items and records their dubbing and subtitle availabilty.
/// </summary>
public partial class ScanDubbingAvailabilityTask : IScheduledTask
{
    private readonly IItemRepository _itemRepository;
    private readonly ILibraryManager _libraryManager;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly ILogger<ScanDubbingAvailabilityTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanDubbingAvailabilityTask"/> class.
    /// </summary>
    /// <param name="itemRepository">Instance of the <see cref="IItemRepository"/> interface.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="configurationManager">Instance of the <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{ScanDubbingAvailabilityTask}"/> interface.</param>
    public ScanDubbingAvailabilityTask(
        IItemRepository itemRepository,
        ILibraryManager libraryManager,
        IServerConfigurationManager configurationManager,
        ILogger<ScanDubbingAvailabilityTask> logger)
    {
        _itemRepository = itemRepository;
        _libraryManager = libraryManager;
        _configurationManager = configurationManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public string Name => "Dubbing Availability Scan";

    /// <inheritdoc />
    public string Description => "Scans libraries and records item dubbing and subtitle availability.";

    /// <inheritdoc />
    public string Category => "RemasterDubbing";

    /// <inheritdoc />
    public string Key => "ScanDubbingAvailability";

    /// <inheritdoc />
    public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        var libraries = _libraryManager.RootFolder.Children;
        var totalSeriesToScan = 0;
        var totalSeriesComplete = 0;

        var languageCodesRaw = _configurationManager.Configuration.DubbingLanguageCodes;
        var languageCodes = languageCodesRaw?.GetValue(0)?.ToString()?.Split(',');

        if (languageCodes is null)
        {
            _logger.LogError("No language codes configured.");
            return;
        }

        foreach (var library in libraries)
        {
            var libraryOptions = _libraryManager.GetLibraryOptions(library);

            // We only want to scan libraries with this feature enabled.
            if (libraryOptions.DubbingIconsEnabled is not true)
            {
                continue;
            }

            _logger.LogInformation("Beginning scan for {LibraryName} library.", library.Name);

            await Task.Run(
                () =>
                {
                    var itemUpdates = new List<BaseItem>();

                    // Get series in library.
                    var series = _libraryManager.GetItemList(new InternalItemsQuery
                    {
                        IncludeItemTypes = [BaseItemKind.Series],
                        Parent = library,
                        Recursive = true
                    }).OfType<Series>().ToList();

                    totalSeriesToScan += series.Count;

                    foreach (var anime in series)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogInformation("[{LibraryName}] Found series: {SeriesName}", library.Name, anime.Name);

                        var seasonDubStates = new List<DubAvailability>();
                        var seasonSubStates = new List<DubAvailability>();

                        // Get seasons in series.
                        var seasons = _libraryManager.GetItemList(new InternalItemsQuery
                        {
                            IncludeItemTypes = [BaseItemKind.Season],
                            Parent = anime,
                            Recursive = true
                        }).OfType<Season>().ToList();

                        foreach (var season in seasons)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            _logger.LogInformation("[{LibraryName}][{SeriesName}] Found season: {SeasonName}", library.Name, anime.Name, season.Name);

                            var episodes = _libraryManager.GetItemList(new InternalItemsQuery
                            {
                                IncludeItemTypes = new[] { BaseItemKind.Episode },
                                Parent = season,
                                Recursive = true
                            }).OfType<Episode>().ToList();

                            var totalEpisodes = episodes.Count;
                            var episodesWithDub = 0;
                            var episodesWithSub = 0;

                            foreach (var ep in episodes)
                            {
                                _logger.LogInformation("[{LibraryName}][{SeriesName}][{SeasonName}] Scanning episode: {EpisodeName}", library.Name, anime.Name, season.Name, ep.Name);

                                var oldDub = ep.DubAvailable;
                                var oldSub = ep.SubAvailable;

                                var isDub = HasEnglishStream(ep, MediaStreamType.Audio, languageCodes);
                                var isSub = HasEnglishStream(ep, MediaStreamType.Subtitle, languageCodes);

                                if (isDub)
                                {
                                    episodesWithDub++;
                                    ep.DubAvailable = DubAvailability.Full;
                                }
                                else
                                {
                                    ep.DubAvailable = DubAvailability.Missing;
                                }

                                if (isSub)
                                {
                                    episodesWithSub++;
                                    ep.SubAvailable = DubAvailability.Full;
                                }
                                else
                                {
                                    ep.SubAvailable = DubAvailability.Missing;
                                }

                                if (ep.DubAvailable != oldDub || ep.SubAvailable != oldSub)
                                {
                                    itemUpdates.Add(ep);
                                }
                            }

                            var oldSeasonDub = season.DubAvailable;
                            var oldSeasonSub = season.SubAvailable;

                            season.DubAvailable = AvailabilityFromCounts(episodesWithDub, totalEpisodes);
                            season.SubAvailable = AvailabilityFromCounts(episodesWithSub, totalEpisodes);

                            seasonDubStates.Add(season.DubAvailable ?? DubAvailability.Missing);
                            seasonSubStates.Add(season.SubAvailable ?? DubAvailability.Missing);

                            if (season.DubAvailable != oldSeasonDub || season.SubAvailable != oldSeasonSub)
                            {
                                itemUpdates.Add(season);
                            }
                        }

                        var oldSeriesDub = anime.DubAvailable;
                        var oldSeriesSub = anime.SubAvailable;

                        anime.DubAvailable = RollupAvailability(seasonDubStates);
                        anime.SubAvailable = RollupAvailability(seasonSubStates);

                        if (anime.DubAvailable != oldSeriesDub || anime.SubAvailable != oldSeriesSub)
                        {
                            itemUpdates.Add(anime);
                        }

                        totalSeriesComplete++;
                        double percent = totalSeriesComplete;
                        percent /= totalSeriesToScan;

                        progress.Report(100 * percent);
                    }

                    _logger.LogInformation("Scan complete, updating database.");
                    _itemRepository.SaveItems(itemUpdates, cancellationToken);
                },
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        yield return new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.IntervalTrigger,
            IntervalTicks = TimeSpan.FromHours(24).Ticks
        };
    }

    private static DubAvailability AvailabilityFromCounts(int withCount, int totalCount)
        => totalCount switch
        {
            0 => DubAvailability.Missing, // choose Missing for "no data"
            _ when withCount == totalCount => DubAvailability.Full,
            _ when withCount > 0 => DubAvailability.Partial,
            _ => DubAvailability.Missing
        };

    private static DubAvailability RollupAvailability(IReadOnlyCollection<DubAvailability> states)
    {
        if (states.Count == 0)
        {
            return DubAvailability.Missing; // choose Missing for "no seasons"
        }

        return states.All(s => s == DubAvailability.Full)
            ? DubAvailability.Full
            : states.All(s => s == DubAvailability.Missing)
                ? DubAvailability.Missing
                : DubAvailability.Partial;
    }

    private static bool HasEnglishStream(BaseItem item, MediaStreamType type, string[] languageCodes)
    {
        var languages = item.GetMediaStreams()
            .Where(m => m.Type == type && !string.IsNullOrEmpty(m.Language))
            .Select(m => m.Language)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return languages.Count > 0 && languages.Any(languageCodes.Contains);
    }
}
