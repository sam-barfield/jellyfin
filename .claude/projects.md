# Project-by-Project Guide

## Solution Projects (`Jellyfin.sln`)

### Core Server

| Project | Purpose |
|---|---|
| `Jellyfin.Server` | **Entrypoint**. Contains `Program.cs`, `Startup.cs`, `CoreAppHost.cs` (DI). Kestrel web host, migrations, health checks. |
| `Jellyfin.Api` | ASP.NET Core REST API. 65 controllers, middleware, auth, model binders, WebSocket listeners. |
| `Emby.Server.Implementations` | **Business logic**. Implements service interfaces. Contains all custom fork features (Friends, Trending, ReleaseCalendar). Also: Library, Session, Playlists, SyncPlay, Notifications, etc. |

### Abstractions & Models

| Project | Purpose |
|---|---|
| `MediaBrowser.Controller` | **Interfaces layer**. Service contracts (e.g. `IFriendService`), entity base classes, library abstractions. |
| `MediaBrowser.Model` | **Shared DTOs/enums**. No logic. Referenced by every project. DTOs for API responses, config models. |
| `MediaBrowser.Common` | Shared utilities, plugin interfaces, HTTP helpers. |
| `Jellyfin.Data` | Data DTOs, enums, query types used across layers. |

### Database

| Project | Purpose |
|---|---|
| `src/Jellyfin.Database/Jellyfin.Database.Implementations` | EF Core `JellyfinDbContext`, all entity classes, model configuration, enums. |
| `src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite` | SQLite-specific provider and migrations. |

### Domain-Specific Libraries (under `src/`)

| Project | Purpose |
|---|---|
| `Jellyfin.Drawing` / `Jellyfin.Drawing.Skia` | Image processing. |
| `Jellyfin.Extensions` | Extension methods. |
| `Jellyfin.LiveTv` | Live TV / DVR functionality. |
| `Jellyfin.MediaEncoding.Hls` | HLS transcoding. |
| `Jellyfin.MediaEncoding.Keyframes` | Keyframe extraction. |
| `Jellyfin.Networking` | Network configuration, binding. |
| `Jellyfin.CodeAnalysis` | Custom Roslyn analyzers. |
| `Jellyfin.Server.Implementations` | Additional server-level implementations (Activity, Backup, Devices, Trickplay, Users, Storage). |

### Other Top-Level Projects

| Project | Purpose |
|---|---|
| `Emby.Naming` | Media file naming/parsing rules. |
| `Emby.Photos` | Photo library support. |
| `MediaBrowser.LocalMetadata` | NFO/XML local metadata readers. |
| `MediaBrowser.MediaEncoding` | FFmpeg integration, transcoding. |
| `MediaBrowser.Providers` | Metadata providers (TMDB, OMDB, etc.). |
| `MediaBrowser.XbmcMetadata` | Kodi/XBMC NFO metadata. |

### Tests (`tests/`)

16 test projects mirroring the main projects. Run with `dotnet test`.
