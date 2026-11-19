// js/pages/index.js
//
// Dashboard: the PoC "live session card". Shows what the backend reports for
// this browser's session (name, expiration, auth state) with sign in/out and
// refresh actions.

import { loadSession, signIn, signOut } from "../layout.js";
import { formatDateTime, escapeHtml } from "../core/ui.js";

const card = document.getElementById("session-card");

function bindRefresh() {
  card.querySelectorAll("[data-action=refresh]").forEach((button) => {
    button.addEventListener("click", refresh);
  });
}

function bindSignOut() {
  card.querySelectorAll("[data-action=signout]").forEach((button) => {
    button.addEventListener("click", signOut);
  });
}

function renderSignedIn(session) {
  card.innerHTML = `
    <div class="d-flex justify-content-between align-items-start gap-2">
      <div>
        <h1 class="h4 mb-1">Signed in</h1>
        <p class="text-secondary mb-0">
          This is the session the Outback backend sees for this browser.
        </p>
      </div>
      <span class="badge text-bg-success fs-6">Active</span>
    </div>
    <dl class="row mb-0 mt-4">
      <dt class="col-sm-4">Name</dt>
      <dd class="col-sm-8 fw-semibold">${escapeHtml(session.name || "—")}</dd>
      <dt class="col-sm-4">Expires</dt>
      <dd class="col-sm-8">${formatDateTime(session.expiration)}</dd>
    </dl>
    <div class="d-flex gap-2 mt-4">
      <button type="button" class="btn btn-primary" data-action="refresh">Refresh</button>
      <button type="button" class="btn btn-outline-secondary" data-action="signout">Sign out</button>
    </div>`;
  bindRefresh();
  bindSignOut();
}

function renderSignedOut() {
  card.innerHTML = `
    <div class="d-flex justify-content-between align-items-start gap-2">
      <div>
        <h1 class="h4 mb-1">Signed out</h1>
        <p class="text-secondary mb-0">
          No active session. Sign in to start using Outback.
        </p>
      </div>
      <span class="badge text-bg-secondary fs-6">Inactive</span>
    </div>
    <div class="d-flex gap-2 mt-4">
      <button type="button" class="btn btn-primary" data-action="signin">Sign in</button>
      <button type="button" class="btn btn-outline-secondary" data-action="refresh">Refresh</button>
    </div>`;
  card.querySelector("[data-action=signin]").addEventListener("click", signIn);
  bindRefresh();
}

function renderError(error) {
  card.innerHTML = `
    <div class="d-flex justify-content-between align-items-start gap-2">
      <div>
        <h1 class="h4 mb-1">Backend unreachable</h1>
        <p class="text-secondary mb-0">
          The session could not be loaded. Is the Outback backend running?
        </p>
      </div>
      <span class="badge text-bg-danger fs-6">Error</span>
    </div>
    <pre class="mt-3 mb-0 small p-2 bg-light border rounded text-break">${escapeHtml(error?.message || "Unknown error")}</pre>
    <div class="d-flex gap-2 mt-4">
      <button type="button" class="btn btn-primary" data-action="refresh">Try again</button>
    </div>`;
  bindRefresh();
}

function render(result) {
  if (result.session) renderSignedIn(result.session);
  else if (result.error) renderError(result.error);
  else renderSignedOut();
}

async function refresh() {
  const buttons = card.querySelectorAll("button");
  buttons.forEach((button) => (button.disabled = true));
  render(await loadSession());
}

render(await loadSession());
