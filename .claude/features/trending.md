# Trending (TMDB Integration)

## Overview

Shows trending movies and TV series sourced from **TMDB (The Movie Database)** API. Cross-references with the local library to indicate items the user already has. Results are cached in memory.

## File Map

| Layer | File |
|---|---|
| **API Controller** | `Jellyfin.Api/Controllers/TrendingController.cs` (95 lines) |
| **Interface** | `MediaBrowser.Controller/Trending/ITrendingService.cs` |
| **Implementation** | `Emby.Server.Implementations/Trending/TrendingService.cs` (306 lines) |
| **DTOs** | `MediaBrowser.Model/Trending/TrendingItem.cs`, `TrendingConfiguration.cs` |
| **DI** | `Jellyfin.Server/CoreAppHost.cs` line ~97 |

## API Endpoints (`/Trending`)

All endpoints require authorization.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/Trending/Movies` | Get trending movies from TMDB |
| `GET` | `/Trending/Shows` | Get trending TV shows from TMDB |

## Key Implementation Details

- **External API**: Calls `https://api.themoviedb.org/3/` using `IHttpClientFactory`.
- **Configuration**: TMDB API key stored in server config via `TrendingConfiguration` (accessed through `IServerConfigurationManager`).
- **Caching**: Uses `IMemoryCache` with `Trending_` prefixed keys.
- **Library matching**: Uses `ILibraryManager` to check if trending items exist in the user's local library.
- **Error handling**: Logs errors, returns empty results on TMDB API failure.
