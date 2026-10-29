#!/usr/bin/env python3
"""Reproducible, offline, alpha-based normal-map generator for SS14 textures.

Mirrors resource paths under _GeneratedNormalMaps inside the same Textures root.
For .rsi sheets the output is a separate, loadable .rsi with the original meta.json.
No runtime generation, source asset modification, or network access is required.
"""

from __future__ import annotations

import argparse
import io
import json
import math
import shutil
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageFilter

TEXTURE_ROOTS = (Path("Resources/Textures"), Path("Content.CMU/Resources/Textures"))
OUTPUT_DIR = "_GeneratedNormalMaps"
EXCLUDED_DIRS = frozenset({"interface", "ui", "hud", "icons", "fonts", "cursors", "logos"})


def source_location(repo: Path, path: Path) -> tuple[Path, Path] | None:
    """Return (texture_root, relative_path) for an eligible source path."""
    absolute = (repo / path).resolve()
    for root in TEXTURE_ROOTS:
        base = (repo / root).resolve()
        if not absolute.is_relative_to(base):
            continue
        relative = absolute.relative_to(base)
        if OUTPUT_DIR in relative.parts or any(p.lower() in EXCLUDED_DIRS for p in relative.parts[:-1]):
            return None
        if relative.suffix.lower() not in {".png", ".json"}:
            return None
        if relative.suffix.lower() == ".json" and relative.name != "meta.json":
            return None
        if relative.suffix.lower() == ".json" and not any(p.endswith(".rsi") for p in relative.parts[:-1]):
            return None
        return repo / root, relative
    return None


def owning_asset(relative: Path) -> Path:
    """Treat one RSI as a single asset; its PNG sheets share metadata."""
    for idx, part in enumerate(relative.parts[:-1]):
        if part.endswith(".rsi"):
            return Path(*relative.parts[: idx + 1])
    return relative


def changed_paths(repo: Path, ref: str) -> list[Path]:
    completed = subprocess.run(
        ["git", "diff", "--name-status", "-z", "--find-renames", ref, "--"],
        cwd=repo,
        check=True,
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
    )
    parts = completed.stdout.split(b"\0")
    result: list[Path] = []
    index = 0
    while index < len(parts) and parts[index]:
        status = parts[index].decode("utf-8", "surrogateescape")
        index += 1
        count = 2 if status.startswith(("R", "C")) else 1
        for raw in parts[index : index + count]:
            result.append(Path(raw.decode("utf-8", "surrogateescape")))
        index += count
    return result


def encode_normal(dx: float, dy: float, strength: float) -> tuple[int, int, int]:
    # The image pixel Y axis points down; tangent-space green points up.
    nx, ny, nz = -dx * strength, dy * strength, 1.0
    norm = math.sqrt(nx * nx + ny * ny + nz * nz)
    return tuple(int(round(max(0.0, min(1.0, (v / norm + 1.0) * 0.5)) * 255)) for v in (nx, ny, nz))


def normal_sheet(source: Image.Image, frame_size: tuple[int, int], bevel: float, strength: float) -> Image.Image:
    """Generate each RSI frame independently, avoiding bleed between sheet cells."""
    width, height = source.size
    fw, fh = frame_size
    if fw <= 0 or fh <= 0 or width % fw or height % fh:
        raise ValueError(f"Image {source.size} is not a multiple of RSI frame size {frame_size}")
    src = source.convert("RGBA")
    result = Image.new("RGBA", src.size, (0, 0, 0, 0))
    for frame_y in range(0, height, fh):
        for frame_x in range(0, width, fw):
            tile = src.crop((frame_x, frame_y, frame_x + fw, frame_y + fh))
            alpha = tile.getchannel("A")
            smooth = alpha.filter(ImageFilter.GaussianBlur(radius=bevel)) if bevel > 0 else alpha
            values = smooth.load()
            opacity = alpha.load()
            output = Image.new("RGBA", (fw, fh), (0, 0, 0, 0))
            pixels = output.load()
            for y in range(fh):
                for x in range(fw):
                    a = opacity[x, y]
                    if not a:
                        continue
                    # Replicated border samples do not wrap to adjacent spritesheet cells.
                    dx = (values[min(x + 1, fw - 1), y] - values[max(x - 1, 0), y]) / 510.0
                    dy = (values[x, min(y + 1, fh - 1)] - values[x, max(y - 1, 0)]) / 510.0
                    pixels[x, y] = (*encode_normal(dx, dy, strength), a)
            result.paste(output, (frame_x, frame_y))
    return result


def png_bytes(image: Image.Image) -> bytes:
    out = io.BytesIO()
    image.save(out, format="PNG", optimize=True, compress_level=9)
    return out.getvalue()


