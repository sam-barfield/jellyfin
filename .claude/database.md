# Database & Migrations

## DbContext

- **Location**: `src/Jellyfin.Database/Jellyfin.Database.Implementations/JellyfinDbContext.cs`
- Uses `IDbContextFactory<JellyfinDbContext>` for scoped access (injected into services).
- Supports SQLite (default) and experimental PostgreSQL.

## Entities

Located in `src/Jellyfin.Database/Jellyfin.Database.Implementations/Entities/`.

### Custom Fork Entities

| Entity | File | Purpose |
|---|---|---|
| `FriendRequest` | `Entities/FriendRequest.cs` | Friend request/friendship record. `Accepted` status doubles as the friendship itself. Fields: `RequesterId`, `AddresseeId`, `Status`, `CreatedAt`, `UpdatedAt`. Nav props to `User`. |
| `UserNotification` | `Entities/UserNotification.cs` | Per-user notification with `Type` (enum), `IsRead`, optional JSON `Data` payload. |

### Key Upstream Entities

`User`, `BaseItemEntity`, `DisplayPreferences`, `ActivityLog`, `MediaSegment`, `TrickplayInfo`, `Permission`, `Preference`, etc.

## Enums (Custom)

Located in `src/Jellyfin.Database/Jellyfin.Database.Implementations/Enums/`:

- `FriendRequestStatus` — States for friend requests (Pending, Accepted, Declined, etc.)
- `NotificationType` — Types of user notifications

## Creating Migrations

From the repo root:

```bash
dotnet ef migrations add {MIGRATION_NAME} \
  --project "src/Jellyfin.Database/Jellyfin.Database.Providers.Sqlite" \
  -- --migration-provider Jellyfin-SQLite
```

If you get "dotnet-ef not found", run `dotnet tool restore` first.

## Model Configuration

EF Core model configuration (fluent API) lives in:
`src/Jellyfin.Database/Jellyfin.Database.Implementations/ModelConfiguration/`
