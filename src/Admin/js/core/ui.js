// Small UI helpers shared across pages. Bootstrap 5 (vendored in assets/) is
// expected to be loaded before the page scripts run.

/**
 * Shows a Bootstrap toast in the shared #toast-container.
 * @param {string} message
 * @param {object} [options]
 * @param {"primary"|"secondary"|"success"|"danger"|"warning"} [options.variant="secondary"]
 * @param {string} [options.title]
 * @param {number} [options.duration=4000]
 */
export function toast(message, { variant = "secondary", title, duration = 4000 } = {}) {
  let container = document.getElementById("toast-container");
  if (!container) {
    container = document.createElement("div");
    container.id = "toast-container";
    container.className = "toast-container position-fixed bottom-0 end-0 p-3";
    document.body.appendChild(container);
  }

  const item = document.createElement("div");
  item.className = `toast align-items-center text-bg-${variant} border-0`;
  item.setAttribute("role", "alert");
  const bodyText = title
    ? `<strong>${escapeHtml(title)}</strong> ${escapeHtml(message)}`
    : escapeHtml(message);
  item.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${bodyText}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>`;
  container.appendChild(item);

  const toastInstance = new bootstrap.Toast(item, { delay: duration });
  item.addEventListener("hidden.bs.toast", () => item.remove());
  toastInstance.show();
}

/**
 * Formats a Unix timestamp in seconds as a local date-time string.
 * @param {number} [unixSeconds]
 * @returns {string}
 */
export function formatDateTime(unixSeconds) {
  if (!unixSeconds) return "—";
  return new Date(unixSeconds * 1000).toLocaleString();
}

/**
 * Formats a duration in seconds as a short human-readable string
 * (at most two largest units), e.g. "30 days", "1 day 2 hours", "45 seconds".
 * @param {number|null|undefined} seconds
 * @returns {string}
 */
export function formatDuration(seconds) {
  if (seconds == null) return "—";
  if (seconds <= 0) return "0 seconds";
  const units = [
    { label: "year", size: 365 * 24 * 60 * 60 },
    { label: "day", size: 24 * 60 * 60 },
    { label: "hour", size: 60 * 60 },
    { label: "minute", size: 60 },
  ];
  let remaining = Math.round(seconds);
  const parts = [];
  for (const unit of units) {
    if (parts.length === 2) break;
    const value = Math.floor(remaining / unit.size);
    if (value > 0) {
      parts.push(`${value} ${unit.label}${value === 1 ? "" : "s"}`);
      remaining -= value * unit.size;
    }
  }
  if (parts.length === 0) return `${remaining} seconds`;
  return parts.join(" ");
}

/**
 * Renders an Active/Inactive badge for a boolean value.
 * @param {boolean|null|undefined} value
 * @returns {string} HTML
 */
export function boolBadge(value) {
  return value
    ? '<span class="badge text-bg-success">Active</span>'
    : '<span class="badge text-bg-secondary">Inactive</span>';
}

/**
 * Highlights the sidebar item for the given page.
 * @param {string} page
 */
export function setActiveNav(page) {
  document.querySelectorAll("[data-nav]").forEach((el) => {
    el.classList.toggle("active", el.dataset.nav === page);
  });
}

/** Escapes a value for safe insertion into an HTML template. */
export function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
