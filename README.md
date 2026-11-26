# Outback

Outback is a recreational project for learning how [OAuth 2.1](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-v2-1-19) and [OpenID Connect 1.0](https://openid.net/specs/openid-connect-core-1_0.html) work.

It implements most of OAuth 2.1 and a lot of OpenID Connect 1.0 as a self-contained identity provider: a C# library with the protocol endpoints, a web app that stores users, clients and scopes in SQL Server, an admin UI, and a sample resource server.

> It is not a complete implementation, but it is a good starting point for learning about OAuth and OpenID Connect.

- **License:** [MIT](LICENSE)
- **Stack:** .NET 11, ASP.NET Core, EF Core 11 (SQL Server), Aspire 17, Bootstrap 5
- **Origin:** split out of the [InnovationBoost](https://github.com/Rinsen/InnovationBoost) project

## Features

### Protocol — `Rinsen.Outback`

- OAuth 2.1 authorization code flow with mandatory PKCE (`S256`)
- Client credentials (machine-to-machine)
- Refresh tokens with rotation and reuse detection (a token family per authorization code)
- Device authorization grant
- OpenID Connect: `id_token` (ES256), userinfo endpoint, discovery document and JWKS
- `form_post` response mode
- CORS allow-listing on the discovery endpoints
- Rate limiting on credential endpoints
- Constant-time client secret comparison

### Identity — `Rinsen.IdentityProvider`

- Local accounts with hashed passwords
- TOTP two-factor authentication with QR-code onboarding
- Server-side sessions backed by SQL Server (ticket store)
- Account lockout and disabled accounts
- Audit logging

### Identity provider app — `Rinsen.Outback.App`

- Razor + Bootstrap 5 UI: login, self-registration (gated by an invitation code), TOTP enablement, two-factor sign-in, device consent pages
- Admin REST APIs for clients, scopes, sessions and operational tasks (configuration, backup)
- Four OpenAPI documents with a Swagger UI in Development
- SQL Server storage; ASP.NET Core Data Protection keys persisted in the database
- Optional GELF logging via `Rinsen.Gelf`

### Admin UI

- `src/Admin` — plain HTML/JS/Bootstrap 5, no build step (see its [README](src/Admin/README.md))

It signs in against the backend's own login page and calls the same admin APIs as the app's own UI.

### Sample

- `sample/SampleServer` — a small resource server that validates the JWTs issued by Outback.

## Repository layout

| Path | Description |
| --- | --- |
| `src/Outback/Rinsen.Outback` | OAuth 2.1 / OpenID Connect 1.0 library (NuGet: `Rinsen.Outback`) |
| `src/OutbackApp/Rinsen.IdentityProvider` | Local accounts, TOTP, sessions and user management |
| `src/OutbackApp/Rinsen.Outback.App` | Identity provider web app (Razor, Bootstrap 5) |
| `src/OutbackApp/Rinsen.Outback.App.Installation` | Database installer and data seeder |
| `src/Admin` | Admin app in plain HTML/JS/Bootstrap 5 |
| `src/Aspire` | Aspire app host: SQL Server + installer + app + admin app |
| `sample/SampleServer` | Sample resource server |
| `tests/` | xUnit tests for the library and the identity provider |

## Prerequisites

- [.NET 11 SDK](https://dotnet.microsoft.com/download)
- Node.js 22 or newer (only for the admin app)
- Docker (for the Aspire-hosted SQL Server) — or an existing SQL Server instance

## Getting started

### Option 1: Aspire (recommended)

The Aspire app host starts SQL Server, runs the database installer, then the identity provider app and the admin app.

```powershell
cd src/Aspire/AspireOutback.AppHost

# Provide the secrets once (they are read from user-secrets, not from source):
dotnet user-secrets set "Parameters:sql-password" "<strong-password>"
dotnet user-secrets set "Parameters:login-debug-password" "<strong-password>"
dotnet user-secrets set "Parameters:login-runtime-password" "<strong-password>"
dotnet user-secrets set "Parameters:invitation-code" "<random-32+-char-code>"

# Start everything:
dotnet run
```

Notes:

- The invitation code gates anonymous self-registration and must be a strong random value of at least 32 characters (the app fails closed below that). Generate one with e.g. `openssl rand -hex 16`.
- The installer creates the `Outback` database, the `OutbackDebug`/`OutbackRuntime` logins and seeds the default clients.
- The app is then reachable at its external endpoint (Development profile: `https://localhost:56399`), and the admin app at its own external endpoint.

### Option 2: Manual

1. **Install the database** — run the installer with `Command=Install`, the `DatabaseName`, a `ConnectionString`, and the `Login:Debug:Password` / `Login:Runtime:Password` values (via environment variables or CLI arguments). It creates the database, the `OutbackDebug`/`OutbackRuntime` logins, the tables, and seeds the default clients.

2. **Configure the app** — the app refuses to start without a connection string. See `secrets.json.sample` for the expected shape:

   ```powershell
   cd src/OutbackApp/Rinsen.Outback.App
   dotnet user-secrets set "ConnectionStrings:Outback" "Server=127.0.0.1;Database=Outback;User Id=OutbackRuntime;Password=<password>"
   dotnet user-secrets set "Rinsen:InvitationCode" "<random-32+-char-code>"
   ```

3. **Run the app**:

   ```powershell
   dotnet run
   ```

   Open `https://localhost:56399`.

### Admin app

```powershell
cd src/Admin
npm install
npm start
```

Open `http://localhost:8123`. The dev server serves the static files and proxies `/api/*`, `/identity/*` and `/outback/*` to the backend, so the browser only talks to one origin (the same setup as production).

## Configuration

| Key | Description |
| --- | --- |
| `ConnectionStrings:Outback` | SQL Server connection string. **Required** — the app throws at startup without it. |
| `Rinsen:InvitationCode` | Strong random code (≥ 32 chars) required for self-registration. |
| `Rinsen:TotpIssuer` | Issuer name shown in authenticator apps (default `Outback`). |
| `Rinsen:ClientId` / `Rinsen:ClientSecret` | The app's own OAuth client credentials (used when it consumes tokens). |
| `Rinsen:GelfHost` | Optional GELF host for production logging (port 12202). |
| `Rinsen:RateLimiting` | Optional rate-limit settings for credential endpoints (defaults: 10 requests / 60 s). |

## Endpoints

| Endpoint | Description |
| --- | --- |
| `GET /.well-known/openid-configuration` | OpenID Connect discovery document |
| `GET /.well-known/openid-configuration/jwks` | JSON Web Key Set (ES256) |
| `GET/POST /oauth/authorize` | Authorization endpoint (PKCE `S256`, `code`, `form_post`) |
| `POST /oauth/token` | Token endpoint (authorization code, client credentials, refresh token, device code) |
| `GET /oauth/userinfo` | OpenID Connect userinfo |
| `POST /oauth/device_authorization` | Device authorization |
| `/Identity/*` | Login, self-registration, TOTP enablement, two-factor sign-in, logout |
| `/Device/*` | Device flow consent pages |
| `GET/POST /Outback/api/Client` (+ `/Family`, `/GrantTypes`) | Client management API |
| `/Outback/api/Scope` | Scope management API |
| `/Outback/api/Random` | Random string/ID helpers |
| `GET /api/Session` | Current session information |
| `GET /api/Admin/configuration` | Read the runtime configuration |
| `GET /api/Admin/backup/download` | Download a database backup |

## API documentation

In Development the app serves four OpenAPI documents and a Swagger UI:

- `/openapi/outback.json` — client and scope configuration
- `/openapi/oauth.json` — OpenID Connect and OAuth endpoints
- `/openapi/session.json` — session information
- `/openapi/operation.json` — operational/admin tasks

The admin app generates its API client from these documents (`npm run generate` in `src/Admin`).

## Testing

The tests run on [Microsoft Testing Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform), which is invoked with `dotnet run` rather than `dotnet test`:

```powershell
dotnet run --project tests/Rinsen.Outback.Tests
dotnet run --project tests/Rinsen.IdentityProvider.Tests
```

The test suites cover the authorization code, device, client credentials and refresh token flows (including rotation, reuse and expiration), token lifetimes, the well-known endpoints, rate limiting, client secret handling, account lockout, TOTP single-use, refresh token families, and login timing.

## Docker

- `src/OutbackApp/Rinsen.Outback.App/Dockerfile` — the identity provider app
- `src/OutbackApp/Rinsen.Outback.App.Installation/Dockerfile` — the database installer

GitHub Actions publishes the images to `ghcr.io` on push to `main` and on `v*.*.*` tags (`docker-publish.yml`, `docker-publish-installation.yml`), and runs a secret scan on every change.

## Security notes

- No secrets are committed to source; the Aspire host reads them from user-secrets and the previously committed values are treated as compromised (SEC-018).
- Credential endpoints (login, two-factor, create, token) are rate-limited (SEC-016).
- Client secrets are compared in constant time and can be rehashed on change.
- TOTP codes are single-use; accounts lock out after repeated failures; disabled accounts are rejected.
- Refresh tokens are rotated and tracked as families: each authorization code is assigned a family, the refresh token issued for it and its rotations share that family, and reusing a consumed token revokes the whole family (RFC 9700 §10.5).
- Audit logging is built into the identity provider.

## License

[MIT](LICENSE) © Fredrik Rinsén
