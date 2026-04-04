import os
import re
import requests
import time

LOCALE_DIR = "../../Resources/Locale/ru-RU/_RMC14/"

# Словарь "ключ → желаемый перевод"
DICTIONARY = {
    "Ravager": "Разрушитель",
    "Pulse rifle": "Импульсная винтовка",
    "Marine": "Морпех",
    "Pistol": "Пистолет",
    "Helmet": "Шлем",
    "Armor": "Броня",

    # Ксеноморфы
    "Crusher": "Крушитель",
    "Praetorian": "Преторианец",
    "Queen": "Королева",
    "Runner": "Бегун",
    "Warrior": "Воин"
}


def apply_dictionary(text):
    for word, translation in DICTIONARY.items():
        text = re.sub(rf'\b{re.escape(word)}\b', translation, text, flags=re.IGNORECASE)
    return text


def translate(text):

    url = "https://translate.googleapis.com/translate_a/single"

    params = {
        "client": "gtx",
        "sl": "en",
        "tl": "ru",
        "dt": "t",
        "q": text
    }

    try:
        r = requests.get(url, params=params, timeout=10)
        return r.json()[0][0][0]
    except:
        return text


def protect_variables(text):

    variables = re.findall(r"\{.*?\}", text)
    protected = text

    for i, var in enumerate(variables):
        protected = protected.replace(var, f"__VAR{i}__")

    return protected, variables


def restore_variables(text, variables):

    for i, var in enumerate(variables):
        text = text.replace(f"__VAR{i}__", var)

    return text


def contains_cyrillic(text):
    return bool(re.search(r'[а-яА-ЯёЁ]', text))


def translate_ftl_file(path):

    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    # если файл уже переведён
    if any(contains_cyrillic(line) for line in lines):
        return False

    new_lines = []

    for line in lines:

        if "=" not in line:
            new_lines.append(line)
            continue

        key, value = line.split("=", 1)
        value = value.strip()

        if not value:
            new_lines.append(line)
            continue

        protected, vars = protect_variables(value)

        protected = apply_dictionary(protected)

        translated = translate(protected)
        translated = restore_variables(translated, vars)

        new_line = f"{key}= {translated}\n"
        new_lines.append(new_line)

        time.sleep(0.05)

    with open(path, "w", encoding="utf-8") as f:
        f.writelines(new_lines)

    return True


def run():

    files = []

    for root, dirs, fs in os.walk(LOCALE_DIR):
        for file in fs:
            if file.endswith(".ftl"):
                files.append(os.path.join(root, file))

    total = len(files)
    processed = 0
    translated = 0

    print("Всего файлов:", total)
    print()

    for path in files:

        processed += 1

        if translate_ftl_file(path):
            translated += 1
            status = "✔ переведён"
        else:
            status = "⏭ пропущен"

        percent = processed / total * 100

        print(f"[{processed}/{total}] {percent:.2f}%  {status}  →  {path}")

    print()
    print("Готово.")
    print("Переведено файлов:", translated)
    print("Пропущено файлов:", total - translated)


if __name__ == "__main__":
    run()
