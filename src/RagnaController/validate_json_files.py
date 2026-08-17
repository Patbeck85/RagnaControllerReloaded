#!/usr/bin/env python3
import json
import os

locales_dir = '/mnt/c/RagnaController/src/RagnaController/Locales'
json_files = [f for f in os.listdir(locales_dir) if f.endswith('.json')]
print(f"Found {len(json_files)} JSON files in Locales directory\n")

errors_found = 0
for json_file in sorted(json_files):
    path = os.path.join(locales_dir, json_file)
    try:
        with open(path, 'r', encoding='utf-8') as f:
            content = f.read()
            data = json.loads(content)
            print(f"✓ {json_file} is valid JSON with {len(data)} keys")
    except json.JSONDecodeError as e:
        print(f"✗ {json_file} has JSON syntax error at line {e.lineno}: {e.msg}")
        errors_found += 1
    except Exception as e:
        print(f"✗ {json_file} error: {type(e).__name__}: {e}")
        errors_found += 1

print(f"\n{'='*60}")
if errors_found == 0:
    print("All JSON files are valid!")
else:
    print(f"Found {errors_found} JSON file(s) with errors")
