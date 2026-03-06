using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Trending;

namespace MediaBrowser.Controller.Trending;

/// <summary>
/// Provides access to trending movies and series data.
/// </summary>
public interface ITrendingService
{
    /// <summary>
    /// Gets the trending movies available on the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of trending movies.</returns>
    Task<IReadOnlyList<TrendingItem>> GetTrendingMoviesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Gets the trending shows available on the server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of trending shows.</returns>
    Task<IReadOnlyList<TrendingItem>> GetTrendingShowsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Invalidates the internal cache.
    /// </summary>
    void InvalidateCache();
}
