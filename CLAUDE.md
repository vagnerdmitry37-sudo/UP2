# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Stack

ASP.NET Core Web API on .NET 10 (SDK pinned in `global.json`), EF Core + Npgsql (PostgreSQL), ASP.NET Core Identity, JWT bearer auth. Solution file is `UP.slnx`.

## Commands

```bash
dotnet build UP.slnx                       # build (warnings are errors, see below)
dotnet run --project src/UP.Api            # run the API
dotnet format UP.slnx                      # apply .editorconfig style fixes

dotnet tool restore                        # one-time: installs pinned dotnet-ef
dotnet ef migrations add <Name> --project src/UP.Infrastructure --startup-project src/UP.Api --output-dir Persistence/Migrations
dotnet ef migrations remove --project src/UP.Infrastructure --startup-project src/UP.Api
dotnet ef database update --project src/UP.Infrastructure --startup-project src/UP.Api
dotnet format UP.slnx --verify-no-changes
```

### Required secrets

The app fails at startup (`ValidateOnStart`) without these. They are not in `appsettings.json`; set them via user secrets on `UP.Api`:

```bash
dotnet user-secrets set "Options:Jwt:Key" "<at least 32 chars>" --project src/UP.Api
dotnet user-secrets set "Options:Database:ConnectionString" "Host=localhost;Port=5432;Database=up;Username=up;Password=<pw>" --project src/UP.Api
```

## Build rules

`Directory.Build.props` enables `TreatWarningsAsErrors`, `Nullable`, `AnalysisLevel=latest-recommended` and `EnforceCodeStyleInBuild`, so analyzer and `.editorconfig` warnings break the build. Migration files are marked `generated_code = true` in `.editorconfig` to stay exempt.

Package versions are managed centrally in `Directory.Packages.props`: add `<PackageVersion>` there and a version-less `<PackageReference>` in the `.csproj`.

## Architecture

Clean Architecture: four projects under `src/`. Dependencies point inward only, enforced by project references:

```
UP.Api  →  UP.Infrastructure  →  UP.Application  →  UP.Domain
```

A project may use only the projects to its right. Why: the outer layers (web, database, libraries) change more often than the inner ones, so inner code must never depend on them.

### Layers

| Project | Responsibility | May contain | Must not contain |
|---|---|---|---|
| **UP.Domain** | Business concepts and rules that hold regardless of storage or transport | Entities, value objects, pure logic | Any package or project reference |
| **UP.Application** | What the app can do, as contracts | Service interfaces and the result types they return | EF Core, Identity, JWT, anything from ASP.NET Core HTTP |
| **UP.Infrastructure** | How it is done, with concrete libraries | Implementations of Application interfaces, EF entities and configurations, options classes | HTTP types (cookies, action results, `HttpContext`) |
| **UP.Api** | Translating HTTP to service calls and back | Controllers, route constants, request/response records, cookie and claims helpers | Business logic, direct database access |

Quick placement test: uses HTTP → Api. Uses EF, Identity or JWT → Infrastructure. A contract or result that both sides share → Application. A rule that needs none of these → Domain.

### Folder rules

- **Group by feature, not by type.** Inside each project, code lives in a feature folder (`Auth/`, and later `Orders/` and so on). One feature therefore has a folder in several projects, each holding only that layer's part. Why: a change to a feature touches one known folder per layer.
- **Keep feature folders flat.** No type subfolders such as `Services/`, `Requests/` or `Responses/`. Why: file names already state the type, and small type folders spread one change across many places. Split a large feature by sub-feature (use case) only when it becomes hard to scan.
- **Create only the layer folders a feature needs.** A feature with no HTTP surface has no Api folder.
- **Shared code sits outside feature folders.** Code used by many features gets its own top-level folder named after what it is. It never goes inside the first feature that needed it. Examples: `Infrastructure/Persistence/` (the single `AppDbContext`, migrations) and `Infrastructure/Identity/` (the user entity and user manager).

### Boundary rules

- **Wire formats stay in Api.** Request/response records describe JSON shape and validation attributes. Application returns its own result types, and the controller maps them to status codes. Why: the HTTP contract and the service contract can change independently.
- **Infrastructure is hidden.** Its classes are `internal sealed` and reachable only through Application interfaces. The only public entry point is `AddInfrastructure()` in `DependencyInjection.cs`, where every implementation and options class is registered.
- **Controllers are thin.** Read input, call a service, translate the result to HTTP. Route strings live in a per-feature `*Routes` constants class, not inline.

Typical request flow: controller → Application interface → Infrastructure implementation (database, Identity, token generation) → Application result type → controller maps it to an HTTP response.

### Configuration pattern

All settings live under the `Options:` section (`Options:Database`, `Options:Jwt`, `Options:RefreshToken`). Each has an options class with a `SectionName` const, bound in `DependencyInjection` with `.BindConfiguration(...)`, `.Validate(...)` rules and `.ValidateOnStart()`. Follow this pattern for new settings.

### Auth design (built in phases, see commit history)

- Access tokens: short-lived HS256 JWTs. `MapInboundClaims = false`; name claim is `sub`, role claim is `JwtClaimTypes.Role`.
- Refresh tokens: random 64-byte values; only the SHA-256 hash is stored (`RefreshTokens` table). Tokens belong to a **family** (`FamilyId`). Every refresh rotates the token. Reusing a revoked token revokes the whole family (theft detection). A missing user also revokes the family. Lockout only blocks new logins and never ends existing sessions, so failed passwords can't be used to force someone out.
- Rotation uses a conditional `ExecuteUpdate` (`RevokedAt == null`) inside a transaction run through `CreateExecutionStrategy()`. This is required because `EnableRetryOnFailure` is on. It guarantees only one concurrent refresh wins.
- Rotation and every revocation (family or user-wide) take a per-user Postgres advisory lock (`pg_advisory_xact_lock`) inside their transaction. Without it, a revocation cannot see the replacement row of a concurrent rotation, and that token would survive logout.
- The refresh token travels only in an HttpOnly, `SameSite=Strict` `__Secure-refresh_token` cookie (`RefreshTokenCookie`). On a failed refresh the controller deliberately does not clear the cookie.
- Logout is anonymous (it must work with an expired access token), revokes the cookie's family and always returns 204. It deletes the cookie only when the request carried one, so a cross-site POST cannot log the user out. The access token stays valid until it expires.
- Logout-all is also cookie-based but requires a live (not revoked, not expired) token, so an old leaked token can't sign the user out everywhere. It revokes all of the user's refresh tokens and answers 401 otherwise.
- `/me` is `[Authorize]` and reads the ID and email from the access-token claims only (no database), so it can be up to 15 min stale after logout or an email change.
- In `AuthController`, `[AllowAnonymous]` goes on each anonymous action, never on the class: a class-level one overrides `[Authorize]` and would make `/me` public.
- Current phase (4) is complete: register, login, refresh, logout, logout-all and me are implemented.

### Conventions

- Inject `TimeProvider` for current time rather than using `DateTimeOffset.UtcNow`; entity IDs use `Guid.CreateVersion7`.
- Logging uses source-generated `[LoggerMessage]` methods, not `logger.LogX(...)` calls. Each feature keeps them in one `<Feature>Log` class as `ILogger` extension methods with unique event IDs (see `Infrastructure/Auth/AuthLog.cs`: Auth owns 1000–1099).
- Code describes itself through names. Do not add `/// <summary>` blocks that restate what a type holds, or section-divider comments (`// Registration: 1000–1019`). Comment only a non-obvious *why*, such as a security or concurrency constraint.
