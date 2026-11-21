# FreeBook (Bookify)

[![CI](https://github.com/mohamedkamal74/FreeBookApp/actions/workflows/ci.yml/badge.svg)](https://github.com/mohamedkamal74/FreeBookApp/actions/workflows/ci.yml)

A small ASP.NET Core MVC book-catalog app with an admin area, built on **.NET 10**,
**EF Core 10**, **SQL Server**, and **ASP.NET Identity**.

## Projects

| Project | Purpose |
| --- | --- |
| `FreeBook` | ASP.NET Core MVC web app (controllers, Razor views, `wwwroot`) |
| `Infrastructure` | EF Core `FreeBookDbContext`, migrations, repositories, Identity store |
| `Domin` | Entities, view models, resource strings |
| `FreeBook.Tests` | xUnit tests (EF Core InMemory + `WebApplicationFactory`) |

`FreeBook` references `Infrastructure`, which references `Domin`. `FreeBook.Tests` references `FreeBook` and `Infrastructure`.

## Prerequisites

- .NET SDK **10.0.x**
- SQL Server **LocalDB** (default local dev database)
- Optional: global EF tool `dotnet-ef` 10.0.14

```powershell
dotnet tool install --global dotnet-ef --version 10.0.14
```

## Setup

```powershell
git clone https://github.com/mohamedkamal74/FreeBookApp Bookify
cd Bookify
dotnet restore FreeBook.sln
dotnet build FreeBook.sln
```

### Database

The connection string is `BookConnection` in `FreeBook/appsettings.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=BookDb;Trusted_Connection=True;MultipleActiveResultSets=True;
```

Start LocalDB if it is not running and apply migrations:

```powershell
sqllocaldb start MSSQLLocalDB
dotnet ef database update --project Infrastructure --startup-project FreeBook
```

There is **no seed data**. Before you can log in, create an `Admin` role and an admin
user manually (for example with a short EF Core/Identity seeder). Roles used by the app:
`Admin` and `User`.

To override the connection string locally without editing tracked files (user-secrets are
already enabled in `FreeBook.csproj`):

```powershell
dotnet user-secrets set "ConnectionStrings:BookConnection" "Server=...;Database=...;" --project FreeBook
```

### First run

The checked-in `launchSettings.json` profile uses IIS Express. For a plain CLI run:

```powershell
dotnet run --project FreeBook --no-launch-profile --urls http://localhost:5128
```

- Public site: <http://localhost:5128>
- Admin login: <http://localhost:5128/Admin>
- Swagger UI (Development only): <http://localhost:5128/swagger>
- Health check: <http://localhost:5128/health>

### Docker

Run SQL Server and the app together; migrations run and an admin is seeded on startup:

```powershell
docker compose up --build
```

- App: <http://localhost:8080>
- Admin login: `admin@bookify.local` / `Admin@12345` (override with `SEED_ADMIN_EMAIL` and `SEED_ADMIN_PASSWORD`)
- SQL Server password: override `MSSQL_SA_PASSWORD` (defaults are for local use only)
- See `.env.example` for the available settings.

**Important notes:**

- `.env` is **git-ignored** (added in `.gitignore`). Copy `.env.example` to `.env` and change the admin password before first run.
- `MSSQL_SA_PASSWORD` is **frozen on first container start** — it's baked into the `sqlserver-data` volume. Changing it later causes login failures; run `docker compose down -v` to reset the database volume.
- App logs appear in `./logs/log-YYYYMMDD.txt` (bind-mounted from the container).
- Port overrides: `MSSQL_PORT` (default 1433) and `APP_PORT` (default 8080) via `.env` or environment variables if you have a local SQL Server on 1433.

The startup hooks are opt-in via configuration: `Startup:MigrateOnStartup` and
`Startup:SeedAdmin` (plus `Seed:AdminEmail` / `Seed:AdminPassword`). Only the admin
account is seeded automatically. To load demo data (categories, books, copies,
subscribers, subscriptions and rentals), run:

```powershell
scripts\seed-demo.ps1
```

This copies `TestData.sql` into the SQL container and runs it. **It deletes and
recreates all library rows**, so use it only for demos — run it again after
`docker compose down -v` to restore the sample data. Users and roles are never
deleted. The script also loads three sample alerts with `demo:` dedupe keys so
the admin notification center has content; rerunning it replaces only those demo
alerts and leaves real job-generated alerts untouched.

## Development commands

```powershell
dotnet restore FreeBook.sln
dotnet build FreeBook.sln
dotnet test FreeBook.sln
dotnet format FreeBook.sln --verify-no-changes
dotnet ef migrations add <Name> --project Infrastructure --startup-project FreeBook
dotnet ef database update --project Infrastructure --startup-project FreeBook
```

Convenience scripts live in `scripts/` (`build.ps1`, `run.ps1`, `migrate.ps1`).

## Behavior notes

- Repository/service methods are async (`*Async`) and use EF Core async APIs.
- `AccountsController.Register` accepts only `.jpg`, `.jpeg`, `.png`, `.gif`, and `.webp`
  images up to 5 MB; the upload directory is created automatically.
- `/health` runs a database connectivity check and returns `503` when the database is
  unreachable.
- Outside Development, unhandled exceptions are logged and returned as a `ProblemDetails`
  JSON response with status `500`.
- Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and
  `Permissions-Policy`) are added to every response. Login is rate-limited to 5 attempts
  per minute per client, and passwords require 8+ characters with upper, lower, digit, and
  symbol.
- Destructive admin actions are POST-only and protected by antiforgery tokens.
- The admin-only Books page at `/Admin/Books/Books` manages book metadata (name, author,
  publisher, publishing date, category/subcategory, hall, rental availability, cover image
  URLs and description). Deletes are soft deletes, so copies and rental history are preserved.
  Cover image upload is not part of this page; the existing image URL fields are editable.
- The admin-only notification center at `/Admin/Notifications/Index` shows alerts from
  the Hangfire expiry, overdue and fine jobs. A navbar bell shows the unread count;
  admins can mark individual/all alerts as read and clear read ones. Jobs upsert by
  `DedupeKey`, so repeated daily runs never duplicate an alert. Messages are localized
  at render time (background jobs have no UI culture), with `en` and `ar` strings.

## Tests

`FreeBook.Tests` uses EF Core InMemory and `WebApplicationFactory`, so no LocalDB is
required:

```powershell
dotnet test FreeBook.sln
```

## Known issues

- `.editorconfig` demotes some nullable diagnostics to `suggestion`; the build is currently
  warning-free.
- `Domin.Entity.VwUser` maps to the DB view `VwUsers`, which is created by migration
  `AddVwUsersView`. `AccountsController.Register` fails if this view is missing.

## CI

`.github/workflows/ci.yml` runs restore, a package vulnerability audit,
`dotnet format --verify-no-changes`, a Release build, `dotnet test` with coverage, and
uploads the test results as an artifact on pushes and pull requests.
