# Outback Admin

Plain **HTML + JavaScript + Bootstrap 5** admin app for Outback. No frameworks,
no build step, no npm dependencies — just static files, ES modules and a small
zero-dependency Node server for local development.

PoC scope:

- a **live session card** (name, expiration, sign in / out) on the dashboard,
- a **top navigation bar** with the session state and sign in/out,
- a **left sidebar** with placeholders for Clients, Scopes, Sessions and Settings,
- **API clients generated** from the backend's OpenAPI documents (committed).

## Quick start

1. Start the Outback backend (it must be running for the app and for
   `npm run generate`). In this repo, run the `Rinsen.Outback.App` project
   (`https://localhost:56399` per its launchSettings).
2. Start the dev server:

   ```powershell
   cd src/Admin
   npm start
   ```

3. Open <http://localhost:8123/>.

The dev server serves the static files and **proxies** `/api/*`, `/identity/*`
and `/outback/*` to the backend, so the browser only ever talks to a single
origin — the same setup as production, where a Kubernetes load balancer fronts
both apps behind one origin. No CORS configuration is needed.

## Scripts

| Script           | What it does                                                              |
| ---------------- | ------------------------------------------------------------------------- |
| `npm start`      | Serve the app + proxy the API (`tools/static-server.mjs`).                |
| `npm run generate` | Regenerate `js/api/*.js` from the backend's live OpenAPI documents (`/openapi/session.json`, `/openapi/outback.json`). Requires the backend to be running. |

## Environment variables (`npm start`)

| Variable             | Default                    | Description |
| -------------------- | -------------------------- | ----------- |
| `PORT`               | `8123`                     | Port to listen on. |
| `HOST`               | `127.0.0.1`                | Interface to bind. |
| `API_BASE`           | `https://localhost:56399`  | Backend base URL for the proxy. |
| `API_REQUIRE_SECURE` | *(unset)*                  | Set to `1` to enforce TLS certificate validation on the backend connection. Skipped by default because the backend uses the self-signed ASP.NET development certificate, which Node does not trust. |
| `KEEP_COOKIE_SECURE` | *(unset)*                  | Set to `1` to keep the `Secure` attribute on `Set-Cookie` headers. Stripped by default because the frontend is plain HTTP and the browser would otherwise refuse to store the session cookie on non-localhost hosts. |

## Signing in / out

The admin app never implements the login form itself. "Sign in" navigates to
the backend's own login page (same origin via the proxy):

```
/Identity/Login?returnUrl=<this page>
```

After the backend validates the credentials it sets the session cookie and
redirects back to `returnUrl`. "Sign out" navigates to `/Identity/Logout`,
which clears the cookie and redirects to the backend's logged-out page.

The `loginUrl`/`logoutUrl` values returned by `GET /api/Session` are **not**
used for navigation because they are stale (they point at the backend's own
HTTP endpoint, which would take the browser cross-origin).

## Project layout

```
Admin/
├── index.html               # Dashboard (live session card)
├── clients.html             # Placeholder pages (sidebar links)
├── scopes.html
├── sessions.html
├── settings.html
├── assets/
│   ├── css/
│   │   ├── app.css          # App shell styling
│   │   └── bootstrap.min.css  # Bootstrap 5.3.9 (vendored, pinned)
│   └── js/
│       └── bootstrap.bundle.min.js
├── js/
│   ├── core/
│   │   ├── config.js        # API_BASE (same-origin by default)
│   │   ├── http.js          # fetch wrapper: timeouts, HttpError, JSON
│   │   └── ui.js            # toast, formatDateTime, setActiveNav, escapeHtml
│   ├── api/
│   │   ├── session.js       # GENERATED — DO NOT EDIT
│   │   └── outback.js       # GENERATED — DO NOT EDIT
│   ├── layout.js            # Top nav + sidebar + session badge + signIn/signOut
│   └── pages/
│       ├── index.js         # Dashboard page module (session card)
│       ├── clients.js       # Placeholder modules
│       ├── scopes.js
│       ├── sessions.js
│       └── settings.js
├── tools/
│   ├── generate-clients.mjs # OpenAPI -> js/api/*.js generator
│   └── static-server.mjs    # Dev server: static files + same-origin proxy
└── package.json
```

Each page loads exactly two app modules: `js/layout.js` (shared chrome) and
its own page module. Bootstrap is vendored locally — no CDN.

## Regenerating the API clients

The backend documents do not define operationIds, so function names are
derived from the routes: drop the `api`/`outback` segments, pascal-case the
rest (including `{placeholders}`) and prefix the HTTP method:

```
GET  /api/Session                -> getSession()
POST /Outback/api/Client         -> postClient(body)
GET  /Outback/api/Client/{id}    -> getClientId(id)
GET  /Outback/api/Random/{count} -> getRandomCount(count)
```

Run with the backend up:

```powershell
npm run generate
```

## Production notes

In production the app is served from the **same origin** as the API behind a
Kubernetes load balancer (e.g. `/` → admin static files, `/api`, `/identity`,
`/outback` → backend). `API_BASE` in `js/core/config.js` stays empty, and no
CORS configuration is required. The `tools/static-server.mjs` proxy is a
local-development convenience only and is not part of the production path.
