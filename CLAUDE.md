# Samflix — Custom Jellyfin Fork

This is a **custom fork** of the [Jellyfin](https://github.com/jellyfin/jellyfin) media server (v10.11.11), tailored for a self-hosted "Samflix" setup. It is a **.NET 9 / C#** backend server.

## Quick Facts

| Key | Value |
|---|---|
| Base | Jellyfin 10.11.11 |
| Runtime | .NET 9 |
| DB | EF Core with SQLite (default), experimental PostgreSQL |
| Solution | `Jellyfin.sln` |
| Entrypoint | `Jellyfin.Server/Program.cs` |
| Web UI | Hosted from `Jellyfin.Server/wwwroot` (separate repo: jellyfin-web) |
| Build | `dotnet build` / `dotnet run --project Jellyfin.Server` |
| Tests | `dotnet test` |
| API Docs | `http://localhost:8096/api-docs/swagger/index.html` |

## Custom Features (Fork Additions)

This fork adds **4 major features** on top of upstream Jellyfin. Read the detailed docs before touching any of them:

| Feature | Context File |
|---|---|
| 🤝 Friends System | [.claude/features/friends.md](.claude/features/friends.md) |
| 📈 Trending (TMDB) | [.claude/features/trending.md](.claude/features/trending.md) |
| 📅 Release Calendar (Radarr/Sonarr) | [.claude/features/release-calendar.md](.claude/features/release-calendar.md) |
| 📢 Announcements | [.claude/features/announcements.md](.claude/features/announcements.md) |

## Architecture Overview

Read this when you need to understand how the codebase is structured:

→ [.claude/architecture.md](.claude/architecture.md)

## Project-by-Project Guide

Read this to understand what each project/folder in the solution does:

→ [.claude/projects.md](.claude/projects.md)

## Database & Migrations

Read this when working with EF Core entities, the DbContext, or creating migrations:

→ [.claude/database.md](.claude/database.md)

## Conventions & Patterns

Read this before writing new code — covers DI patterns, API controller conventions, coding style:

→ [.claude/conventions.md](.claude/conventions.md)
