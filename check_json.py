#!/usr/bin/env python3
import json
import os
import sys

# Find all JSON locale files in the project
locale_dir = "/mnt/c/RagnaController/Locales"
json_files = []

for filename in sorted(os.listdir(locale_dir)):
    if filename.endswith('.json'):
        json_files.append(os.path.join(locale_dir, filename))

print(f"Found {len(json_files)} JSON locale files")
print("\nChecking each file for JSON syntax errors...\n")

errors_found = []

for filepath in json_files:
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            # Try to parse the JSON
            data = json.loads(content)
            print(f"✓ {os.path.basename(filepath)} - Valid JSON")
    except json.JSONDecodeError as e:
        errors_found.append({
            'file': filepath,
            'error': str(e),
            'line': e.lineno,
            'column': e.colno
        })
        print(f"✗ {os.path.basename(filepath)} - JSON Error at line {e.lineno}, column {e.colno}: {e.msg}")

if errors_found:
    print(f"\n\n=== SUMMARY: {len(errors_found)} files with JSON syntax errors ===")
    for err in errors_found:
        print(f"File: {err['file']}")
        print(f"Line: {err['line']}, Column: {err['column']}")
        print(f"Error: {err['error']}")
        print("---")
else:
    print("\n\n=== ALL JSON FILES ARE VALID ===")
