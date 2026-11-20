// js/core/grid.js
//
// A reusable data grid built on a plain Bootstrap table: column config,
// client-side sorting, pagination and optional checkbox row selection.
// The grid is a view only — pages own the data and call setData() with it.
//
// Usage:
//   const grid = new Grid(container, {
//     columns: [
//       { key: "name", label: "Name" },
//       { key: "active", label: "Status", render: (v) => boolBadge(v) },
//       { key: "scopes", label: "Scopes", sortValue: (r) => r.scopes?.length ?? 0 },
//     ],
//     selectable: true,
//     rowKey: (row) => row.id,
//     pageSize: 10,
//     pageSizes: [10, 25, 50],
//     emptyMessage: "No clients yet.",
//     onSelectionChange: (selected) => {},
//   });
//   grid.setData(rows);
//   grid.getSelected();

import { escapeHtml } from "./ui.js";

/**
 * @typedef {object} GridColumn
 * @property {string} key Property of the row to display (dot notation ok).
 * @property {string} [label] Header text (defaults to the key).
 * @property {function(*, object): string} [render] Returns HTML for the cell.
 *   The default renders the value as escaped text ("—" when null/undefined).
 * @property {boolean} [sortable=true]
 * @property {function(object): *} [sortValue] Value used for sorting.
 *   Defaults to the column value; use for computed keys (e.g. item counts).
 * @property {string} [width] CSS width for the column.
 */

/**
 * @typedef {object} GridOptions
 * @property {GridColumn[]} columns
 * @property {boolean} [selectable=false] Show a checkbox column.
 * @property {function(object): string} [rowKey] Identity used to keep the
 *   selection stable across setData() calls (e.g. (row) => row.id).
 * @property {number} [pageSize=10]
 * @property {number[]} [pageSizes=[10, 25, 50]]
 * @property {string} [emptyMessage="No data."]
 * @property {function(object[]): void} [onSelectionChange]
 * @property {string} [sortKey] Initial sort column.
 * @property {"asc"|"desc"} [sortDir="asc"]
 */

/** Reads a (possibly dotted) property from a row object. */
function getValue(row, key) {
  let value = row;
  for (const part of key.split(".")) {
    if (value == null) return undefined;
    value = value[part];
  }
  return value;
}

/** Sort comparator: numbers/booleans numerically, everything else as text; nulls last. */
function compareValues(a, b) {
  if (a == null && b == null) return 0;
  if (a == null) return 1;
  if (b == null) return -1;
  if (typeof a === "number" && typeof b === "number") return a - b;
  if (typeof a === "boolean" && typeof b === "boolean") return Number(a) - Number(b);
  return String(a).localeCompare(String(b), undefined, {
    numeric: true,
    sensitivity: "base",
  });
}

/** Page numbers to render, with "…" gaps, e.g. [1, "…", 4, 5, 6, "…", 12]. */
function pageItems(current, total) {
  const items = [];
  let last = 0;
  for (let p = 1; p <= total; p++) {
    if (p === 1 || p === total || Math.abs(p - current) <= 1) {
      if (last && p - last > 1) items.push("…");
      items.push(p);
      last = p;
    }
  }
  return items;
}

/** A reusable data grid (table + sorting + pagination + selection). */
export class Grid {
  /**
   * @param {HTMLElement} container Element to build the grid inside.
   * @param {GridOptions} [options]
   */
  constructor(container, options = {}) {
    /** @type {GridColumn[]} */
    this.columns = options.columns || [];
    this.selectable = !!options.selectable;
    /** @type {((row: object) => string) | null} */
    this.rowKey = options.rowKey || null;
    this.pageSize = options.pageSize || 10;
    this.pageSizes = options.pageSizes || [10, 25, 50];
    this.emptyMessage = options.emptyMessage || "No data.";
    /** @type {((selected: object[]) => void) | null} */
    this.onSelectionChange = options.onSelectionChange || null;

    this.sortKey = options.sortKey || null;
    this.sortDir = options.sortDir || "asc";
    /** @type {object[]} */
    this.rows = [];
    /** @type {Set<object>} */
    this.selected = new Set();
    this.page = 1;
    this.loading = false;

    this.root = this.#buildDom();
    container.appendChild(this.root);
  }

  #buildDom() {
    const root = document.createElement("div");
    root.className = "grid";

    const table = document.createElement("table");
    table.className = "table table-hover align-middle mb-0";
    table.innerHTML = `
      <thead><tr></tr></thead>
      <tbody></tbody>`;
    const theadRow = table.querySelector("thead tr");
    const tbody = table.querySelector("tbody");
    this.tbodyEl = tbody;

