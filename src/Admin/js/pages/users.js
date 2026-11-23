// js/pages/users.js
//
// Users: a grid of all Outback users (identity + its 1:1 local account) with
// a toolbar above it. Grid (Name, Email, Phone, Login ID, Admin, 2FA, Status),
// loading/error states, a Refresh button, and selection-driven
// Update / Reset password / Enable 2FA / Disable 2FA / Delete enablement.
// Double-click a row to open the user (the Update modal).
// "New user" create modal (no Login ID — it is set to the email server-side),
// a Reset password modal, an Enable 2FA modal that shows the one-time TOTP
// secret, a Disable 2FA confirmation, and a Delete confirmation (soft delete).

import {
  getUser,
  postUser,
  putUserId,
  deleteUserId,
  postUserIdPassword,
  postUserIdTotpEnable,
  postUserIdTotpDisable,
} from "../api/outback.js";
import { Grid } from "../core/grid.js";
import { Modal } from "../core/modal.js";
import { Toolbar } from "../core/toolbar.js";
import { escapeHtml, toast } from "../core/ui.js";
import { QRCode } from "../../assets/js/qr.mjs";

/** Renders "Given Surname" (or "—" when both are empty). */
function fullName(row) {
  const name = `${row.givenName ?? ""} ${row.surname ?? ""}`.trim();
  return name || "—";
}

/** Renders an Admin/User badge. */
function adminBadge(value) {
  return value
    ? '<span class="badge text-bg-primary">Admin</span>'
    : '<span class="badge text-bg-secondary">User</span>';
}

/** Renders a 2FA on/off badge. */
function totpBadge(value) {
  return value
    ? '<span class="badge text-bg-success">2FA on</span>'
    : '<span class="badge text-bg-secondary">2FA off</span>';
}

/** Renders an Active/Disabled badge (isDisabled is inverted). */
function statusBadge(isDisabled) {
  return isDisabled
    ? '<span class="badge text-bg-danger">Disabled</span>'
    : '<span class="badge text-bg-success">Active</span>';
}

/** Grid columns: the user's identity at a glance. Everything else is
 *  shown and edited in the Update modal. */
const COLUMNS = [
  {
    key: "name",
    label: "Name",
    render: (_v, row) => escapeHtml(fullName(row)),
    sortValue: (row) => fullName(row).toLowerCase(),
  },
  { key: "email", label: "Email", render: (v) => (v ? escapeHtml(v) : "—") },
  { key: "phoneNumber", label: "Phone", render: (v) => (v ? escapeHtml(v) : "—") },
  {
    key: "loginId",
    label: "Login ID",
    render: (v) => (v ? `<code>${escapeHtml(v)}</code>` : "—"),
  },
  { key: "isAdministrator", label: "Admin", render: (v) => adminBadge(v), width: "100px" },
  { key: "twoFactorTotpEnabled", label: "2FA", render: (v) => totpBadge(v), width: "110px" },
  { key: "isDisabled", label: "Status", render: (v) => statusBadge(v), width: "110px" },
];

const EMPTY_MESSAGE = "No users yet.";
const EMPTY_MESSAGE_ERROR = "Could not load users.";

const gridEl = document.getElementById("users-grid");
const toolbarEl = document.getElementById("users-toolbar");
const errorEl = document.getElementById("users-error");
const errorMessageEl = document.getElementById("users-error-message");
const errorRetryEl = document.getElementById("users-error-retry");

const toolbar = new Toolbar(toolbarEl);

const grid = new Grid(gridEl, {
  columns: COLUMNS,
  selectable: true,
  rowKey: (row) => row.identityId,
  pageSize: 10,
  pageSizes: [10, 25, 50],
  sortKey: "name",
  sortDir: "asc",
  emptyMessage: EMPTY_MESSAGE,
  onSelectionChange: updateSelectionButtons,
});

// Double-click a row to open the user (the Update modal shows every field).
// The checkbox cell is excluded so selection clicks never open a modal.
grid.tbodyEl.addEventListener("dblclick", (event) => {
  const tr = event.target.closest("tr");
  if (!tr || event.target.closest(".grid-check-cell")) return;
  for (const [row, rowEl] of grid.rowEls) {
    if (rowEl === tr) {
      openUpdateModal(row);
      return;
    }
  }
});

