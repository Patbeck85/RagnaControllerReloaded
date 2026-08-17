#!/usr/bin/env python3
import json
import os

# Check all JSON files in .hermes directory
hermes_dir = "/mnt/c/RagnaController/.hermes"
json_files = [f for f in os.listdir(hermes_dir) if f.endswith('.json')]

print(f".hermes: {len(json_files)} JSON files\n")
for f in sorted(json_files):
    filepath = os.path.join(hermes_dir, f)
    try:
        with open(filepath, 'r', encoding='utf-8') as file:
            content = file.read()
            data = json.loads(content)
            print(f"✓ {f} - Valid JSON")
    except json.JSONDecodeError as e:
        print(f"✗ {f} - Error at line {e.lineno}, column {e.colno}: {e.msg}")
        # Show first 200 chars of content to debug
        print(f"  Content preview: {content[:200]}")
