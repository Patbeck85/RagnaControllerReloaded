#!/usr/bin/env python3
import json
import os
import sys

def check_json_syntax(file_path):
    """Check JSON syntax and return any errors found."""
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            json.loads(content)
        return None  # No errors
    except json.JSONDecodeError as e:
        return {
            'file': file_path,
            'error': str(e),
            'line': e.lineno,
            'column': e.colno,
            'message': e.msg
        }

# Find all JSON files in the project (excluding bin/obj)
json_files = []
for root, dirs, files in os.walk('/mnt/c/RagnaController/src/RagnaController'):
    # Skip bin and obj directories
    if 'bin' in root or 'obj' in root:
        continue
    for file in files:
        if file.endswith('.json'):
            json_files.append(os.path.join(root, file))

print(f"Found {len(json_files)} JSON files to check")

# Check each JSON file for syntax errors
errors = []
for file_path in json_files:
    error = check_json_syntax(file_path)
    if error:
        errors.append(error)
        print(f"\nERROR in {file_path}:")
        print(f"  Line {error['line']}, Column {error['column']}: {error['message']}")
        print(f"  Error: {error['error']}")

if errors:
    print(f"\n\nTotal JSON syntax errors found: {len(errors)}")
else:
    print("\nNo JSON syntax errors found in any files!")
