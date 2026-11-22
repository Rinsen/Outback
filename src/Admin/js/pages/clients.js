// js/pages/clients.js
//
// Clients: a grid of all Outback clients with a toolbar above it.
// Phase B: grid (Client ID, Name, Description, Type, Status),
// loading/error states, a Refresh button, and selection-driven
// Update/Delete enablement. Double-click a row to open the client.
// Phase C: "New client" (name, description, type, family) and
// "New client family" (name, description) create modals, and a Delete
// confirmation modal that hard-deletes the selected clients.
// Phase D: Update modal — a tabbed form (General, Tokens, Scopes, Secrets,
// Claims, Grant types, Redirect URIs, CORS origins) that edits every scalar
// field and all seven collections, saved with PUT /Outback/api/Client/{id}.

import {
  getClient,
  postClient,
  postClientFamily,
  deleteClientId,
  getClientFamily,
  putClientId,
  getRandomCount,
  getScope,
  getClientGrantTypes,
} from "../api/outback.js";
import { Grid } from "../core/grid.js";
import { ListEditor } from "../core/list-editor.js";
import { Modal } from "../core/modal.js";
import { Toolbar } from "../core/toolbar.js";
import { boolBadge, escapeHtml, toast } from "../core/ui.js";

/** ClientType enum values (Rinsen.Outback.Clients.ClientType). */
const CLIENT_TYPES = { 0: "Confidential", 1: "Public" };

/** Renders a Confidential/Public badge. */
function typeBadge(value) {
  const label = CLIENT_TYPES[value] ?? value;
  const cls =
    value === 0 ? "text-bg-primary" : value === 1 ? "text-bg-info" : "text-bg-secondary";
  return `<span class="badge ${cls}">${escapeHtml(label)}</span>`;
}

/** Max characters shown in the Description column before truncating. */
const DESCRIPTION_LIMIT = 80;

/** Renders a description truncated to DESCRIPTION_LIMIT characters,
 *  with the full text available in a hover tooltip. */
function truncateDescription(value) {
  if (!value) return '<span class="text-secondary">—</span>';
  const text = String(value);
  const short =
    text.length > DESCRIPTION_LIMIT ? `${text.slice(0, DESCRIPTION_LIMIT)}…` : text;
  return `<span class="text-secondary" title="${escapeHtml(text)}">${escapeHtml(short)}</span>`;
}

/** Grid columns: the client's identity at a glance — everything else
 *  (status flags, lifetimes, scopes, secrets, claims, URIs, CORS) is
 *  shown and edited in the Update modal. */
const COLUMNS = [
  {
    key: "clientId",
    label: "Client ID",
    render: (v) => (v ? `<code>${escapeHtml(v)}</code>` : "—"),
    width: "320px",
  },
  { key: "name", label: "Name" },
  { key: "description", label: "Description", render: (v) => truncateDescription(v) },
  { key: "clientType", label: "Type", render: (v) => typeBadge(v), width: "120px" },
  { key: "active", label: "Status", render: (v) => boolBadge(v), width: "100px" },
];

const EMPTY_MESSAGE = "No clients yet.";
const EMPTY_MESSAGE_ERROR = "Could not load clients.";

const gridEl = document.getElementById("clients-grid");
const toolbarEl = document.getElementById("clients-toolbar");
const errorEl = document.getElementById("clients-error");
const errorMessageEl = document.getElementById("clients-error-message");
const errorRetryEl = document.getElementById("clients-error-retry");

const toolbar = new Toolbar(toolbarEl);

const grid = new Grid(gridEl, {
  columns: COLUMNS,
  selectable: true,
  rowKey: (row) => row.id,
  pageSize: 10,
  pageSizes: [10, 25, 50],
  sortKey: "name",
  sortDir: "asc",
  emptyMessage: EMPTY_MESSAGE,
  onSelectionChange: (selected) => {
    // Update applies to exactly one client; Delete to one or more.
    toolbar.setButtonDisabled("update", selected.length !== 1);
    toolbar.setButtonDisabled("delete", selected.length < 1);
    toolbar.setButtonLabel(
      "delete",
      selected.length ? `Delete (${selected.length})` : "Delete",
    );
  },
});

