#!/usr/bin/env python3
"""CMU14: reject overlapping virtual paths in the packaged resource mounts."""

import argparse
from collections import defaultdict
from collections.abc import Iterable, Sequence
from pathlib import Path


REPOSITORY_ROOT = Path(__file__).resolve().parents[2]
RESOURCE_ROOTS = (
    REPOSITORY_ROOT / "Resources",
    REPOSITORY_ROOT / "Content.CMU" / "Resources",
    REPOSITORY_ROOT / "Content.RUMC" / "Resources",
)


def find_duplicate_paths(resource_roots: Iterable[Path]) -> dict[str, list[Path]]:
    sources: dict[str, list[Path]] = defaultdict(list)
    for root in resource_roots:
        if not root.is_dir():
            raise FileNotFoundError(f"Resource directory does not exist: {root}")
        for file in root.rglob("*"):
            if file.is_file():
                # Match the engine's case-sensitive virtual paths, including on Windows.
                sources[file.relative_to(root).as_posix()].append(file)
    return {path: files for path, files in sorted(sources.items()) if len(files) > 1}


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("roots", nargs="*", type=Path, default=RESOURCE_ROOTS)
    args = parser.parse_args(argv)
    try:
        duplicates = find_duplicate_paths(args.roots)
    except FileNotFoundError as error:
        print(error)
        return 1
    for path, files in duplicates.items():
        print(f"Duplicate resource path: {path}")
        for file in files:
            print(f"  {file}")
    if duplicates:
        print(f"Found {len(duplicates)} overlapping resource paths. Keep one source per virtual path.")
        return 1
    print("Resource mounts have no overlapping file paths.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