    if (this.selectable) {
      const th = document.createElement("th");
      th.className = "grid-check-cell";
      this.selectAllEl = document.createElement("input");
      this.selectAllEl.type = "checkbox";
      this.selectAllEl.className = "form-check-input";
      this.selectAllEl.setAttribute("aria-label", "Select all rows on this page");
      this.selectAllEl.addEventListener("change", () => this.#selectPage(this.selectAllEl.checked));
      th.appendChild(this.selectAllEl);
      theadRow.appendChild(th);
    }

    for (const column of this.columns) {
      const th = document.createElement("th");
      th.scope = "col";
      th.dataset.key = column.key;
      if (column.width) th.style.width = column.width;
      const sortable = column.sortable !== false;
      if (sortable) {
        th.className = "grid-th-sortable";
        const button = document.createElement("button");
        button.type = "button";
        button.className = "grid-sort-btn";
        button.innerHTML = `${escapeHtml(column.label ?? column.key)} <span class="grid-sort-arrow"></span>`;
        button.addEventListener("click", () => this.#toggleSort(column));
        th.appendChild(button);
      } else {
        th.textContent = column.label ?? column.key;
      }
      theadRow.appendChild(th);
    }

    const scroll = document.createElement("div");
    scroll.className = "table-responsive";
    scroll.appendChild(table);
    root.appendChild(scroll);

    const footer = document.createElement("div");
    footer.className = "grid-footer d-flex flex-wrap align-items-center gap-2 pt-2";
    footer.innerHTML = `
      <span class="grid-range text-secondary small"></span>
      <div class="ms-auto d-flex align-items-center gap-2">
        <select class="form-select form-select-sm grid-pagesize w-auto" aria-label="Rows per page">
          ${this.pageSizes.map((size) => `<option value="${size}">${size}</option>`).join("")}
        </select>
        <nav aria-label="Grid pages">
          <ul class="pagination pagination-sm mb-0"></ul>
        </nav>
      </div>`;
    this.rangeEl = footer.querySelector(".grid-range");
    this.pageSizeEl = footer.querySelector(".grid-pagesize");
    this.pageSizeEl.value = String(this.pageSize);
    this.pageSizeEl.addEventListener("change", () => {
      this.pageSize = Number(this.pageSizeEl.value) || this.pageSizes[0];
      this.page = 1;
      this.render();
    });
    this.paginationEl = footer.querySelector(".pagination");
    root.appendChild(footer);

    return root;
  }

  /**
   * Sets the rows to display. Resets to page 1; when a rowKey is configured,
   * rows whose key was selected before stay selected.
   * @param {object[]} rows
   */
  setData(rows) {
    this.loading = false;
    const keepKeys =
      this.rowKey && this.selected.size
        ? new Set([...this.selected].map((row) => this.rowKey(row)))
        : null;
    this.rows = rows || [];
    this.selected = new Set();
    if (keepKeys) {
      for (const row of this.rows) {
        if (keepKeys.has(this.rowKey(row))) this.selected.add(row);
      }
    }
    this.page = 1;
    this.render();
    this.#fireSelectionChange();
  }

  /** Shows a spinner row (or clears it). @param {boolean} loading */
  setLoading(loading) {
    this.loading = loading;
    this.render();
  }

  /** Changes the message shown when there are no rows. @param {string} message */
  setEmptyMessage(message) {
    this.emptyMessage = message;
    this.render();
  }

  /** @returns {object[]} The selected rows, in grid order. */
  getSelected() {
    return this.rows.filter((row) => this.selected.has(row));
  }

  /** Clears the selection. */
  clearSelection() {
    if (!this.selected.size) return;
    this.selected = new Set();
    this.render();
  }

  /** Re-renders the table, header arrows and pagination. */
  render() {
    const colCount = this.columns.length + (this.selectable ? 1 : 0);
    const tbody = this.tbodyEl;
    tbody.innerHTML = "";
    this.rowEls = new Map();

    if (this.loading) {
      tbody.innerHTML = `
        <tr><td colspan="${colCount}">
          <div class="text-center py-4">
            <div class="spinner-border spinner-border-sm me-2" role="status"></div>
            Loading…
          </div>
        </td></tr>`;
    } else if (this.rows.length === 0) {
      tbody.innerHTML = `
        <tr><td colspan="${colCount}" class="text-center text-secondary py-4">
          ${escapeHtml(this.emptyMessage)}
        </td></tr>`;
    } else {
      for (const row of this.#pageRows()) {
        const tr = this.#renderRow(row);
        this.rowEls.set(row, tr);
        tbody.appendChild(tr);
      }
    }

    this.#renderHeader();
    this.#renderFooter();
    this.#updateSelectAll();
  }

  #pageRows() {
    const start = (this.page - 1) * this.pageSize;
    return this.#sortedRows().slice(start, start + this.pageSize);
  }

