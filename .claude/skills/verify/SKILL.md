---
name: verify
description: Verify the UP solution is ready to commit — build, code style, and EF model/migration sync. Use when the user asks to verify, check, or validate changes, or before committing.
---

Run these checks in order from the repo root. Stop at the first failure, show the relevant errors, and propose a fix.

1. Build: `dotnet build UP.slnx` — warnings are errors here, so any analyzer warning fails.
2. Style: `dotnet format UP.slnx --verify-no-changes`
3. Migrations in sync: `dotnet ef migrations has-pending-model-changes --project src/UP.Infrastructure --startup-project src/UP.Api`
   If it reports pending changes, a model change is missing its migration — suggest the `migrations add` command from CLAUDE.md, do not run it.

Finish with a short table: check → ✅/❌.