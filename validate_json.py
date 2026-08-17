#!/usr/bin/env python3
import json
import os
import sys

# Check all JSON files in the Locales directory
locales_dir = "/mnt/c/RagnaController/src/RagnaController/Locales"
json_files = [f for f in os.listdir(locales_dir) if f.endswith('.json')]

invalid_files = []
valid_count = 0

for filename in json_files:
    filepath = os.path.join(locales_dir, filename)
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            json.load(f)
        valid_count += 1
    except json.JSONDecodeError as e:
        invalid_files.append((filename, str(e)))

print(f"Total JSON files checked: {len(json_files)}")
print(f"Valid JSON files: {valid_count}")
print(f"Invalid JSON files: {len(invalid_files)}")

if invalid_files:
    print("\nInvalid files:")
    for filename, error in invalid_files:
        print(f"  - {filename}: {error[:200]}...")
else:
    print("\nAll JSON files are valid!")
