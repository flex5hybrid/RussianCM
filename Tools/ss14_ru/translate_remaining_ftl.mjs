import https from "node:https";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { dirname, join } from "node:path";

const args = new Map();
for (const arg of process.argv.slice(2)) {
  const [key, value = "true"] = arg.split("=", 2);
  args.set(key.replace(/^--/, ""), value);
}

const localeRoot = args.get("root") ?? "Resources/Locale/ru-RU";
const limit = Number(args.get("limit") ?? "0");
const dryRun = args.get("dry-run") === "true";
const cachePath = args.get("cache") ?? ".tmp/ftl-translation-cache-ru.json";
const batchSize = Number(args.get("batch") ?? "40");
const batchSeparator = "\n9876543210\n";

const skipPathPatterns = [
  /[\\/]accent[\\/]/i,
  /[\\/]traits[\\/]newyorkaccent\.ftl$/i,
  /[\\/]tts[\\/]tts-voices\.ftl$/i,
  /[\\/]avali[\\/](first_names|last_names)\.ftl$/i,
  /[\\/]greek_names\.ftl$/i,
  /[\\/]number_names\.ftl$/i,
  /[\\/]_CMU14[\\/]yautja[\\/]names\.ftl$/i,
];

function hasCyrillic(text) {
  return /\p{Script=Cyrillic}/u.test(text);
}

function isEnglishish(text) {
  return /[A-Za-z]{3,}/.test(text) && !hasCyrillic(text);
}

function shouldSkipPath(path) {
  return skipPathPatterns.some((pattern) => pattern.test(path));
}

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

function protectFluentText(text) {
  const placeholders = [];
  const protect = (match) => {
    const token = `ZXPH${placeholders.length}ZX`;
    placeholders.push(match);
    return token;
  };

  const protectedText = text
    .replace(/\{[^{}]*\}/gs, protect)
    .replace(/\[[^\]\n]+\]/g, protect)
    .replace(/<[^>\n]+>/g, protect);

  return {
    text: protectedText,
    restore(translated) {
      let output = translated;
      for (let i = 0; i < placeholders.length; i++) {
        const loose = new RegExp(`Z\\s*X\\s*P\\s*H\\s*${i}\\s*Z\\s*X`, "g");
        output = output.replace(loose, placeholders[i]);
        output = output.replaceAll(`ZXPH${i}ZX`, placeholders[i]);
      }

      return output;
    },
  };
}

function loadCache() {
  if (!existsSync(cachePath)) return {};
  return JSON.parse(readFileSync(cachePath, "utf8"));
}

function saveCache(cache) {
  mkdirSync(dirname(cachePath), { recursive: true });
  writeFileSync(cachePath, JSON.stringify(cache, null, 2), "utf8");
}

function translateText(text) {
  const url = new URL("https://translate.googleapis.com/translate_a/single");
  url.searchParams.set("client", "gtx");
  url.searchParams.set("sl", "en");
  url.searchParams.set("tl", "ru");
  url.searchParams.set("dt", "t");
  url.searchParams.set("q", text);

  return new Promise((resolve, reject) => {
    https
      .get(url, (res) => {
        let data = "";
        res.setEncoding("utf8");
        res.on("data", (chunk) => (data += chunk));
        res.on("end", () => {
          if (res.statusCode !== 200) {
            reject(new Error(`HTTP ${res.statusCode}: ${data.slice(0, 200)}`));
            return;
          }

          try {
            const parsed = JSON.parse(data);
            resolve(parsed[0].map((part) => part[0]).join(""));
          } catch (error) {
            reject(error);
          }
        });
      })
      .on("error", reject);
  });
}

async function translateBatch(texts) {
  if (texts.length === 0) return [];
  if (texts.length === 1) return [await translateText(texts[0])];

  const translated = await translateText(texts.join(batchSeparator));
  const parts = translated.split(batchSeparator.trim()).map((part) => sanitizeValue(part));
  if (parts.length !== texts.length) {
    return Promise.all(texts.map((text) => translateText(text)));
  }

  return parts;
}

function sanitizeValue(text) {
  return text.replace(/\r/g, "").replace(/\n+/g, " ").trim();
}

function visibleText(value) {
  return value
    .replace(/\{[^{}]*\}/g, "")
    .replace(/\[[^\]\n]+\]/g, "")
    .replace(/<[^>\n]+>/g, "")
    .replace(/\\\[[^\]\n]+\\\]/g, "")
    .trim();
}

function shouldTranslateValue(value) {
  const visible = visibleText(value);
  if (!isEnglishish(visible)) return false;
  if (value.includes("->")) return false;
  if (/^\$[A-Za-z0-9_.:-]+$/.test(value)) return false;
  if (/^[a-z0-9_.:-]+$/.test(visible) && /[-_.:]/.test(visible)) return false;
  return true;
}

const cache = loadCache();
let translatedEntries = 0;
let changedFiles = 0;

for (const file of listFtlFiles(localeRoot)) {
  if (shouldSkipPath(file)) continue;

  const original = readFileSync(file, "utf8");
  const lines = original.replace(/\r\n/g, "\n").replace(/\r/g, "\n").split("\n");
  let changed = false;

  const candidates = [];
  for (let i = 0; i < lines.length; i++) {
    if (limit > 0 && translatedEntries >= limit) break;

    const match = lines[i].match(/^(\s*(?:[A-Za-z0-9_.-]+|\.[A-Za-z0-9_-]+)\s*=\s+)(.+?)\s*$/);
    if (!match) continue;

    const [, prefix, value] = match;
    if (!shouldTranslateValue(value)) continue;

    const { text, restore } = protectFluentText(value);
    candidates.push({ index: i, prefix, value, text, restore });
    translatedEntries++;
  }

  for (let offset = 0; offset < candidates.length; offset += batchSize) {
    const batch = candidates.slice(offset, offset + batchSize);
    const missing = batch.filter((candidate) => !cache[candidate.text]);

    if (missing.length > 0 && !dryRun) {
      const translated = await translateBatch(missing.map((candidate) => candidate.text));
      for (let i = 0; i < missing.length; i++) {
        cache[missing[i].text] = translated[i];
      }

      saveCache(cache);
      await new Promise((resolve) => setTimeout(resolve, 120));
    }

    for (const candidate of batch) {
      let translated = dryRun ? candidate.value : cache[candidate.text];
      translated = sanitizeValue(candidate.restore(translated));
      if (!translated || (!dryRun && translated === candidate.value)) continue;

      if (!dryRun) {
        lines[candidate.index] = `${candidate.prefix}${translated}`;
      }

      changed = true;
    }
  }

  if (changed && !dryRun) {
    writeFileSync(file, lines.join("\n"), "utf8");
    changedFiles++;
  } else if (changed) {
    changedFiles++;
  }

  if (limit > 0 && translatedEntries >= limit) break;
}

console.log(`${dryRun ? "Would translate" : "Translated"} entries=${translatedEntries} files=${changedFiles}`);
