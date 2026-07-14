# Equipment Rental Platform

Milestone 0A establishes the local foundation for the single-vendor event-equipment rental MVP. It contains no catalog, identity, availability, or rental business features yet.

## Repository Layout

- `Core/`: domain layer, kept independent of implementation details.
- `Services/`: application layer; references `Core`.
- `infrastructure/`: PostgreSQL and external implementation layer; references `Core` and `Services`.
- `Api/`: ASP.NET Core host; references `Services`, `infrastructure`, and `Shared`.
- `Shared/`: cross-layer contracts only; it has no dependency on application layers.
- `web/`: the Next.js application, with route groups for storefront, customer, operations, and admin surfaces.

## Prerequisites

- .NET SDK `10.0.107` or another compatible .NET 10 SDK.
- Docker Engine with Docker Compose v2.
- Node.js 22 LTS and npm 10 or newer.

## Local Configuration

No passwords or connection strings are committed. Create the ignored Docker environment file and append a random local password:

```bash
cp .env.example .env
printf 'POSTGRES_PASSWORD=%s\n' "$(openssl rand -hex 24)" >> .env
```

Docker Compose requires `POSTGRES_PASSWORD`; startup fails with an actionable error when it is absent or empty. PostgreSQL is published only on the host loopback interface. Load the generated values and construct the API/EF connection string before running backend commands:

```bash
set -a
source .env
set +a
export ConnectionStrings__EquipmentRental="Host=localhost;Port=${POSTGRES_PORT:-5433};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
```

The API intentionally refuses to start if `ConnectionStrings:EquipmentRental` is empty. Do not place a real connection string in `Api/appsettings*.json`.

## Start Locally

Start PostgreSQL and wait until its health check reports healthy:

```bash
docker compose up -d
docker compose ps
```

Restore the local EF tool and backend packages, then apply the initial migration:

```bash
dotnet tool restore
dotnet restore EquipmentRental.slnx
dotnet build EquipmentRental.slnx --no-restore
dotnet tool run dotnet-ef database update --project infrastructure/infrastructure.csproj --startup-project Api/Api.csproj
```

Start the API in one terminal:

```bash
dotnet run --project Api
```

The Development API listens on `http://localhost:5062`. Useful endpoints are:

- Swagger UI: `http://localhost:5062/swagger`
- Health check: `http://localhost:5062/health`

Install and start the web application in a second terminal:

```bash
npm --prefix web ci
npm --prefix web run dev
```

The Next.js server listens on `http://localhost:3000`. If that port is occupied, use `npm --prefix web run dev -- --port 3001` and open `http://localhost:3001`. Foundation routes are `/`, `/account`, `/operations`, and `/admin`; authorization is intentionally deferred to Milestone 1.

## Frontend Checks

```bash
npm --prefix web run lint
npm --prefix web run typecheck
npm --prefix web run build
```

`web/components.json`, the `cn` utility, Tailwind v4, and Radix-compatible dependencies provide the shadcn/ui groundwork. User-facing text is English and root `dir="ltr"` is explicit so future Arabic/RTL work has one clear layout boundary.

## Database Migrations

The initial `InitialCreate` migration deliberately contains no product entities: data modelling begins in Milestone 0B. It establishes the EF migration history cleanly without preempting that work.

Create a future migration only after modelling a reviewed change:

```bash
dotnet tool run dotnet-ef migrations add MeaningfulName --project infrastructure/infrastructure.csproj --startup-project Api/Api.csproj --output-dir Persistence/Migrations
```

## Shutdown

Stop the API and Next.js processes with `Ctrl+C`, then stop PostgreSQL:

```bash
docker compose down
```

Use `docker compose down -v` only when intentionally discarding local PostgreSQL data.
