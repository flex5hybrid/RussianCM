#!/usr/bin/env python3
"""CMU14: run the engine's RSI checks with CMU's existing asset licenses."""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


def main() -> int:
    engine_schemas = Path(__file__).resolve().parents[2] / "RobustToolbox" / "Schemas"
    schema = json.loads((engine_schemas / "rsi.json").read_text(encoding="utf-8"))
    licenses = schema["properties"]["license"]["enum"]
    for license_name in (
        "MIT",
        "Proprietary Donor Cape Asset License (see ../LICENSE.txt)",
    ):
        if license_name not in licenses:
            licenses.append(license_name)

    # The upstream CLI reads its schema beside its script and has no schema option.
    # Stage an unchanged copy so every structural check is retained without editing the engine.
    with tempfile.TemporaryDirectory(prefix="cmu-rsi-") as directory:
        staged = Path(directory)
        validator = staged / "validate_rsis.py"
        shutil.copyfile(engine_schemas / "validate_rsis.py", validator)
        (staged / "rsi.json").write_text(json.dumps(schema), encoding="utf-8")
        return subprocess.run([sys.executable, str(validator), *sys.argv[1:]], check=False).returncode


if __name__ == "__main__":
    sys.exit(main())
