// js/core/list-editor.js
//
// A reusable list editor: a set of item rows (each with the configured
// fields, laid out on the Bootstrap grid) plus an "add" button and a
// remove button per row. Used for the collection properties of a client
// (scopes, secrets, claims, grant types, redirect URIs, CORS origins).
//
// Usage:
//   const editor = new ListEditor(container, {
//     fields: [
//       { key: "type", label: "Type", type: "text", className: "col-md-4" },
//       { key: "value", label: "Value", type: "text", className: "col-md-4" },
//       { key: "description", label: "Description", type: "text", className: "col-md-4" },
//     ],
//     addLabel: "Add claim",
//     emptyMessage: "No claims.",
//     onChange: (items) => console.log(items),
//   });
//   editor.setItems([{ type: "email", value: "a@b.c" }]);
//   editor.getItems(); // -> [{ type: "email", value: "a@b.c", description: "" }]

/**
 * @typedef {object} ListEditorField
 * @property {string} key Property name on the item.
 * @property {string} label
 * @property {"text"|"number"|"select"} [type="text"]
 * @property {{value: string|number, label: string}[]} [options] For type "select".
 * @property {string} [placeholder] Input placeholder, or the select's empty option.
 * @property {string} [className="col-md-6"] Bootstrap grid column for the field.
 * @property {number} [min] For type "number".
 * @property {number} [step] For type "number".
 */

/**
 * @typedef {object} ListEditorOptions
 * @property {ListEditorField[]} fields
 * @property {string} [addLabel="Add"]
 * @property {string} [emptyMessage] Shown while the list has no rows.
 * @property {function(object[]): void} [onChange] Fires on add/remove/edit with
 *   the current items.
 */

/** A reusable add/remove list editor for collection properties. */
export class ListEditor {
  /**
   * @param {HTMLElement} container Element to build the editor inside.
   * @param {ListEditorOptions} options
   */
  constructor(container, { fields, addLabel = "Add", emptyMessage, onChange } = {}) {
    /** @type {ListEditorField[]} */
    this.fields = fields || [];
    /** @type {((items: object[]) => void) | null} */
    this.onChange = onChange || null;

    this.el = document.createElement("div");
    this.el.className = "list-editor";

    if (emptyMessage) {
      this.emptyEl = document.createElement("div");
      this.emptyEl.className = "list-editor-empty text-secondary small mb-2";
      this.emptyEl.textContent = emptyMessage;
      this.el.appendChild(this.emptyEl);
    }

    this.itemsEl = document.createElement("div");
    this.itemsEl.className = "list-editor-items";
    this.el.appendChild(this.itemsEl);

    this.addButton = document.createElement("button");
    this.addButton.type = "button";
    this.addButton.className = "btn btn-outline-secondary btn-sm";
    this.addButton.textContent = addLabel;
    this.addButton.addEventListener("click", () => this.addItem());
    this.el.appendChild(this.addButton);

    this.itemsEl.addEventListener("input", () => this.#fireChange());
    this.itemsEl.addEventListener("change", () => this.#fireChange());

    container.appendChild(this.el);
  }

  /**
   * Replaces the rows with the given items.
   * @param {object[]} items
   */
  setItems(items) {
    this.itemsEl.innerHTML = "";
    for (const item of items || []) this.#appendRow(item);
    this.#updateEmpty();
  }

  /**
   * Appends a row (blank by default).
   * @param {object} [item]
   */
  addItem(item = {}) {
    this.#appendRow(item);
    this.#updateEmpty();
    this.#fireChange();
  }

  /** Removes all rows. */
  clear() {
    this.itemsEl.innerHTML = "";
    this.#updateEmpty();
    this.#fireChange();
  }

  /** @returns {number} Number of rows. */
  get count() {
    return this.itemsEl.children.length;
  }

  /**
   * Reads the current items from the DOM. Rows where every field is empty
   * are dropped, so a half-filled row is kept but a blank one is not.
   * @returns {object[]}
   */
  getItems() {
    const items = [];
    for (const row of this.itemsEl.children) {
      const item = {};
      let hasValue = false;
      for (const field of this.fields) {
        const input = row.querySelector(`[data-field="${field.key}"]`);
        if (!input) continue;
        let value = input.value;
        if (field.type === "number") value = value === "" ? null : Number(value);
        item[field.key] = value;
        if (value !== null && value !== "") hasValue = true;
      }
      if (hasValue) items.push(item);
    }
    return items;
  }

  #appendRow(item = {}) {
    const row = document.createElement("div");
    row.className = "list-editor-item row g-2 align-items-end mb-2";

    for (const field of this.fields) {
      const cell = document.createElement("div");
      cell.className = field.className ?? "col-md-6";
      const label = document.createElement("label");
      label.className = "form-label small text-secondary mb-1";
      label.textContent = field.label;
      cell.appendChild(label);
      cell.appendChild(this.#buildInput(field, item[field.key]));
      row.appendChild(cell);
    }

    const removeCell = document.createElement("div");
    removeCell.className = "col-auto pe-0";
    const removeButton = document.createElement("button");
    removeButton.type = "button";
    removeButton.className = "btn btn-outline-danger btn-sm";
    removeButton.textContent = "Remove";
    removeButton.addEventListener("click", () => {
      row.remove();
      this.#updateEmpty();
      this.#fireChange();
    });
    removeCell.appendChild(removeButton);
    row.appendChild(removeCell);

    this.itemsEl.appendChild(row);
  }

  #buildInput(field, value) {
    let input;
    if (field.type === "select") {
      input = document.createElement("select");
      input.className = "form-select form-select-sm";
      const empty = document.createElement("option");
      empty.value = "";
      empty.textContent = field.placeholder ?? "—";
      input.appendChild(empty);
      for (const option of field.options || []) {
        const opt = document.createElement("option");
        opt.value = String(option.value);
        opt.textContent = option.label ?? String(option.value);
        input.appendChild(opt);
      }
      if (value != null && value !== "") {
        const wanted = String(value);
        const exists = [...input.options].some((o) => o.value === wanted);
        if (!exists) {
          const opt = document.createElement("option");
          opt.value = wanted;
          opt.textContent = wanted;
          input.appendChild(opt);
        }
        input.value = wanted;
      }
    } else {
      input = document.createElement("input");
      input.type = field.type === "number" ? "number" : "text";
      input.className = "form-control form-control-sm";
      if (field.placeholder) input.placeholder = field.placeholder;
      if (field.type === "number") {
        input.min = field.min ?? 0;
        input.step = field.step ?? 1;
      }
      input.value = value ?? "";
    }
    input.dataset.field = field.key;
    return input;
  }

  #updateEmpty() {
    if (this.emptyEl) this.emptyEl.classList.toggle("d-none", this.itemsEl.children.length > 0);
  }

  #fireChange() {
    if (this.onChange) this.onChange(this.getItems());
  }
}
