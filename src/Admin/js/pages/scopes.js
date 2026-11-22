// js/pages/scopes.js
//
// Scopes: a grid of all Outback scopes with a toolbar above it.
// Grid (Scope name, Name, Description, Audience, Claims, Status),
// loading/error states, a Refresh button, and selection-driven
// Update/Delete enablement. Double-click a row to open the scope.
// "New scope" create modal, a Delete confirmation modal that
// hard-deletes the selected scopes, and an Update modal (scalar fields
// plus a scope-claims list editor) saved with PUT /Outback/api/Scope/{id}.

import {
  getScope,
  postScope,
  putScopeId,
  deleteScopeId,
} from "../api/outback.js";
import { Grid } from "../core/grid.js";
import { ListEditor } from "../core/list-editor.js";
import { Modal } from "../core/modal.js";
import { Toolbar } from "../core/toolbar.js";
import { boolBadge, escapeHtml, toast } from "../core/ui.js";

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

/** Grid columns: the scope's identity at a glance — the scalar flags and
 *  claims are shown and edited in the Update modal. */
const COLUMNS = [
  {
    key: "scopeName",
    label: "Scope name",
    render: (v) => (v ? `<code>${escapeHtml(v)}</code>` : "—"),
    width: "200px",
  },
  { key: "displayName", label: "Name" },
  { key: "description", label: "Description", render: (v) => truncateDescription(v) },
  {
    key: "audience",
    label: "Audience",
    render: (v) => (v ? escapeHtml(v) : '<span class="text-secondary">—</span>'),
  },
  {
    key: "scopeClaims",
    label: "Claims",
    render: (v) => (v?.length ? String(v.length) : '<span class="text-secondary">—</span>'),
    sortValue: (row) => row.scopeClaims?.length ?? 0,
    width: "90px",
  },
  { key: "enabled", label: "Status", render: (v) => boolBadge(v), width: "100px" },
];

const EMPTY_MESSAGE = "No scopes yet.";
const EMPTY_MESSAGE_ERROR = "Could not load scopes.";

const gridEl = document.getElementById("scopes-grid");
const toolbarEl = document.getElementById("scopes-toolbar");
const errorEl = document.getElementById("scopes-error");
const errorMessageEl = document.getElementById("scopes-error-message");
const errorRetryEl = document.getElementById("scopes-error-retry");

const toolbar = new Toolbar(toolbarEl);

const grid = new Grid(gridEl, {
  columns: COLUMNS,
  selectable: true,
  rowKey: (row) => row.id,
  pageSize: 10,
  pageSizes: [10, 25, 50],
  sortKey: "scopeName",
  sortDir: "asc",
  emptyMessage: EMPTY_MESSAGE,
  onSelectionChange: (selected) => {
    // Update applies to exactly one scope; Delete to one or more.
    toolbar.setButtonDisabled("update", selected.length !== 1);
    toolbar.setButtonDisabled("delete", selected.length < 1);
    toolbar.setButtonLabel(
      "delete",
      selected.length ? `Delete (${selected.length})` : "Delete",
    );
  },
});

// Double-click a row to open the scope (the Update modal shows every
// field and the claims). The checkbox cell is excluded so selection
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

// Toolbar. Create button first, then the selection-driven actions
// (Update, Delete), and Refresh right-aligned.
toolbar.addButton({
  id: "create",
  label: "New scope",
  variant: "primary",
  onClick: () => createScopeModal.show(),
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
  onClick: () => loadScopes(),
});

let loading = false;

/** Fetches all scopes and renders them into the grid. */
async function loadScopes() {
  if (loading) return;
  loading = true;
  grid.setLoading(true);
  hideError();
  try {
    const scopes = await getScope();
    grid.setEmptyMessage(EMPTY_MESSAGE);
    grid.setData(Array.isArray(scopes) ? scopes : []);
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
      : error?.message || "Could not load scopes.";
  errorEl.classList.remove("d-none");
}

function hideError() {
  errorEl.classList.add("d-none");
}

errorRetryEl.addEventListener("click", () => loadScopes());

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

// --- New scope --------------------------------------------------------------

const createScopeForm = document.createElement("form");
createScopeForm.id = "create-scope-form";
createScopeForm.className = "needs-validation";
createScopeForm.noValidate = true;
createScopeForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="mb-3">
    <label class="form-label" for="create-scope-name">Name</label>
    <input class="form-control" id="create-scope-name" name="name" required>
    <div class="invalid-feedback">A name is required.</div>
  </div>
  <div class="mb-3">
    <label class="form-label" for="create-scope-scope-name">Scope name</label>
    <input class="form-control" id="create-scope-scope-name" name="scopeName" required>
    <div class="invalid-feedback">A scope name is required.</div>
    <div class="form-text">
      The unique scope identifier (e.g. <code>openid</code>, <code>offline_access</code>).
    </div>
  </div>
  <div class="mb-3">
    <label class="form-label" for="create-scope-description">Description</label>
    <input class="form-control" id="create-scope-description" name="description">
  </div>
  <div class="mb-3">
    <label class="form-label" for="create-scope-audience">Audience</label>
    <input class="form-control" id="create-scope-audience" name="audience" placeholder="urn:…">
  </div>
  <div class="mb-3">
    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="create-scope-enabled" name="enabled" checked>
      <label class="form-check-label" for="create-scope-enabled">Enabled</label>
    </div>
  </div>
  <div class="mb-3">
    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="create-scope-show-in-discovery" name="showInDiscoveryDocument" checked>
      <label class="form-check-label" for="create-scope-show-in-discovery">Show in discovery document</label>
    </div>
  </div>
  <div class="mb-1">
    <div class="form-check form-switch">
      <input class="form-check-input" type="checkbox" id="create-scope-claims-in-id-token" name="claimsInIdToken">
      <label class="form-check-label" for="create-scope-claims-in-id-token">Claims in ID token</label>
    </div>
  </div>`;

const createScopeModal = new Modal({
  title: "New scope",
  body: createScopeForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="create-scope-form" class="btn btn-primary">Create</button>`,
  onHidden: () => {
    createScopeForm.reset();
    createScopeForm.classList.remove("was-validated");
    setFormError(createScopeForm.querySelector(".alert"), null);
  },
});

