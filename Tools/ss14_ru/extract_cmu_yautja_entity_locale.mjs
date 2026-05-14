import { mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const sourceRoot = "Resources/Prototypes/_CMU14/Yautja";
const outputRoots = [
  "Resources/Locale/en-US/_CMU14/Entities/Yautja",
  "Resources/Locale/ru-RU/_CMU14/Entities/Yautja",
];

function clean(value) {
  value = value.trim();
  const first = value.charCodeAt(0);
  const last = value.charCodeAt(value.length - 1);
  if ((first === 34 && last === 34) || (first === 39 && last === 39)) {
    value = value.slice(1, -1);
  }

  return value.trim();
}

function isLocId(value) {
  return /^[A-Za-z0-9_.-]+$/.test(value) && value.includes("-");
}

function formatValue(value) {
  return value.replace(/\r?\n/g, " ").trim();
}

function extractEntities(text) {
  const entities = [];
  let current = null;

  const finish = () => {
    if (!current?.id) {
      return;
    }

    const name = current.name && !isLocId(current.name) ? current.name : null;
    const description = current.description && !isLocId(current.description) ? current.description : null;
    if (name || description) {
      entities.push({ id: current.id, name, description });
    }
  };

  for (const line of text.replace(/\r\n/g, "\n").split("\n")) {
    const type = line.match(/^- type:\s*(\S+)/);
    if (type) {
      finish();
      current = type[1] === "entity" ? {} : null;
      continue;
    }

    if (!current) {
      continue;
    }

    let match = line.match(/^  id:\s*(.+?)\s*$/);
    if (match && !current.id) {
      current.id = clean(match[1]);
      continue;
    }

    match = line.match(/^  name:\s*(.+?)\s*$/);
    if (match && !current.name) {
      current.name = clean(match[1]);
      continue;
    }

    match = line.match(/^  description:\s*(.+?)\s*$/);
    if (match && !current.description) {
      current.description = clean(match[1]);
    }
  }

  finish();
  return entities;
}

function serialize(entities) {
  return `${entities
    .map((entity) => {
      let text = entity.name
        ? `ent-${entity.id} = ${formatValue(entity.name)}`
        : `ent-${entity.id} =`;

      if (entity.description) {
        text += `\n    .desc = ${formatValue(entity.description)}`;
      }

      return text;
    })
    .join("\n\n")}\n`;
}

let files = 0;
let entries = 0;

for (const file of readdirSync(sourceRoot).filter((name) => name.endsWith(".yml")).sort()) {
  const entities = extractEntities(readFileSync(join(sourceRoot, file), "utf8"));
  if (entities.length === 0) {
    continue;
  }

  const output = serialize(entities);
  for (const root of outputRoots) {
    mkdirSync(root, { recursive: true });
    writeFileSync(join(root, file.replace(/\.yml$/, ".ftl")), output, "utf8");
  }

  files++;
  entries += entities.length;
}

console.log(`Extracted entity locale files=${files} entries=${entries}`);
