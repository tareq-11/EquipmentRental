# Milestone 0A Manual Testing Evidence

All cases use manual evidence because the approved MVP excludes automated test projects. Entries marked `Planned` are not treated as exit-criterion evidence.

## M00A-001: Solution dependency boundaries

| Field | Value |
| --- | --- |
| ID | M00A-001 |
| Preconditions | .NET SDK 10.0.107 installed. |
| Steps | Run `dotnet restore EquipmentRental.slnx` and `dotnet build EquipmentRental.slnx --no-restore`; inspect project references. |
| Expected Result | All five projects build. `Services` references `Core`; `infrastructure` references `Services` and `Core`; `Api` references `Services`, `infrastructure`, and `Shared`. |
| Actual Result | `dotnet restore EquipmentRental.slnx` completed with all projects up to date. `dotnet build EquipmentRental.slnx --no-restore` built `Shared`, `Core`, `Services`, `infrastructure`, and `Api`; result: `Build succeeded`, 0 warnings, 0 errors, elapsed 2.15 seconds. Project references match the expected dependency direction. |
| Status | Passed |
| Evidence | .NET SDK `10.0.107`. Restore exit code 0. Build exit code 0. `Services/Services.csproj` references `Core`; `infrastructure/infrastructure.csproj` references `Core` and `Services`; `Api/Api.csproj` references `Services`, `infrastructure`, and `Shared`. `dotnet tool restore` also succeeded and restored `dotnet-ef` `10.0.7`. |
| Tested At | 2026-07-15T00:44:00+03:00 |

## M00A-002: Docker, migration, and database health

| Field | Value |
| --- | --- |
| ID | M00A-002 |
| Preconditions | Docker Compose available; local `.env` created from `.env.example`; `ConnectionStrings__EquipmentRental` exported. |
| Steps | Run `docker compose up -d`, `docker compose ps`, and the documented EF database update command; start the API and request `/health`. |
| Expected Result | PostgreSQL becomes healthy, `InitialCreate` applies, `/health` returns HTTP 200, and Serilog logs the request. |
| Actual Result | `docker compose ps` reported `equipmentrental-postgres-1` healthy at `127.0.0.1:5433->5432/tcp`; `pg_isready` returned `/var/run/postgresql:5432 - accepting connections`. `dotnet tool run dotnet-ef database update --project infrastructure/infrastructure.csproj --startup-project Api/Api.csproj` built successfully and returned `No migrations were applied. The database is already up to date.` An isolated Development API instance returned HTTP 200 and body `Healthy` from `/health`. |
| Status | Passed |
| Evidence | PostgreSQL `postgres:17-alpine` is healthy and loopback-only on `127.0.0.1:5433`. EF update exit code 0; migration `20260714194025_InitialCreate` is current. Health request returned 200. Retained Serilog output at `/tmp/opencode/equipment-rental-m00a-api.log` records `GET /health` responses with HTTP 200. |
| Tested At | 2026-07-15T00:48:18+03:00 |

## M00A-003: Development API surface

| Field | Value |
| --- | --- |
| ID | M00A-003 |
| Preconditions | M00A-002 completed; API running in `Development`. |
| Steps | Open `/swagger`; request `/health` with Origin `http://localhost:3000`; inspect response headers and console. |
| Expected Result | Swagger UI loads; health succeeds; Development CORS accepts the web origin; response compression and request logging middleware are active. |
| Actual Result | An isolated Development API instance returned HTTP 200 from `/health`, `/swagger/index.html`, and `/swagger/v1/swagger.json`. A health request with `Origin: http://localhost:3000` returned HTTP 200 and `Access-Control-Allow-Origin: http://localhost:3000`. A Swagger request with `Accept-Encoding: gzip` returned HTTP 200 with `Content-Encoding: gzip` and `Vary: Accept-Encoding`. |
| Status | Passed |
| Evidence | Exact curl targets on the isolated verification listener: `http://127.0.0.1:5063/health`, `/swagger/index.html`, and `/swagger/v1/swagger.json`; each returned 200. Swagger UI reported `x-swagger-ui-version: 5.30.2`; OpenAPI returned `application/json;charset=utf-8`. Retained Serilog output at `/tmp/opencode/equipment-rental-m00a-api.log` records successful GET requests for health, Swagger UI, and OpenAPI. The normal Development launch profile remains on port 5062. |
| Tested At | 2026-07-15T00:48:18+03:00 |