// --- Selection-driven button enablement -------------------------------------

/**
 * Enables the single-user actions (Update, Reset password, Enable/Disable 2FA)
 * when exactly one row is selected, and Delete when one or more are selected.
 * Enable/Disable 2FA additionally respect the selected user's current 2FA state.
 */
function updateSelectionButtons(selected) {
  const one = selected.length === 1 ? selected[0] : null;
  toolbar.setButtonDisabled("update", !one);
  toolbar.setButtonDisabled("reset-password", !one);
  toolbar.setButtonDisabled("enable-2fa", !one || !!one.twoFactorTotpEnabled);
  toolbar.setButtonDisabled("disable-2fa", !one || !one.twoFactorTotpEnabled);
  toolbar.setButtonDisabled("delete", selected.length < 1);
  toolbar.setButtonLabel(
    "delete",
    selected.length ? `Delete (${selected.length})` : "Delete",
  );
}

// --- Toolbar ------------------------------------------------------------------

toolbar.addButton({
  id: "create",
  label: "New user",
  variant: "primary",
  onClick: () => openCreateModal(),
});
toolbar.addButton({
  id: "update",
  label: "Update",
  disabled: true,
  onClick: () => {
    const selected = grid.getSelected();
    if (selected.length !== 1) return;
    openUpdateModal(selected[0]);
  },
});
toolbar.addButton({
  id: "reset-password",
  label: "Reset password",
  disabled: true,
  onClick: () => openResetPasswordModal(),
});
toolbar.addButton({
  id: "enable-2fa",
  label: "Enable 2FA",
  disabled: true,
  onClick: () => openEnableTotpModal(),
});
toolbar.addButton({
  id: "disable-2fa",
  label: "Disable 2FA",
  disabled: true,
  onClick: () => openDisableTotpModal(),
});
toolbar.addButton({
  id: "delete",
  label: "Delete",
  variant: "outline-danger",
  disabled: true,
  onClick: () => openDeleteModal(),
});
toolbar.addButton({
  id: "refresh",
  label: "Refresh",
  className: "btn btn-sm btn-outline-secondary ms-auto",
  onClick: () => loadUsers(),
});

let loading = false;

/** Fetches all users and renders them into the grid. */
async function loadUsers() {
  if (loading) return;
  loading = true;
  grid.setLoading(true);
  hideError();
  try {
    const users = await getUser();
    grid.setEmptyMessage(EMPTY_MESSAGE);
    grid.setData(Array.isArray(users) ? users : []);
  } catch (error) {
    grid.setLoading(false);
    if (!grid.rows.length) grid.setEmptyMessage(EMPTY_MESSAGE_ERROR);
    showError(error);
  } finally {
    loading = false;
  }
}

function showError(error) {
  errorMessageEl.textContent =
    error?.status === 401
      ? "You are not signed in. Use the Sign in button in the top bar, then try again."
      : error?.message || "Could not load users.";
  errorEl.classList.remove("d-none");
}

function hideError() {
  errorEl.classList.add("d-none");
}

errorRetryEl.addEventListener("click", () => loadUsers());

// --- Shared form helpers ------------------------------------------------------

/** Toggles a button's busy state (spinner + disabled). */
function setButtonBusy(button, busy, idleLabel, busyLabel) {
  button.disabled = busy;
  button.innerHTML = busy
    ? `<span class="spinner-border spinner-border-sm me-2" role="status"></span>${escapeHtml(busyLabel)}`
    : escapeHtml(idleLabel);
}

/** Shows an inline error alert, or hides it when message is null/empty. */
function setFormError(alertEl, message) {
  if (message) {
    alertEl.textContent = message;
    alertEl.classList.remove("d-none");
  } else {
    alertEl.textContent = "";
    alertEl.classList.add("d-none");
  }
}

// --- New user -----------------------------------------------------------------

