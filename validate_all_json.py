#!/usr/bin/env python3
import json
import os
import sys

# Check all JSON files in the entire project
project_root = "/mnt/c/RagnaController"
json_files = []

for root, dirs, files in os.walk(project_root):
    # Skip bin directories and release output
    if 'bin' in root or 'obj' in root:
        continue
    for f in files:
        if f.endswith('.json'):
            json_files.append(os.path.join(root, f))

invalid_files = []
valid_count = 0

for filepath in json_files:
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            json.load(f)
        valid_count += 1
    except json.JSONDecodeError as e:
        invalid_files.append((filepath, str(e)))

print(f"Total JSON files checked: {len(json_files)}")
print(f"Valid JSON files: {valid_count}")
print(f"Invalid JSON files: {len(invalid_files)}")

if invalid_files:
    print("\nInvalid files:")
    for filepath, error in invalid_files:
        print(f"  - {filepath}: {error[:200]}...")
else:
    print("\nAll JSON files are valid!")
