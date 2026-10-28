# FiscApi

Back-end API for **FiscApp**, a personal finance tracker built with .NET MAUI.

- Stores data in a **JSON file** or **SQL Server** (running in Docker). One setting switches between them.
- **JWT authentication**: anyone can read; creating, editing, and deleting require a token.
- The MAUI app lives in a separate repo: [montyflatoff8/Intro_Mobile → FiscApp](https://github.com/montyflatoff8/Intro_Mobile/tree/main/FiscApp). It talks to the database **only through this API**.

```
FiscApp (MAUI, Android emulator)
   │  HTTPS + "Authorization: Bearer <token>" on writes
   ▼
FiscApi  ──  TransactionsController → TransactionService → ITransactionRepository
                                                             ├── JsonTransactionRepository  (Data/transactions.json)
                                                             └── SqlTransactionRepository   (EF Core)
                                                                       │
                                                                       ▼
                                                       SQL Server in Docker (compose.yaml)
```

---

## Prerequisites

| Tool | Notes |
|---|---|
| .NET 10 SDK | `dotnet --version` |
| Docker Desktop | Must be installed. In Development the API starts it for you |
| Visual Studio with the .NET MAUI workload | Only needed to run FiscApp |
| Postman | To run the test collection |

---

## Run it from a fresh clone

All commands run from the repo root (the folder with `FiscApi.csproj`).

### 1. Trust the HTTPS development certificate (once per PC)

```powershell
dotnet dev-certs https --trust
```

### 2. Create `.env` with the SQL Server password

```powershell
Copy-Item .env.example .env
notepad .env
```

Set `MSSQL_SA_PASSWORD` to a strong password: 8+ characters with upper case, lower case, a digit, and a symbol. Avoid `$`, `"` and `;`. `.env` is git-ignored.

### 3. Store the secrets in User Secrets

User Secrets live in your Windows profile, outside the repo, so they can never be committed.

```powershell
# Same password as in .env
dotnet user-secrets set "ConnectionStrings:FiscApi" "Server=127.0.0.1,14334;Database=FiscApi;User Id=sa;Password=YOUR_SA_PASSWORD;TrustServerCertificate=True;"

# Any long random string: it signs the login tokens
dotnet user-secrets set "Authentication:SigningKey" "PASTE_A_RANDOM_32+_CHARACTER_STRING"

# The password for the demo account (username: demo)
dotnet user-secrets set "Authentication:DemoUser:Password" "CHOOSE_A_DEMO_PASSWORD"
```

Check them with `dotnet user-secrets list`.

### 4. Start SQL Server

```powershell
docker compose up -d
docker compose ps        # wait for: fiscapi-sqlserver ... (healthy)
```

> In Development this step is optional. With `DataSource:AutoStartDocker = true`, the API starts Docker Desktop and runs `docker compose up -d --wait` itself on startup.

### 5. Run the API

```powershell
dotnet run --launch-profile https
```

The first lines confirm what it's running with:

```
info: Data source: SqlServer (server 127.0.0.1,14334, database FiscApi)
info: Authentication: ENABLED - POST/PUT/DELETE require a bearer token
info: Applying database migrations.
      Now listening on: https://localhost:7008
```

On first run, the EF Core migration creates the `FiscApi` database and the `Transactions` table.

### 6. Run the app

See **[Running FiscApp](#running-fiscapp)** below.

---

## Configuration (`appsettings.json`)

Settings are read **once at startup**, so restart the API after changing any of them.

| Setting | Values | What it does |
|---|---|---|
| `DataSource:Provider` | `SqlServer` / `Json` | Which repository is registered. Same endpoints either way |
| `DataSource:AutoStartDocker` | `true` / `false` | Development only: start Docker and the SQL container on startup |
| `DataSource:MigrateOnStartup` | `true` / `false` | Development only: apply EF Core migrations on startup |
| `Authentication:Enabled` | `true` / `false` | `false` opens all writes (Development only; refused in Production) |
| `Authentication:Issuer` / `Audience` | `FiscApi` / `FiscApp` | Tokens must match both |
| `Authentication:AccessTokenMinutes` | `60` | Token lifetime |
| `Authentication:DemoUser:Username` | `demo` | The single login account (no registration, no roles) |

### Where each secret lives

| Secret | Location | In Git? |
|---|---|---|
| SQL Server SA password | `.env` | ❌ git-ignored (`.env.example` is the template) |
| Connection string | User Secrets | ❌ |
| JWT signing key | User Secrets | ❌ |
| Demo user password | User Secrets (hashed in memory at startup) | ❌ |
| Postman password / token | Your Postman environment | ❌ (the committed environment file has them blank) |

---

## Endpoints

### Transactions

| Method | URL | Auth | Success | Failure codes |
|---|---|---|---|---|
| GET | `/api/transactions` (optional `?type=` and `?category=`) | Public | 200 | — |
| GET | `/api/transactions/{id}` | Public | 200 | 404 |
| POST | `/api/transactions` | **Bearer token** | 201 + `Location` header | 400, 401, 409 |
| PUT | `/api/transactions/{id}` | **Bearer token** | 204 | 400, 401, 404, 409 |
| DELETE | `/api/transactions/{id}` | **Bearer token** | 204 | 401, 404 |

### Authentication

| Method | URL | Body / header | Response |
|---|---|---|---|
| POST | `/api/auth/login` | `{ "username": "demo", "password": "..." }` | 200 `{ accessToken, tokenType: "Bearer", expiresIn: 3600 }`, or 401 for wrong credentials |
| GET | `/api/auth/me` | `Authorization: Bearer <token>` | 200 `{ username }`, or 401 |

Send the token on writes as `Authorization: Bearer <accessToken>`.

### Resource: Transaction

| Field | Type | Set by | Rules |
|---|---|---|---|
| `id` | int | Server | Assigned by the database |
| `createdAt` | DateTime | Server | UTC time the record was created |
| `date` | DateTime | Client | Required |
| `amount` | decimal | Client | Required, 0.01 – 1,000,000 |
| `type` | string | Client | Required, `Income` or `Expense` |
| `category` | string? | Client | Optional, max 50. Cleared for `Income` |
| `description` | string | Client | Required, 1 – 100 characters |
| `notes` | string? | Client | Optional, max 250 |

**Business rule (409 Conflict):** no two transactions may share the same **date** (calendar day), **amount**, and **description** (case-insensitive, trimmed). On PUT, the transaction being updated is excluded from the check.

**Validation errors (400)** are RFC 7807 problem details. The `errors` keys are the JSON field names (`description`, `amount`, …).

---

## Testing with Postman

1. Import `postman/FiscApi.postman_collection.json` and `postman/FiscApi.Local.postman_environment.json`.
2. Select the **FiscApi Local** environment and set `password` to your demo password from step 3 (`dotnet user-secrets list`).
3. Run the collection. **Login** runs first and saves the token. Every other request inherits it from the collection's Bearer auth.

The **Create without token** and **Delete without token** tests prove the auth rules. They read the environment's `authEnabled`:

| `Authentication:Enabled` (API) | `authEnabled` (Postman) | Expected for no-token writes |
|---|---|---|
| `true` | `true` | **401 Unauthorized** |
| `false` | `false` | Reaches the controller: 400 (invalid body) / 404 (missing id) |

To switch modes, change **both** values and **restart the API**.

---

## Running FiscApp

FiscApp is in [montyflatoff8/Intro_Mobile](https://github.com/montyflatoff8/Intro_Mobile/tree/main/FiscApp).

1. Start this API with the **https** profile (step 5). The app calls `https://10.0.2.2:7008/` from the Android emulator (`10.0.2.2` is the emulator's name for your PC) and `https://localhost:7008/` on Windows.
2. Open `FiscApp/FiscApp.slnx` in Visual Studio, choose an **Android emulator**, and press **F5** (a **Debug** build. Only Debug builds trust the local development certificate).
3. The transaction list loads without logging in. Adding, editing, or deleting while logged out opens the **Log in** page. Log in as `demo` with your demo password.

---

## Logging

Logs go to the console where the API runs (and Visual Studio's Output window).

- **Startup:** environment, data source (server and database name only), and authentication state.
- **Every write:** `Created / Updated / Deleted transaction {id}`, plus warnings for not-found and duplicate.
- **Logins:** `User demo logged in` / `Failed login for username demo`.
- **Never logged:** passwords, tokens, the connection string, or transaction descriptions, notes, and amounts.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Postman: "Could not send request" | The API isn't running. It may have crashed at startup because Docker was off | Check the API console. Start Docker Desktop, then rerun the API |
| No-token write returns 400/404 when you expect 401 | The API was started before `Authentication:Enabled` was set to `true` | Restart the API and check the startup log line |
| Login returns 401 | The password doesn't match User Secrets | `dotnet user-secrets list`. In Postman, check the environment's current value |
| App: "Can't reach the API. Is it running?" | API stopped, or started with the `http` profile | Run with `--launch-profile https` and use a Debug build of the app |
| `ConnectionStrings:FiscApi is required...` on startup | User Secrets not set | Repeat step 3 |
| Container keeps restarting | SA password too weak | Fix `.env`, run `docker compose down -v`, then `docker compose up -d` |

### Reset the database

```powershell
docker compose down -v   # deletes the container AND its data volume
```

The next API start recreates the database from the migrations. Plain `docker compose down` or a reboot keeps the data.
