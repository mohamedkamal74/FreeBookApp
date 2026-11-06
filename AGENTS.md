# FreeBook (Bookify)

ASP.NET Core MVC app on **.NET 10** with EF Core 10, SQL Server, and ASP.NET Identity.
Three product projects: `Domin` (entities), `Infrastructure` (EF Core + Identity + repositories), `FreeBook` (web UI), plus `FreeBook.Tests` (xUnit).

CI (`.github/workflows/ci.yml`) runs restore, package vulnerability audit, format check, Release build, `dotnet test`, and uploads test results.

## Commands

```powershell
dotnet restore FreeBook.sln
dotnet build FreeBook.sln
dotnet format FreeBook.sln --verify-no-changes
dotnet test FreeBook.sln
dotnet list FreeBook.sln package --vulnerable --include-transitive
```

Run locally (the `launchSettings.json` profile uses IIS Express, so bypass it):

```powershell
dotnet run --project FreeBook --no-launch-profile --urls http://localhost:5128
```

Docker (SQL Server + app, migrations and admin seeding on startup):

```powershell
docker compose up --build
```

The app is at `http://localhost:8080`. Override `MSSQL_SA_PASSWORD`, `SEED_ADMIN_EMAIL`,
and `SEED_ADMIN_PASSWORD` in the environment (see `.env.example`).

DB / EF (requires global `dotnet-ef` 10.0.14: `dotnet tool install --global dotnet-ef --version 10.0.14`):

```powershell
dotnet ef database update --project Infrastructure --startup-project FreeBook
dotnet ef migrations add <Name> --project Infrastructure --startup-project FreeBook
```

Helper scripts: `scripts/build.ps1`, `scripts/run.ps1`, `scripts/migrate.ps1`.

Override the connection string locally without editing tracked files (user-secrets are enabled in `FreeBook.csproj`):

```powershell
dotnet user-secrets set "ConnectionStrings:BookConnection" "Server=...;Database=...;" --project FreeBook
```

## Database

- Connection string `BookConnection` in `FreeBook/appsettings.json` points to `(localdb)\MSSQLLocalDB`, DB `BookDb`.
- Start LocalDB first if needed: `sqllocaldb start MSSQLLocalDB`.
- Migrations live in `Infrastructure/Migrations`.
- No seed data by default. `AccountsController` is `[Authorize]` at class level, so an admin user/role must exist before login. Enable `Startup:SeedAdmin` (Docker Compose does) or create the admin manually (roles: `Admin`, `User`).
- `Domin.Entity.VwUser` maps to DB view `VwUsers`, created by migration `AddVwUsersView`. If the view is missing, run migrations; `AccountsController.Register` throws without it.

## Architecture

- Project references: `FreeBook` → `Infrastructure` → `Domin`.
- `FreeBook.Tests` references `FreeBook` and `Infrastructure`; tests use EF Core InMemory + `WebApplicationFactory` (no LocalDB needed).
- Repository/service contracts (`IServiceRepository<T>`, `IServiceRepositoryLog<T>`) are async (`*Async`) and use EF Core async APIs.
- `Program.cs` ends with `public partial class Program { }` so `WebApplicationFactory<Program>` can boot the app.
- Areas: `Admin` (`Accounts`, `Categories`, `Home`); public `Home`.
- Auth: cookie auth via ASP.NET Identity; login path is `/Admin` (`Accounts/Login`).
- Uploaded user images go to `wwwroot/Images/Users` (`Domin.Entity.Helper.PathSaveImageUser`). `AccountsController.Register` only accepts `.jpg`, `.jpeg`, `.png`, `.gif`, `.webp` up to 5 MB (`Helper.MaxImageSizeBytes`).
- Health endpoint: `/health` (`AddDbContextCheck<FreeBookDbContext>`).
- Unhandled exceptions are logged and returned as `ProblemDetails` 500 via `GlobalExceptionHandler` outside Development.
- Security: `SecurityHeadersMiddleware` adds `nosniff`, `DENY`, `no-referrer`, and a permissions policy. Login POST is rate-limited to 5 attempts/minute per client. Password policy is 8+ chars with upper, lower, digit, and symbol.
- Destructive actions (`Delete`, `DeleteLog`, `DeleteRole`, `DeleteUser`) are POST-only with antiforgery validation; the Razor views submit hidden `deleteForm`s.
- Startup hooks (`Startup:MigrateOnStartup`, `Startup:SeedAdmin` + `Seed:AdminEmail`/`Seed:AdminPassword`) are opt-in; Docker Compose enables both.
- Docker: `Dockerfile` publishes `FreeBook`; `docker-compose.yml` runs SQL Server 2022 and the app.

## Gotchas

- Nullable is enabled and the build is currently warning-free. `.editorconfig` still demotes some nullable diagnostics to `suggestion`.
- No known vulnerable NuGet packages; CI runs `dotnet list package --vulnerable --include-transitive`.
- Swagger UI is available only in Development at `/swagger`.
- `/health` is anonymous and runs a DB connectivity check; it returns `503` when the database is unreachable.
- Docker Compose seeds an admin (`admin@bookify.local` / `Admin@12345` by default). Change these in any shared/production environment.
- `VwUsers` is a SQL Server view, so InMemory tests do not exercise its data; verify it against LocalDB (`sqllocaldb start MSSQLLocalDB`) or the Docker SQL Server.
