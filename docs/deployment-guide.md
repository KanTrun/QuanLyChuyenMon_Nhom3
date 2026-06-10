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

For an existing local database volume, use:

```powershell
docker compose up --build -d
```

Do not run `docker compose down --volumes` unless you intentionally want to erase SQL Server data.

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
| `CHATBOT_PROVIDER` | `Gemini` | Chatbot provider (`Gemini` or `Anthropic`) |
| `CHATBOT_MODEL` | `gemini-2.5-flash` | Provider-compatible model |
| `CHATBOT_BASE_URL` | `https://generativelanguage.googleapis.com` | Provider endpoint mapped into `Chatbot:BaseUrl` |
| `CHATBOT_MAX_TOKENS` | `4096` | Bounded chatbot output budget for longer grounded answers |
| `SMARTCA_ENABLED` | `false` | Enable VNPT SmartCA sandbox signing |
| `SMARTCA_CREDENTIAL_MODE` | `Auto` | `Auto`, `DirectSP`, or `OAuth`; `Auto` treats `*.apps.smartcaapi.com` as OAuth |
| `SMARTCA_BASE_URL` | `https://rmgateway.vnptit.vn` | VNPT SmartCA sandbox gateway |
| `SMARTCA_API_PREFIX` | `/sca/sp769` | SmartCA integrated API prefix |
| `SMARTCA_SP_ID` | empty | VNPT-issued direct SP account for `/sca/sp769`, never commit |
| `SMARTCA_SP_PASSWORD` | empty | VNPT-issued direct SP password for `/sca/sp769`, never commit |
| `SMARTCA_MOBILE_CODE` | empty | VNPT OAuth app mobile code, optional metadata |
| `SMARTCA_OAUTH_CLIENT_ID` | empty | OAuth client id; optional when `SMARTCA_SP_ID` already stores `*.apps.smartcaapi.com` |
| `SMARTCA_OAUTH_CLIENT_SECRET` | empty | OAuth client secret; optional when `SMARTCA_SP_PASSWORD` already stores the client secret |
| `SMARTCA_OAUTH_REFRESH_TOKEN` | empty | OAuth refresh token from user consent; never commit |
| `SMARTCA_OAUTH_USERNAME` | empty | SmartCA user uid for password grant when VNPT approves this grant type |
| `SMARTCA_OAUTH_PASSWORD` | empty | SmartCA user password for password grant; never commit |
| `SMARTCA_OAUTH_CREDENTIAL_ID` | empty | Optional credential id; when blank QLCM calls `/csc/credentials/list` |
| `SMARTCA_DEFAULT_USER_ID` | empty | Sandbox subscriber CCCD/MST used for signing |
| `SMARTCA_DEFAULT_SERIAL_NUMBER` | empty | Optional certificate serial when subscriber has multiple certificates |
| `SMARTCA_SIGNER_USER_ID` | empty | App user id allowed to use `SMARTCA_DEFAULT_USER_ID` |
| `SMARTCA_SIGNER_USERNAME` | empty | App username allowed to use `SMARTCA_DEFAULT_USER_ID` |
| `SMARTCA_USER_BINDINGS_JSON` | empty | Multi-user binding JSON, e.g. `[{"appUsername":"admin","subscriberId":"012345678901","serialNumber":"optional"}]` |
| `SMARTCA_CALLBACK_URL` | empty | Public callback URL registered with VNPT, e.g. `https://domain/api/signatures/smartca/callback` |
| `SMARTCA_CALLBACK_SECRET` | empty | Shared callback secret expected in `X-QLCM-SMARTCA-CALLBACK-SECRET`; leave empty to disable callback |
| `SMARTCA_REQUEST_TIMEOUT_SECONDS` | `45` | HTTP timeout for SmartCA calls |

### VNPT SmartCA Sandbox
Enable SmartCA by setting `SMARTCA_ENABLED=true`, direct `/sca/sp769` SP credentials in `SMARTCA_SP_ID` and `SMARTCA_SP_PASSWORD`, and a signer binding in local `.env`. For a single sandbox subscriber, set `SMARTCA_DEFAULT_USER_ID` plus either `SMARTCA_SIGNER_USER_ID` or `SMARTCA_SIGNER_USERNAME`. For multiple clinicians, use `SMARTCA_USER_BINDINGS_JSON` and map each app user to the VNPT subscriber id and optional certificate serial. The web container calls VNPT from server-side only; SP secrets and subscriber ids are never sent to the browser.

