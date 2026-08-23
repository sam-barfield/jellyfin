# Release Calendar (Radarr/Sonarr Integration)

## Overview

Aggregates upcoming movie and TV episode releases from **Radarr** and **Sonarr** APIs into a unified release calendar view. Results are cached in memory.

## File Map

| Layer | File |
|---|---|
| **API Controller** | `Jellyfin.Api/Controllers/ReleaseCalendarController.cs` (88 lines) |
| **Interface** | `MediaBrowser.Controller/ReleaseCalendar/IReleaseCalendarService.cs` |
| **Implementation** | `Emby.Server.Implementations/ReleaseCalendar/ReleaseCalendarService.cs` (305 lines) |
| **DTOs** | `MediaBrowser.Model/ReleaseCalendar/` — `ReleaseCalendarItem.cs`, `ReleaseCalendarConfiguration.cs`, `ReleaseCalendarType.cs` |
| **DI** | `Jellyfin.Server/CoreAppHost.cs` line ~96 |

## API Endpoints (`/ReleaseCalendar`)

All endpoints require authorization.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/ReleaseCalendar` | Get combined calendar (params: `start`, `end` as DateTime, optional `type` filter) |

## Key Implementation Details

- **External APIs**: Calls Radarr (`/api/v3/calendar`) and Sonarr (`/api/v3/calendar`) APIs.
- **Configuration**: Radarr/Sonarr URLs and API keys stored in `ReleaseCalendarConfiguration` via server config.
- **`ReleaseCalendarType` enum**: `Movie` or `Episode` — used for filtering.
- **Caching**: `IMemoryCache` with `ReleaseCalendar_` prefixed keys.
- **Merging**: Results from both services are merged and sorted by air/release date.
- **Error handling**: Logs errors per service, returns partial results if one API fails.
