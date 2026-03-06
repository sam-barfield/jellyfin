using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.ReleaseCalendar;

namespace MediaBrowser.Controller.ReleaseCalendar;

/// <summary>
/// Provides access to release calendar data from Radarr and Sonarr.
/// </summary>
public interface IReleaseCalendarService
{
    /// <summary>
    /// Gets the combined release calendar for the specified date range.
    /// </summary>
    /// <param name="start">The start date (UTC).</param>
    /// <param name="end">The end date (UTC).</param>
    /// <param name="type">Optional filter by release type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of calendar items sorted by air date.</returns>
    Task<IReadOnlyList<ReleaseCalendarItem>> GetCalendarAsync(
        DateTime start,
        DateTime end,
        ReleaseCalendarType? type,
        CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates the in-memory calendar cache.
    /// </summary>
    void InvalidateCache();
}