// Double-click a row to open the client (the Update modal shows every
// field and collection). The checkbox cell is excluded so selection
// clicks never open a modal.
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

// Toolbar. Create buttons first, then the selection-driven actions
// (Update lands in Phase D), and Refresh right-aligned.
toolbar.addButton({
  id: "create",
  label: "New client",
  variant: "primary",
  onClick: () => openCreateClientModal(),
});
toolbar.addButton({
  id: "create-family",
  label: "New client family",
  onClick: () => createFamilyModal.show(),
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
  onClick: () => loadClients(),
});

let loading = false;

/** Fetches all clients and renders them into the grid. */
async function loadClients() {
  if (loading) return;
  loading = true;
  grid.setLoading(true);
  hideError();
  try {
    const clients = await getClient();
    grid.setEmptyMessage(EMPTY_MESSAGE);
    grid.setData(Array.isArray(clients) ? clients : []);
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
      : error?.message || "Could not load clients.";
  errorEl.classList.remove("d-none");
}

function hideError() {
  errorEl.classList.add("d-none");
}

errorRetryEl.addEventListener("click", () => loadClients());

// --- Shared form helpers ----------------------------------------------------

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

// --- New client -------------------------------------------------------------

const createClientForm = document.createElement("form");
createClientForm.id = "create-client-form";
createClientForm.className = "needs-validation";
createClientForm.noValidate = true;
createClientForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="mb-3">
    <label class="form-label" for="create-client-name">Name</label>
    <input class="form-control" id="create-client-name" name="clientName" required>
    <div class="invalid-feedback">A name is required.</div>
  </div>
  <div class="mb-3">
    <label class="form-label" for="create-client-description">Description</label>
    <input class="form-control" id="create-client-description" name="description">
  </div>
  <div class="mb-3">
    <label class="form-label" for="create-client-type">Type</label>
    <select class="form-select" id="create-client-type" name="clientType">
      <option value="0" selected>Confidential</option>
      <option value="1">Public</option>
    </select>
  </div>
  <div class="mb-1">
    <label class="form-label" for="create-client-family">Family</label>
    <select class="form-select" id="create-client-family" name="familyId" required>
      <option value="" selected disabled>Loading families…</option>
    </select>
    <div class="invalid-feedback">Choose a family.</div>
    <div class="form-text create-client-family-hint d-none">
      No client families yet — use "New client family" to create one.
    </div>
  </div>`;

const createClientModal = new Modal({
  title: "New client",
  body: createClientForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="create-client-form" class="btn btn-primary">Create</button>`,
  onHidden: () => {
    createClientForm.reset();
    createClientForm.classList.remove("was-validated");
    setFormError(createClientForm.querySelector(".alert"), null);
  },
});

const createClientErrorEl = createClientForm.querySelector(".alert");
const createClientFamilyEl = createClientForm.querySelector("#create-client-family");
const createClientFamilyHintEl = createClientForm.querySelector(".create-client-family-hint");
const createClientSubmitEl = createClientModal.footerEl.querySelector('button[type="submit"]');

let createClientBusy = false;

/** Fills the family select from the API. @returns {Promise<boolean>} Whether any families exist. */
async function loadFamilies() {
  const families = await getClientFamily();
  createClientFamilyEl.innerHTML = "";
  const list = Array.isArray(families) ? families : [];
  if (!list.length) {
    const option = document.createElement("option");
    option.value = "";
    option.textContent = "No families yet";
    option.disabled = true;
    option.selected = true;
    createClientFamilyEl.appendChild(option);
    return false;
  }
  const placeholder = document.createElement("option");
  placeholder.value = "";
  placeholder.textContent = "Choose a family…";
  placeholder.disabled = true;
  placeholder.selected = true;
  createClientFamilyEl.appendChild(placeholder);
  for (const family of list) {
    const option = document.createElement("option");
    option.value = String(family.id);
    option.textContent = family.name;
    createClientFamilyEl.appendChild(option);
  }
  return true;
}

