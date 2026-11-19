#!/usr/bin/env node
// tools/static-server.mjs
//
// Local development server for the Outback admin app (src/Admin).
//
//   - Serves the static files in this folder over plain HTTP.
//   - Proxies /api/*, /identity/* and /outback/* (case-insensitive) to the
//     Outback backend, so the browser only ever talks to a single origin.
//     This mirrors production, where a load balancer fronts both apps on one
//     origin; the browser never needs CORS.
//
// Usage:
//   node tools/static-server.mjs        (or: npm start)
//
// Environment:
//   PORT                Port to listen on (default 8123).
//   HOST                Interface to bind (default 127.0.0.1).
//   API_BASE            Backend base URL (default https://localhost:56399).
//                       Under Aspire the backend URL is injected as OUTBACKAPP,
//                       which is used when API_BASE is not set.
//   API_REQUIRE_SECURE  Set to 1 to enforce TLS certificate validation on the
//                       backend connection. Skipped by default, because the
//                       backend uses the self-signed ASP.NET development
//                       certificate which Node does not trust.
//   KEEP_COOKIE_SECURE  Set to 1 to keep the "Secure" attribute on
//                       Set-Cookie headers. Stripped by default, because the
//                       frontend is served over plain HTTP and the browser
//                       would otherwise refuse to store the session cookie on
//                       non-localhost hosts.
//
// Uses Node built-ins only.

import { createServer } from "node:http";
import { request as httpRequest } from "node:http";
import { request as httpsRequest } from "node:https";
import { readFile, stat } from "node:fs/promises";
import { extname, join, normalize, resolve, sep } from "node:path";
import { dirname } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const PORT = Number(process.env.PORT || 8123);
const HOST = process.env.HOST || "127.0.0.1";
const API_BASE = (process.env.API_BASE || process.env.OUTBACKAPP || "https://localhost:56399").replace(/\/+$/, "");
const REQUIRE_SECURE = process.env.API_REQUIRE_SECURE === "1";
const STRIP_COOKIE_SECURE = process.env.KEEP_COOKIE_SECURE !== "1";
const BACKEND_TIMEOUT_MS = 30000;

const CONTENT_TYPES = {
  ".html": "text/html; charset=utf-8",
  ".js": "text/javascript; charset=utf-8",
  ".mjs": "text/javascript; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".map": "application/json; charset=utf-8",
  ".svg": "image/svg+xml",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".gif": "image/gif",
  ".ico": "image/x-icon",
  ".woff": "font/woff",
  ".woff2": "font/woff2",
  ".txt": "text/plain; charset=utf-8",
};

const PROXY_PREFIXES = ["/api", "/identity", "/outback"];
const HOP_BY_HOP = new Set([
  "connection",
  "keep-alive",
  "proxy-authenticate",
  "proxy-authorization",
  "te",
  "trailers",
  "transfer-encoding",
  "upgrade",
]);

function isProxyPath(pathname) {
  const p = pathname.toLowerCase();
  return PROXY_PREFIXES.some((prefix) => p === prefix || p.startsWith(prefix + "/"));
}

function sendError(res, status, message) {
  res.writeHead(status, { "content-type": "text/html; charset=utf-8" });
  res.end(
    `<!doctype html><html><head><meta charset="utf-8"><title>${status}</title></head>` +
      `<body style="font-family:system-ui,sans-serif;padding:3rem"><h1>${status}</h1><p>${message}</p>` +
      `<p><a href="/">Back to the admin app</a></p></body></html>`,
  );
}

/** Serves a file from the app root, traversal-safe. */
async function serveStatic(req, res) {
  if (req.method !== "GET" && req.method !== "HEAD") {
    sendError(res, 405, "Method not allowed.");
    return;
  }

  const url = new URL(req.url, "http://localhost");
  let pathname;
  try {
    pathname = decodeURIComponent(url.pathname);
  } catch {
    sendError(res, 400, "Bad request.");
    return;
  }

  if (pathname.endsWith("/")) pathname += "index.html";

  const filePath = resolve(join(ROOT, normalize(pathNameFix(pathname))));
  if (filePath !== ROOT && !filePath.startsWith(ROOT + sep)) {
    sendError(res, 403, "Forbidden.");
    return;
  }

  let info = await stat(filePath).catch(() => null);
  if (info?.isDirectory()) {
    filePath = join(filePath, "index.html");
    info = await stat(filePath).catch(() => null);
  }
  if (!info) {
    sendError(res, 404, `Not found: ${url.pathname}`);
    return;
  }

  const body = await readFile(filePath);
  const type = CONTENT_TYPES[extname(filePath).toLowerCase()] || "application/octet-stream";
  res.writeHead(200, { "content-type": type, "content-length": body.length });
  if (req.method === "HEAD") res.end();
  else res.end(body);
}

