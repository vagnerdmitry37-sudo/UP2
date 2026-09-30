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

Clean Architecture, four projects under `src/`. Code is grouped by feature folder (e.g. `Auth/`) inside each project:

- **UP.Domain** → no dependencies.
- **UP.Application** → interfaces + DTOs only (e.g. IRefreshTokenService). No EF Core.
- **UP.Infrastructure** → implementations are `internal sealed`, registered in DependencyInjection.cs.
- **UP.Api** → thin controllers; routes live in AuthRoutes constants.

### Configuration pattern

All settings live under the `Options:` section (`Options:Database`, `Options:Jwt`, `Options:RefreshToken`). Each has an options class with a `SectionName` const, bound in `DependencyInjection` with `.BindConfiguration(...)`, `.Validate(...)` rules and `.ValidateOnStart()`. Follow this pattern for new settings.

### Auth design (built in phases, see commit history)

- Access tokens: short-lived HS256 JWTs. `MapInboundClaims = false`; name claim is `sub`, role claim is `JwtClaimTypes.Role`.
- Refresh tokens: random 64-byte values; only the SHA-256 hash is stored (`RefreshTokens` table). Tokens belong to a **family** (`FamilyId`). Every refresh rotates the token. Reusing a revoked token revokes the whole family (theft detection). A missing or locked-out user also revokes the family.
- Rotation uses a conditional `ExecuteUpdate` (`RevokedAt == null`) inside a transaction run through `CreateExecutionStrategy()`. This is required because `EnableRetryOnFailure` is on. It guarantees only one concurrent refresh wins.
- The refresh token travels only in an HttpOnly, `SameSite=Strict` `__Secure-refresh_token` cookie (`RefreshTokenCookie`). On a failed refresh the controller deliberately does not clear the cookie.
- Current phase (3): `POST api/auth/refresh` is implemented. `AuthRoutes.Login` and `AuthRoutes.Logout` are declared but have no endpoints yet.

### Conventions

- Inject `TimeProvider` for current time rather than using `DateTimeOffset.UtcNow`; entity IDs use `Guid.CreateVersion7`.
- Logging uses source-generated `[LoggerMessage]` methods (see `RefreshTokenService`), not `logger.LogX(...)` calls.
