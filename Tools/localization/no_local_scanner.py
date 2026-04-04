import os
import yaml
from pathlib import Path

PROTOTYPES_DIR = "../../Resources/Prototypes"
LOCALE_DIR = "../../Resources/Locale/ru-RU"

# -------------------------------
# Игнорирование !type тегов YAML
# -------------------------------

class IgnoreUnknown(yaml.SafeLoader):
    pass


def ignore(loader, tag_suffix, node):

    if isinstance(node, yaml.ScalarNode):
        return loader.construct_scalar(node)

    if isinstance(node, yaml.SequenceNode):
        return loader.construct_sequence(node)

    if isinstance(node, yaml.MappingNode):
        return loader.construct_mapping(node)


IgnoreUnknown.add_multi_constructor("!", ignore)

# -------------------------------
# Чтение FTL ключей
# -------------------------------

def read_ftl_keys(path):

    keys = set()

    if not os.path.exists(path):
        return keys

    with open(path, encoding="utf-8") as f:

        current_key = None

        for line in f:

            line = line.rstrip()

            if not line or line.strip().startswith("#"):
                continue

            if "=" in line and not line.startswith(" "):

                key = line.split("=")[0].strip()
                keys.add(key)
                current_key = key

            elif line.strip().startswith("."):

                attr = line.strip().split("=")[0].strip()

                if current_key:
                    keys.add(f"{current_key}{attr}")

    return keys


# -------------------------------
# Получение пути FTL файла
# -------------------------------

def get_ftl_path(proto_path):

    relative = os.path.relpath(proto_path, PROTOTYPES_DIR)

    relative = relative.replace(".yml", ".ftl")
    relative = relative.replace(".yaml", ".ftl")

    return os.path.join(LOCALE_DIR, relative)


# -------------------------------
# Сканирование прототипов
# -------------------------------

def process_prototype(proto_path):

    try:

        with open(proto_path, encoding="utf-8") as f:
            data = yaml.load(f, Loader=IgnoreUnknown)

    except Exception as e:
        print("Ошибка:", proto_path, e)
        return

    if not data:
        return

    if isinstance(data, dict):
        entities = [data]
    else:
        entities = data

    ftl_path = get_ftl_path(proto_path)

    os.makedirs(os.path.dirname(ftl_path), exist_ok=True)

    existing_keys = read_ftl_keys(ftl_path)

    new_entries = []

    for ent in entities:

        if not isinstance(ent, dict):
            continue

        if ent.get("type") != "entity":
            continue

        ent_id = ent.get("id")

        if not ent_id:
            continue

        name = ent.get("name")
        desc = ent.get("description")

        name_key = f"ent-{ent_id}"
        desc_key = f"{name_key}.desc"

        if name and name_key not in existing_keys:
            new_entries.append(f"{name_key} = {name}")

        if desc and desc_key not in existing_keys:
            new_entries.append(f"  .desc = {desc}")

    if not new_entries:
        return

    with open(ftl_path, "a", encoding="utf-8") as f:

        f.write("\n")
        for entry in new_entries:
            f.write(entry + "\n")

    print("Добавлено:", ftl_path)


# -------------------------------
# Основной запуск
# -------------------------------

def main():

    for root, _, files in os.walk(PROTOTYPES_DIR):

        for file in files:

            if not file.endswith((".yml", ".yaml")):
                continue

            path = os.path.join(root, file)

            process_prototype(path)


if __name__ == "__main__":
    main()
