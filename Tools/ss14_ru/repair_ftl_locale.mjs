import { readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { execFileSync } from "node:child_process";

const root = process.argv.find((arg) => arg.startsWith("--root="))?.slice("--root=".length) ?? "Resources/Locale/ru-RU";
const dryRun = process.argv.includes("--dry-run");
const sourceRoot = "Resources/Locale/en-US";

const win1251Decoder = new TextDecoder("windows-1251");
const win1251Encode = new Map();
for (let byte = 0; byte < 256; byte++) {
  win1251Encode.set(win1251Decoder.decode(Uint8Array.from([byte])), byte);
}

function listFtlFiles(dir) {
  const files = [];
  const stack = [dir];
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

function hasCyrillic(text) {
  return /\p{Script=Cyrillic}/u.test(text);
}

function mojibakeScore(text) {
  const matches = text.match(/[РС][\u0080-\u04FF]|[ÂÐÑ][\u0080-\u04FF]?/gu);
  return matches?.length ?? 0;
}

function decodeMojibake(text) {
  if (mojibakeScore(text) < 2) return text;

  const bytes = [];
  for (const char of text) {
    const byte = win1251Encode.get(char);
    if (byte === undefined) return text;
    bytes.push(byte);
  }

  const decoded = Buffer.from(bytes).toString("utf8");
  if (decoded.includes("\uFFFD")) return text;
  if (!hasCyrillic(decoded)) return text;
  if (mojibakeScore(decoded) >= mojibakeScore(text)) return text;
  return decoded;
}

function protectedParts(text) {
  const parts = [];
  const collect = (match) => {
    parts.push(match);
    return match;
  };

  text.replace(/\{[^{}]*\}/gs, collect).replace(/\[[^\]\n]+\]/g, collect).replace(/<[^>\n]+>/g, collect);
  return parts;
}

function restoreProtectionTokens(line, originalLine) {
  if (!line.includes("ZXPH")) return line;

  const parts = protectedParts(originalLine);
  if (parts.length === 0) return line;

  let restored = line;
  for (let i = 0; i < parts.length; i++) {
    const tag = parts[i].match(/^\[([A-Za-z ]+)=([^\]]+)\]$/);
    if (tag) {
      const attrToken = new RegExp(`(\\b${tag[1].replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\s*=)\\s*ZXPH${i}ZX`, "g");
      restored = restored.replace(attrToken, `$1${tag[2]}`);
    }

    const loose = new RegExp(`Z\\s*X\\s*P\\s*H\\s*${i}\\s*Z\\s*X`, "g");
    restored = restored.replace(loose, parts[i]);
    restored = restored.replaceAll(`ZXPH${i}ZX`, parts[i]);
  }

  return restored;
}

function readGitOriginalLines(file) {
  const gitPath = file.replace(/\\/g, "/");
  try {
    return execFileSync("git", ["show", `HEAD:${gitPath}`], { encoding: "utf8", stdio: ["ignore", "pipe", "ignore"] })
      .replace(/\r\n/g, "\n")
      .replace(/\r/g, "\n")
      .split("\n");
  } catch {
    return null;
  }
}

