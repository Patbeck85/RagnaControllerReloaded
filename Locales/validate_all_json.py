#!/usr/bin/env python3
import json
import glob
import sys

files = glob.glob('*.json')
errors = []

print(f"Checking {len(files)} JSON files...")
print("=" * 50)

for f in sorted(files):
    try:
        with open(f, 'r', encoding='utf-8') as file:
            json.load(file)
        print(f'✓ {f}')
    except json.JSONDecodeError as e:
        print(f'✗ {f}: {e.msg} at line {e.lineno}')
        errors.append((f, str(e.msg), e.lineno))

print("=" * 50)
if errors:
    print(f'\n{len(errors)} files have JSON errors')
    for f, msg, line in errors:
        print(f"  - {f}: {msg} (line {line})")
else:
    print('\nAll JSON files are valid!')
