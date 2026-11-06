# FreeBook (Bookify)

ASP.NET Core MVC app on **.NET 10** with EF Core 10, SQL Server, and ASP.NET Identity.
Three product projects: `Domin` (entities), `Infrastructure` (EF Core + Identity + repositories), `FreeBook` (web UI), plus `FreeBook.Tests` (xUnit).

CI (`.github/workflows/ci.yml`) runs restore, format check, Release build, and `dotnet test`.

## Commands

```powershell
dotnet restore FreeBook.sln
dotnet build FreeBook.sln
dotnet format FreeBook.sln --verify-no-changes
dotnet test FreeBook.sln
```

Run locally (the `launchSettings.json` profile uses IIS Express, so bypass it):

```powershell
dotnet run --project FreeBook --no-launch-profile --urls http://localhost:5128
```

DB / EF (requires global `dotnet-ef` 10.0.3: `dotnet tool install --global dotnet-ef --version 10.0.3`):

```powershell
dotnet ef database update --project Infrastructure --startup-project FreeBook
dotnet ef migrations add <Name> --project Infrastructure --startup-project FreeBook
```

Helper scripts: `scripts/build.ps1`, `scripts/run.ps1`, `scripts/migrate.ps1`.

## Database

- Connection string `BookConnection` in `FreeBook/appsettings.json` points to `(localdb)\MSSQLLocalDB`, DB `BookDb`.
- Start LocalDB first if needed: `sqllocaldb start MSSQLLocalDB`.
- Migrations live in `Infrastructure/Migrations`.
- No seed data. `AccountsController` is `[Authorize]` at class level, so an admin user/role must be created manually (roles: `Admin`, `User`) before login is possible.
- `Domin.Entity.VwUser` maps to DB view `VwUsers`. Migrations do not create it; migration `AddVwUsersView` does. If the view is missing, `AccountsController.Register` throws.

## Architecture

- Project references: `FreeBook` → `Infrastructure` → `Domin`.
- `FreeBook.Tests` references `FreeBook` and `Infrastructure`; tests use EF Core InMemory + `WebApplicationFactory` (no LocalDB needed).
- `Program.cs` ends with `public partial class Program { }` so `WebApplicationFactory<Program>` can boot the app.
- Areas: `Admin` (`Accounts`, `Categories`, `Home`); public `Home`.
- Auth: cookie auth via ASP.NET Identity; login path is `/Admin` (`Accounts/Login`).
- Uploaded user images go to `wwwroot/Images/Users` (`Domin.Entity.Helper.PathSaveImageUser`).

## Gotchas

- Nullable is enabled. `.editorconfig` demotes common nullable diagnostics to `suggestion`; remaining build warnings are Razor view nullability and NuGet advisories.
- Build shows non-blocking NU190x advisories for `Microsoft.OpenApi` (via Swashbuckle) and `System.Security.Cryptography.Pkcs` (transitive).
- Swagger UI is available only in Development at `/swagger`.
- `VwUsers` is a SQL Server view, so InMemory tests do not exercise its data; verify it against LocalDB (`sqllocaldb start MSSQLLocalDB`).
