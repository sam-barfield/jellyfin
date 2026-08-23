# Architecture Overview

## Layered Architecture

The codebase follows a **clean architecture** pattern with clear dependency flow:

```
Jellyfin.Server (entrypoint, DI, Startup)
    ↓
Jellyfin.Api (REST controllers, middleware, auth)
    ↓
Emby.Server.Implementations (service implementations)
    ↓
MediaBrowser.Controller (interfaces / abstractions)
    ↓
MediaBrowser.Model (DTOs, enums, shared models — no logic)
    ↓
Jellyfin.Database (EF Core DbContext, entities, migrations)
```

## Key Dependency Rules

- **MediaBrowser.Model**: Pure data types. No dependencies on other Jellyfin projects. Referenced by everything.
- **MediaBrowser.Controller**: Defines service interfaces (`IFriendService`, `ITrendingService`, etc.). Depends on Model.
- **Emby.Server.Implementations**: Implements the interfaces from Controller. This is where business logic lives.
- **Jellyfin.Api**: ASP.NET Core controllers. Injects interfaces from Controller, never references Implementations directly.
- **Jellyfin.Server**: Wires everything together via DI in `CoreAppHost.cs` and `Startup.cs`.

## Adding a New Feature (Pattern)

When adding a new server-side feature, follow this pattern:

1. **Model DTOs** → `MediaBrowser.Model/<FeatureName>/`
2. **Interface** → `MediaBrowser.Controller/<FeatureName>/I<Feature>Service.cs`
3. **Implementation** → `Emby.Server.Implementations/<FeatureName>/<Feature>Service.cs`
4. **API Controller** → `Jellyfin.Api/Controllers/<Feature>Controller.cs`
5. **DI Registration** → `Jellyfin.Server/CoreAppHost.cs` — `RegisterServices()`
6. **DB Entity** (if needed) → `src/Jellyfin.Database/Jellyfin.Database.Implementations/Entities/`
7. **DB Enum** (if needed) → `src/Jellyfin.Database/Jellyfin.Database.Implementations/Enums/`
