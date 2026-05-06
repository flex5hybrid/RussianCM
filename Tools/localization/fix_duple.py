import os
import re

LOCALE_DIR = "../../Resources/Locale/ru-RU/"

# Регулярки
NAME_PATTERN = re.compile(r"^(ent-[\w\d]+)-name\s*=\s*(.*)")
DESC_PATTERN = re.compile(r"^(ent-[\w\d]+)-desc\s*=\s*(.*)")
OLD_DESC_PATTERN = re.compile(r"^\s*\.desc\s*=\s*(.*)")
KEY_PATTERN = re.compile(r"^(ent-[\w\d]+)\s*=")

def read_ftl_file(path):
    with open(path, "r", encoding="utf-8") as f:
        return [line.rstrip() for line in f]

def write_ftl_file(path, lines):
    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

def process_ftl_file(path, global_keys):
    """
    Преобразует все ключи в формат ent-XYZ-name/ent-XYZ-desc
    и удаляет старые .desc, ent-XYZ = ...
    """
    lines = read_ftl_file(path)
    entries = {}  # ключ -> {name:..., desc:...}
    removed_duplicates = 0

    for line in lines:
        if not line.strip():
            continue

        # Старый формат name/desc
        name_match = NAME_PATTERN.match(line)
        desc_match = DESC_PATTERN.match(line)
        old_desc_match = OLD_DESC_PATTERN.match(line)
        key_match = KEY_PATTERN.match(line)

        if name_match:
            key, value = name_match.groups()
            entries.setdefault(key, {})["name"] = value
            continue
        if desc_match:
            key, value = desc_match.groups()
            entries.setdefault(key, {})["desc"] = value
            continue
        if old_desc_match:
            # старый .desc — удаляем
            continue
        if key_match:
            # старый ent-XYZ = ... — преобразуем в name
            key = key_match.group(1)
            value = line.split("=", 1)[1].strip()
            entries.setdefault(key, {})["name"] = value
            continue
        # все прочие строки оставляем
        entries.setdefault("__other__", {}).setdefault("lines", []).append(line)

    # Создаем новые строки в нужном формате
    new_lines = []
    for key, data in entries.items():
        if key == "__other__":
            new_lines.extend(data.get("lines", []))
            continue

        if key in global_keys:
            removed_duplicates += 1
            continue

        global_keys.add(key)
        name_val = data.get("name", "").strip()
        desc_val = data.get("desc", "").strip()

        if name_val:
            new_lines.append(f'{key}-name = {name_val}')
        if desc_val:
            new_lines.append(f'{key}-desc = {desc_val}')
        new_lines.append("")

    write_ftl_file(path, new_lines)
    return removed_duplicates, len(entries)

def run():
    global_keys = set()
    ftl_files = []

    # Собираем все .ftl файлы
    for root, dirs, files in os.walk(LOCALE_DIR):
        for file in files:
            if file.endswith(".ftl"):
                ftl_files.append(os.path.join(root, file))

    total_files = len(ftl_files)
    total_removed = 0
    total_entries = 0

    for i, path in enumerate(ftl_files, 1):
        removed, entries = process_ftl_file(path, global_keys)
        total_removed += removed
        total_entries += entries
        percent = (i / total_files) * 100
        print(f"[{i}/{total_files}] {percent:.2f}% → {path}, entries: {entries}, removed duplicates: {removed}")

    print("\n✅ FTL Fixer завершил работу")
    print("Файлов обработано:", total_files)
    print("Удалено дубликатов:", total_removed)
    print("Обработано записей:", total_entries)

if __name__ == "__main__":
    run()
