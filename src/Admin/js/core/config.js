// Runtime configuration for the Outback admin app.
//
// In local development the static server (tools/static-server.mjs) serves this
// app and proxies any request it does not own to the Outback backend, so the
// browser only ever talks to a single origin and API_BASE stays empty.
//
// When serving the app from a different origin than the Outback API, set
// window.ADMIN_API_BASE to the API base URL before the app scripts load, e.g.:
//   <script>window.ADMIN_API_BASE = "https://localhost:56399"</script>
export const API_BASE =
  (typeof window !== "undefined" && window.ADMIN_API_BASE) || "";

export const APP_NAME = "Outback";
export const APP_SUFFIX = "Admin";
