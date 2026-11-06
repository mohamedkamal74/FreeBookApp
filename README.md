# FreeBook (Bookify)

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
- Optional: global EF tool `dotnet-ef` 10.0.3

```powershell
dotnet tool install --global dotnet-ef --version 10.0.3
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

### First run

The checked-in `launchSettings.json` profile uses IIS Express. For a plain CLI run:

```powershell
dotnet run --project FreeBook --no-launch-profile --urls http://localhost:5128
```

- Public site: <http://localhost:5128>
- Admin login: <http://localhost:5128/Admin>
- Swagger UI (Development only): <http://localhost:5128/swagger>

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

## Known issues

- `.editorconfig` demotes common nullable diagnostics to `suggestion`; the build still shows a few Razor view nullability warnings.
- NuGet reports non-blocking advisories for `Microsoft.OpenApi` (via Swashbuckle) and
  `System.Security.Cryptography.Pkcs` (transitive dependency).
- `Domin.Entity.VwUser` maps to the DB view `VwUsers`, which is created by migration
  `AddVwUsersView`. `AccountsController.Register` fails if this view is missing.

## CI

`.github/workflows/ci.yml` runs restore, `dotnet format --verify-no-changes`, a Release
build, and `dotnet test` on pushes and pull requests.
