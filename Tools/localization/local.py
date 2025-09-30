import os
import yaml
import re

def sanitize_ftl_value(text):
    """Экранирует специальные символы в FTL значениях"""
    if not text:
        return text
    # Экранируем фигурные скобки и кавычки
    text = text.replace('{', '{{').replace('}', '}}')
    # Убираем лишние пробелы
    text = re.sub(r'\s+', ' ', text).strip()
    return text

def create_ftl_from_prototypes_advanced():
    prototypes_path = "Resources/Prototypes"
    locale_base_path = "Resources/Locale/ru-RU/_RuMC"

    # Статистика
    stats = {
        'processed': 0,
        'created': 0,
        'entries_created': 0,
        'errors': 0
    }

    for root, dirs, files in os.walk(prototypes_path):
        for file in files:
            if file.endswith((".yml", ".yaml")):
                file_path = os.path.join(root, file)
                stats['processed'] += 1

                try:
                    with open(file_path, 'r', encoding='utf-8') as f:
                        data = yaml.load(f, Loader=yaml.BaseLoader)

                    if data is None:
                        continue

                    entities = data.values() if isinstance(data, dict) else data
                    ftl_entries = []
                    processed_entities = set()

                    for entity in entities:
                        if not isinstance(entity, dict):
                            continue

                        entity_type = entity.get('type')
                        entity_id = entity.get('id')

                        # Пропускаем дубликаты
                        if not entity_id or entity_id in processed_entities:
                            continue

                        processed_entities.add(entity_id)

                        # Обрабатываем разные типы entity
                        if entity_type == 'entity':
                            process_entity(entity, entity_id, ftl_entries)
                        # Можно добавить обработку других типов:
                        # elif entity_type == 'reagent':
                        #     process_reagent(entity, entity_id, ftl_entries)

                    if ftl_entries:
                        create_ftl_file(file_path, prototypes_path, locale_base_path,
                                      file, ftl_entries, stats)

                except yaml.YAMLError as e:
                    print(f"❌ Ошибка YAML в {file_path}: {e}")
                    stats['errors'] += 1
                except Exception as e:
                    print(f"❌ Ошибка обработки {file_path}: {e}")
                    stats['errors'] += 1

    # Вывод статистики
    print(f"\n📊 Статистика:")
    print(f"   Обработано YAML файлов: {stats['processed']}")
    print(f"   Создано FTL файлов: {stats['created']}")
    print(f"   Добавлено записей: {stats['entries_created']}")
    print(f"   Ошибок: {stats['errors']}")

def process_entity(entity, entity_id, ftl_entries):
    """Обрабатывает entity и добавляет записи в FTL"""
    entity_name = entity.get('name')
    entity_desc = entity.get('description')

    # Обрабатываем имя
    if (entity_name and
        isinstance(entity_name, str) and
        not entity_name.startswith('ent-') and
        len(entity_name) > 1):  # Не однобуквенные имена

        sanitized_name = sanitize_ftl_value(entity_name)
        loc_key = f"ent-{entity_id}-name"
        ftl_entries.append(f"{loc_key} = {sanitized_name}")

    # Обрабатываем описание
    if (entity_desc and
        isinstance(entity_desc, str) and
        not entity_desc.startswith('ent-') and
        len(entity_desc) > 5):  # Описание должно быть достаточно длинным

        sanitized_desc = sanitize_ftl_value(entity_desc)
        loc_key_desc = f"ent-{entity_id}-desc"
        ftl_entries.append(f"{loc_key_desc} = {sanitized_desc}")

def create_ftl_file(file_path, prototypes_path, locale_base_path, yaml_file, ftl_entries, stats):
    """Создает FTL файл с записями для перевода"""
    # Создаем путь для FTL файла
    rel_path = os.path.relpath(os.path.dirname(file_path), prototypes_path)
    ftl_dir = os.path.join(locale_base_path, rel_path)

    # Создаем директорию если не существует
    os.makedirs(ftl_dir, exist_ok=True)

    # Имя FTL файла
    ftl_filename = os.path.splitext(yaml_file)[0] + ".ftl"
    ftl_path = os.path.join(ftl_dir, ftl_filename)

    # Проверяем, не существует ли уже файл (чтобы не перезаписать существующие переводы)
    if os.path.exists(ftl_path):
        print(f"⚠️ Файл уже существует, пропускаем: {ftl_path}")
        return

    # Записываем FTL файл
    with open(ftl_path, 'w', encoding='utf-8') as ftl_file:
        ftl_file.write("\n".join(ftl_entries))
        ftl_file.write("\n")

    stats['created'] += 1
    stats['entries_created'] += len(ftl_entries)
    print(f"✅ Создан: {ftl_path} ({len(ftl_entries)} записей)")

if __name__ == "__main__":
    create_ftl_from_prototypes_advanced()
