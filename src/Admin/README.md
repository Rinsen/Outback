# Outback Admin

Plain **HTML + JavaScript + Bootstrap 5** admin app for Outback. No frameworks,
no build step — just static files, ES modules and a small zero-dependency Node
server for local development. Bootstrap is installed via npm and synced into
`assets/` (see [Updating Bootstrap](#updating-bootstrap)); a small QR-code
generator is vendored directly into `assets/` (see
[QR code library](#qr-code-library)).

PoC scope:

- a **live session card** (name, expiration, sign in / out) on the dashboard,
- a **top navigation bar** with the session state and sign in/out,
- a **left sidebar** with links to Clients, Scopes, Users, Sessions and Settings,
- a **Clients page** with a sortable/paginated grid (Client ID, Name,
  Description, Type, Status) — double-click a row to open the full details —
  plus create / update / delete modals,
- a **Users page** with a sortable/paginated grid (Name, Email, Phone, Login ID,
  Admin, 2FA, Status) — double-click a row to open the user — plus create,
  update, reset password, enable / disable 2FA and delete modals. The Enable 2FA
  modal renders a scannable `otpauth://` **QR code** next to the one-time secret,
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
| `npm run sync:bootstrap` | Copy Bootstrap from `node_modules` into `assets/` (`tools/sync-bootstrap.mjs`). Runs automatically after `npm install` (postinstall). |

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
├── clients.html             # Clients: grid + toolbar + create/update/delete modals
├── users.html               # Users: grid + toolbar + create/update/password/2FA/delete modals
├── scopes.html              # Placeholder pages (sidebar links)
├── sessions.html
├── settings.html
├── assets/
│   ├── css/
│   │   ├── app.css                    # App shell styling
│   │   ├── bootstrap.min.css          # Synced from npm (sync:bootstrap)
│   │   └── bootstrap.min.css.map
│   ├── js/
│   │   ├── bootstrap.bundle.min.js    # Synced from npm (sync:bootstrap)
│   │   ├── bootstrap.bundle.min.js.map
│   │   ├── qr.js                      # Vendored QR generator (MIT) — classic script
│   │   └── qr.mjs                     # Vendored QR generator — ES module wrapper
│   ├── bootstrap-LICENSE              # Bootstrap's MIT license
│   └── qr-LICENSE                     # QR generator's MIT license
├── js/
│   ├── core/
│   │   ├── config.js        # API_BASE (same-origin by default)
│   │   ├── http.js          # fetch wrapper: timeouts, HttpError, JSON
│   │   ├── ui.js            # toast, formatDateTime, formatDuration, boolBadge, setActiveNav, escapeHtml
│   │   ├── grid.js          # Reusable data grid: columns, sorting, pagination, row selection
│   │   ├── toolbar.js       # Reusable button toolbar (add/enable/disable buttons by id)
│   │   ├── modal.js         # Thin wrapper around a Bootstrap modal (show/hide, onHidden)
│   │   └── list-editor.js   # Reusable add/remove list editor for collection properties
│   ├── api/
│   │   ├── session.js       # GENERATED — DO NOT EDIT
│   │   └── outback.js       # GENERATED — DO NOT EDIT
│   ├── layout.js            # Top nav + sidebar + session badge + signIn/signOut
│   └── pages/
│       ├── index.js         # Dashboard page module (session card)
│       ├── clients.js       # Clients page: grid, toolbar, create/update/delete modals
│       ├── users.js         # Users page: grid, toolbar, create/update/password/2FA/delete modals
│       ├── scopes.js
│       ├── sessions.js
│       └── settings.js
├── tools/
│   ├── generate-clients.mjs # OpenAPI -> js/api/*.js generator
│   ├── static-server.mjs    # Dev server: static files + same-origin proxy
│   └── sync-bootstrap.mjs   # Copy Bootstrap from node_modules into assets/
└── package.json
```

Each page loads exactly two app modules: `js/layout.js` (shared chrome) and
its own page module. Bootstrap is installed via npm and synced into `assets/`
— no CDN.

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

## Updating Bootstrap

Bootstrap is a regular npm dependency; `assets/` holds a synced copy of the
published dist files so production can ship pure static files without
`node_modules`.

To update:

```powershell
npm update bootstrap      # bump to the latest 5.x
npm run sync:bootstrap    # re-copy the dist files into assets/
```

`sync:bootstrap` also runs automatically after every `npm install`
(postinstall), so `assets/` can never drift from the installed version.
Review the diff in `assets/` before committing.

## QR code library

The Enable 2FA modal renders a scannable `otpauth://` QR code so the secret can
be added to an authenticator app by scanning. The generator is a small,
dependency-free, single-file MIT library vendored directly into
`assets/js/` (`qr.js` + its `qr.mjs` ES-module wrapper) — it is **not** an npm
dependency, so there is nothing to install or sync. `users.js` imports it as an
ES module:

```js
import { QRCode } from "../../assets/js/qr.mjs";
QRCode.toSVG(otpauthUri, { scale: 5, margin: 4 }); // -> an <svg> element
```

The secret never leaves the browser; the QR is generated entirely client-side.
To update the library, replace `assets/js/qr.js` and `assets/js/qr.mjs` with a
newer copy and review the diff before committing.

## Production notes

In production the app is served from the **same origin** as the API behind a
Kubernetes load balancer (e.g. `/` → admin static files, `/api`, `/identity`,
`/outback` → backend). `API_BASE` in `js/core/config.js` stays empty, and no
CORS configuration is required. The `tools/static-server.mjs` proxy is a
local-development convenience only and is not part of the production path.
