# MyCal

A private calorie and macro tracker built with .NET 10, Blazor, ASP.NET Core Identity, and PostgreSQL. Supports at least five independent accounts, with invite-only access and private diaries. Hosting is deferred while we validate local use.

## Run locally

Install the .NET 10 SDK, Docker Desktop, and Python 3. Start Docker, then run:

```sh
./scripts/start-local.sh
```

Open **http://localhost:5099**. Your generated local administrator email and password are in `.local/admin-credentials.txt` (ignored by Git, readable only by your user). Set up your profile after signing in. Settings → Manage beta invitations creates single-use invitations or recovery links to share privately.

PostgreSQL uses a persistent Docker volume and binds only to `127.0.0.1:55432`. This is separate from the previous Aspire database on port 51885. The development database password is intentionally local-only; do not reuse it for hosting. Stop the web server with Ctrl+C. `docker compose -f compose.local.yml stop` stops PostgreSQL without deleting your data. Do not use `down --volumes` unless you intend to erase the local database.

The browser scanner works on localhost or an HTTPS origin. A phone reaching your computer over ordinary HTTP will not get camera access; use HTTPS for real-device testing. There is always manual barcode entry and custom-food logging. Home-screen installation remains online: the app does not cache private diaries for offline use.

### Food search

Custom foods, recents, favourites, and Open Food Facts work without a USDA key. For generic-food search, configure your own key locally:

```sh
dotnet user-secrets set --project MyCal.Web "UsdaFoodData:ApiKey" "YOUR_KEY"
```

Restart the app. During development, the app also reuses the nutrition key from the original API's user-secret store, if present; it does not import the old database settings. Keys are sent to USDA in an HTTP header. No real credentials belong in source control.

Food data comes from [USDA FoodData Central](https://fdc.nal.usda.gov/) and [Open Food Facts](https://world.openfoodfacts.org/) (ODbL). Keep this attribution when reusing the interface. The latter is community-maintained: verify package values and whether the label is per 100 g or per 100 ml. The app requires that unit check before logging a packaged result. Missing nutrients stay unknown, never silently zero. Camera decoding uses a bundled [ZXing browser library](https://github.com/zxing-js/browser); its license is included with the asset.

## Daily use and calculations

Select a date, log food by meal, and edit, delete, or copy entries. Mark a day complete only after all food is logged. An empty day requires an explicit zero-intake confirmation; unlogged days are not interpreted as fasting. Any entry change reopens that day.

Each entry stores its nutrition snapshot. Target revisions begin the next local day. Changing a catalogue food, profile, or time zone does not rewrite previous diary dates or targets. Existing logs are migrated using their recorded values; old macros that cannot be reconstructed are marked unverified. Historical targets are not invented.

Starting estimates use [Mifflin–St Jeor](https://pubmed.ncbi.nlm.nih.gov/2305711/) and the chosen activity multiplier. Defaults are a 15% deficit for loss, estimated maintenance for maintenance, or a 5% surplus for gain. Protein starts at 1.6 g/kg, fat at 30% of calories, and carbohydrate receives the remaining energy. Targets are editable. These are starting estimates, not measured energy expenditure. Declared food calories are preserved separately from 4/4/9 calculations because fibre, alcohol, rounding, and other constituents can explain differences.

The beta's weekly coaching rule supports fat loss. It requires 28 completed days on an unchanged target, at least three weigh-ins each week, and average intake within 10% of target. It compares two fortnightly weight averages and proposes a 100 kcal change outside the 0.25–0.75% weekly loss range. Suggested cuts stop at 1,500 kcal or a 25% estimated deficit. These limits are conservative application choices, not universal medical thresholds. The rule is transparent and tested, but is not a clinically validated reproduction of MacroFactor. Every change requires acceptance; stale or already accepted suggestions cannot be applied again.

## Verify changes

```sh
./scripts/test-local.sh
```

The integration suite creates randomly named disposable databases. It does not clear the development database. It covers calculations, date boundaries, migrations, snapshots, account isolation, five real logins, ten concurrent sessions, completion states, invitation expiry/reuse, and coaching boundaries.

For browser tests, start the local app first, then:

```sh
cd MyCal.Web/ClientAssets
npm ci
npx playwright install chromium webkit
npx playwright test
```

Browser journeys create temporary accounts with `mycal-e2e-` email addresses in the development database. Chromium and mobile WebKit cover logging, edits, history, target changes, themes, and camera-denial fallback. This is browser-engine testing; real camera focus and barcode recognition still need a physical-device check. Screenshots and reports stay in `.local/`. Run `./scripts/cleanup-browser-tests.sh` from the repository root afterwards to remove only those test accounts and invitations.

After changing the scanner dependency, run `npm run bundle` in `MyCal.Web/ClientAssets` and commit both the lockfile and bundled asset. The application runs without Node; Node is used only for asset maintenance and browser tests.

GitHub Actions builds the solution, runs the PostgreSQL integration suite, and checks desktop Chromium and mobile WebKit on every PR. Review the backend foundation before its dependent UI PR; do not merge the UI changes independently of that foundation.

## Back up local data

```sh
./scripts/backup-local.sh
./scripts/verify-backup-local.sh .local/backups/TIMESTAMP
```

The verification script restores both dumps into disposable databases and compares data fingerprints. Stop writing to the app between backup and verification so the comparison is meaningful. Keep a copy of backups outside this computer before relying on the app for long-term data. `.local/` contains private credentials, reports, and database dumps and must remain out of Git.

## Code map

- **Domain / Shared:** stored entities, enums, and explicit request/response contracts.
- **Application:** nutrition math, coaching policy, and account-scoped tracking operations. PostgreSQL account locks serialize writes; every operation creates its own database context.
- **Infrastructure:** provider adapters, Identity, and both databases' migrations. Migration IDs and account IDs are preserved from the prototype.
- **Web:** the single running host, cookie-protected APIs, Razor Pages for login/password setup, and Blazor components. Larger components keep their logic in adjacent `.razor.cs` files.
- **AppHost:** optional Aspire orchestration of Web and PostgreSQL. The old API and authentication service launchers are retired to prevent accidental use of their prototype endpoints.

Production hosting requires HTTPS ingress, private database access, persistent protected data-protection keys, externally stored backups, and a verified restore. No hosting resources are provisioned by local setup.
