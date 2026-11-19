#!/usr/bin/env node
// tools/generate-clients.mjs
//
// Generates the JavaScript API clients in js/api/ from the Outback backend's
// live OpenAPI documents. The backend must be running.
//
//   node tools/generate-clients.mjs
//
// Uses Node built-ins only. The generated files are committed to the repo;
// re-run this script (or `npm run generate`) whenever the API changes.
//
// Function names are derived mechanically from the route, because the
// documents do not define operationIds. The rule:
//   1. Split the route path into segments.
//   2. Drop the literal "api" and "outback" segments (case-insensitive).
//   3. Strip braces from {placeholders} and pascal-case every segment, then
//      prefix the HTTP method:
//        GET  /api/Session               -> getSession
//        POST /Outback/api/Client        -> postClient
//        GET  /Outback/api/Client/{id}   -> getClientId
//        GET  /Outback/api/Random/{count} -> getRandomCount
//   4. On a name clash a numeric suffix is appended.

import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { request as httpRequest } from "node:http";
import { request as httpsRequest } from "node:https";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..");

const API_BASE_URL = (
  process.env.ADMIN_API_BASE_URL || "https://localhost:56399"
).replace(/\/+$/, "");

const DOCUMENTS = [
  { url: `${API_BASE_URL}/openapi/session.json`, out: "js/api/session.js" },
  { url: `${API_BASE_URL}/openapi/outback.json`, out: "js/api/outback.js" },
];

const HTTP_METHODS = new Set(["get", "post", "put", "delete", "patch", "head"]);
const JSON_CONTENT_TYPES = ["application/json", "text/json", "application/*+json"];

/**
 * Fetches a URL as JSON. Uses node:https (not global fetch) so the local
 * development self-signed certificate does not need to be trusted by Node's
 * CA store; verification is disabled for this single request only.
 */
function fetchJson(url, timeoutMs = 15000) {
  return new Promise((resolvePromise, rejectPromise) => {
    const target = url.startsWith("https") ? httpsRequest : httpRequest;
    const req = target(url, {
      headers: { Accept: "application/json" },
      ...(url.startsWith("https") ? { rejectUnauthorized: false } : {}),
    }, (res) => {
      if (!res.statusCode || res.statusCode >= 400) {
        res.resume();
        rejectPromise(new Error(`HTTP ${res.statusCode}`));
        return;
      }
      const chunks = [];
      res.on("data", (chunk) => chunks.push(chunk));
      res.on("end", () => {
        try {
          resolvePromise(JSON.parse(Buffer.concat(chunks).toString("utf8")));
        } catch (error) {
          rejectPromise(new Error(`not valid JSON: ${error.message}`));
        }
      });
    });
    req.on("error", rejectPromise);
    req.setTimeout(timeoutMs, () => req.destroy(new Error("timed out")));
    req.end();
  });
}

function pascal(segment) {
  const s = segment
    .replace(/[^a-zA-Z0-9]+(.)/g, (_, c) => c.toUpperCase())
    .replace(/[^a-zA-Z0-9]+/g, "");
  return s.charAt(0).toUpperCase() + s.slice(1);
}

function camel(word) {
  const p = pascal(word);
  return p.charAt(0).toLowerCase() + p.slice(1);
}

/** Derives the exported function name for a route + method. */
function baseName(pathTemplate, method) {
  const segments = pathTemplate
    .split("/")
    .filter(Boolean)
    .filter((s) => !["api", "outback"].includes(s.toLowerCase()))
    .map((s) => s.replace(/^\{|\}$/g, ""));
  return camel(method) + segments.map(pascal).join("");
}

/** Maps an OpenAPI schema to a JSDoc type expression. */
function jsType(schema) {
  if (!schema) return "any";
  if (schema.$ref) return schema.$ref.split("/").pop();
  if (Array.isArray(schema.type)) {
    // Union types, e.g. ["integer","string"] or ["null","string"].
    const parts = schema.type
      .filter((t) => t !== "null")
      .map((t) => primitiveType(t, schema.format))
      .filter(Boolean);
    const base = parts.length ? parts.join(" | ") : "null";
    return schema.type.includes("null") ? `${base} | null` : base;
  }
  if (schema.enum) {
    const values = schema.enum.map((v) =>
      typeof v === "string" ? `"${v}"` : String(v),
    );
    return values.join(" | ");
  }
  if (schema.type === "array") {
    return `${jsType(schema.items)}[]`;
  }
  if (schema.type === "object") {
    if (schema.properties) {
      const fields = Object.entries(schema.properties)
        .map(([k, v]) => `${k}: ${jsType(v)}`)
        .join("; ");
      return `{ ${fields} }`;
    }
    return "object";
  }
  const base = primitiveType(schema.type, schema.format);
  return schema.nullable ? `${base} | null` : base;
}

function primitiveType(type, format) {
  switch (type) {
    case "string":
      return "string";
    case "integer":
    case "number":
      return "number";
    case "boolean":
      return "boolean";
    case "null":
      return "null";
    default:
      return format ? `string /* ${format} */` : type || "any";
  }
}

/** Picks the JSON content entry from a request/response content map. */
function pickContent(content) {
  if (!content) return undefined;
  const preferred = JSON_CONTENT_TYPES.find((ct) => content[ct]);
  return content[preferred] ?? content[Object.keys(content)[0]];
}

