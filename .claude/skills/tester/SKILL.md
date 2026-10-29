---
name: tester
description: Write, update and run the integration tests for recent work in the UP backend (xUnit v3, real PostgreSQL and an S3 mock in Docker). Use ONLY when the user explicitly asks to test ("test it", "test your previous work", "write tests for…", "run the tests", /tester). Never invoke on your own after coding; the developer skill does not write or run tests.
---

Testing is on demand. Outside this skill, never write, rewrite or run tests. The only other place tests run is the `commit` skill's own gate. When the user asks to test, follow these steps in order.

## 1. Decide the scope

- If the user names something ("test the images delete endpoint"), test exactly that.
- Otherwise test the previous work: the uncommitted changes (`git status --short` and `git diff HEAD`). If the tree is clean, use the last commit (`git show --stat HEAD`).
- List the behaviours to cover in two to five short lines in the chat before writing anything. Each behaviour is something an API client can observe: status codes, response bodies, stored rows, stored files.

## 2. Write or update the tests

Rules from CLAUDE.md and the developer skill still apply (no comments, one name per concept, feature folders).

- **Place:** `tests/UP.Api.IntegrationTests/<Feature>/`, named after the use case (`CreateImagesTests`, `DeleteImagesTests`). Add to an existing class when the use case already has one.
- **Names:** underscores, behaviour as the client sees it, no articles: `Upload_for_missing_item_is_refused_and_saves_nothing`.
- **Assert through the API first,** then confirm state when it matters: rows with `QueryDatabaseAsync(db => ...)`, files with `ReadStoredFileAsync(key)` and `CountStoredFilesAsync(prefix)` (both in `TestHost/IntegrationTest`).
- **Never delete, skip or weaken** a test only to get a green run. Update tests that the change made wrong.
- **Test host:**
  - `TestHost/ApiFactory` starts `postgres:17-alpine` and `adobe/s3mock:5.2.3`. `minio/minio` is no longer on Docker Hub, so don't switch to it.
  - The factory runs the migrations, creates the `images` bucket, and sets `Options:Database`, `Options:Jwt` and `Options:R2:*`.
  - Environment is `Testing`, so no user secrets are needed.
- **Isolation:** all classes share one collection and run sequentially. Respawn clears the database before each test, but the S3 mock bucket is never cleared. Assert stored files by their unique key or by a per-entity prefix (`Product/{id}/`), never by the total count.
- **Helpers:**
  - Each feature keeps a `<Feature>Api` static class with request helpers and readers (`ImagesApi.CreateImagesAsync`, `ReadImagesAsync`).
  - Sign in with `Client.RegisterAndLoginAsync(Email, Password)`.
  - Seed rows directly with `QueryDatabaseAsync(db => { db.Products.Add(...); return db.SaveChangesAsync(CancellationToken); })`.
- **Images:**
  - Uploads must be real pictures. Build them with SkiaSharp (`ImagesApi.NewPhoto(width, height, format)`). For a phone photo with a rotation tag, use `NewPhoneTurnedRightPhoto`.
  - Check stored WebP files with `ImagesApi.DecodeWebp`.
  - For fake or broken files, use text bytes or a truncated JPEG.

## 3. Run

- Docker must be running (`docker info`). If it isn't, stop and ask the user to start it.
- If the user's API is running (Visual Studio locks `src/UP.Api/bin`), build and test into a scratch folder so nothing has to be stopped:

  ```sh
  dotnet test --solution UP.slnx --artifacts-path <scratchpad>/artifacts 2>&1 | grep -E "error|^failed|Exception:|should be|but was|total:|failed:|succeeded:"
  ```

  Otherwise plain `dotnet test --solution UP.slnx`.
- Then `dotnet format UP.slnx --verify-no-changes` for the test files.

## 4. Manual checks against the running API (only if the user asks)

- The API is at `https://localhost:7215`. `curl -sk` works for GET endpoints, which are public. Endpoints that write need a JWT from `POST /api/auth/login`.
- Never write to the user's database or R2 bucket without being asked. A pending migration shows as `relation "…" does not exist`. Applying it (`dotnet ef database update ... --configuration Release`) changes the user's database, so ask first.

## 5. When something fails

- A wrong test: fix the test.
- A real bug in the code under test: do not change product code silently. Report the failing behaviour, the cause and the proposed fix, and ask before fixing.
- A check that cannot run (Docker off, image pull fails): say which and why.

## 6. Report

| Check | Status | Details |
| --- | --- | --- |
| Tests written / updated | ✅ | files and what they cover |
| Integration tests | ✅ / ❌ | N passed, or each failure with its first message line |
| Format | ✅ / ❌ | files, if any |

Then list anything left untested and why. Never claim something works without a run you actually did.
