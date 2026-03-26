# Copilot Instructions for PlayersManager

## Project Overview
PlayersManager is an ASP.NET Core Web API (.NET 10) for managing game players, batch imports, and historical player records. It uses a PostgreSQL database via Entity Framework Core (Npgsql).

## Tech Stack
- **Framework:** .NET 10, ASP.NET Core Web API
- **ORM:** Entity Framework Core 10 with Npgsql (PostgreSQL)
- **Frontend:** Static HTML served via `wwwroot/`
- **API style:** RESTful controllers under `api/[controller]`

## Project Structure
- `Models/` – EF Core entity classes (`Player`, `Batch`, `BatchRecord`, `HistoricalPlayerRecord`)
- `Dtos/` – Request/response DTOs
- `Data/` – `AppDbContext` with Fluent API configuration
- `Controllers/` – API controllers
- `Migrations/` – EF Core migrations
- `wwwroot/` – Static frontend files

## Coding Conventions
- Use **file-scoped namespaces** (`namespace X;`).
- Use **collection expressions** (`[]`) for list initialization.
- Use `null!` for required navigation properties in EF entities.
- Use `string.Empty` as default for string properties in models.
- Prefer `var` for local variable declarations.
- Use **async/await** for all database operations.
- Use XML `<summary>` comments on controller action methods.
- DTOs are simple record-like classes in the `Dtos/` folder.

## Entity Framework
- Configure entities using **Fluent API** in `AppDbContext.OnModelCreating`, not data annotations.
- Store enums as strings in the database using `.HasConversion<string>()`.
- Use `timestamp without time zone` for `DateTime` columns in PostgreSQL.
- Migrations are applied automatically at startup via `db.Database.Migrate()`.
- Tool: `dotnet-ef` 10.0.5 (configured in `dotnet-tools.json`).

## API Patterns
- Controllers inject `AppDbContext` directly (no repository/service layer).
- Return anonymous objects for simple responses; use DTOs for request bodies.
- Use `IActionResult` as the return type for all actions.
- Follow REST conventions: `Ok()`, `NotFound()`, `BadRequest()`, `NoContent()`, `Conflict()`.

## Database
- PostgreSQL with connection string named `"Default"` in `appsettings.json`.