const createScopeErrorEl = createScopeForm.querySelector(".alert");
const createScopeSubmitEl = createScopeModal.footerEl.querySelector('button[type="submit"]');

let createScopeBusy = false;

createScopeForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  event.stopPropagation();
  if (createScopeBusy) return;
  if (!createScopeForm.checkValidity()) {
    createScopeForm.classList.add("was-validated");
    return;
  }
  setFormError(createScopeErrorEl, null);
  createScopeBusy = true;
  setButtonBusy(createScopeSubmitEl, true, "Create", "Creating…");
  try {
    const created = await postScope({
      name: createScopeForm.elements["name"].value.trim(),
      scopeName: createScopeForm.elements["scopeName"].value.trim(),
      description: createScopeForm.elements["description"].value.trim(),
      audience: createScopeForm.elements["audience"].value.trim(),
      enabled: createScopeForm.elements["enabled"].checked,
      showInDiscoveryDocument: createScopeForm.elements["showInDiscoveryDocument"].checked,
      claimsInIdToken: createScopeForm.elements["claimsInIdToken"].checked,
    });
    createScopeModal.hide();
    toast(created?.scopeName || "scope", { variant: "success", title: "Scope created." });
    loadScopes();
  } catch (error) {
    setFormError(createScopeErrorEl, error?.message || "Could not create the scope.");
  } finally {
    createScopeBusy = false;
    setButtonBusy(createScopeSubmitEl, false, "Create", "Creating…");
  }
});

// --- Delete -----------------------------------------------------------------

const deleteListEl = document.createElement("ul");
deleteListEl.className = "list-unstyled small mb-0";
deleteListEl.style.maxHeight = "160px";
deleteListEl.style.overflowY = "auto";

