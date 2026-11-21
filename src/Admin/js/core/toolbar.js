// js/core/toolbar.js
//
// A reusable button toolbar (a Bootstrap flex row of buttons). Designed to be
// extended over time: pages add buttons as they need them and enable/disable
// them by id as state changes.
//
// Usage:
//   const toolbar = new Toolbar(container);
//   toolbar.addButton({ id: "create", label: "New client", variant: "primary", onClick });
//   toolbar.addButton({ id: "update", label: "Update", disabled: true });
//   toolbar.setButtonDisabled("update", false);
//   toolbar.setButtonLabel("delete", "Delete (2)");

/**
 * @typedef {object} ToolbarButton
 * @property {string} [id] Stable id used by setButtonDisabled() & friends.
 * @property {string} label Button text.
 * @property {"primary"|"secondary"|"success"|"danger"|"warning"|"outline-primary"|"outline-secondary"|"outline-danger"|"outline-success"|"outline-warning"} [variant="outline-secondary"]
 *   Suffix of the Bootstrap button class: "primary" -> btn-primary.
 * @property {string} [className] Full class list; overrides the variant when set.
 * @property {boolean} [disabled=false]
 * @property {string} [title]
 * @property {function(KeyboardEvent): void} [onClick]
 */

/** A reusable, extensible button toolbar. */
export class Toolbar {
  /**
   * @param {HTMLElement} container Element to build the toolbar inside.
   * @param {object} [options]
   * @param {string} [options.className="d-flex flex-wrap align-items-center gap-2 mb-3"]
   */
  constructor(container, { className = "d-flex flex-wrap align-items-center gap-2 mb-3" } = {}) {
    this.el = document.createElement("div");
    this.el.className = className;
    /** @type {Map<string, HTMLButtonElement>} */
    this.buttons = new Map();
    container.appendChild(this.el);
  }

  /**
   * Adds a button at the end of the toolbar.
   * @param {ToolbarButton} button
   * @returns {HTMLButtonElement}
   */
  addButton({ id, label, variant = "outline-secondary", className, disabled = false, title, onClick }) {
    if (id && this.buttons.has(id)) {
      throw new Error(`Toolbar already has a button with id "${id}"`);
    }
    const button = document.createElement("button");
    button.type = "button";
    button.className = className ?? `btn btn-sm btn-${variant}`;
    button.textContent = label;
    if (title) button.title = title;
    button.disabled = disabled;
    if (id) {
      button.dataset.toolbarId = id;
      this.buttons.set(id, button);
    }
    if (onClick) button.addEventListener("click", onClick);
    this.el.appendChild(button);
    return button;
  }

  /** @param {string} id @returns {HTMLButtonElement|null} */
  getButton(id) {
    return this.buttons.get(id) ?? null;
  }

  /** @param {string} id @param {boolean} disabled */
  setButtonDisabled(id, disabled) {
    const button = this.getButton(id);
    if (button) button.disabled = disabled;
  }

  /** @param {string} id @param {string} label */
  setButtonLabel(id, label) {
    const button = this.getButton(id);
    if (button) button.textContent = label;
  }

  /** @param {string} id */
  removeButton(id) {
    const button = this.getButton(id);
    if (button) {
      button.remove();
      this.buttons.delete(id);
    }
  }
}