// decodeURIComponent already ran; normalise backslashes (Windows) and strip
// any leading separator so join() cannot escape the root.
function pathNameFix(pathname) {
  return pathname.replace(/\\/g, "/").replace(/^\/+/, "");
}

/**
 * Forwards the request to the backend and pipes the response back, rewriting
 * headers so the browser stays on this origin:
 *   - hop-by-hop headers are dropped,
 *   - absolute Location headers pointing at the backend become relative,
 *   - "Secure" is stripped from Set-Cookie (see KEEP_COOKIE_SECURE).
 */
function proxy(req, res) {
  let target;
  try {
    target = new URL(req.url, API_BASE);
  } catch {
    sendError(res, 400, "Bad request.");
    return;
  }

  const headers = {};
  for (const [key, value] of Object.entries(req.headers)) {
    if (HOP_BY_HOP.has(key.toLowerCase()) || key.toLowerCase() === "host") continue;
    headers[key] = value;
  }
  headers.host = target.host;

  const lib = target.protocol === "https:" ? httpsRequest : httpRequest;
  const upstream = lib(
    target,
    {
      method: req.method,
      headers,
      timeout: BACKEND_TIMEOUT_MS,
      ...(target.protocol === "https:" ? { rejectUnauthorized: !REQUIRE_SECURE } : {}),
    },
    (upstreamRes) => {
      const responseHeaders = { ...upstreamRes.headers };

      // Rewrite an absolute redirect to the backend origin into a relative
      // path, so the browser never leaves this origin.
      const location = responseHeaders.location;
      if (location) {
        try {
          const loc = new URL(location);
          if (loc.origin === target.origin) {
            responseHeaders.location = loc.pathname + loc.search;
          }
        } catch {
          /* relative location — forward as-is */
        }
      }

      if (Array.isArray(responseHeaders["set-cookie"])) {
        responseHeaders["set-cookie"] = responseHeaders["set-cookie"].map((cookie) =>
          STRIP_COOKIE_SECURE ? cookie.replace(/;\s*Secure\b/gi, "") : cookie,
        );
      }

      delete responseHeaders["transfer-encoding"];
      res.writeHead(upstreamRes.statusCode, responseHeaders);
      upstreamRes.pipe(res);
    },
  );

  upstream.on("timeout", () => upstream.destroy(new Error("backend timed out")));
  upstream.on("error", (error) => {
    if (!res.headersSent) {
      sendError(res, 502, `Could not reach the Outback backend at ${API_BASE}: ${error.message}`);
    } else {
      res.destroy();
    }
  });
  res.on("close", () => {
    if (!res.writableEnded) upstream.destroy();
  });

  // Stream the request body (form posts, JSON, ...) through untouched.
  req.pipe(upstream);
}

const server = createServer((req, res) => {
  const started = Date.now();
  const finish = () => {
    console.log(
      `[admin] ${new Date().toISOString()} ${req.method} ${req.url} -> ${res.statusCode} (${Date.now() - started} ms)`,
    );
  };
  res.on("finish", finish);
  res.on("close", () => {
    if (!res.writableEnded && !res.headersSent) console.log(`[admin] ${req.method} ${req.url} -> (aborted)`);
  });

  try {
    const url = new URL(req.url, "http://localhost");
    if (isProxyPath(url.pathname)) proxy(req, res);
    else serveStatic(req, res).catch((error) => sendError(res, 500, error.message));
  } catch (error) {
    sendError(res, 500, error.message);
  }
});

server.listen(PORT, HOST, () => {
  console.log(`[admin] serving ${ROOT}`);
  console.log(`[admin] http://${HOST}:${PORT}/`);
  console.log(`[admin] proxying ${PROXY_PREFIXES.map((p) => `${p}/*`).join(", ")} -> ${API_BASE}`);
  if (!REQUIRE_SECURE) console.log("[admin] TLS validation against the backend is DISABLED (set API_REQUIRE_SECURE=1 to enforce)");
  if (STRIP_COOKIE_SECURE) console.log('[admin] "Secure" attribute is STRIPPED from Set-Cookie (set KEEP_COOKIE_SECURE=1 to keep it)');
});