const createForm = document.createElement("form");
createForm.id = "create-user-form";
createForm.className = "needs-validation";
createForm.noValidate = true;
createForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="row g-3">
    <div class="col-md-6">
      <label class="form-label" for="create-given-name">Given name</label>
      <input class="form-control" id="create-given-name" name="givenName" required>
      <div class="invalid-feedback">A given name is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="create-surname">Surname</label>
      <input class="form-control" id="create-surname" name="surname" required>
      <div class="invalid-feedback">A surname is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="create-email">Email</label>
      <input type="email" class="form-control" id="create-email" name="email" required>
      <div class="invalid-feedback">A valid email is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="create-phone">Phone</label>
      <input class="form-control" id="create-phone" name="phoneNumber">
    </div>
    <div class="col-12">
      <label class="form-label" for="create-password">Password</label>
      <input type="password" class="form-control" id="create-password" name="password" required>
      <div class="invalid-feedback">A password is required.</div>
      <div class="form-text">
        The login ID is set to the email automatically.
      </div>
    </div>
  </div>`;

const createModal = new Modal({
  title: "New user",
  body: createForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="create-user-form" class="btn btn-primary">Create</button>`,
  onHidden: () => {
    createForm.reset();
    createForm.classList.remove("was-validated");
    setFormError(createForm.querySelector(".alert"), null);
  },
});

const createErrorEl = createForm.querySelector(".alert");
const createSubmitEl = createModal.footerEl.querySelector('button[type="submit"]');

let createBusy = false;

function openCreateModal() {
  createModal.show();
}

createForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (createBusy) return;
  if (!createForm.checkValidity()) {
    createForm.classList.add("was-validated");
    return;
  }
  setFormError(createErrorEl, null);
  createBusy = true;
  setButtonBusy(createSubmitEl, true, "Create", "Creating…");
  try {
    const created = await postUser({
      givenName: createForm.elements["givenName"].value.trim(),
      surname: createForm.elements["surname"].value.trim(),
      email: createForm.elements["email"].value.trim(),
      phoneNumber: createForm.elements["phoneNumber"].value.trim(),
      password: createForm.elements["password"].value,
    });
    createModal.hide();
    toast(created?.email || "user", { variant: "success", title: "User created." });
    loadUsers();
  } catch (error) {
    setFormError(createErrorEl, error?.message || "Could not create the user.");
  } finally {
    createBusy = false;
    setButtonBusy(createSubmitEl, false, "Create", "Creating…");
  }
});

// --- Update -------------------------------------------------------------------

const updateForm = document.createElement("form");
updateForm.id = "update-user-form";
updateForm.className = "needs-validation";
updateForm.noValidate = true;
updateForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="row g-3">
    <div class="col-md-6">
      <label class="form-label" for="update-given-name">Given name</label>
      <input class="form-control" id="update-given-name" name="givenName" required>
      <div class="invalid-feedback">A given name is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-surname">Surname</label>
      <input class="form-control" id="update-surname" name="surname" required>
      <div class="invalid-feedback">A surname is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-email">Email</label>
      <input type="email" class="form-control" id="update-email" name="email" required>
      <div class="invalid-feedback">A valid email is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-phone">Phone</label>
      <input class="form-control" id="update-phone" name="phoneNumber">
    </div>
    <div class="col-12">
      <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="update-admin" name="isAdministrator">
        <label class="form-check-label" for="update-admin">Administrator</label>
      </div>
    </div>
    <div class="col-12">
      <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="update-disabled" name="isDisabled">
        <label class="form-check-label" for="update-disabled">Disabled</label>
      </div>
    </div>
  </div>`;

const updateModal = new Modal({
  title: "Update user",
  body: updateForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="update-user-form" class="btn btn-primary">Save</button>`,
  onHidden: () => {
    updateForm.reset();
    updateForm.classList.remove("was-validated");
    setFormError(updateForm.querySelector(".alert"), null);
  },
});

const updateErrorEl = updateForm.querySelector(".alert");
const updateSubmitEl = updateModal.footerEl.querySelector('button[type="submit"]');

let updateRow = null;
let updateBusy = false;

