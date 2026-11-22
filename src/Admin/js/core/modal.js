// js/core/modal.js
//
// A thin wrapper around a Bootstrap 5 modal: builds the standard modal DOM,
// exposes show()/hide(), and fires an onHidden callback after the close
// animation so pages can reset form state.
//
// Usage:
//   const modal = new Modal({
//     title: "New client",
//     body: `<form id="create-form">...</form>`,
//     footer: `<button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
//              <button type="submit" form="create-form" class="btn btn-primary">Create</button>`,
//     onHidden: () => form.reset(),
//   });
//   modal.show();

import { escapeHtml } from "./ui.js";

/** Fills `target` with a string (as HTML) or a DOM node. */
function setContent(target, content) {
  target.innerHTML = "";
  if (content == null) return;
  if (typeof content === "string") target.innerHTML = content;
  else target.appendChild(content);
}

/** A thin wrapper around a Bootstrap modal. */
export class Modal {
  /**
   * @param {object} [options]
   * @param {string|HTMLElement} [options.title] Modal title (strings are escaped).
   * @param {string|HTMLElement} [options.body]
   * @param {string|HTMLElement} [options.footer]
   * @param {boolean} [options.large=false] Uses modal-lg.
   * @param {boolean} [options.wide=false] Uses modal-wide (50% wider than modal-lg).
   * @param {function(): void} [options.onHidden] Fires after the modal is fully hidden.
   */
  constructor({ title, body, footer, large = false, wide = false, onHidden } = {}) {
    const el = document.createElement("div");
    el.className = "modal fade";
    el.tabIndex = -1;
    el.setAttribute("aria-hidden", "true");
    el.innerHTML = `
      <div class="modal-dialog ${large ? "modal-lg" : ""} ${wide ? "modal-wide" : ""} modal-dialog-centered modal-dialog-scrollable">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title"></h5>
            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
          </div>
          <div class="modal-body"></div>
          <div class="modal-footer"></div>
        </div>
      </div>`;
    document.body.appendChild(el);

    /** @type {HTMLElement} The modal root element. */
    this.el = el;
    /** @type {HTMLElement} */
    this.titleEl = el.querySelector(".modal-title");
    /** @type {HTMLElement} */
    this.bodyEl = el.querySelector(".modal-body");
    /** @type {HTMLElement} */
    this.footerEl = el.querySelector(".modal-footer");

    if (title != null) this.setTitle(title);
    if (body != null) this.setBody(body);
    if (footer != null) this.setFooter(footer);

    this.instance = new bootstrap.Modal(el);
    if (onHidden) el.addEventListener("hidden.bs.modal", onHidden);
  }

  /** @param {string|HTMLElement} title */
  setTitle(title) {
    this.titleEl.innerHTML = typeof title === "string" ? escapeHtml(title) : title;
  }

  /** @param {string|HTMLElement} body */
  setBody(body) {
    setContent(this.bodyEl, body);
  }

  /** @param {string|HTMLElement} footer */
  setFooter(footer) {
    setContent(this.footerEl, footer);
  }

  show() {
    this.instance.show();
  }

  hide() {
    this.instance.hide();
  }

  /** @returns {boolean} True while the modal is open (shown or animating). */
  get isOpen() {
    return this.el.classList.contains("show");
  }
}
