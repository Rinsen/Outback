#!/usr/bin/env node
// tools/sync-bootstrap.mjs
//
// Copies Bootstrap from node_modules into assets/, so the app can be
// deployed as pure static files (no node_modules in production).
//
//   node_modules/bootstrap/dist/css/bootstrap.min.css       -> assets/css/bootstrap.min.css
//   node_modules/bootstrap/dist/css/bootstrap.min.css.map   -> assets/css/bootstrap.min.css.map
//   node_modules/bootstrap/dist/js/bootstrap.bundle.min.js  -> assets/js/bootstrap.bundle.min.js
//   node_modules/bootstrap/dist/js/bootstrap.bundle.min.js.map -> assets/js/bootstrap.bundle.min.js.map
//   node_modules/bootstrap/LICENSE                           -> assets/bootstrap-LICENSE
//
// Usage:
//   node tools/sync-bootstrap.mjs        (or: npm run sync:bootstrap)
//
// Also runs automatically after `npm install` (postinstall), so assets/
// can never drift from the installed version.
//
// Uses Node built-ins only.

import { copyFile, mkdir, readFile } from "node:fs/promises";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = join(dirname(fileURLToPath(import.meta.url)), "..");
const SRC = join(ROOT, "node_modules", "bootstrap");
const DEST = join(ROOT, "assets");

const FILES = [
  ["dist/css/bootstrap.min.css", "css/bootstrap.min.css"],
  ["dist/css/bootstrap.min.css.map", "css/bootstrap.min.css.map"],
  ["dist/js/bootstrap.bundle.min.js", "js/bootstrap.bundle.min.js"],
  ["dist/js/bootstrap.bundle.min.js.map", "js/bootstrap.bundle.min.js.map"],
  ["LICENSE", "bootstrap-LICENSE"],
];

let version;
try {
  const pkg = JSON.parse(await readFile(join(SRC, "package.json"), "utf8"));
  version = pkg.version;
} catch {
  console.error("[sync-bootstrap] node_modules/bootstrap not found. Run `npm install` first.");
  process.exit(1);
}

for (const [src, dest] of FILES) {
  const destPath = join(DEST, dest);
  await mkdir(dirname(destPath), { recursive: true });
  await copyFile(join(SRC, src), destPath);
  console.log(`[sync-bootstrap] ${src} -> assets/${dest}`);
}

console.log(`[sync-bootstrap] synced Bootstrap ${version} into assets/`);
