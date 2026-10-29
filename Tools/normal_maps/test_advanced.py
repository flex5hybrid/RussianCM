"""Tests for the second-generation CMU normal estimator."""
import unittest

import numpy as np
from PIL import Image, ImageDraw

from normal_algorithms import NormalOptions, light_preview, normal_sheet, profile_for_path


class NormalAdvancedTests(unittest.TestCase):
    def make_box(self):
        im = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        d = ImageDraw.Draw(im)
        d.rectangle((4, 4, 27, 27), fill=(100, 132, 160, 255))
        d.rectangle((7, 7, 24, 24), fill=(165, 185, 207, 255))
        d.line((8, 12, 23, 12), fill=(25, 31, 37, 255), width=2)
        d.line((8, 21, 23, 21), fill=(35, 45, 50, 255), width=2)
        return im

    def test_interior_structure_on_opaque_object(self):
        box = self.make_box()
        out = np.asarray(normal_sheet(box, (32, 32), NormalOptions(),
                                      resource_path="Objects/box.rsi/box.png"))
        area = out[9:23, 9:23, :3]
        flat = np.array([128, 128, 255])
        self.assertGreater(int(np.max(np.abs(area.astype(int) - flat))), 35)
        self.assertTrue(np.all(out[0, 0] == [0, 0, 0, 0]))

    def test_flat_and_bevel_modes_do_not_derive_height(self):
        box = self.make_box()
        flat = np.asarray(normal_sheet(box, (32, 32), NormalOptions(mode="flat")))
        bevel = np.asarray(normal_sheet(box, (32, 32), NormalOptions(mode="bevel")))
        height = np.asarray(normal_sheet(box, (32, 32), NormalOptions(mode="height")))
        self.assertTrue(np.all(flat[flat[:, :, 3] > 0, :3] == [128, 128, 255]))
        self.assertEqual(tuple(bevel[11, 12, :3]), (128, 128, 255))
        self.assertNotEqual(tuple(height[11, 12, :3]), (128, 128, 255))

    def test_material_profile_mutes_floor_relief(self):
        img = self.make_box()
        a = np.asarray(normal_sheet(img, (32, 32), NormalOptions(profile="floor"))).astype(float)
        b = np.asarray(normal_sheet(img, (32, 32), NormalOptions(profile="equipment"))).astype(float)
        self.assertGreater(
            np.abs(b[11, 12, :3] - [128, 128, 255]).sum(),
            np.abs(a[11, 12, :3] - [128, 128, 255]).sum())

    def test_frame_isolation_and_determinism(self):
        box = self.make_box()
        sheet = Image.new("RGBA", (64, 32), (0, 0, 0, 0))
        sheet.paste(box, (0, 0))
        sheet.paste(box, (32, 0))
        opts = NormalOptions()
        out = normal_sheet(sheet, (32, 32), opts, resource_path="Objects/box.rsi/box.png")
        single = normal_sheet(box, (32, 32), opts, resource_path="Objects/box.rsi/box.png")
        self.assertEqual(out.crop((0, 0, 32, 32)).tobytes(), single.tobytes())
        self.assertEqual(out.crop((32, 0, 64, 32)).tobytes(), single.tobytes())
        self.assertEqual(
            out.tobytes(),
            normal_sheet(sheet, (32, 32), opts, resource_path="Objects/box.rsi/box.png").tobytes())

    def test_preview_and_profiles(self):
        box = self.make_box()
        normal = normal_sheet(box, (32, 32), NormalOptions())
        self.assertEqual(light_preview(box, normal, (32, 32), 4).mode, "RGBA")
        self.assertEqual(
            profile_for_path("Content.CMU/Resources/Textures/CMU14/Structures/Doors/test.rsi/foo.png"),
            "equipment")
        self.assertEqual(profile_for_path("Resources/Textures/Tiles/metal.png"), "floor")

    def test_validation(self):
        with self.assertRaises(ValueError):
            NormalOptions(mode="bad")
        with self.assertRaises(ValueError):
            NormalOptions(detail_radius=0)


if __name__ == "__main__":
    unittest.main()
