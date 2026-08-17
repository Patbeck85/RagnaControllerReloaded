#!/usr/bin/env python3
import json
import sys
import os

# Get all JSON files in the current directory excluding bin and obj
json_files = [f for f in os.listdir('.') if f.endswith('.json') and not f.startswith('bin') and not f.startswith('obj')]

print(f"Checking {len(json_files)} JSON files...")
errors = []

for filename in json_files:
    filepath = os.path.join('.', filename)
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        # Try to parse the JSON
        data = json.loads(content)
        print(f"✓ {filename}: Valid JSON")
    except json.JSONDecodeError as e:
        errors.append((filename, str(e)))
        print(f"✗ {filename}: Invalid JSON - {e}")
    except Exception as e:
        errors.append((filename, str(e)))
        print(f"✗ {filename}: Error - {e}")

if errors:
    print(f"\n{len(errors)} files have errors:")
    for filename, error in errors:
        print(f"  - {filename}: {error}")
    sys.exit(1)
else:
    print("\nAll JSON files are valid!")
    sys.exit(0)
