#!/usr/bin/env python3
# CMU14 Begin: downstream changelog assembly.
"""
Assemble changelog .yml parts into a changelog file.

Entry IDs are append-only and never renumbered. This is required by the client,
which persists the last-read ID between sessions. Keep old entries available for
Discord recovery, even after a prolonged outage.

usage: update_changelog.py <changelog-file> <parts-dir> --category "Main"
"""

import argparse
import datetime
import hashlib
import json
import re
import subprocess
from pathlib import Path
from typing import Any

import yaml

CATEGORY_MAIN = "Main"
CHANGELOG_CATEGORIES = {"CMU": "CMU", "RMC14": "RMC14", "Main": "Changelog",
                        "Maps": "Maps", "Admin": "Admin", "Rules": "Rules"}
CHANGE_TYPES = {"Add", "Remove", "Tweak", "Fix", "Map", "Code", "Admin"}


# Prevent PyYAML from turning ISO-8601 strings into datetime instances and then
# serializing them in a different format on every changelog update.
class NoDatesSafeLoader(yaml.SafeLoader):
    @classmethod
    def remove_implicit_resolver(cls, tag_to_remove):
        if "yaml_implicit_resolvers" not in cls.__dict__:
            cls.yaml_implicit_resolvers = cls.yaml_implicit_resolvers.copy()

        for first_letter, mappings in cls.yaml_implicit_resolvers.items():
            cls.yaml_implicit_resolvers[first_letter] = [
                (tag, regexp) for tag, regexp in mappings if tag != tag_to_remove
            ]


NoDatesSafeLoader.remove_implicit_resolver("tag:yaml.org,2002:timestamp")


def entry_sort_key(entry: dict[str, Any]) -> tuple:
    timestamp = entry.get("time")
    parsed = datetime.datetime.min.replace(tzinfo=datetime.timezone.utc)
    if timestamp:
        timestamp = re.sub(r"^(\d{4})-(\d{1,2})-(\d{1,2})(?=T|$)",
                           lambda m: f"{m[1]}-{int(m[2]):02d}-{int(m[3]):02d}", str(timestamp))
        parsed = datetime.datetime.fromisoformat(timestamp.replace("Z", "+00:00"))
        if parsed.tzinfo is None:
            parsed = parsed.replace(tzinfo=datetime.timezone.utc)
    return (parsed, int(entry.get("id", 0)))


def load_yaml(path: str) -> dict[str, Any]:
    with open(path, "r", encoding="utf-8-sig") as f:
        return yaml.load(f, Loader=NoDatesSafeLoader) or {}


def entry_key(entry: dict[str, Any]) -> tuple:
    url = str(entry.get("url") or "").strip().rstrip("/")
    if url:
        return ("url", url)
    timestamp = entry.get("time")
    if timestamp:
        timestamp = entry_sort_key(entry)[0].astimezone(datetime.timezone.utc).isoformat()
    fingerprint = hashlib.sha256(json.dumps(entry.get("changes", []), sort_keys=True).encode()).hexdigest()
    return ("manual", entry.get("author"), timestamp, fingerprint)


def merge_changes(target, source):
    for change in source["changes"]:
        if change not in target["changes"]:
            target["changes"].append(change)


