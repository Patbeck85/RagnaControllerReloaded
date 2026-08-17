#!/usr/bin/env python3
import json
import sys
from pathlib import Path

locales_dir = Path("/mnt/c/RagnaController/src/RagnaController/Locales")
errors = []

for json_file in sorted(locales_dir.glob("*.json")):
    try:
        with open(json_file, 'r', encoding='utf-8') as f:
            json.load(f)
        print(f"✓ {json_file.name}")
    except json.JSONDecodeError as e:
        errors.append((json_file.name, str(e)))
        print(f"✗ {json_file.name}: {e}")

if errors:
    print(f"\n{len(errors)} files with JSON errors:")
    for name, error in errors:
        print(f"  - {name}: {error}")
    sys.exit(1)
else:
    print("\nAll JSON files are valid!")
    sys.exit(0)
