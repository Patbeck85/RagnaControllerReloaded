#!/usr/bin/env python3
import json
import os
import sys

def check_json_files(directory):
    errors = []
    for filename in sorted(os.listdir(directory)):
        if filename.endswith('.json'):
            filepath = os.path.join(directory, filename)
            try:
                with open(filepath, 'r', encoding='utf-8') as f:
                    json.load(f)
                print(f"✓ {filename} - Valid JSON")
            except json.JSONDecodeError as e:
                errors.append((filename, str(e)))
                print(f"✗ {filename} - ERROR: {e}")
            except Exception as e:
                errors.append((filename, str(e)))
                print(f"✗ {filename} - ERROR: {e}")
    return errors

if __name__ == "__main__":
    locales_dir = "/mnt/c/RagnaController/Locales"
    errors = check_json_files(locales_dir)
    if errors:
        print(f"\n\nFound {len(errors)} JSON errors:")
        for filename, error in errors[:20]:  # Show first 20 errors
            print(f"  {filename}: {error}")
        sys.exit(1)
    else:
        print("\n\nAll JSON files are valid!")
