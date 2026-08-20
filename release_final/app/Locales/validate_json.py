#!/usr/bin/env python3
"""Validate all JSON localization files for syntax errors."""

import json
import glob
import sys
from pathlib import Path

LOCALES_DIR = Path(__file__).parent

errors = []
valid_count = 0

json_files = list(LOCALES_DIR.glob("*.json"))

for f in json_files:
    try:
        with open(f, 'r', encoding='utf-8') as file:
            data = json.load(file)
        valid_count += 1
    except json.JSONDecodeError as e:
        errors.append(f"{f.name}: {e}")
    except Exception as e:
        errors.append(f"{f.name}: {type(e).__name__}: {e}")

if errors:
    print("❌ JSON Validation FAILED:")
    for error in errors:
        print(f"  {error}")
    sys.exit(1)
else:
    print(f"✅ All {valid_count} JSON files are valid!")
    sys.exit(0)
