# Friends System

## Overview

A social feature allowing Jellyfin users to add each other as friends, see online/activity status, and get notified about friend requests. Includes real-time WebSocket updates.

## File Map

| Layer | File |
|---|---|
| **API Controller** | `Jellyfin.Api/Controllers/FriendsController.cs` (261 lines) |
| **WebSocket** | `Jellyfin.Api/WebSocketListeners/FriendActivityWebSocketListener.cs` |
| **Interface** | `MediaBrowser.Controller/Friends/IFriendService.cs` |
| **Implementation** | `Emby.Server.Implementations/Friends/FriendService.cs` (428 lines) |
| **DTOs** | `MediaBrowser.Model/Friends/` — `FriendDto.cs`, `FriendRequestDto.cs`, `FriendWidgetDto.cs`, `NowPlayingFriendDto.cs` |
| **DB Entity** | `src/Jellyfin.Database/.../Entities/FriendRequest.cs` |
| **DB Enum** | `src/Jellyfin.Database/.../Enums/FriendRequestStatus.cs` |
| **DI** | `Jellyfin.Server/CoreAppHost.cs` line ~98 |

## API Endpoints (`/Friends`)

All endpoints require authorization.

| Method | Route | Purpose |
|---|---|---|
| `GET` | `/Friends` | Get current user's friend list with online status |
| `GET` | `/Friends/Requests` | Get pending friend requests (incoming + outgoing) |
| `POST` | `/Friends/Request` | Send a friend request (by username) |
| `POST` | `/Friends/Accept/{requestId}` | Accept a friend request |
| `POST` | `/Friends/Decline/{requestId}` | Decline a friend request |
| `DELETE` | `/Friends/{friendUserId}` | Remove a friend |

## Database Model

`FriendRequest` entity — a single table handles both requests and friendships:
- `Status = Pending` → open request
- `Status = Accepted` → active friendship (row serves double duty)
- `Status = Declined` → rejected request
- Bidirectional: queries check both `RequesterId` and `AddresseeId`

## Key Implementation Details

- **Online status**: Determined via `ISessionManager` — checks active sessions for friend's user ID.
- **Activity info**: Shows what friends are currently watching (`NowPlayingFriendDto`).
- **Notifications**: Sends `UserNotification` on friend request events via `IUserNotificationService`.
- **Real-time**: `FriendActivityWebSocketListener` pushes friend status changes over WebSocket.
- **Rate limiting**: Uses `TicksPerHour` constant for time-based checks.
