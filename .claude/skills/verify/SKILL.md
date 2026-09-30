---
name: verify
description: Verify the UP solution is ready to commit — build, code style, and EF model/migration sync. Use when the user asks to verify, check, or validate changes, or before committing.
---

Run all three checks from the repo root, even if an earlier one fails, so the report is complete. This skill only reports; it does not change files.

| # | Check | Command |
|---|---|---|
| 1 | Build (compile + analyzers; warnings are errors) | `dotnet build UP.slnx -nologo -v quiet` |
| 2 | Style (`.editorconfig`) | `dotnet format UP.slnx --verify-no-changes -v quiet` |
| 3 | Migrations in sync with the EF model | `dotnet ef migrations has-pending-model-changes --project src/UP.Infrastructure --startup-project src/UP.Api --no-build` |

- Check 3 needs a successful build. If check 1 failed, mark it ⏭️ skipped. If `dotnet ef` is missing, run `dotnet tool restore` first.
- Collect each error as file, line, code and message. Build output repeats errors, so remove duplicates. Make paths repo-relative.

## Report

**Checklist**

| Check | Status | Details |
|---|---|---|
| Build | ✅ / ❌ | N errors |
| Style | ✅ / ❌ | N issues (`dotnet format UP.slnx` fixes most) |
| Migrations | ✅ / ❌ / ⏭️ | "pending model changes" or "skipped: build failed" |

If anything failed, add an **Errors** table with one row per error, build errors first:

| Type | Location | Code | Message |
|---|---|---|---|
| Build | `src/UP.Api/Auth/AuthController.cs:42` | CS0103 | The name 'x' does not exist in the current context |
| Style | `src/UP.Infrastructure/Auth/AuthService.cs:18` | IDE0055 | Fix formatting |

After the tables, briefly suggest a fix for each error. If migrations are pending, suggest the `dotnet ef migrations add` command from CLAUDE.md, but do not run it.