VNPT also issues OAuth SmartCA app credentials such as `*.apps.smartcaapi.com` with a `MobileCode`. Those belong to the OAuth/Bearer API family (`/auth/token`, `/csc/...`). QLCM supports this family with `SMARTCA_CREDENTIAL_MODE=OAuth`, but client id/client secret alone are not enough: VNPT requires user consent through authorization code or an approved password grant before `/csc/signature/signhash` can be called.

If you do not know where each value belongs, use the local configurator from the repo root:

```powershell
.\scripts\configure-smartca-env.ps1
docker compose up --build -d web
.\scripts\smoke-smartca-api.ps1
.\scripts\test-smartca-vnpt-credential.ps1
.\scripts\test-smartca-oauth-credential.ps1
```

The configurator copies/updates only local `.env` values from `.env.example`, enables SmartCA, prompts for VNPT SP credentials, binds the QLCM signer, and generates a callback secret when a callback URL is supplied. `.env` is ignored by git and must never be committed.
The direct credential tester calls VNPT `v1/credentials/get_certificate` with local `.env` values and reports VNPT status/certificate count without printing `SMARTCA_SP_PASSWORD`. It also warns when `SMARTCA_SP_ID` looks like an OAuth `*.apps.smartcaapi.com` client because VNPT can return `401 sp_id or sp_password invalid` when OAuth app credentials are mixed into the direct SP API. The OAuth tester validates the OAuth token source and `/csc/credentials/list` without printing client secret, refresh token, username or password.

The clinical signing UI sends a canonical SHA-256 hash to SmartCA, shows the returned transaction code, and lets the same app user poll status after confirming in the SmartCA app. QLCM writes the final immutable `med.signature_records` row only after SmartCA returns a signature for the expected document id and certificate evidence with subject, serial, and expiry. Pending state is stored in `med.signature_transactions`.

The Docker web container also exposes server-side SmartCA API routes:

| Route | Auth | Purpose |
|---|---|---|
| `GET /api/signatures/smartca/readiness` | App login cookie | Check provider readiness and missing config |
| `GET /api/signatures/smartca/transactions/latest?targetType=patient_protocol_application&targetId=<guid>` | App login cookie | Read latest SmartCA transaction for current signer |
| `POST /api/signatures/smartca/start` | App login cookie | Start signing for a target; body: `targetType`, `targetId`, optional `metadataJson` |
| `POST /api/signatures/smartca/transactions/<signatureTransactionId>/refresh` | App login cookie | Poll SmartCA and finalize legal signature |
| `POST /api/signatures/smartca/callback` | Callback secret | VNPT callback entry; body can include `transactionCode`, `tranCode`, `transactionId`, or `externalReference` |

For VNPT callback, set `SMARTCA_CALLBACK_SECRET` and configure VNPT/public gateway to send header `X-QLCM-SMARTCA-CALLBACK-SECRET`. The callback does not trust signed data from the request body; it uses the transaction reference to poll VNPT server-side before finalizing.

After Docker starts, run this smoke check from the repo root:

```powershell
.\scripts\smoke-smartca-api.ps1
```

When `SMARTCA_CALLBACK_SECRET` is configured locally, verify the positive callback path without exposing the secret in source:

```powershell
.\scripts\smoke-smartca-api.ps1 -CallbackSecret "<local-secret>"
```

### Chatbot Credential and Privacy Guard
Create a user-owned Gemini key manually in [Google AI Studio](https://aistudio.google.com/api-keys), restrict the key to Gemini API, and inject it only through environment variables or user-secrets. Never commit a key.

For Docker teammates, put the key in local `.env` as `CHATBOT_API_KEY`; Compose maps `CHATBOT_API_KEY`, `CHATBOT_PROVIDER`, `CHATBOT_MODEL`, `CHATBOT_BASE_URL`, and `CHATBOT_MAX_TOKENS` into the web container. Do not edit files inside the running container.

Free-tier prompts and responses may be used to improve Google products. The runtime privacy guard blocks likely patient identifiers and medical-advice prompts locally before transport. Treat this as a supplemental guard, not permission to send sensitive data. Keep chatbot usage limited to sanitized software-operation guidance. Do not send patient records, prescriptions, case details, notification content, or audit payloads.

`gemini-2.5-flash` remains the stable default. Re-check [Gemini models](https://ai.google.dev/gemini-api/docs/models) and [deprecations](https://ai.google.dev/gemini-api/docs/deprecations) before production rollout.

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
5. `scripts/migrations/*.sql`

Current migrations add Identity bootstrap, onboarding status, demo signatures, SmartCA signature transactions, and related permissions. They are written to run on existing Docker volumes.

To rebuild a fresh database:

```powershell
docker compose down --volumes
docker compose up --build
```

### Unresolved Questions
None.