class Synchronizer:
    def __init__(self, check: bool, bevel: float, strength: float):
        self.check = check
        self.bevel = bevel
        self.strength = strength
        self.errors: list[str] = []
        self.changed = 0
        self.checked = 0

    def sync_bytes(self, path: Path, expected: bytes) -> None:
        self.checked += 1
        if path.is_file() and path.read_bytes() == expected:
            return
        self.changed += 1
        if self.check:
            self.errors.append(f"missing or outdated: {path}")
            return
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(expected)
        print(f"generated: {path}")

    def remove(self, path: Path) -> None:
        if not path.exists():
            return
        self.changed += 1
        if self.check:
            self.errors.append(f"orphaned generated file: {path}")
        elif path.is_dir():
            shutil.rmtree(path)
            print(f"removed: {path}")
        else:
            path.unlink()
            print(f"removed: {path}")

    def sync_png(self, source: Path, target: Path, frame_size: tuple[int, int] | None = None) -> None:
        if not source.is_file():
            self.remove(target)
            return
        with Image.open(source) as img:
            size = frame_size or img.size
            result = normal_sheet(img, size, self.bevel, self.strength)
            # Verify decoded pixels, not PNG compression bytes, across Pillow/zlib versions.
            if target.is_file():
                try:
                    with Image.open(target) as old:
                        if old.size == result.size and old.convert("RGBA").tobytes() == result.tobytes():
                            self.checked += 1
                            return
                except (OSError, ValueError):
                    pass
            self.sync_bytes(target, png_bytes(result))

    def sync_rsi(self, source: Path, target: Path) -> None:
        metadata = source / "meta.json"
        if not metadata.is_file():
            # A removed RSI should not leave an unrelated, loadable stale normal RSI.
            self.remove(target)
            if source.exists() and any(source.glob("*.png")):
                raise ValueError(f"RSI is missing meta.json: {source}")
            return
        raw_meta = metadata.read_bytes()
        data = json.loads(raw_meta)
        size = data["size"]
        # Robust RSI metadata uses {"x": W, "y": H}; accept array form in tests too.
        frame_size = ((int(size["x"]), int(size["y"])) if isinstance(size, dict)
                      else (int(size[0]), int(size[1])))
        self.sync_bytes(target / "meta.json", raw_meta)
        expected = {"meta.json"}
        for image in sorted(source.glob("*.png")):
            expected.add(image.name)
            self.sync_png(image, target / image.name, frame_size)
        if target.is_dir():
            for old in target.iterdir():
                if old.name not in expected:
                    self.remove(old)


def run(repo: Path, paths: list[Path], check: bool, bevel: float, strength: float, full: bool = False) -> Synchronizer:
    worker = Synchronizer(check, bevel, strength)
    assets: set[tuple[Path, Path]] = set()
    for path in paths:
        location = source_location(repo, path)
        if location:
            root, relative = location
            assets.add((root, owning_asset(relative)))
    for root, relative in sorted(assets, key=lambda x: str(x)):
        source = root / relative
        target = root / OUTPUT_DIR / relative
        if relative.suffix == ".rsi":
            worker.sync_rsi(source, target)
        else:
            worker.sync_png(source, target)
    if full:
        # In full-reconciliation mode, remove obsolete output files/directories.
        for texture_root in TEXTURE_ROOTS:
            output_root = repo / texture_root / OUTPUT_DIR
            if not output_root.exists():
                continue
            expected_assets = {root / OUTPUT_DIR / relative for root, relative in assets if root == repo / texture_root}
            for candidate in sorted(output_root.rglob("*"), key=lambda x: len(x.parts), reverse=True):
                if candidate.is_file() and not any(candidate == a or a in candidate.parents for a in expected_assets):
                    worker.remove(candidate)
                elif candidate.is_dir() and not any(candidate == a or candidate in a.parents or a in candidate.parents for a in expected_assets):
                    if candidate.exists():
                        worker.remove(candidate)
    return worker


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--all", action="store_true", help="Process every eligible existing texture")
    selection.add_argument("--changed-from", metavar="GIT_REF", help="Process files changed since a Git ref")
    selection.add_argument("--paths", nargs="+", type=Path, metavar="PATH", help="Process selected source files or folders")
    parser.add_argument("--check", action="store_true", help="Verify generated output without writing")
    parser.add_argument("--bevel", type=float, default=1.25, help="Blur radius for alpha-derived bevel (default: 1.25)")
    parser.add_argument("--strength", type=float, default=5.0, help="Normal strength (default: 5.0)")
    args = parser.parse_args()
    if not (0 <= args.bevel <= 16 and 0 <= args.strength <= 100):
        parser.error("--bevel must be 0..16 and --strength 0..100")
    repo = Path(__file__).resolve().parents[2]
    try:
        if args.all:
            paths = [p.relative_to(repo) for r in TEXTURE_ROOTS if (repo / r).exists()
                     for p in (repo / r).rglob("*") if p.is_file()]
        elif args.changed_from:
            paths = changed_paths(repo, args.changed_from)
        else:
            paths = []
            for path in args.paths:
                full = (repo / path).resolve()
                if full.is_dir():
                    paths.extend(p.relative_to(repo) for p in full.rglob("*") if p.is_file())
                else:
                    paths.append(path)
        worker = run(repo, paths, args.check, args.bevel, args.strength, args.all)
    except (ValueError, OSError, KeyError, subprocess.CalledProcessError) as exc:
        print(f"normal-map generation failed: {exc}", file=sys.stderr)
        return 2
    for err in worker.errors:
        print(err, file=sys.stderr)
    print(f"normal maps: checked {worker.checked}; changes needed {worker.changed}")
    return 1 if worker.errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