async function openCreateClientModal() {
  createClientFamilyEl.innerHTML =
    '<option value="" selected disabled>Loading families…</option>';
  createClientFamilyHintEl.classList.add("d-none");
  createClientModal.show();
  try {
    const hasFamilies = await loadFamilies();
    if (!hasFamilies) createClientFamilyHintEl.classList.remove("d-none");
  } catch (error) {
    setFormError(createClientErrorEl, error?.message || "Could not load client families.");
  }
}

createClientForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (createClientBusy) return;
  if (!createClientForm.checkValidity()) {
    createClientForm.classList.add("was-validated");
    return;
  }
  setFormError(createClientErrorEl, null);
  createClientBusy = true;
  setButtonBusy(createClientSubmitEl, true, "Create", "Creating…");
  try {
    const created = await postClient({
      clientName: createClientForm.elements["clientName"].value.trim(),
      description: createClientForm.elements["description"].value.trim(),
      clientType: Number(createClientForm.elements["clientType"].value),
      familyId: Number(createClientForm.elements["familyId"].value),
    });
    createClientModal.hide();
    toast(created?.name || "client", { variant: "success", title: "Client created." });
    loadClients();
  } catch (error) {
    setFormError(createClientErrorEl, error?.message || "Could not create the client.");
  } finally {
    createClientBusy = false;
    setButtonBusy(createClientSubmitEl, false, "Create", "Creating…");
  }
});

// --- New client family ------------------------------------------------------

const createFamilyForm = document.createElement("form");
createFamilyForm.id = "create-family-form";
createFamilyForm.className = "needs-validation";
createFamilyForm.noValidate = true;
createFamilyForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="mb-3">
    <label class="form-label" for="create-family-name">Name</label>
    <input class="form-control" id="create-family-name" name="name" required>
    <div class="invalid-feedback">A name is required.</div>
  </div>
  <div class="mb-1">
    <label class="form-label" for="create-family-description">Description</label>
    <input class="form-control" id="create-family-description" name="description">
  </div>`;

const createFamilyModal = new Modal({
  title: "New client family",
  body: createFamilyForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="create-family-form" class="btn btn-primary">Create</button>`,
  onHidden: () => {
    createFamilyForm.reset();
    createFamilyForm.classList.remove("was-validated");
    setFormError(createFamilyForm.querySelector(".alert"), null);
  },
});

const createFamilyErrorEl = createFamilyForm.querySelector(".alert");
const createFamilySubmitEl = createFamilyModal.footerEl.querySelector('button[type="submit"]');

let createFamilyBusy = false;

createFamilyForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (createFamilyBusy) return;
  if (!createFamilyForm.checkValidity()) {
    createFamilyForm.classList.add("was-validated");
    return;
  }
  setFormError(createFamilyErrorEl, null);
  createFamilyBusy = true;
  setButtonBusy(createFamilySubmitEl, true, "Create", "Creating…");
  try {
    const created = await postClientFamily({
      name: createFamilyForm.elements["name"].value.trim(),
      description: createFamilyForm.elements["description"].value.trim(),
    });
    createFamilyModal.hide();
    toast(created?.name || "family", { variant: "success", title: "Client family created." });
    loadClients();
  } catch (error) {
    setFormError(createFamilyErrorEl, error?.message || "Could not create the client family.");
  } finally {
    createFamilyBusy = false;
    setButtonBusy(createFamilySubmitEl, false, "Create", "Creating…");
  }
});

// --- Delete -----------------------------------------------------------------

const deleteListEl = document.createElement("ul");
deleteListEl.className = "list-unstyled small mb-0";
deleteListEl.style.maxHeight = "160px";
deleteListEl.style.overflowY = "auto";