def assemble(changelog_file: str, parts_dir: str, category: str, untagged_category=None,
             previous=None, repository=None) -> None:
    current_data = load_yaml(changelog_file)
    original = current_data.get("Entries", [])
    previous = previous or {}
    published = {entry_key(e): e for e in previous.get("Entries", [])}
    published_ids = {}
    reserved_ids = set()
    for key, entry in published.items():
        if entry["id"] not in reserved_ids:
            published_ids[key] = entry["id"]
            reserved_ids.add(entry["id"])
    published_max = max([previous.get("LastId", 0), *published_ids.values()])
    max_id = max([published_max, current_data.get("LastId", 0), *(e["id"] for e in original)])
    changed = False
    existing = {}
    # Merge duplicate URLs without discarding changes or renumbering surviving IDs.
    for entry in original:
        key = entry_key(entry)
        if key in existing:
            existing[key]["id"] = max(existing[key]["id"], entry["id"])
            merge_changes(existing[key], entry)
            changed = True
        else:
            existing[key] = entry

    if repository:
        prefix = f"https://github.com/{repository}/pull/".lower()
        for key, entry in published.items():
            if key not in existing and key[0] == "url" and key[1].lower().startswith(prefix):
                # An upstream conflict resolution can replace the entire file.
                existing[key] = entry
                changed = True

    used_ids = reserved_ids
    for key, entry in existing.items():
        current_id = entry["id"]
        if key in published_ids:
            entry["id"] = published_ids[key]
        elif current_id <= published_max or current_id in used_ids:
            max_id += 1
            entry["id"] = max_id
        used_ids.add(entry["id"])
        changed |= entry["id"] != current_id

    consumed = []
    for partpath in sorted(Path(parts_dir).glob("*.yml")):
        part = load_yaml(str(partpath))
        if part.get("category", untagged_category or category) != category:
            continue
        if "time" not in part:
            # Keep the key stable if assembly is interrupted before removing the part.
            part["time"] = datetime.datetime.fromtimestamp(partpath.stat().st_mtime, datetime.timezone.utc).isoformat()
        changes = part["changes"]
        if not isinstance(changes, list):
            changes = [changes]
        # Validate all parts before replacing the changelog or deleting inputs.
        for change in changes:
            if not isinstance(change, dict) or not isinstance(change.get("message"), str):
                raise ValueError(f"Invalid changelog change in {partpath}")
            if change.get("type") not in CHANGE_TYPES:
                raise ValueError(f"Invalid changelog type in {partpath}")
        if changes:
            part["changes"] = changes
            key = entry_key(part)
            if key in existing:
                previous_count = len(existing[key]["changes"])
                merge_changes(existing[key], part)
                changed |= len(existing[key]["changes"]) != previous_count
            else:
                max_id += 1
                entry = {"author": part["author"], "time": part["time"],
                         "changes": changes, "id": max_id, "url": part.get("url")}
                if part.get("labels"):
                    entry["labels"] = part["labels"]
                existing[key] = entry
                changed = True
        consumed.append(partpath)

    if max_id > max((e["id"] for e in existing.values()), default=0) and current_data.get("LastId") != max_id:
        # An upstream merge can delete or renumber the highest previously issued ID.
        current_data["LastId"] = max_id
        changed = True
    if changed:
        current_data["Entries"] = sorted(existing.values(), key=entry_sort_key)
        output = yaml.safe_dump(current_data, allow_unicode=True, sort_keys=False)
        target = Path(changelog_file)
        temporary = target.with_suffix(target.suffix + ".tmp")
        temporary.write_text(output, encoding="utf-8", newline="\n")
        temporary.replace(target)
    for partpath in consumed:
        partpath.unlink()
    print(f"Have {len(existing)} entries; consumed {len(consumed)} parts")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("changelog_file")
    parser.add_argument("parts_dir")
    parser.add_argument("--category", default=CATEGORY_MAIN)
    parser.add_argument("--all", action="store_true", help="Assemble all supported sections next to the target file")
    parser.add_argument("--previous-ref", help="Restore IDs from the last downstream changelog commit")
    parser.add_argument("--repository", help="Preserve this repository's entries after an upstream merge")
    args = parser.parse_args()

    def previous_changelog(target):
        if not args.previous_ref:
            return None
        result = subprocess.run(
            ["git", "show", f"{args.previous_ref}:{target}"], check=True, stdout=subprocess.PIPE,
        )
        return yaml.load(result.stdout, Loader=NoDatesSafeLoader)

    if args.all:
        if args.category not in CHANGELOG_CATEGORIES:
            raise ValueError(f"Unknown changelog category: {args.category}")
        for category, filename in CHANGELOG_CATEGORIES.items():
            target = Path(args.changelog_file).parent / f"{filename}.yml"
            assemble(str(target), args.parts_dir, category, untagged_category="CMU",
                     previous=previous_changelog(target), repository=args.repository)
    else:
        assemble(args.changelog_file, args.parts_dir, args.category,
                 previous=previous_changelog(args.changelog_file), repository=args.repository)


if __name__ == "__main__":
    main()
# CMU14 End