function fillUpdateForm(row) {
  const el = (name) => updateForm.elements[name];
  el("givenName").value = row.givenName ?? "";
  el("surname").value = row.surname ?? "";
  el("email").value = row.email ?? "";
  el("phoneNumber").value = row.phoneNumber ?? "";
  el("isAdministrator").checked = !!row.isAdministrator;
  el("isDisabled").checked = !!row.isDisabled;
}

function openUpdateModal(row) {
  if (updateBusy) return;
  updateRow = row;
  updateModal.setTitle(`Update ${fullName(row)}`);
  updateForm.reset();
  updateForm.classList.remove("was-validated");
  setFormError(updateErrorEl, null);
  updateModal.show();
  fillUpdateForm(row);
}

updateForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (updateBusy) return;
  if (!updateForm.checkValidity()) {
    updateForm.classList.add("was-validated");
    return;
  }
  if (!updateRow) return;
  setFormError(updateErrorEl, null);
  updateBusy = true;
  setButtonBusy(updateSubmitEl, true, "Save", "Saving…");
  try {
    await putUserId(updateRow.identityId, {
      givenName: updateForm.elements["givenName"].value.trim(),
      surname: updateForm.elements["surname"].value.trim(),
      email: updateForm.elements["email"].value.trim(),
      phoneNumber: updateForm.elements["phoneNumber"].value.trim(),
      isAdministrator: updateForm.elements["isAdministrator"].checked,
      isDisabled: updateForm.elements["isDisabled"].checked,
    });
    updateModal.hide();
    toast(updateRow.email || "user", { variant: "success", title: "User updated." });
    loadUsers();
  } catch (error) {
    setFormError(updateErrorEl, error?.message || "Could not update the user.");
  } finally {
    updateBusy = false;
    setButtonBusy(updateSubmitEl, false, "Save", "Saving…");
  }
});

// --- Reset password -----------------------------------------------------------

const resetForm = document.createElement("form");
resetForm.id = "reset-password-form";
resetForm.className = "needs-validation";
resetForm.noValidate = true;
resetForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <p class="text-secondary small mb-3" id="reset-password-target"></p>
  <div class="mb-0">
    <label class="form-label" for="reset-password">New password</label>
    <input type="password" class="form-control" id="reset-password" name="password" required>
    <div class="invalid-feedback">A password is required.</div>
  </div>`;

const resetModal = new Modal({
  title: "Reset password",
  body: resetForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="reset-password-form" class="btn btn-primary">Reset</button>`,
  onHidden: () => {
    resetForm.reset();
    resetForm.classList.remove("was-validated");
    setFormError(resetForm.querySelector(".alert"), null);
  },
});

const resetErrorEl = resetForm.querySelector(".alert");
const resetTargetEl = resetForm.querySelector("#reset-password-target");
const resetSubmitEl = resetModal.footerEl.querySelector('button[type="submit"]');

let resetRow = null;
let resetBusy = false;

function openResetPasswordModal() {
  const selected = grid.getSelected();
  if (selected.length !== 1) return;
  resetRow = selected[0];
  resetModal.setTitle(`Reset password — ${fullName(resetRow)}`);
  resetTargetEl.textContent = `Set a new password for ${resetRow.email || fullName(resetRow)}.`;
  resetForm.reset();
  resetForm.classList.remove("was-validated");
  setFormError(resetErrorEl, null);
  resetModal.show();
}

resetForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (resetBusy) return;
  if (!resetForm.checkValidity()) {
    resetForm.classList.add("was-validated");
    return;
  }
  if (!resetRow) return;
  setFormError(resetErrorEl, null);
  resetBusy = true;
  setButtonBusy(resetSubmitEl, true, "Reset", "Resetting…");
  try {
    await postUserIdPassword(resetRow.identityId, {
      password: resetForm.elements["password"].value,
    });
    resetModal.hide();
    toast(resetRow.email || "user", { variant: "success", title: "Password reset." });
    loadUsers();
  } catch (error) {
    setFormError(resetErrorEl, error?.message || "Could not reset the password.");
  } finally {
    resetBusy = false;
    setButtonBusy(resetSubmitEl, false, "Reset", "Resetting…");
  }
});

// --- Enable 2FA ---------------------------------------------------------------