const deleteModal = new Modal({
  title: "Delete clients",
  body: `
    <p class="text-secondary small mb-2">
      This permanently deletes the selected client(s). This action cannot be undone.
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
    `Delete ${selected.length} client${selected.length === 1 ? "" : "s"}?`,
  );
  deleteListEl.innerHTML = selected
    .map((row) => `<li>${escapeHtml(row.name || row.clientId || String(row.id))}</li>`)
    .join("");
  deleteModal.show();
}

deleteConfirmEl.addEventListener("click", async () => {
  const selected = grid.getSelected();
  if (!selected.length || deleteBusy) return;
  deleteBusy = true;
  setButtonBusy(deleteConfirmEl, true, "Delete", "Deleting…");
  try {
    // The API routes on the ClientId (GUID), not the numeric Id.
    await Promise.all(selected.map((row) => deleteClientId(row.clientId)));
    deleteModal.hide();
    grid.clearSelection();
    toast(
      selected.length === 1
        ? selected[0].name || "client"
        : `${selected.length} clients`,
      { variant: "success", title: "Deleted." },
    );
    loadClients();
  } catch (error) {
    deleteModal.hide();
    // Re-fetch so the grid reflects whatever actually got deleted.
    toast(error?.message || "Could not delete the client(s).", { variant: "danger" });
    loadClients();
  } finally {
    deleteBusy = false;
    setButtonBusy(deleteConfirmEl, false, "Delete", "Deleting…");
  }
});

// --- Update -----------------------------------------------------------------

const updateForm = document.createElement("form");
updateForm.id = "update-client-form";
updateForm.className = "needs-validation";
updateForm.noValidate = true;
updateForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <ul class="nav nav-tabs" role="tablist">
    <li class="nav-item" role="presentation">
      <button class="nav-link active" data-bs-toggle="tab" data-bs-target="#update-tab-general" type="button" role="tab">General</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-tokens" type="button" role="tab">Tokens</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-scopes" type="button" role="tab">Scopes</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-secrets" type="button" role="tab">Secrets</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-claims" type="button" role="tab">Claims</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-grant-types" type="button" role="tab">Grant types</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-redirect-uris" type="button" role="tab">Redirect URIs</button>
    </li>
    <li class="nav-item" role="presentation">
      <button class="nav-link" data-bs-toggle="tab" data-bs-target="#update-tab-cors" type="button" role="tab">CORS origins</button>
    </li>
  </ul>
  <div class="tab-content pt-3">
    <div class="tab-pane fade show active" id="update-tab-general" role="tabpanel">
      <div class="row g-3">
        <div class="col-md-6">
          <label class="form-label" for="update-name">Name</label>
          <input class="form-control" id="update-name" name="name" required>
          <div class="invalid-feedback">A name is required.</div>
        </div>
        <div class="col-md-6">
          <label class="form-label" for="update-description">Description</label>
          <input class="form-control" id="update-description" name="description">
        </div>
        <div class="col-md-6">
          <label class="form-label" for="update-client-type">Type</label>
          <select class="form-select" id="update-client-type" name="clientType">
            <option value="0">Confidential</option>
            <option value="1">Public</option>
          </select>
        </div>
        <div class="col-md-6">
          <label class="form-label" for="update-family">Family</label>
          <select class="form-select" id="update-family" name="clientFamilyId">
            <option value="" selected disabled>Loading families…</option>
          </select>
        </div>
        <div class="col-12">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-active" name="active">
            <label class="form-check-label" for="update-active">Active</label>
          </div>
        </div>
        <div class="col-md-6">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-consent-required" name="consentRequired">
            <label class="form-check-label" for="update-consent-required">Consent required</label>
          </div>
        </div>
        <div class="col-md-6">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-save-consent" name="saveConsent">
            <label class="form-check-label" for="update-save-consent">Save consent</label>
          </div>
        </div>
      </div>
    </div>
    <div class="tab-pane fade" id="update-tab-tokens" role="tabpanel">
      <div class="row g-3">
        <div class="col-md-4">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-issue-refresh-token" name="issueRefreshToken">
            <label class="form-check-label" for="update-issue-refresh-token">Issue refresh tokens</label>
          </div>
        </div>
        <div class="col-md-4">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-issue-identity-token" name="issueIdentityToken">
            <label class="form-check-label" for="update-issue-identity-token">Issue identity tokens</label>
          </div>
        </div>
        <div class="col-md-4">
          <div class="form-check form-switch">
            <input class="form-check-input" type="checkbox" id="update-user-info-claims" name="addUserInfoClaimsInIdentityToken">
            <label class="form-check-label" for="update-user-info-claims">User info claims in ID token</label>
          </div>
        </div>
        <div class="col-md-3">
          <label class="form-label" for="update-access-token-lifetime">Access token lifetime (s)</label>
          <input type="number" min="0" class="form-control" id="update-access-token-lifetime" name="accessTokenLifetime">
        </div>
        <div class="col-md-3">
          <label class="form-label" for="update-identity-token-lifetime">Identity token lifetime (s)</label>
          <input type="number" min="0" class="form-control" id="update-identity-token-lifetime" name="identityTokenLifetime">
        </div>
        <div class="col-md-3">
          <label class="form-label" for="update-refresh-token-lifetime">Refresh token lifetime (s)</label>
          <input type="number" min="0" class="form-control" id="update-refresh-token-lifetime" name="refreshTokenLifetime">
        </div>
        <div class="col-md-3">
          <label class="form-label" for="update-saved-consent-lifetime">Saved consent lifetime (s)</label>
          <input type="number" min="0" class="form-control" id="update-saved-consent-lifetime" name="savedConsentLifetime">
        </div>
        <div class="col-md-3">
          <label class="form-label" for="update-authority-code-lifetime">Authorization code lifetime (s)</label>
          <input type="number" min="0" class="form-control" id="update-authority-code-lifetime" name="authorityCodeLifetime">
        </div>
      </div>
    </div>
    <div class="tab-pane fade" id="update-tab-scopes" role="tabpanel">
      <div id="update-scopes-editor"></div>
    </div>
    <div class="tab-pane fade" id="update-tab-secrets" role="tabpanel">
      <p class="text-secondary small mb-2">
        Existing secrets are masked and cannot be changed — remove one and add a new one to replace it.
        New secrets are stored as a SHA-256 hash.
      </p>
      <div id="update-secrets-editor"></div>
      <button type="button" class="btn btn-outline-secondary btn-sm" id="update-add-secret">Add secret</button>
    </div>
    <div class="tab-pane fade" id="update-tab-claims" role="tabpanel">
      <div id="update-claims-editor"></div>
    </div>
    <div class="tab-pane fade" id="update-tab-grant-types" role="tabpanel">
      <div id="update-grant-types-editor"></div>
    </div>
    <div class="tab-pane fade" id="update-tab-redirect-uris" role="tabpanel">
      <h6 class="mb-2">Login redirect URIs</h6>
      <div id="update-login-uris-editor"></div>
      <h6 class="mt-3 mb-2">Post-logout redirect URIs</h6>
      <div id="update-post-logout-uris-editor"></div>
    </div>
    <div class="tab-pane fade" id="update-tab-cors" role="tabpanel">
      <div id="update-cors-editor"></div>
    </div>
  </div>`;

const updateModal = new Modal({
  title: "Update client",
  body: updateForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="update-client-form" class="btn btn-primary">Save</button>`,
  wide: true,
  onHidden: () => {
    updateForm.reset();
    updateForm.classList.remove("was-validated");
    setFormError(updateForm.querySelector(".alert"), null);
  },
});

const updateErrorEl = updateForm.querySelector(".alert");
const updateSubmitEl = updateModal.footerEl.querySelector('button[type="submit"]');
const updateFamilyEl = updateForm.querySelector("#update-family");
const secretsEditorEl = updateForm.querySelector("#update-secrets-editor");
const addSecretButton = updateForm.querySelector("#update-add-secret");

// --- Collection editors -------------------------------------------------------

const scopeField = {
  key: "scopeId",
  label: "Scope",
  type: "select",
  placeholder: "Choose a scope…",
  className: "col-md-8",
};

const scopeEditor = new ListEditor(updateForm.querySelector("#update-scopes-editor"), {
  fields: [scopeField],
  addLabel: "Add scope",
  emptyMessage: "No scopes assigned.",
});

const claimsEditor = new ListEditor(updateForm.querySelector("#update-claims-editor"), {
  fields: [
    { key: "type", label: "Type", className: "col-md-3" },
    { key: "value", label: "Value", className: "col-md-4" },
    { key: "description", label: "Description", className: "col-md-4" },
  ],
  addLabel: "Add claim",
  emptyMessage: "No claims.",
});

const grantField = {
  key: "grantType",
  label: "Grant type",
  type: "select",
  placeholder: "Choose a grant type…",
  className: "col-md-8",
};

const grantTypesEditor = new ListEditor(updateForm.querySelector("#update-grant-types-editor"), {
  fields: [grantField],
  addLabel: "Add grant type",
  emptyMessage: "No grant types.",
});

const loginUrisEditor = new ListEditor(updateForm.querySelector("#update-login-uris-editor"), {
  fields: [
    { key: "loginRedirectUri", label: "URI", placeholder: "https://…", className: "col-md-6" },
    { key: "description", label: "Description", className: "col-md-5" },
  ],
  addLabel: "Add login redirect URI",
  emptyMessage: "No login redirect URIs.",
});

const postLogoutUrisEditor = new ListEditor(
  updateForm.querySelector("#update-post-logout-uris-editor"),
  {
    fields: [
      { key: "postLogoutRedirectUri", label: "URI", placeholder: "https://…", className: "col-md-6" },
      { key: "description", label: "Description", className: "col-md-5" },
    ],
    addLabel: "Add post-logout redirect URI",
    emptyMessage: "No post-logout redirect URIs.",
  },
);

const corsEditor = new ListEditor(updateForm.querySelector("#update-cors-editor"), {
  fields: [
    { key: "origin", label: "Origin", placeholder: "https://…", className: "col-md-6" },
    { key: "description", label: "Description", className: "col-md-5" },
  ],
  addLabel: "Add origin",
  emptyMessage: "No CORS origins.",
});

// --- Secrets editor -----------------------------------------------------------
// Existing secrets are masked by the API and immutable (only the description
// can change); new ones are generated or typed and hashed server-side.

function secretRemoveCell() {
  const cell = document.createElement("div");
  cell.className = "col-auto pe-0";
  cell.innerHTML =
    '<button type="button" class="btn btn-outline-danger btn-sm secret-remove">Remove</button>';
  return cell;
}

function fillSecretsEditor(secrets) {
  secretsEditorEl.innerHTML = "";
  for (const secret of secrets) {
    const row = document.createElement("div");
    row.className = "row g-2 align-items-center mb-2 secret-row";
    row.dataset.secretId = secret.id;
    row.innerHTML = `
      <div class="col-md-4"><code class="text-secondary">${escapeHtml(secret.secret || "****")}</code></div>
      <div class="col-md-6">
        <input class="form-control form-control-sm" data-secret-desc placeholder="Description">
      </div>`;
    row.querySelector("[data-secret-desc]").value = secret.description ?? "";
    row.appendChild(secretRemoveCell());
    secretsEditorEl.appendChild(row);
  }
}

addSecretButton.addEventListener("click", async () => {
  if (addSecretButton.disabled) return;
  addSecretButton.disabled = true;
  let value = "";
  try {
    value = await getRandomCount(30);
  } catch {
    // Generation failed (e.g. not signed in) — the user can type a value.
  }
  const row = document.createElement("div");
  row.className = "row g-2 align-items-center mb-2 secret-row";
  row.innerHTML = `
    <div class="col-md-4">
      <input class="form-control form-control-sm" data-new-secret placeholder="Secret value">
    </div>
    <div class="col-md-6">
      <input class="form-control form-control-sm" data-secret-desc placeholder="Description">
    </div>`;
  row.querySelector("[data-new-secret]").value = value;
  row.appendChild(secretRemoveCell());
  secretsEditorEl.appendChild(row);
  row.querySelector("[data-new-secret]").focus();
  addSecretButton.disabled = false;
});

secretsEditorEl.addEventListener("click", (event) => {
  const button = event.target.closest(".secret-remove");
  if (button) button.closest(".secret-row").remove();
});

function readSecrets() {
  const secrets = [];
  for (const row of secretsEditorEl.querySelectorAll(".secret-row")) {
    const description = row.querySelector("[data-secret-desc]")?.value.trim() ?? "";
    if (row.dataset.secretId !== undefined && row.dataset.secretId !== "") {
      // Existing: the backend matches by id and keeps the stored hash.
      secrets.push({ id: Number(row.dataset.secretId), description });
    } else {
      const secret = row.querySelector("[data-new-secret]")?.value.trim() ?? "";
      if (secret) secrets.push({ secret, description });
    }
  }
  return secrets;
}

// --- Open / fill / save --------------------------------------------------------

let updateRow = null;
let updateBusy = false;

/** Fills the family select, keeping the client's current family selected. */
function fillFamilySelect(families, selectedId, fallbackName) {
  updateFamilyEl.innerHTML = "";
  const list = Array.isArray(families) ? families : [];
  if (list.length) {
    for (const family of list) {
      const option = document.createElement("option");
      option.value = String(family.id);
      option.textContent = family.name;
      option.selected = family.id === selectedId;
      updateFamilyEl.appendChild(option);
    }
  }
  if (!updateFamilyEl.selectedOptions.length) {
    // Current family is missing (or no families loaded): keep it as a
    // disabled entry so the client can still be saved with its family.
    const option = document.createElement("option");
    option.value = String(selectedId);
    option.textContent = fallbackName || `Family ${selectedId}`;
    option.disabled = true;
    option.selected = true;
    updateFamilyEl.appendChild(option);
  }
}

function fillUpdateForm(row) {
  const el = (name) => updateForm.elements[name];
  el("name").value = row.name ?? "";
  el("description").value = row.description ?? "";
  el("clientType").value = String(row.clientType ?? 0);
  el("active").checked = !!row.active;
  el("consentRequired").checked = !!row.consentRequired;
  el("saveConsent").checked = !!row.saveConsent;
  el("issueRefreshToken").checked = !!row.issueRefreshToken;
  el("issueIdentityToken").checked = !!row.issueIdentityToken;
  el("addUserInfoClaimsInIdentityToken").checked = !!row.addUserInfoClaimsInIdentityToken;
  el("accessTokenLifetime").value = row.accessTokenLifetime ?? "";
  el("identityTokenLifetime").value = row.identityTokenLifetime ?? "";
  el("refreshTokenLifetime").value = row.refreshTokenLifetime ?? "";
  el("savedConsentLifetime").value = row.savedConsentLifetime ?? "";
  el("authorityCodeLifetime").value = row.authorityCodeLifetime ?? "";

  scopeEditor.setItems((row.scopes || []).map((s) => ({ scopeId: s.scopeId })));
  fillSecretsEditor(row.secrets || []);
  claimsEditor.setItems(
    (row.clientClaims || []).map((c) => ({
      type: c.type,
      value: c.value,
      description: c.description,
    })),
  );
  grantTypesEditor.setItems(
    (row.supportedGrantTypes || []).map((g) => ({ grantType: g.grantType })),
  );
  loginUrisEditor.setItems(
    (row.loginRedirectUris || []).map((u) => ({
      loginRedirectUri: u.loginRedirectUri,
      description: u.description,
    })),
  );
  postLogoutUrisEditor.setItems(
    (row.postLogoutRedirectUris || []).map((u) => ({
      postLogoutRedirectUri: u.postLogoutRedirectUri,
      description: u.description,
    })),
  );
  corsEditor.setItems(
    (row.allowedCorsOrigins || []).map((o) => ({
      origin: o.origin,
      description: o.description,
    })),
  );
}

async function openUpdateModal(row) {
  if (updateBusy) return;
  updateRow = row;
  updateModal.setTitle(`Update ${row.name || "client"}`);
  updateForm.reset();
  updateForm.classList.remove("was-validated");
  setFormError(updateErrorEl, null);
  scopeEditor.clear();
  claimsEditor.clear();
  grantTypesEditor.clear();
  loginUrisEditor.clear();
  postLogoutUrisEditor.clear();
  corsEditor.clear();
  secretsEditorEl.innerHTML = "";
  updateFamilyEl.innerHTML =
    '<option value="" selected disabled>Loading families…</option>';
  updateModal.show();
  try {
    const [families, scopes, grantTypes] = await Promise.all([
      getClientFamily(),
      getScope(),
      getClientGrantTypes(),
    ]);
    fillFamilySelect(families, row.clientFamilyId, row.clientFamily?.name);
    scopeField.options = (Array.isArray(scopes) ? scopes : []).map((scope) => ({
      value: scope.id,
      label: scope.scopeName || String(scope.id),
    }));
    grantField.options = (Array.isArray(grantTypes) ? grantTypes : []).map((type) => ({
      value: type,
      label: type,
    }));
  } catch (error) {
    // Keep the current family so the client can still be saved.
    fillFamilySelect([], row.clientFamilyId, row.clientFamily?.name);
    setFormError(updateErrorEl, error?.message || "Could not load families, scopes, or grant types.");
  }
  fillUpdateForm(row);
}

/** Builds the full OutbackClient body for PUT, starting from the original row. */
function buildUpdatePayload(row) {
  const el = (name) => updateForm.elements[name];
  const lifetime = (name, fallback) => {
    const value = el(name).value.trim();
    return value === "" ? fallback : Number(value);
  };
  const familyValue = el("clientFamilyId").value;
  return {
    // Keep the row's id, clientId, clientFamily, created/updated, etc.
    ...row,
    name: el("name").value.trim(),
    description: el("description").value.trim(),
    clientType: Number(el("clientType").value),
    clientFamilyId: familyValue === "" ? row.clientFamilyId : Number(familyValue),
    active: el("active").checked,
    consentRequired: el("consentRequired").checked,
    saveConsent: el("saveConsent").checked,
    issueRefreshToken: el("issueRefreshToken").checked,
    issueIdentityToken: el("issueIdentityToken").checked,
    addUserInfoClaimsInIdentityToken: el("addUserInfoClaimsInIdentityToken").checked,
    accessTokenLifetime: lifetime("accessTokenLifetime", row.accessTokenLifetime),
    identityTokenLifetime: lifetime("identityTokenLifetime", row.identityTokenLifetime),
    refreshTokenLifetime: lifetime("refreshTokenLifetime", row.refreshTokenLifetime),
    savedConsentLifetime: lifetime("savedConsentLifetime", row.savedConsentLifetime),
    authorityCodeLifetime: lifetime("authorityCodeLifetime", row.authorityCodeLifetime),
    scopes: scopeEditor
      .getItems()
      .filter((s) => s.scopeId !== "" && s.scopeId != null)
      .map((s) => ({ scopeId: Number(s.scopeId) })),
    secrets: readSecrets(),
    clientClaims: claimsEditor.getItems().map((c) => ({
      type: c.type,
      value: c.value,
      description: c.description,
    })),
    supportedGrantTypes: grantTypesEditor.getItems().map((g) => ({ grantType: g.grantType })),
    loginRedirectUris: loginUrisEditor.getItems().map((u) => {
      // The backend matches login URIs by id, so keep the id when the URI is unchanged.
      const original = (row.loginRedirectUris || []).find(
        (o) => o.loginRedirectUri === u.loginRedirectUri,
      );
      return original
        ? { id: original.id, loginRedirectUri: u.loginRedirectUri, description: u.description }
        : { loginRedirectUri: u.loginRedirectUri, description: u.description };
    }),
    postLogoutRedirectUris: postLogoutUrisEditor.getItems().map((u) => ({
      postLogoutRedirectUri: u.postLogoutRedirectUri,
      description: u.description,
    })),
    allowedCorsOrigins: corsEditor.getItems().map((o) => ({
      origin: o.origin,
      description: o.description,
    })),
  };
}

updateForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (updateBusy) return;
  if (!updateForm.checkValidity()) {
    updateForm.classList.add("was-validated");
    // Bring the first invalid field into view (it may be on another tab).
    const invalid = updateForm.querySelector(":invalid");
    const pane = invalid?.closest(".tab-pane");
    if (pane) {
      const trigger = updateForm.querySelector(`[data-bs-target="#${pane.id}"]`);
      if (trigger) bootstrap.Tab.getOrCreateInstance(trigger).show();
    }
    return;
  }
  if (!updateRow) return;
  setFormError(updateErrorEl, null);
  updateBusy = true;
  setButtonBusy(updateSubmitEl, true, "Save", "Saving…");
  try {
    await putClientId(updateRow.clientId, buildUpdatePayload(updateRow));
    updateModal.hide();
    toast(updateRow.name || "client", { variant: "success", title: "Client updated." });
    loadClients();
  } catch (error) {
    setFormError(updateErrorEl, error?.message || "Could not update the client.");
  } finally {
    updateBusy = false;
    setButtonBusy(updateSubmitEl, false, "Save", "Saving…");
  }
});

loadClients();
