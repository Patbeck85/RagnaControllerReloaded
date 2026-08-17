#!/usr/bin/env python3
import json
import os

error_files = []
locales_dir = "/mnt/c/RagnaController/Locales"

for filename in sorted(os.listdir(locales_dir)):
    if not filename.endswith('.json'):
        continue
    
    filepath = os.path.join(locales_dir, filename)
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        try:
            json.loads(content)
        except json.JSONDecodeError as e:
            error_files.append((filename, str(e)))
            print(f"ERROR in {filename}: {e}")
    except Exception as e:
        error_files.append((filename, f"Read error: {e}"))
        print(f"ERROR reading {filename}: {e}")

if error_files:
    print(f"\n=== SUMMARY: {len(error_files)} JSON files with errors ===")
    for filename, error in sorted(error_files):
        print(f"  {filename}: {error}")
else:
    print("All JSON locale files are valid!")