function readSourceOriginalLines(file) {
  const rel = file.replace(/\\/g, "/").replace(/^Resources\/Locale\/ru-RU\//, "");
  const sourceFile = `${sourceRoot}/${rel}`;
  try {
    return readFileSync(sourceFile, "utf8").replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
  } catch {
    return null;
  }
}

function originalLineById(lines) {
  const byId = new Map();
  if (!lines) return byId;

  for (const line of lines) {
    const id = messageId(line);
    if (id) byId.set(id, line);
  }

  return byId;
}

function messageId(line) {
  const match = line.match(/^\uFEFF?(-?[A-Za-z0-9_.-]+)\s*=/);
  return match?.[1] ?? null;
}

function parseEntries(lines) {
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

  if (current) {
    entries.push(current);
  }

  return entries;
}

function entryText(lines, entry) {
  return lines.slice(entry.start, entry.end).join("\n");
}

function duplicateKeepScore(text) {
  let score = 0;
  if (hasCyrillic(text)) score += 10;
  if (!/[A-Za-z]{3,}/.test(text.replace(/\{[^{}]*\}/g, ""))) score += 3;
  if (mojibakeScore(text) > 0) score -= 2;
  return score;
}

let changedFiles = 0;
let decodedLines = 0;
let removedDuplicates = 0;
const fileLines = new Map();
const globalEntries = [];

for (const file of listFtlFiles(root)) {
  const original = readFileSync(file, "utf8");
  let lines = original.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
  const gitOriginalLines = original.includes("ZXPH") ? readGitOriginalLines(file) : null;
  const sourceOriginalLines = original.includes("ZXPH") ? readSourceOriginalLines(file) : null;
  const gitOriginalById = originalLineById(gitOriginalLines);
  const sourceOriginalById = originalLineById(sourceOriginalLines);
  let changed = false;

  lines = lines.map((line, index) => {
    let decoded = decodeMojibake(line);
    if (decoded !== line) {
      decodedLines++;
      changed = true;
    }

    const id = messageId(decoded);
    const originalLine = gitOriginalById.get(id) ?? sourceOriginalById.get(id) ?? gitOriginalLines?.[index] ?? sourceOriginalLines?.[index];
    if (originalLine) {
      const restored = restoreProtectionTokens(decoded, originalLine);
      if (restored !== decoded) {
        decoded = restored;
        changed = true;
      }
    }

    return decoded;
  });

  const entries = parseEntries(lines);
  const byId = new Map();
  for (const entry of entries) {
    if (!byId.has(entry.id)) byId.set(entry.id, []);
    byId.get(entry.id).push(entry);
  }

  const removeLines = new Set();
  for (const duplicates of byId.values()) {
    if (duplicates.length < 2) continue;

    let keep = duplicates[0];
    let keepScore = duplicateKeepScore(entryText(lines, keep));
    for (const candidate of duplicates.slice(1)) {
      const score = duplicateKeepScore(entryText(lines, candidate));
      if (score >= keepScore) {
        keep = candidate;
        keepScore = score;
      }
    }

    for (const duplicate of duplicates) {
      if (duplicate === keep) continue;
      for (let i = duplicate.start; i < duplicate.end; i++) {
        removeLines.add(i);
      }
      removedDuplicates++;
      changed = true;
    }
  }

  if (removeLines.size) {
    lines = lines.filter((_, index) => !removeLines.has(index));
  }

  if (changed && !dryRun) {
    writeFileSync(file, lines.join("\n"), "utf8");
    changedFiles++;
  } else if (changed) {
    changedFiles++;
  }

  fileLines.set(file, lines);
  for (const entry of parseEntries(lines)) {
    globalEntries.push({ file, ...entry });
  }
}

const globalById = new Map();
for (const entry of globalEntries) {
  if (!globalById.has(entry.id)) globalById.set(entry.id, []);
  globalById.get(entry.id).push(entry);
}

for (const duplicates of globalById.values()) {
  if (duplicates.length < 2) continue;

  const keep = duplicates[0];
  for (const duplicate of duplicates.slice(1)) {
    const lines = fileLines.get(duplicate.file);
    for (let i = duplicate.start; i < duplicate.end; i++) {
      lines[i] = null;
    }
    removedDuplicates++;
  }
}

for (const [file, lines] of fileLines) {
  if (!lines.some((line) => line === null)) continue;

  const compact = lines.filter((line) => line !== null);
  if (!dryRun) {
    writeFileSync(file, compact.join("\n"), "utf8");
  }
}

console.log(`${dryRun ? "Would repair" : "Repaired"} files=${changedFiles} decodedLines=${decodedLines} removedDuplicates=${removedDuplicates}`);
