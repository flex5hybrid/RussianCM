import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, relative } from "node:path";

const refs = [
  "origin/fix/fix-locale",
  "origin/feature/localization300925",
  "origin/feature/translation-not-all",
  "origin/bug/locale",
  "origin/fix/rule-loc",
  "origin/rebase/RuMC",
];

const localeRoot = "Resources/Locale/ru-RU";

function runGit(args) {
  return execFileSync("git", args, { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] });
}

function run(args, options = {}) {
  return execFileSync(args[0], args.slice(1), { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"], ...options });
}

function hasCyrillic(text) {
  return /\p{Script=Cyrillic}/u.test(text);
}

function isEnglishish(text) {
  return /[A-Za-z]{3,}/.test(text) && !hasCyrillic(text);
}

function entryKey(line) {
  const message = line.match(/^([A-Za-z0-9_.-]+)\s*=/);
  if (message) return message[1];

  const attr = line.match(/^(\s*)\.([A-Za-z0-9_-]+)\s*=/);
  if (attr) return `${attr[1]}.${attr[2]}`;

  return null;
}

function parseEntries(text) {
  const lines = text.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
  const entries = new Map();
  let current = null;
  let start = 0;

  for (let i = 0; i < lines.length; i++) {
    const key = entryKey(lines[i]);
    if (!key) continue;

    if (current !== null) {
      entries.set(current, { start, end: i - 1, lines: lines.slice(start, i) });
    }

    current = key;
    start = i;
  }

  if (current !== null) {
    entries.set(current, { start, end: lines.length - 1, lines: lines.slice(start) });
  }

  return { lines, entries };
}

function entryValue(entry) {
  return entry.lines.join("\n").replace(/^[^=]*=/, "");
}

function listFtlFiles(root) {
  if (!existsSync(root)) return [];

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

  return files;
}

function sanitizeRef(ref) {
  return ref.replace(/[^A-Za-z0-9_.-]/g, "_");
}

const translations = new Map();
const tempRoot = join(".tmp", "existing-locale-translations");
rmSync(tempRoot, { recursive: true, force: true });
mkdirSync(tempRoot, { recursive: true });

for (const ref of refs) {
  const refRoot = join(tempRoot, sanitizeRef(ref));
  const archivePath = join(tempRoot, `${sanitizeRef(ref)}.tar`);

  try {
    runGit(["archive", "--format=tar", `--output=${archivePath}`, ref, localeRoot]);
    mkdirSync(refRoot, { recursive: true });
    run(["tar", "-xf", archivePath, "-C", refRoot]);
  } catch {
    continue;
  }

  for (const fullPath of listFtlFiles(join(refRoot, localeRoot))) {
    const path = relative(refRoot, fullPath).replaceAll("\\", "/");
    const text = readFileSync(fullPath, "utf8");
    const { entries } = parseEntries(text);
    for (const [key, entry] of entries) {
      const value = entryValue(entry);
      if (!hasCyrillic(value)) continue;

      const mapKey = `${path}\0${key}`;
      if (!translations.has(mapKey)) {
        translations.set(mapKey, entry.lines);
      }
    }
  }
}

const currentFiles = listFtlFiles(localeRoot);

let filesChanged = 0;
let entriesChanged = 0;

for (const path of currentFiles) {
  const text = readFileSync(path, "utf8");
  const repoPath = path.replaceAll("\\", "/");
  let parsed = parseEntries(text);
  let changed = false;

  for (const [key, entry] of [...parsed.entries]) {
    const value = entryValue(entry);
    if (!isEnglishish(value)) continue;

    const replacement = translations.get(`${repoPath}\0${key}`);
    if (!replacement) continue;

    parsed.lines.splice(entry.start, entry.end - entry.start + 1, ...replacement);
    parsed = parseEntries(parsed.lines.join("\n"));
    changed = true;
    entriesChanged++;
  }

  if (changed) {
    writeFileSync(path, parsed.lines.join("\n"), "utf8");
    filesChanged++;
  }
}

console.log(`Merged existing translations: files=${filesChanged} entries=${entriesChanged}`);
