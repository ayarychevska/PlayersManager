# PlayersManager

A web application for managing game players, batch imports, and historical player records. Built with ASP.NET Core Web API and PostgreSQL.

## Tech Stack

- **.NET 10** — ASP.NET Core Web API
- **Entity Framework Core 10** — ORM with Npgsql (PostgreSQL)
- **PostgreSQL** — Database
- **Static HTML** — Frontend served via `wwwroot/`

## Project Structure

```
PlayersManager/
├── Controllers/        # API controllers
├── Data/               # EF Core DbContext
├── Dtos/               # Request/response DTOs
├── Migrations/         # EF Core migrations
├── Models/             # Entity classes
├── wwwroot/            # Static frontend files
├── Program.cs          # Application entry point
└── appsettings.json    # Configuration
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [PostgreSQL](https://www.postgresql.org/download/)

### Setup

1. **Clone the repository**

   ```bash
   git clone https://github.com/ayarychevska/PlayersManager.git
   cd PlayersManager
   ```

2. **Configure the database connection**

   Update the connection string in `PlayersManager/appsettings.json`:

   ```json
   {
     "ConnectionStrings": {
       "Default": "Server=localhost;Port=5432;Database=PlayersDB;Username=<your_user>;Password=<your_password>"
     }
   }
   ```

3. **Run the application**

   ```bash
   cd PlayersManager
   dotnet run
   ```

   Migrations are applied automatically at startup.

### EF Core Migrations

Restore the local `dotnet-ef` tool and manage migrations:

```bash
dotnet tool restore
dotnet ef migrations add <MigrationName> --project PlayersManager
dotnet ef database update --project PlayersManager
```

## API Endpoints

All endpoints are under `api/players`.

### Players

| Method   | Route                              | Description                                      |
|----------|------------------------------------|--------------------------------------------------|
| `GET`    | `/api/players`                     | Get all players (active by default; `?includeInactive=true` for all) |
| `GET`    | `/api/players/{id}/details`        | Get player details with full history              |
| `DELETE` | `/api/players/{id}`                | Delete a player                                   |
| `DELETE` | `/api/players/all`                 | Delete all players and their history              |
| `PATCH`  | `/api/players/{id}/nickname`       | Update a player's nickname                        |
| `PATCH`  | `/api/players/{id}/status`         | Toggle a player's status (Active/Inactive)        |

### Batches

| Method   | Route                              | Description                                      |
|----------|------------------------------------|--------------------------------------------------|
| `POST`   | `/api/players/import`              | Import a list of players as a new batch           |
| `GET`    | `/api/players/batches`             | Get all batches                                   |
| `GET`    | `/api/players/batches/{batchId}/records` | Get records for a batch, including missing players |
| `DELETE` | `/api/players/batches/{batchId}`   | Delete a batch and its records                    |

### Matching & Creation

| Method   | Route                              | Description                                      |
|----------|------------------------------------|--------------------------------------------------|
| `POST`   | `/api/players/from-record/{recordId}` | Create a player from an unmatched batch record |
| `POST`   | `/api/players/match-record/{recordId}` | Manually match a record to an existing player  |
| `POST`   | `/api/players/from-batch/{batchId}`   | Create players from all unmatched records in a batch |
| `PATCH`  | `/api/players/deactivate-missing/{batchId}` | Deactivate players not found in a batch |

### History & Statistics

| Method   | Route                              | Description                                      |
|----------|------------------------------------|--------------------------------------------------|
| `GET`    | `/api/players/history`             | Get all historical records                        |
| `GET`    | `/api/players/history/{id}`        | Get a single historical record                    |
| `GET`    | `/api/players/statistics`          | Get player power across all batch dates           |

## Data Model

```
Player (Id, Nickname, Status)
  └── HistoricalPlayerRecord (Id, PlayerId, BatchId, Nickname, Power, TownHallLevel, RecordedAt)

Batch (Id, Date)
  └── BatchRecord (Id, BatchId, Nickname, Power, TownHallLevel, MatchStatus)
```

- **Player** — A game player with an active/inactive status.
- **Batch** — A dated import of player data.
- **BatchRecord** — An individual entry within a batch, matched or unmatched against existing players.
- **HistoricalPlayerRecord** — A snapshot of a player's stats at a given batch date.

## License

This project is provided as-is for personal use.
