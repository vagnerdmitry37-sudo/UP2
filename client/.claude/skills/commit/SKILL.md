---
name: commit
description: Commit changes to the Angular client in client/. Auto-formats with Prettier, then checks lint, build and tests, and commits only if everything passes; otherwise stops and reports a checklist table. Use whenever the user asks to commit and the changes are under client/. Never pushes.
---

Run git commands from the repo root and npm commands from `client/`. Run every step in order. Never skip a check, never use `--no-verify`, and never push.

## 1. Inspect the changes

- Run `git status --short` and `git diff HEAD --stat`. If nothing changed, say so and stop.
- **Scope:** if some files are already staged, commit only those. Otherwise commit all changes.
- **Changes outside `client/`:** if any changed file is outside `client/`, the root `commit` skill also applies. Follow it for those files and run this skill's checks for the client files, then make **one** commit with one combined report.
- **Never stage** `node_modules/`, `dist/`, `.angular/`, `coverage/`, `.env*` or other local files. If one shows up, leave it out and mention it in the report.
- If `client/node_modules` is missing, run `npm ci` first.

## 2. Auto-fix formatting

Run `npm run format`, then `git status --short` again:

- A reformatted file that is **in scope** is staged with the commit.
- A reformatted file that is **out of scope** stays unstaged. List it in the report.

## 3. Verify

Run every check, even if an earlier one fails, so the report is complete. This step only reports and changes no files.

| #   | Check                                                        | Command                |
| --- | ------------------------------------------------------------ | ---------------------- |
| 1   | Format (Prettier)                                            | `npm run format:check` |
| 2   | Lint (ESLint, zero warnings, layer import rules)             | `npm run lint`         |
| 3   | Build (strict TypeScript, `strictTemplates`, bundle budgets) | `npm run build`        |
| 4   | Unit tests (Vitest)                                          | `npm test`             |

- **Tests:** if `client/src` has no `*.spec.ts` files, mark the check ⏭️ "no test files" instead of running it, because the runner errors when there is nothing to run.
- **No comments:** scan the diff of hand-written `.ts`, `.html` and `.scss` files for `//`, `/*` or `<!--`. Any hit fails this check (the rule is in CLAUDE.md, and lint can't enforce it).
- Collect each error as file, line, rule/code and message, with repo-relative paths and duplicates removed.

## 4. If any check failed: stop, do not commit

Leave the working tree as it is, including the formatting changes. Reply with the report below, with `Commit | ❌ | Not created`, plus a short suggested fix for each error.

## 5. If every check passed: commit

1. Stage the in-scope files and in-scope formatting fixes **by explicit path** (not `git add -A`). New files that belong in git include `package-lock.json`.
2. Write the message from `git diff --cached`:
   - **Subject: at most 100 characters**, imperative mood, no trailing period. Prefix `client:`, e.g. `client: add login page with reactive form`.
   - Add a body only if the change needs explaining, wrapped at about 100 characters.
   - End with the attribution trailer required by the session's instructions, if any.
3. Check the subject length with `printf '%s' "<subject>" | wc -m`, and shorten it if it is over 100.
4. Commit using a heredoc, then run `git log -1 --stat` to confirm.

## Report

**Checklist**

| Check                  | Status       | Details                                   |
| ---------------------- | ------------ | ----------------------------------------- |
| Auto-format (Prettier) | 🔧 / ✅      | "N files reformatted" or "no changes"     |
| Format                 | ✅ / ❌      | N files                                   |
| Lint                   | ✅ / ❌      | N errors                                  |
| Build                  | ✅ / ❌      | N errors                                  |
| Tests                  | ✅ / ❌ / ⏭️ | "N passed", "N failed" or "no test files" |
| No comments            | ✅ / ❌      | N found                                   |
| Commit                 | ✅ / ❌      | short hash, or "Not created"              |

If anything failed, add an **Errors** table:

| Type  | Location                       | Rule / Code           | Message                                |
| ----- | ------------------------------ | --------------------- | -------------------------------------- |
| Lint  | `client/src/app/shared/x.ts:1` | no-restricted-imports | This layer must not import from core/. |
| Build | `client/src/app/app.ts:4`      | TS2307                | Cannot find module '@layout/shel'      |

On success, reply with the Checklist, the commit subject, and any files left out with the reason.
