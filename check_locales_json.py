#!/usr/bin/env python3
import json
import os
import sys

locales_dir = "Locales"
errors = []
total_files = 0
valid_files = 0

for filename in sorted(os.listdir(locales_dir)):
    if not filename.endswith('.json'):
        continue
    
    filepath = os.path.join(locales_dir, filename)
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        valid_files += 1
        print(f"✓ {filename}: Valid JSON")
    except json.JSONDecodeError as e:
        errors.append(f"{filename}: {e}")
        print(f"✗ {filename}: JSON ERROR - {e}")
    except Exception as e:
        errors.append(f"{filename}: {type(e).__name__} - {e}")
        print(f"✗ {filename}: {type(e).__name__} - {e}")

total_files = valid_files + len(errors)
print(f"\n=== SUMMARY ===")
print(f"Total files checked: {total_files}")
print(f"Valid files: {valid_files}")
print(f"Errors found: {len(errors)}")

if errors:
    print("\n=== ERROR DETAILS ===")
    for error in errors:
        print(error)
    sys.exit(1)
else:
    print("\nAll JSON files are valid!")
    sys.exit(0)
