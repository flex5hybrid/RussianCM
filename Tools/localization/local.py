import os
import yaml

prototypes_path = "Resources/Prototypes"
locale_path = "Resources/Locale/ru-RU"

untranslated_entities = []

for root, dirs, files in os.walk(prototypes_path):
    for file in files:
        if file.endswith((".yml", ".yaml")):
            file_path = os.path.join(root, file)
            with open(file_path, 'r', encoding='utf-8') as f:
                try:
                    # Пробуем загрузить с BaseLoader для обхода неизвестных тегов
                    data = yaml.load(f, Loader=yaml.BaseLoader)

                    # Проверяем, что data не пуста
                    if data is None:
                        print(f"Предупреждение: Файл {file_path} пуст или не может быть распарсен. Пропускаем.")
                        continue

                    # Если data - словарь, преобразуем его в список значений для единообразия
                    if isinstance(data, dict):
                        data = data.values()

                    for entity in data:
                        # Ваша дальнейшая логика обработки каждой entity...
                        # Не забудьте добавить проверки на тип и наличие ключей, как у вас было ранее.
                        if isinstance(entity, dict) and entity.get('type') == 'entity':
                            entity_id = entity.get('id')
                            entity_name = entity.get('name')
                            entity_desc = entity.get('desc')
                            if entity_id and entity_name:
                                # Проверяем, является ли name прямой строкой
                                if not entity_name.startswith(('ent-', '...')):  # Уточните критерий
                                    untranslated_entities.append({
                                        'id': entity_id,
                                        'name': entity_name,
                                        'desc': entity_desc,
                                        'source_file': file_path
                                    })

                except yaml.YAMLError as e:
                    print(f"Ошибка чтения YAML в файле {file_path}: {e}")
                except Exception as e:
                    print(f"Неизвестная ошибка при обработке файла {file_path}: {e}")

# ... (дальнейший код для сравнения с .ftl файлами)

# 2. Сбор всех ключей из .ftl файлов (здесь нужен парсер FTL)
# Для этого шага можно использовать библиотеку fluent.syntax (pip install fluent.syntax)
# Пример приведен для демонстрации логики
existing_ftl_keys = []
for root, dirs, files in os.walk(locale_path):
    for file in files:
        if file.endswith(".ftl"):
            # ... код для парсинга .ftl и извлечения ключей сообщений
            # Например, existing_ftl_keys.append("ent-RedOxygenTank-name")
            pass

# 3. Сравнение и вывод результата
for entity in untranslated_entities:
    potential_ftl_key = f"ent-{entity['id']}-name" # Формируем ожидаемый ключ
    if potential_ftl_key not in existing_ftl_keys:
        print(f"Не переведено: {entity['id']} -> {potential_ftl_key}")
        print(f"   Имя в YAML: '{entity['name']}'")
        print(f"   Описание в YAML: '{entity['desc']}'")
        print(f"   Файл: {entity['source_file']}\n")
