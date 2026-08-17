#!/usr/bin/env python3
import json
import sys

def validate_json(filepath):
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            json.load(f)
        return True, None
    except json.JSONDecodeError as e:
        return False, str(e)

# Check all locale files
locale_files = [
    '/mnt/c/RagnaController/src/RagnaController/Locales/de.json',
    '/mnt/c/RagnaController/src/RagnaController/Locales/en.json',
    '/mnt/c/RagnaController/src/RagnaController/Locales/zh-cn.json',
    '/mnt/c/RagnaController/src/RagnaController/Locales/ur.json',
]

for filepath in locale_files:
    valid, error = validate_json(filepath)
    if valid:
        print(f"✓ {filepath} - Valid JSON")
    else:
        print(f"✗ {filepath} - Invalid JSON: {error}")