  #sortedRows() {
    if (!this.sortKey) return this.rows;
    const column = this.columns.find((c) => c.key === this.sortKey);
    if (!column) return this.rows;
    const valueOf = column.sortValue || ((row) => getValue(row, this.sortKey));
    const dir = this.sortDir === "desc" ? -1 : 1;
    return [...this.rows].sort((a, b) => dir * compareValues(valueOf(a), valueOf(b)));
  }

  #renderRow(row) {
    const tr = document.createElement("tr");
    const isSelected = this.selected.has(row);
    if (isSelected) tr.classList.add("grid-row-selected");

    if (this.selectable) {
      const td = document.createElement("td");
      td.className = "grid-check-cell";
      const box = document.createElement("input");
      box.type = "checkbox";
      box.className = "form-check-input";
      box.checked = isSelected;
      box.setAttribute("aria-label", "Select row");
      box.addEventListener("change", () => {
        if (box.checked) this.selected.add(row);
        else this.selected.delete(row);
        tr.classList.toggle("grid-row-selected", box.checked);
        this.#updateSelectAll();
        this.#fireSelectionChange();
      });
      td.appendChild(box);
      tr.appendChild(td);
    }

    for (const column of this.columns) {
      const td = document.createElement("td");
      const value = getValue(row, column.key);
      td.innerHTML = column.render
        ? column.render(value, row)
        : escapeHtml(value ?? "—");
      tr.appendChild(td);
    }
    return tr;
  }

  #renderHeader() {
    for (const th of this.root.querySelectorAll("thead th[data-key]")) {
      const arrow = th.querySelector(".grid-sort-arrow");
      if (!arrow) continue;
      const active = this.sortKey === th.dataset.key;
      arrow.textContent = active ? (this.sortDir === "asc" ? "▲" : "▼") : "";
      th.setAttribute("aria-sort", active ? (this.sortDir === "asc" ? "ascending" : "descending") : "none");
    }
  }

  #renderFooter() {
    const total = this.rows.length;
    const totalPages = Math.max(1, Math.ceil(total / this.pageSize));
    if (this.page > totalPages) this.page = totalPages;
    const start = total === 0 ? 0 : (this.page - 1) * this.pageSize + 1;
    const end = Math.min(total, this.page * this.pageSize);
    this.rangeEl.textContent = total === 0 ? "0 of 0" : `${start}–${end} of ${total}`;

    const ul = this.paginationEl;
    ul.innerHTML = "";
    const addPageButton = (label, disabled, active, onClick) => {
      const li = document.createElement("li");
      li.className = "page-item";
      if (disabled) li.classList.add("disabled");
      if (active) li.classList.add("active");
      const button = document.createElement("button");
      button.type = "button";
      button.className = "page-link";
      button.textContent = label;
      if (!disabled && onClick) button.addEventListener("click", onClick);
      li.appendChild(button);
      ul.appendChild(li);
      return li;
    };

    addPageButton("‹", this.page <= 1, false, () => this.setPage(this.page - 1));
    for (const item of pageItems(this.page, totalPages)) {
      if (item === "…") {
        const li = document.createElement("li");
        li.className = "page-item disabled";
        li.innerHTML = `<span class="page-link">…</span>`;
        ul.appendChild(li);
      } else {
        addPageButton(String(item), false, item === this.page, () => this.setPage(item));
      }
    }
    addPageButton("›", this.page >= totalPages, false, () => this.setPage(this.page + 1));
  }

  /** Jumps to a page (clamped to 1..totalPages). @param {number} page */
  setPage(page) {
    const totalPages = Math.max(1, Math.ceil(this.rows.length / this.pageSize));
    this.page = Math.min(Math.max(1, page), totalPages);
    this.render();
  }

  #toggleSort(column) {
    if (this.sortKey === column.key) {
      this.sortDir = this.sortDir === "asc" ? "desc" : "asc";
    } else {
      this.sortKey = column.key;
      this.sortDir = "asc";
    }
    this.page = 1;
    this.render();
  }

  #selectPage(on) {
    const pageRows = this.#pageRows();
    if (on) for (const row of pageRows) this.selected.add(row);
    else for (const row of pageRows) this.selected.delete(row);
    for (const [row, tr] of this.rowEls) {
      tr.classList.toggle("grid-row-selected", this.selected.has(row));
    }
    this.#updateSelectAll();
    this.#fireSelectionChange();
  }

  #updateSelectAll() {
    if (!this.selectAllEl) return;
    const pageRows = this.#pageRows();
    const selectedOnPage = pageRows.filter((row) => this.selected.has(row)).length;
    this.selectAllEl.checked = pageRows.length > 0 && selectedOnPage === pageRows.length;
    this.selectAllEl.indeterminate = selectedOnPage > 0 && selectedOnPage < pageRows.length;
  }

  #fireSelectionChange() {
    if (this.onSelectionChange) this.onSelectionChange(this.getSelected());
  }
}
