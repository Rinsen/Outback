import { API_BASE } from "./config.js";

/** Error thrown for any failed API request. */
export class HttpError extends Error {
  /**
   * @param {number} status HTTP status code, or 0 for network/timeout errors.
   * @param {string} statusText
   * @param {any} body Parsed response body, if any.
   * @param {string} path Request path.
   */
  constructor(status, statusText, body, path) {
    super(HttpError.#message(status, statusText, body, path));
    this.name = "HttpError";
    this.status = status;
    this.statusText = statusText;
    this.body = body;
    this.path = path;
  }

  static #message(status, statusText, body, path) {
    if (status === 0) return `Request to ${path} failed: ${statusText}`;
    const detail =
      body && typeof body === "object"
        ? body.detail || body.title || body.message
        : null;
    return detail
      ? `${path} failed with ${status}: ${detail}`
      : `${path} failed with ${status} ${statusText}`.trimEnd();
  }
}

async function parseBody(response) {
  const text = await response.text();
  if (!text) return undefined;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

function buildUrl(path, query) {
  const url = new URL(API_BASE + path, window.location.origin);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== null) {
        url.searchParams.append(key, value);
      }
    }
  }
  return url;
}

/**
 * Performs a JSON request against the Outback API.
 *
 * @param {string} path Path starting with "/", e.g. "/api/Session".
 * @param {object} [options]
 * @param {string} [options.method="GET"] HTTP method.
 * @param {object} [options.query] Query string parameters; null/undefined values are dropped.
 * @param {object} [options.body] JSON-serializable request body.
 * @param {object} [options.headers] Extra request headers.
 * @param {number} [options.timeout=15000] Timeout in milliseconds.
 * @param {AbortSignal} [options.signal] External abort signal.
 * @returns {Promise<any>} The parsed JSON body (or raw text when not JSON).
 * @throws {HttpError}
 */
export async function request(
  path,
  { method = "GET", query, body, headers = {}, timeout = 15000, signal } = {},
) {
  const url = buildUrl(path, query);
  const controller = new AbortController();
  const timer = timeout > 0 ? setTimeout(() => controller.abort(), timeout) : undefined;
  if (signal) {
    if (signal.aborted) controller.abort();
    else signal.addEventListener("abort", () => controller.abort(), { once: true });
  }

  try {
    const response = await fetch(url, {
      method,
      headers: {
        ...(body !== undefined ? { "Content-Type": "application/json" } : {}),
        ...headers,
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
      credentials: "same-origin",
      signal: controller.signal,
    });

    const payload = await parseBody(response);

    if (!response.ok) {
      throw new HttpError(response.status, response.statusText, payload, path);
    }

    return payload;
  } catch (error) {
    if (error instanceof HttpError) throw error;
    if (error instanceof DOMException && error.name === "AbortError") {
      throw new HttpError(0, "timed out after " + timeout + " ms", undefined, path);
    }
    throw new HttpError(0, error.message || "network error", undefined, path);
  } finally {
    if (timer !== undefined) clearTimeout(timer);
  }
}
