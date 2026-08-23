# Conventions & Patterns

## DI Registration

All custom service registrations go in `Jellyfin.Server/CoreAppHost.cs` → `RegisterServices()`:

```csharp
serviceCollection.AddSingleton<IFriendService, FriendService>();
serviceCollection.AddSingleton<ITrendingService, TrendingService>();
serviceCollection.AddSingleton<IReleaseCalendarService, ReleaseCalendarService>();
```

Register as `Singleton` for stateless or cached services. Use `Scoped` for per-request DB work (though most services get a `IDbContextFactory` and create their own scopes).

## API Controller Patterns

- Inherit from `BaseJellyfinApiController` (not `ControllerBase` directly).
- Use `[Route("FeatureName")]` and `[Authorize]` at class level.
- Use primary constructors for DI injection (new C# pattern used in fork additions).
- Always use `[ProducesResponseType]` attributes.
- Get current user ID via `User.GetUserId()` extension.
- Use `CancellationToken` parameters and `.ConfigureAwait(false)`.
- Admin-only endpoints use `[Authorize(Policy = Policies.RequiresElevation)]`.

## Code Style

- File-scoped namespaces (`namespace X;` not `namespace X { }`).
- XML doc comments on all public members.
- StyleCop enforced (see `stylecop.json`).
- EditorConfig rules in `.editorconfig`.
- Banned symbols listed in `BannedSymbols.txt`.

## Configuration Pattern

Server configuration extends `BaseApplicationConfiguration`. Custom config sections:
- `TrendingConfiguration` — TMDB API key, toggle
- `ReleaseCalendarConfiguration` — Radarr/Sonarr URLs and API keys
- Announcements — stored in server configuration

Access via `IServerConfigurationManager` → `.Configuration`.

## Caching Pattern

Memory cache (`IMemoryCache`) used for external API results (Trending, ReleaseCalendar). Cache keys prefixed with feature name (e.g. `Trending_`, `ReleaseCalendar_`).

## External API Integration Pattern

- Use `IHttpClientFactory` (never `new HttpClient()`).
- Use `System.Text.Json` with `PropertyNameCaseInsensitive = true`.
- Wrap in try/catch, log errors, return empty results on failure.
