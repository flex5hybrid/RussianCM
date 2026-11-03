"""CMU14: regression coverage for resource mount collisions before packaging."""

from contextlib import redirect_stdout
from io import StringIO
from pathlib import Path
from tempfile import TemporaryDirectory
import unittest

from check_resource_paths import find_duplicate_paths, main


class ResourcePathsTest(unittest.TestCase):
    def setUp(self) -> None:
        self.directory = TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.roots = tuple(Path(self.directory.name) / name for name in ("base", "cmu", "rumc"))
        for root in self.roots:
            root.mkdir()

    def write(self, mount: int, path: str, content: str = "same") -> Path:
        file = self.roots[mount] / path
        file.parent.mkdir(parents=True, exist_ok=True)
        file.write_text(content, encoding="utf-8")
        return file

    def test_identical_and_differing_copies_are_rejected(self) -> None:
        for path in ("Textures/table.rsi/sewing_table.png", "Textures/table.rsi/meta.json",
                     "Prototypes/test.yml", "Locale/ru-RU/test.ftl", "Audio/test.ogg"):
            with self.subTest(path=path):
                for mount, content in enumerate(("same", "same", "different")):
                    self.write(mount, path, content)
                self.assertEqual(find_duplicate_paths(self.roots)[path],
                                 [root / path for root in self.roots])

    def test_unique_paths_and_shared_directory_names_are_allowed(self) -> None:
        for mount in range(3):
            self.write(mount, f"Textures/table.rsi/state{mount}.png")
        self.assertEqual(find_duplicate_paths(self.roots), {})

    def test_paths_are_case_sensitive(self) -> None:
        self.write(0, "Textures/Icon.png")
        self.write(1, "Textures/icon.png")
        self.assertEqual(find_duplicate_paths(self.roots), {})

    def test_missing_mount_fails(self) -> None:
        with redirect_stdout(StringIO()):
            self.assertEqual(main([str(self.roots[0] / "missing")]), 1)

    def test_cli_reports_every_source_and_fails_for_duplicates(self) -> None:
        first = self.write(0, "Prototypes/test.yml")
        second = self.write(1, "Prototypes/test.yml")
        output = StringIO()
        with redirect_stdout(output):
            result = main([str(root) for root in self.roots])
        self.assertEqual(result, 1)
        for text in ("Prototypes/test.yml", str(first), str(second)):
            self.assertIn(text, output.getvalue())

    def test_cli_succeeds_without_duplicates(self) -> None:
        self.write(0, "Textures/table.rsi/sewing_table.png")
        with redirect_stdout(StringIO()):
            self.assertEqual(main([str(root) for root in self.roots]), 0)


if __name__ == "__main__":
    unittest.main()