const totpBodyEl = document.createElement("div");

const totpModal = new Modal({
  title: "Enable 2FA",
  body: totpBodyEl,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
    <button type="button" class="btn btn-primary totp-enable-btn">Enable</button>`,
});

const totpEnableBtn = totpModal.footerEl.querySelector(".totp-enable-btn");

let totpRow = null;
let totpBusy = false;

function totpConfirmHtml() {
  return `
    <p class="text-secondary small mb-0">
      This generates a new TOTP secret for
      <strong>${escapeHtml(fullName(totpRow))}</strong>.
      The secret is shown only once — save it now (it is what your authenticator app needs).
    </p>`;
}

/**
 * Builds the standard `otpauth://` URI an authenticator app scans. The raw
 * base32 secret stays available for manual entry. Parameters (SHA1 / 6 digits
 * / 30s period) match the backend's Otp.NET `new Totp(secret)` defaults.
 */
function totpOtpauthUri(secret, issuer, label) {
  const enc = encodeURIComponent;
  const params = [
    `secret=${secret}`,
    `issuer=${enc(issuer)}`,
    "algorithm=SHA1",
    "digits=6",
    "period=30",
  ].join("&");
  return `otpauth://totp/${enc(issuer)}:${enc(label)}?${params}`;
}

function totpSecretHtml(secret) {
  return `
    <p class="text-secondary small mb-2">
      2FA is now enabled. Scan the QR code with your authenticator app, or
      enter the secret manually — it will not be shown again.
    </p>
    <div class="d-flex align-items-start gap-3">
      <div
        class="totp-qr bg-white p-2 rounded border flex-shrink-0"
        role="img"
        aria-label="TOTP QR code"
      ></div>
      <div class="input-group mb-0" style="max-width: 320px;">
        <code class="form-control font-monospace text-break">${escapeHtml(secret)}</code>
        <button type="button" class="btn btn-outline-secondary totp-copy" title="Copy to clipboard">Copy</button>
      </div>
    </div>`;
}

/**
 * Renders the scannable `otpauth://` QR code into the `.totp-qr` container.
 * Best-effort: on any failure the container shows a note and the secret text
 * (already rendered) remains the way to add the account manually.
 */
function renderTotpQr(bodyEl, secret, issuer, row) {
  const qrEl = bodyEl.querySelector(".totp-qr");
  if (!qrEl) return;
  try {
    const name = issuer || "Outback";
    const label = row.email || row.loginId || fullName(row);
    const uri = totpOtpauthUri(secret, name, label);
    qrEl.appendChild(QRCode.toSVG(uri, { scale: 5, margin: 4 }));
  } catch (error) {
    console.warn("Could not render the TOTP QR code.", error);
    qrEl.innerHTML =
      '<span class="text-secondary small">QR unavailable — enter the secret manually.</span>';
  }
}

function openEnableTotpModal() {
  const selected = grid.getSelected();
  if (selected.length !== 1) return;
  totpRow = selected[0];
  totpModal.setTitle(`Enable 2FA — ${fullName(totpRow)}`);
  totpBodyEl.innerHTML = totpConfirmHtml();
  totpEnableBtn.style.display = "";
  totpEnableBtn.disabled = false;
  totpEnableBtn.textContent = "Enable";
  totpModal.show();
}

totpEnableBtn.addEventListener("click", async () => {
  if (!totpRow || totpBusy) return;
  totpBusy = true;
  setButtonBusy(totpEnableBtn, true, "Enable", "Enabling…");
  try {
    const result = await postUserIdTotpEnable(totpRow.identityId);
    const secret = result?.secret || "";
    totpBodyEl.innerHTML = totpSecretHtml(secret);
    totpEnableBtn.style.display = "none";
    renderTotpQr(totpBodyEl, secret, result?.issuer, totpRow);
    totpBodyEl.querySelector(".totp-copy").addEventListener("click", async () => {
      try {
        await navigator.clipboard.writeText(secret);
        toast("Secret copied to clipboard.", { variant: "success" });
      } catch {
        toast("Could not copy to clipboard.", { variant: "danger" });
      }
    });
    loadUsers();
  } catch (error) {
    totpModal.hide();
    toast(error?.message || "Could not enable 2FA.", { variant: "danger" });
  } finally {
    totpBusy = false;
    setButtonBusy(totpEnableBtn, false, "Enable", "Enabling…");
  }
});

// --- Disable 2FA --------------------------------------------------------------

const disableTotpModal = new Modal({
  title: "Disable 2FA",
  body: `
    <p class="text-secondary small mb-0" id="disable-totp-target"></p>`,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="button" class="btn btn-danger disable-totp-confirm">Disable</button>`,
});

const disableTotpTargetEl = disableTotpModal.bodyEl.querySelector("#disable-totp-target");
const disableTotpConfirmEl = disableTotpModal.footerEl.querySelector(".disable-totp-confirm");

let disableTotpRow = null;
let disableTotpBusy = false;

function openDisableTotpModal() {
  const selected = grid.getSelected();
  if (selected.length !== 1) return;
  disableTotpRow = selected[0];
  disableTotpModal.setTitle(`Disable 2FA — ${fullName(disableTotpRow)}`);
  disableTotpTargetEl.textContent = `Remove TOTP from ${disableTotpRow.email || fullName(disableTotpRow)}?`;
  disableTotpModal.show();
}

disableTotpConfirmEl.addEventListener("click", async () => {
  if (!disableTotpRow || disableTotpBusy) return;
  disableTotpBusy = true;
  setButtonBusy(disableTotpConfirmEl, true, "Disable", "Disabling…");
  try {
    await postUserIdTotpDisable(disableTotpRow.identityId);
    disableTotpModal.hide();
    toast(disableTotpRow.email || "user", { variant: "success", title: "2FA disabled." });
    loadUsers();
  } catch (error) {
    disableTotpModal.hide();
    toast(error?.message || "Could not disable 2FA.", { variant: "danger" });
  } finally {
    disableTotpBusy = false;
    setButtonBusy(disableTotpConfirmEl, false, "Disable", "Disabling…");
  }
});

// --- Delete -------------------------------------------------------------------

const deleteListEl = document.createElement("ul");
deleteListEl.className = "list-unstyled small mb-0";
deleteListEl.style.maxHeight = "160px";
deleteListEl.style.overflowY = "auto";

const deleteModal = new Modal({
  title: "Delete users",
  body: `
    <p class="text-secondary small mb-2">
      This removes the selected user(s). They will no longer be able to sign in.
    </p>`,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="button" class="btn btn-danger delete-confirm">Delete</button>`,
});
deleteModal.bodyEl.appendChild(deleteListEl);

const deleteConfirmEl = deleteModal.footerEl.querySelector(".delete-confirm");

let deleteBusy = false;

function openDeleteModal() {
  const selected = grid.getSelected();
  if (!selected.length) return;
  deleteModal.setTitle(
    `Delete ${selected.length} user${selected.length === 1 ? "" : "s"}?`,
  );
  deleteListEl.innerHTML = selected
    .map((row) => `<li>${escapeHtml(row.email || fullName(row))}</li>`)
    .join("");
  deleteModal.show();
}

deleteConfirmEl.addEventListener("click", async () => {
  const selected = grid.getSelected();
  if (!selected.length || deleteBusy) return;
  deleteBusy = true;
  setButtonBusy(deleteConfirmEl, true, "Delete", "Deleting…");
  try {
    // The API routes on the identityId (GUID).
    await Promise.all(selected.map((row) => deleteUserId(row.identityId)));
    deleteModal.hide();
    grid.clearSelection();
    toast(
      selected.length === 1
        ? selected[0].email || "user"
        : `${selected.length} users`,
      { variant: "success", title: "Deleted." },
    );
    loadUsers();
  } catch (error) {
    deleteModal.hide();
    // Re-fetch so the grid reflects whatever actually got deleted.
    toast(error?.message || "Could not delete the user(s).", { variant: "danger" });
    loadUsers();
  } finally {
    deleteBusy = false;
    setButtonBusy(deleteConfirmEl, false, "Delete", "Deleting…");
  }
});

loadUsers();
