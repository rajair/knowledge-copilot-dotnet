"""Generate Pydantic models from contracts/schema/openapi.json (D-1-02). Run via `make contracts`.

Settings live in pyproject.toml under [tool.datamodel-codegen]. The generator writes the platform's
line endings, so the output is normalised to LF to keep the drift check stable on Windows.
"""

import subprocess
import sys
from pathlib import Path

EVALS_DIR = Path(__file__).resolve().parents[1]
OUTPUT = EVALS_DIR / "src" / "knowledge_evals" / "contracts" / "models.py"


def main() -> int:
    subprocess.run([sys.executable, "-m", "datamodel_code_generator"], cwd=EVALS_DIR, check=True)
    OUTPUT.write_bytes(OUTPUT.read_bytes().replace(b"\r\n", b"\n"))
    return 0


if __name__ == "__main__":
    sys.exit(main())
