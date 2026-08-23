# Announcements

## Overview

Server-wide announcement system allowing admins to post messages visible to all (or specific) users. Users can track read/unread status.

## File Map

| Layer | File |
|---|---|
| **API Controller** | `Jellyfin.Api/Controllers/AnnouncementsController.cs` (369 lines) |
| **DTOs** | `MediaBrowser.Model/Announcements/` — `AnnouncementInfo.cs`, `AnnouncementInfoDto.cs`, `AnnouncementRequest.cs`, `AnnouncementUnreadStatus.cs` |

## Key Implementation Details

- **No separate service class** — logic lives directly in the controller (unlike Friends/Trending/ReleaseCalendar).
- **Configuration-backed**: Announcements are stored in server configuration (via `IServerConfigurationManager`), not in the database.
- **User manager**: Uses `IUserManager` for user lookups and permission checks.
- **Admin-only creation**: Only elevated users can create/modify announcements.
- **Read tracking**: Tracks per-user unread status via `AnnouncementUnreadStatus`.

## Data Models

- `AnnouncementInfo` — The announcement itself (title, message, date, etc.)
- `AnnouncementInfoDto` — API response DTO (includes user-specific read state)
- `AnnouncementRequest` — Request body for creating/updating announcements
- `AnnouncementUnreadStatus` — Per-user read/unread tracking
