// js/layout.js
//
// Builds the shared chrome (top nav + left sidebar) around <main id="main">
// and keeps the session state in the top nav in sync with GET /api/Session.
//
// Loaded once per page (before the page module). Exports:
//   loadSession()  -> Promise<{session: SessionInformation | null, error: Error | null}>
//   signIn()       -> navigates to the backend login page
//   signOut()      -> navigates to the backend logout endpoint

import { APP_NAME, APP_SUFFIX } from "./core/config.js";
import { getSession } from "./api/session.js";
import { escapeHtml } from "./core/ui.js";

/** Sidebar / top-level navigation. */
export const PAGES = [
  { page: "dashboard", label: "Dashboard", href: "/index.html" },
  { page: "clients", label: "Clients", href: "/clients.html" },
  { page: "scopes", label: "Scopes", href: "/scopes.html" },
  { page: "sessions", label: "Sessions", href: "/sessions.html" },
  { page: "settings", label: "Settings", href: "/settings.html" },
];

/**
 * Signs in by navigating to the backend's login page. The backend renders its
 * own form (antiforgery token included) and, after a successful POST, redirects
 * back to `returnUrl` — this page, same-origin.
 */
export function signIn() {
  window.location.assign(
    `/Identity/Login?returnUrl=${encodeURIComponent(window.location.href)}`,
  );
}

/** Signs out via the backend's logout endpoint (clears the session cookie). */
export function signOut() {
  window.location.assign("/Identity/Logout");
}

let pending = null;

/**
 * Fetches the current session and updates the top-nav badge.
 * Concurrent callers share one in-flight request; each call afterwards
 * fetches fresh, so "Refresh" and the page card never fight over state.
 *
 * @returns {Promise<{session: import("./api/session.js").SessionInformation | null, error: Error | null}>}
 *   `session` is null when signed out or when the backend could not be
 *   reached; `error` is set only in the latter case.
 */
export function loadSession() {
  if (!pending) pending = doLoadSession();
  return pending;
}

async function doLoadSession() {
  try {
    const session = await getSession();
    applySession(session);
    return { session, error: null };
  } catch (error) {
    applySession(null);
    return { session: null, error };
  } finally {
    pending = null;
  }
}

function applySession(session) {
  const area = document.getElementById("nav-session");
  if (!area) return;

  if (session?.isAuthenticated) {
    area.innerHTML = `
      <span class="navbar-text me-3">
        <span class="d-none d-sm-inline">Signed in as</span>
        <strong title="${escapeHtml(session.name || "")}">${escapeHtml(session.name || "—")}</strong>
      </span>
      <button type="button" class="btn btn-outline-light btn-sm" data-action="signout">Sign out</button>`;
    area.querySelector("[data-action=signout]").addEventListener("click", signOut);
  } else {
    area.innerHTML = `
      <button type="button" class="btn btn-primary btn-sm" data-action="signin">Sign in</button>`;
    area.querySelector("[data-action=signin]").addEventListener("click", signIn);
  }
}

function buildShell() {
  const main = document.getElementById("main");
  if (!main) return;
  const page = document.body.dataset.page || "dashboard";

  const navItems = PAGES.map((p) => `
    <li class="nav-item">
      <a class="nav-link${p.page === page ? " active" : ""}" data-nav="${p.page}" href="${p.href}">
        ${p.label}
      </a>
    </li>`).join("");

  const shell = document.createElement("div");
  shell.className = "app-shell";
  shell.innerHTML = `
    <nav class="navbar navbar-dark bg-dark app-topnav">
      <div class="container-fluid">
        <button class="navbar-toggler app-sidebar-toggler d-lg-none" type="button"
                data-sidebar-toggle aria-label="Toggle sidebar" aria-expanded="false">
          <span class="navbar-toggler-icon"></span>
        </button>
        <a class="navbar-brand fw-semibold" href="/index.html">
          ${APP_NAME} <span class="badge text-bg-secondary ms-1 align-middle">${APP_SUFFIX}</span>
        </a>
        <div class="ms-auto d-flex align-items-center" id="nav-session">
          <span class="navbar-text session-loading">Loading session…</span>
        </div>
      </div>
    </nav>
    <div class="app-body">
      <nav class="app-sidebar" id="app-sidebar" aria-label="Sections">
        <ul class="nav nav-pills flex-column app-nav">${navItems}</ul>
        <div class="app-sidebar-footer text-secondary small">
          Outback admin &middot; local
        </div>
      </nav>
      <div class="app-content" id="app-content"></div>
      <div class="app-backdrop d-none" id="app-backdrop"></div>
    </div>`;

  main.insertAdjacentElement("beforebegin", shell);
  shell.querySelector("#app-content").appendChild(main);

  const toggle = shell.querySelector("[data-sidebar-toggle]");
  const backdrop = shell.querySelector("#app-backdrop");
  const closeSidebar = () => document.body.classList.remove("sidebar-open");
  toggle.addEventListener("click", () => {
    const open = document.body.classList.toggle("sidebar-open");
    backdrop.classList.toggle("d-none", !open);
    toggle.setAttribute("aria-expanded", String(open));
  });
  backdrop.addEventListener("click", closeSidebar);
}

buildShell();
loadSession();
