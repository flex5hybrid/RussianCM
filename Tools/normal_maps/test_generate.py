"""Offline unit tests for the deterministic CMU normal map generator."""

import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image

from generate import OUTPUT_DIR, normal_sheet, run, source_location


class NormalGenerationTests(unittest.TestCase):
    def test_flat_opaque_frame_has_upward_normal(self):
        image = Image.new("RGBA", (8, 8), (200, 120, 20, 255))
        normal = normal_sheet(image, (8, 8), 1.25, 5)
        self.assertEqual(normal.getpixel((4, 4)), (128, 128, 255, 255))

    def test_alpha_edges_and_transparency(self):
        image = Image.new("RGBA", (16, 16), (0, 0, 0, 0))
        for x in range(4, 12):
            for y in range(4, 12):
                image.putpixel((x, y), (150, 150, 150, 255))
        result = normal_sheet(image, (16, 16), 1.25, 5)
        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertLess(result.getpixel((4, 8))[0], 128)  # Left bevel faces left.
        self.assertGreater(result.getpixel((11, 8))[0], 128)  # Right bevel faces right.
        self.assertAlmostEqual(result.getpixel((8, 8))[0], 128, delta=6)
        self.assertAlmostEqual(result.getpixel((8, 8))[1], 128, delta=6)

    def test_sheet_frames_do_not_bleed_into_each_other(self):
        image = Image.new("RGBA", (16, 8), (0, 0, 0, 0))
        for y in range(8):
            for x in range(8):
                image.putpixel((x, y), (180, 180, 180, 255))
        result = normal_sheet(image, (8, 8), 1.25, 5)
        self.assertEqual(result.getpixel((7, 4)), (128, 128, 255, 255))
        self.assertEqual(result.getpixel((8, 4)), (0, 0, 0, 0))

    def test_invalid_sheet_dimensions_raise_error(self):
        with self.assertRaises(ValueError):
            normal_sheet(Image.new("RGBA", (17, 8)), (8, 8), 1.25, 5)

    def test_rsi_sync_and_check_and_remove_deleted_sheet(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            relative = Path("Content.CMU/Resources/Textures/CMU14/Objects/box.rsi")
            folder = repo / relative
            folder.mkdir(parents=True)
            meta = {"version": 1, "size": {"x": 8, "y": 8}, "states": [{"name": "crate"}]}
            (folder / "meta.json").write_text(json.dumps(meta))
            Image.new("RGBA", (16, 8), (200, 200, 200, 255)).save(folder / "crate.png")
            sources = [relative / "meta.json", relative / "crate.png"]
            first = run(repo, sources, check=False, bevel=1.25, strength=5)
            self.assertEqual(first.changed, 2)
            target = repo / "Content.CMU/Resources/Textures" / OUTPUT_DIR / "CMU14/Objects/box.rsi"
            self.assertTrue((target / "crate.png").is_file())
            self.assertEqual((target / "meta.json").read_bytes(), (folder / "meta.json").read_bytes())
            self.assertEqual(run(repo, sources, check=True, bevel=1.25, strength=5).changed, 0)
            (folder / "crate.png").unlink()
            missing = run(repo, sources, check=True, bevel=1.25, strength=5)
            self.assertEqual(missing.changed, 1)
            run(repo, sources, check=False, bevel=1.25, strength=5)
            self.assertFalse((target / "crate.png").exists())

    def test_check_accepts_different_png_compression(self):
        with tempfile.TemporaryDirectory() as temp:
            repo = Path(temp)
            relative = Path("Resources/Textures/Objects/box.png")
            source = repo / relative
            source.parent.mkdir(parents=True)
            image = Image.new("RGBA", (8, 8), (180, 180, 180, 255))
            image.save(source)
            run(repo, [relative], check=False, bevel=1.25, strength=5)
            output = repo / "Resources/Textures" / OUTPUT_DIR / "Objects/box.png"
            # Recompress losslessly: same normals, different binary PNG.
            with Image.open(output) as original:
                original.load()
                original.save(output, format="PNG", compress_level=0)
            self.assertEqual(run(repo, [relative], check=True, bevel=1.25, strength=5).changed, 0)

    def test_excluded_interface_and_generated_files(self):
        repo = Path("/tmp/sample-repo")
        self.assertIsNone(source_location(repo, Path("Content.CMU/Resources/Textures/CMU14/Interface/hud.rsi/meta.json")))
        self.assertIsNone(source_location(repo, Path("Resources/Textures/_GeneratedNormalMaps/Objects/foo.png")))


if __name__ == "__main__":
    unittest.main()
