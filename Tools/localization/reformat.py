import os
import re

ROOT = "../../Resources/Locale/ru-RU/_RMC14/"
REPORT_FILE = "desc_without_name_report.txt"

name_pattern = re.compile(r'^(.*)-name\s*=\s*(.*)')
desc_pattern = re.compile(r'^(.*)-desc\s*=\s*(.*)')


def process_file(path, report):

    with open(path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    names = {}
    descs = {}

    for i, line in enumerate(lines):

        name_match = name_pattern.match(line)
        desc_match = desc_pattern.match(line)

        if name_match:
            key = name_match.group(1)
            names[key] = (i, name_match.group(2))

        if desc_match:
            key = desc_match.group(1)
            descs[key] = (i, desc_match.group(2))

    processed_lines = set()
    new_lines = []

    for i, line in enumerate(lines):

        if i in processed_lines:
            continue

        name_match = name_pattern.match(line)

        if name_match:

            key = name_match.group(1)
            name_value = name_match.group(2)

            if key in descs:

                desc_index, desc_value = descs[key]

                new_lines.append(f"{key} = {name_value}")
                new_lines.append(f"  .desc = {desc_value}")

                processed_lines.add(i)
                processed_lines.add(desc_index)

            else:
                # name без desc
                new_lines.append(f"{key} = {name_value}")
                processed_lines.add(i)

            continue

        desc_match = desc_pattern.match(line)

        if desc_match:

            key = desc_match.group(1)

            if key not in names:

                report.write(
                    f"{path}:{i+1} -> {line.strip()}\n"
                )

                new_lines.append(line.rstrip("\n"))
                processed_lines.add(i)

                continue

        new_lines.append(line.rstrip("\n"))
        processed_lines.add(i)

    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(new_lines) + "\n")


def scan():

    with open(REPORT_FILE, "w", encoding="utf-8") as report:

        for root, dirs, files in os.walk(ROOT):
            for file in files:

                if file.endswith(".ftl"):

                    path = os.path.join(root, file)
                    print("Processing:", path)

                    process_file(path, report)


if __name__ == "__main__":
    scan()
