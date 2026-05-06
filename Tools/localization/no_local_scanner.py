import os
import yaml
from pathlib import Path

PROTOTYPES_DIR = "../../Resources/Prototypes/"
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
            elif line.strip().startswith(".") and current_key:
                attr = line.strip().split("=")[0].strip()
                keys.add(f"{current_key}{attr}")
    return keys

# -------------------------------
# Получение пути FTL файла
# -------------------------------

def get_ftl_path(proto_path):
    relative = os.path.relpath(proto_path, PROTOTYPES_DIR)
    relative = relative.replace(".yml", ".ftl").replace(".yaml", ".ftl")
    return os.path.join(LOCALE_DIR, relative)

# -------------------------------
# Построение иерархии прототипов
# -------------------------------

def build_hierarchy():
    hierarchy = {}
    for root, _, files in os.walk(PROTOTYPES_DIR):
        for file in files:
            if not file.endswith((".yml", ".yaml")):
                continue
            path = os.path.join(root, file)
            try:
                with open(path, encoding="utf-8") as f:
                    data = yaml.load(f, Loader=IgnoreUnknown)
            except Exception:
                continue
            if not data:
                continue
            entities = [data] if isinstance(data, dict) else data
            for ent in entities:
                if isinstance(ent, dict):
                    ent_id = ent.get("id")
                    parent_id = ent.get("parent")
                    if ent_id and parent_id:
                        hierarchy[ent_id] = parent_id
    return hierarchy

# -------------------------------
# Получение имени и описания с наследованием
# -------------------------------
def inherit_name_desc(ent_id, entities_dict, hierarchy):
    """
    Рекурсивно ищем name/desc родителя, если нет у текущего
    """
    # Если ent_id - список, берём первый элемент или пропускаем
    if isinstance(ent_id, list):
        if not ent_id:
            return None, None
        ent_id = str(ent_id[0])
    else:
        ent_id = str(ent_id)

    if ent_id not in entities_dict:
        return None, None

    ent = entities_dict[ent_id]
    name = ent.get("name")
    desc = ent.get("description")

    # Если и имя, и описание есть — возвращаем
    if name and desc:
        return name, desc

    # Ищем родителя
    parent_id = hierarchy.get(ent_id)
    if parent_id:
        parent_name, parent_desc = inherit_name_desc(parent_id, entities_dict, hierarchy)
        return name or parent_name, desc or parent_desc

    return name, desc
    return name, desc

# -------------------------------
# Сканирование прототипов
# -------------------------------

def process_prototype(proto_path, entities_dict, hierarchy):
    try:
        with open(proto_path, encoding="utf-8") as f:
            data = yaml.load(f, Loader=IgnoreUnknown)
    except Exception as e:
        print("Ошибка:", proto_path, e)
        return

    if not data:
        return

    entities = [data] if isinstance(data, dict) else data

    ftl_path = get_ftl_path(proto_path)
    os.makedirs(os.path.dirname(ftl_path), exist_ok=True)
    existing_keys = read_ftl_keys(ftl_path)

    new_entries = []

    for ent in entities:
        if not isinstance(ent, dict) or ent.get("type") != "entity":
            continue
        ent_id = ent.get("id")
        if not ent_id:
            continue

        # Получаем имя и описание с наследованием
        name, desc = inherit_name_desc(ent_id, entities_dict, hierarchy)
        name_key = f"ent-{ent_id}-name"
        desc_key = f"ent-{ent_id}-desc"

        if name and name_key not in existing_keys:
            new_entries.append(f'{name_key} = {name}')
        if desc and desc_key not in existing_keys:
            new_entries.append(f'{desc_key} = {desc}')

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
    # Строим словарь всех сущностей: id -> entity
    entities_dict = {}
    hierarchy = build_hierarchy()

    for root, _, files in os.walk(PROTOTYPES_DIR):
        for file in files:
            if not file.endswith((".yml", ".yaml")):
                continue
            path = os.path.join(root, file)
            try:
                with open(path, encoding="utf-8") as f:
                    data = yaml.load(f, Loader=IgnoreUnknown)
            except Exception:
                continue
            if not data:
                continue
            entities = [data] if isinstance(data, dict) else data
            for ent in entities:
                if isinstance(ent, dict) and ent.get("id"):
                    entities_dict[ent["id"]] = ent
    # print("CMClosetEmergencyFilled" in entities_dict)
    # Обрабатываем прототипы
    for root, _, files in os.walk(PROTOTYPES_DIR):
        for file in files:
            if not file.endswith((".yml", ".yaml")):
                continue
            path = os.path.join(root, file)
            process_prototype(path, entities_dict, hierarchy)


if __name__ == "__main__":
    main()