## M00A-004: Responsive route-group shell

| Field | Value |
| --- | --- |
| ID | M00A-004 |
| Preconditions | Node.js 22.22.2 and npm 10.9.7 installed; `web/package-lock.json` present. Grafana occupies host port 3000 in this environment. |
| Steps | From `web`, run `npm ci`, `npm run lint`, `npm run typecheck`, and `npm run build`. Confirm `npm run dev` defaults to `localhost:3000`; for this environment run `npm run dev -- --port 3001`. Request `/`, `/account`, `/customer`, `/storefront`, `/operations`, and `/admin`. In DevTools, inspect the desktop home and customer, operations, and admin surfaces; resize to the browser's minimum mobile-like viewport; compare viewport width with `document.documentElement.scrollWidth`; press Tab; and inspect the console. |
| Expected Result | Installation and all quality gates succeed; all six foundation routes return HTTP 200. Focus is visible for interactive controls, and the storefront remains usable at mobile width. |
| Actual Result | `npm ci`, `npm run lint`, `npm run typecheck`, and `npm run build` passed. Next.js `16.2.10` generated static routes for `/`, `/account`, `/customer`, `/storefront`, `/operations`, and `/admin`. `npm audit --omit=dev --audit-level=low` reported 0 vulnerabilities. The project defaults to port 3000; because unrelated Grafana occupied that port, the final DevTools visual session ran on port 3001 after the accessibility fixes. All six routes returned HTTP 200. The final home semantic snapshot contained a skip link, banner, labelled primary navigation, main, contentinfo, one H1, and H2 section headings. The customer, operations, and admin snapshots each contained one H1 and the intended placeholder surface. At the browser-minimum mobile-like viewport of 500x844, `scrollWidth` was 500, so no horizontal overflow was present. The first Tab focus was `Skip to main content`. DevTools reported no console warnings or errors. |
| Status | Passed |
| Evidence | Lint, typecheck, and production build exit code 0; build output lists all six routes. `npm audit --omit=dev --audit-level=low`: `found 0 vulnerabilities`. HTTP 200 verified on the existing Next listener at `http://[::1]:3001/`, `/account`, `/customer`, `/storefront`, `/operations`, and `/admin`. Final DevTools semantic snapshot after accessibility fixes: `/tmp/opencode/m00a-final-home.snapshot.txt`. Final mobile-like screenshot at 500x844: `/tmp/opencode/m00a-final-mobile.webp`. The semantic snapshot verifies the skip link, banner, labelled primary navigation, main, contentinfo, one H1, and H2 section headings; first Tab focus was `Skip to main content`; DevTools console had no warnings or errors. Port 3001 was an environment-only override; the project default remains 3000. |
| Tested At | 2026-07-15T00:49:00+03:00 |

## M00A-005: Reproducible configuration and secret hygiene

| Field | Value |
| --- | --- |
| ID | M00A-005 |
| Preconditions | Repository working tree available. |
| Steps | Inspect `.gitignore`, `.env.example`, `README.md`, and committed configuration. |
| Expected Result | No credentials are committed; startup, migrations, ports, and shutdown are documented from actual manifests. |
| Actual Result | `.env` and `.env.*` are ignored while `.env.example` is retained. The local `.env` does not contain the literal `POSTGRES_PASSWORD` placeholder. Source inspection found only variable references and the README's password-generation command, not embedded credentials. README commands cover prerequisites, ignored environment setup, loopback PostgreSQL on `127.0.0.1:5433`, restore/build, EF update, API and web startup, quality checks, and shutdown. |
| Status | Passed |
| Evidence | Inspected `.gitignore`, `.env.example`, `docker-compose.yml`, `README.md`, project manifests, and source configuration. The repository-source credential scan returned no embedded connection-string password or populated `POSTGRES_PASSWORD` value. `docker compose config --quiet` succeeded. README commands match `EquipmentRental.slnx`, `dotnet-tools.json`, `docker-compose.yml`, and `web/package.json`. |
| Tested At | 2026-07-15T00:48:00+03:00 |