const deleteModal = new Modal({
  title: "Delete scopes",
  body: `
    <p class="text-secondary small mb-2">
      This permanently deletes the selected scope(s). This action cannot be undone.
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
    `Delete ${selected.length} scope${selected.length === 1 ? "" : "s"}?`,
  );
  deleteListEl.innerHTML = selected
    .map((row) => `<li>${escapeHtml(row.scopeName || row.displayName || String(row.id))}</li>`)
    .join("");
  deleteModal.show();
}

deleteConfirmEl.addEventListener("click", async () => {
  const selected = grid.getSelected();
  if (!selected.length || deleteBusy) return;
  deleteBusy = true;
  setButtonBusy(deleteConfirmEl, true, "Delete", "Deleting…");
  try {
    // The API routes on the numeric Id.
    await Promise.all(selected.map((row) => deleteScopeId(row.id)));
    deleteModal.hide();
    grid.clearSelection();
    toast(
      selected.length === 1
        ? selected[0].scopeName || "scope"
        : `${selected.length} scopes`,
      { variant: "success", title: "Deleted." },
    );
    loadScopes();
  } catch (error) {
    deleteModal.hide();
    // Re-fetch so the grid reflects whatever actually got deleted.
    toast(error?.message || "Could not delete the scope(s).", { variant: "danger" });
    loadScopes();
  } finally {
    deleteBusy = false;
    setButtonBusy(deleteConfirmEl, false, "Delete", "Deleting…");
  }
});

// --- Update -----------------------------------------------------------------

const updateForm = document.createElement("form");
updateForm.id = "update-scope-form";
updateForm.className = "needs-validation";
updateForm.noValidate = true;
updateForm.innerHTML = `
  <div class="alert alert-danger d-none" role="alert"></div>
  <div class="row g-3">
    <div class="col-md-6">
      <label class="form-label" for="update-scope-name">Name</label>
      <input class="form-control" id="update-scope-name" name="displayName" required>
      <div class="invalid-feedback">A name is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-scope-scope-name">Scope name</label>
      <input class="form-control" id="update-scope-scope-name" name="scopeName" required>
      <div class="invalid-feedback">A scope name is required.</div>
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-scope-description">Description</label>
      <input class="form-control" id="update-scope-description" name="description">
    </div>
    <div class="col-md-6">
      <label class="form-label" for="update-scope-audience">Audience</label>
      <input class="form-control" id="update-scope-audience" name="audience" placeholder="urn:…">
    </div>
    <div class="col-12">
      <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="update-scope-enabled" name="enabled">
        <label class="form-check-label" for="update-scope-enabled">Enabled</label>
      </div>
    </div>
    <div class="col-md-6">
      <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="update-scope-show-in-discovery" name="showInDiscoveryDocument">
        <label class="form-check-label" for="update-scope-show-in-discovery">Show in discovery document</label>
      </div>
    </div>
    <div class="col-md-6">
      <div class="form-check form-switch">
        <input class="form-check-input" type="checkbox" id="update-scope-claims-in-id-token" name="claimsInIdToken">
        <label class="form-check-label" for="update-scope-claims-in-id-token">Claims in ID token</label>
      </div>
    </div>
  </div>
  <h6 class="mt-4 mb-2">Scope claims</h6>
  <div id="update-scope-claims-editor"></div>`;

const updateModal = new Modal({
  title: "Update scope",
  body: updateForm,
  footer: `
    <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
    <button type="submit" form="update-scope-form" class="btn btn-primary">Save</button>`,
  wide: true,
  onHidden: () => {
    updateForm.reset();
    updateForm.classList.remove("was-validated");
    setFormError(updateForm.querySelector(".alert"), null);
  },
});

const updateErrorEl = updateForm.querySelector(".alert");
const updateSubmitEl = updateModal.footerEl.querySelector('button[type="submit"]');

const claimsEditor = new ListEditor(updateForm.querySelector("#update-scope-claims-editor"), {
  fields: [
    { key: "type", label: "Type", className: "col-md-4" },
    { key: "description", label: "Description", className: "col-md-6" },
  ],
  addLabel: "Add claim",
  emptyMessage: "No claims.",
});

// --- Open / fill / save --------------------------------------------------------

let updateRow = null;
let updateBusy = false;

function fillUpdateForm(row) {
  const el = (name) => updateForm.elements[name];
  el("displayName").value = row.displayName ?? "";
  el("scopeName").value = row.scopeName ?? "";
  el("description").value = row.description ?? "";
  el("audience").value = row.audience ?? "";
  el("enabled").checked = !!row.enabled;
  el("showInDiscoveryDocument").checked = !!row.showInDiscoveryDocument;
  el("claimsInIdToken").checked = !!row.claimsInIdToken;

  claimsEditor.setItems(
    (row.scopeClaims || []).map((c) => ({
      type: c.type,
      description: c.description,
    })),
  );
}

function openUpdateModal(row) {
  if (updateBusy) return;
  updateRow = row;
  updateModal.setTitle(`Update ${row.displayName || row.scopeName || "scope"}`);
  updateForm.reset();
  updateForm.classList.remove("was-validated");
  setFormError(updateErrorEl, null);
  claimsEditor.clear();
  updateModal.show();
  fillUpdateForm(row);
}

/** Builds the full OutbackScope body for PUT, starting from the original row. */
function buildUpdatePayload(row) {
  const el = (name) => updateForm.elements[name];
  return {
    // Keep the row's id, created/updated, etc.
    ...row,
    displayName: el("displayName").value.trim(),
    scopeName: el("scopeName").value.trim(),
    description: el("description").value.trim(),
    audience: el("audience").value.trim(),
    enabled: el("enabled").checked,
    showInDiscoveryDocument: el("showInDiscoveryDocument").checked,
    claimsInIdToken: el("claimsInIdToken").checked,
    scopeClaims: claimsEditor.getItems().map((c) => {
      // The backend matches claims by id, so keep the id when the type is unchanged.
      const original = (row.scopeClaims || []).find((o) => o.type === c.type);
      return original
        ? { id: original.id, type: c.type, description: c.description }
        : { type: c.type, description: c.description };
    }),
  };
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
    await putScopeId(updateRow.id, buildUpdatePayload(updateRow));
    updateModal.hide();
    toast(updateRow.displayName || updateRow.scopeName || "scope", {
      variant: "success",
      title: "Scope updated.",
    });
    loadScopes();
  } catch (error) {
    setFormError(updateErrorEl, error?.message || "Could not update the scope.");
  } finally {
    updateBusy = false;
    setButtonBusy(updateSubmitEl, false, "Save", "Saving…");
  }
});

loadScopes();
