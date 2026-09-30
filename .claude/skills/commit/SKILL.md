---
name: commit
description: Commit the current changes after auto-formatting and running the verify skill. Stops and reports a checklist table if anything fails. Use when the user runs /commit or asks to commit.
disable-model-invocation: true
---

Run every step from the repo root, in order. Never skip verification, never use `--no-verify`, and never push.

## 1. Inspect the changes

- Run `git status --short` and `git diff HEAD --stat`. If there is nothing to commit, say so and stop.
- **Scope:** if some files are already staged, commit only those. Otherwise commit all changes shown by `git status`.
- **Never stage** files that may hold secrets or local state (`.env*`, `*.user`, `secrets.json`, `appsettings.*.local.json`, `bin/`, `obj/`). If one appears in the list, leave it out and mention it in the report.

## 2. Auto-fix formatting

Run `dotnet format UP.slnx`. This fixes whitespace, `.editorconfig` style and fixable analyzer issues.

Afterwards, run `git status --short` again and note which files it changed:
- A changed file that is **in scope** gets staged with the commit.
- A changed file that is **out of scope** (was clean, or unstaged while the user staged a subset) is left unstaged. List it in the report.

## 3. Verify

Invoke the `verify` skill (Skill tool, `skill: "verify"`) and follow it. It runs build, style and migration checks and produces the Checklist and Errors tables.

In the Checklist table, add two rows around the verify rows:
- first row: `Auto-format (dotnet format)`, with 🔧 and "N files reformatted", or ✅ and "no changes"
- last row: `Commit`, with its result

## 4. If any check failed: stop, do not commit

Leave the working tree as it is (including the auto-format changes). Reply with the verify report (Checklist with `Commit | ❌ | Not created`, the Errors table, and suggested fixes).

## 5. If every check passed: commit

1. Stage the in-scope files from step 1 and in-scope formatting fixes from step 2 by explicit path (not `git add -A`).
2. Write the commit message from the staged diff (`git diff --cached`):
   - **Subject line: at most 100 characters.** It says what changed and why it matters. Use imperative mood ("add", "fix", "rename"), no trailing period.
   - Follow the repo style: an optional feature/phase prefix, e.g. `Phase 3: Refresh tokens - add RefreshAsync` or `auth: revoke tokens on password change`.
   - Avoid vague subjects like "update files", "fixes" or "wip".
   - Add a body (after a blank line) only if the change needs more explanation. Wrap body lines at about 100 characters.
   - End with the attribution trailer required by the session's instructions, if any.
3. Before committing, check the subject length with `printf '%s' "<subject>" | wc -m`. If it is over 100, shorten it.
4. Commit with a heredoc so the message is passed exactly, then run `git log -1 --stat` to confirm.

Reply with the Checklist table (all ✅, `Commit | ✅ | <short hash>`), the commit subject, and any files left out of the commit with the reason.
