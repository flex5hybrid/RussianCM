import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";

const sourceRoot = process.argv.find((arg) => arg.startsWith("--source="))?.slice("--source=".length) ?? "Resources/Locale/en-US";
const targetRoot = process.argv.find((arg) => arg.startsWith("--target="))?.slice("--target=".length) ?? "Resources/Locale/ru-RU";
const dryRun = process.argv.includes("--dry-run");

function listFtlFiles(root) {
  const files = [];
  const stack = [root];
  while (stack.length) {
    const current = stack.pop();
    for (const entry of readdirSync(current, { withFileTypes: true })) {
      const full = join(current, entry.name);
      if (entry.isDirectory()) {
        stack.push(full);
      } else if (entry.isFile() && entry.name.endsWith(".ftl")) {
        files.push(full);
      }
    }
  }

  return files.sort();
}

function messageId(line) {
  const match = line.match(/^([A-Za-z0-9_.-]+)\s*=/);
  return match?.[1] ?? null;
}

function parseEntries(text) {
  const lines = text.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
  const entries = [];
  let current = null;

  for (let i = 0; i < lines.length; i++) {
    const id = messageId(lines[i]);
    if (!id) continue;

    if (current) {
      current.end = i;
      entries.push(current);
    }

    current = { id, start: i, end: lines.length };
  }

  if (current) entries.push(current);
  return entries.map((entry) => ({ id: entry.id, text: lines.slice(entry.start, entry.end).join("\n").trimEnd() }));
}

let addedEntries = 0;
let changedFiles = 0;

for (const sourceFile of listFtlFiles(sourceRoot)) {
  const rel = relative(sourceRoot, sourceFile);
  const targetFile = join(targetRoot, rel);
  const sourceEntries = parseEntries(readFileSync(sourceFile, "utf8"));
  const targetText = existsSync(targetFile) ? readFileSync(targetFile, "utf8") : "";
  const targetIds = new Set(parseEntries(targetText).map((entry) => entry.id));
  const missing = sourceEntries.filter((entry) => !targetIds.has(entry.id));
  if (missing.length === 0) continue;

  addedEntries += missing.length;
  changedFiles++;

  if (dryRun) continue;

  const chunks = [];
  if (targetText.trim().length > 0) chunks.push(targetText.replace(/\s+$/u, ""));
  chunks.push("# Missing entries synced from en-US");
  chunks.push(...missing.map((entry) => entry.text));

  mkdirSync(dirname(targetFile), { recursive: true });
  writeFileSync(targetFile, `${chunks.join("\n\n")}\n`, "utf8");
}

console.log(`${dryRun ? "Would sync" : "Synced"} entries=${addedEntries} files=${changedFiles}`);
