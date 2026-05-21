# Deployment Guide

## Docker Compose
Use Docker Compose for local full-stack startup: Blazor web host, SQL Server, and database initialization.

### Services
| Service | Purpose |
|---|---|
| `web` | ASP.NET Core Blazor app exposing port `8080` in the container |
| `sqlserver` | SQL Server 2022 Developer with persistent named volume |
| `db-init` | One-shot sqlcmd runner for schema and seed scripts |

### Run
```powershell
docker compose up --build
```

Open `http://localhost:8080`.

### Local Bootstrap Admin
| Username | Password |
|---|---|
| `admin` | `Admin@2026` |

The bootstrap migration reactivates this local admin account when an older Docker volume was locked by the null-password migration.

### Configuration
| Variable | Default | Purpose |
|---|---|---|
| `APP_HTTP_PORT` | `8080` | Host port for the web app |
| `DB_PORT` | `14333` | Host port for SQL Server |
| `MSSQL_SA_PASSWORD` | `QlcmDev_ChangeMe_2026!` | Local SQL Server SA password |
| `CHATBOT_API_KEY` | empty | Optional Gemini API key |

The app container uses:

```text
Server=sqlserver,1433;Database=MedicalProcedureManagement;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;Encrypt=False;
```

### Database Init
`db-init` waits for SQL Server, checks for `MedicalProcedureManagement.med.departments`, and only runs schema/seed scripts when the database is not initialized.

Scripts run in order:
1. `MedicalProcedureManagement.sql`
2. `scripts/seed-lookup-catalogs.sql`
3. `scripts/seed-realistic-data.sql`
4. `scripts/seed-hospital-data.sql`

To rebuild a fresh database:

```powershell
docker compose down --volumes
docker compose up --build
```

### Unresolved Questions
None.