/** Emits a `@typedef` line block for one component schema. */
function typedefFor(name, schema) {
  if (schema.properties) {
    const required = new Set(schema.required ?? []);
    const lines = [` * @typedef {object} ${name}`];
    for (const [prop, propSchema] of Object.entries(schema.properties)) {
      const propName = required.has(prop) ? prop : `[${prop}]`;
      lines.push(` * @property {${jsType(propSchema)}} ${propName}`);
    }
    return lines.join("\n");
  }
  if (schema.enum) {
    const values = schema.enum
      .map((v) => (typeof v === "string" ? `"${v}"` : String(v)))
      .join(" | ");
    return ` * @typedef {${values}} ${name}`;
  }
  return ` * @typedef {${jsType(schema)}} ${name}`;
}

/** Collects path/query parameter metadata for an operation. */
function collectParameters(doc, pathTemplate, op) {
  const byName = new Map(
    (op.parameters ?? []).map((p) => [p.name, p]),
  );
  // Merge parameters declared at the path level (none today, but be generic).
  const pathItem = doc.paths[pathTemplate] ?? {};
  if (Array.isArray(pathItem.parameters)) {
    for (const param of pathItem.parameters) {
      if (!byName.has(param.name)) byName.set(param.name, param);
    }
  }

  const pathParams = [];
  for (const match of pathTemplate.matchAll(/\{([^}]+)\}/g)) {
    const name = match[1];
    const param = byName.get(name) ?? {};
    pathParams.push({
      name,
      type: jsType(param.schema),
      required: param.in !== "query",
    });
  }

  const queryParams = (op.parameters ?? []).filter((p) => p.in === "query");
  return { pathParams, queryParams };
}

/** Emits one exported function with its JSDoc. */
function emitOperation(pathTemplate, method, op, { pathParams, queryParams }, name) {
  const body = pickContent(op.requestBody?.content);
  const hasBody = Boolean(body);
  const hasQuery = queryParams.length > 0;

  // Return type: first 2xx response with a content schema, else undefined.
  let returnType = "undefined";
  for (const [code, response] of Object.entries(op.responses ?? {})) {
    if (!code.startsWith("2")) continue;
    const content = pickContent(response.content);
    if (content?.schema) {
      returnType = jsType(content.schema);
      break;
    }
  }

  const args = [
    ...pathParams.map((p) => p.name),
    ...(hasBody ? ["body"] : []),
    ...(hasQuery ? ["query"] : []),
  ];

  const lines = [];
  lines.push("/**");
  lines.push(` * \`${method.toUpperCase()} ${pathTemplate}\``);
  if (op.summary) lines.push(` * ${op.summary}`);
  for (const p of pathParams) {
    lines.push(` * @param {${p.type}} ${p.name}`);
  }
  if (hasBody) {
    lines.push(` * @param {${jsType(body.schema)}} body`);
  }
  if (hasQuery) {
    lines.push(` * @param {{ ${queryParams.map((p) => `[${p.name}]?: ${jsType(p.schema)}`).join("; ")} }} query`);
  }
  lines.push(` * @returns {Promise<${returnType}>}`);
  lines.push(" */");
  lines.push(`export function ${name}(${args.join(", ")}) {`);

  if (pathParams.length > 0) {
    const url = pathTemplate.replace(
      /\{([^}]+)\}/g,
      (_, n) => `\${encodeURIComponent(${n})}`,
    );
    lines.push(`  const url = \`${url}\`;`);
  } else {
    lines.push(`  const url = ${JSON.stringify(pathTemplate)};`);
  }

  const options = [`method: "${method.toUpperCase()}"`];
  if (hasQuery) options.push("query");
  if (hasBody) options.push("body");
  lines.push(`  return request(url, { ${options.join(", ")} });`);
  lines.push("}");

  return lines.join("\n");
}

function generate(doc, sourceUrl) {
  const schemas = doc.components?.schemas ?? {};
  const typedefs = Object.entries(schemas)
    .map(([name, schema]) => typedefFor(name, schema))
    .filter(Boolean)
    .join("\n");

  const used = new Set();
  const operations = [];
  for (const [pathTemplate, pathItem] of Object.entries(doc.paths ?? {})) {
    for (const [method, op] of Object.entries(pathItem)) {
      if (!HTTP_METHODS.has(method) || !op) continue;
      let name = baseName(pathTemplate, method);
      let suffix = 2;
      while (used.has(name)) name = `${baseName(pathTemplate, method)}_${suffix++}`;
      used.add(name);
      operations.push(
        emitOperation(
          pathTemplate,
          method,
          op,
          collectParameters(doc, pathTemplate, op),
          name,
        ),
      );
    }
  }

  const header = [
    "// Code generated by tools/generate-clients.mjs — DO NOT EDIT BY HAND.",
    `// Source: ${sourceUrl}`,
    `// Generated: ${new Date().toISOString()}`,
    "// Regenerate with: npm run generate",
    "",
    'import { request } from "../core/http.js";',
    "",
  ].join("\n");

  const typedefBlock = typedefs
    ? `\n/**\n${typedefs}\n */\n`
    : "";

  return header + typedefBlock + operations.join("\n\n") + "\n";
}

// --- main -----------------------------------------------------------------

let failed = false;
for (const { url, out } of DOCUMENTS) {
  let doc;
  try {
    doc = await fetchJson(url);
  } catch (error) {
    failed = true;
    console.error(`[generate] FAILED  ${url}`);
    console.error(`[generate]         ${error.message}`);
    console.error(
      "[generate]         Is the Outback backend running? (see src/OutbackApp launchSettings)",
    );
    continue;
  }

  const file = join(ROOT, out);
  mkdirSync(dirname(file), { recursive: true });
  writeFileSync(file, generate(doc, url), "utf8");
  console.log(`[generate] wrote ${out}`);
}

if (failed) process.exit(1);
