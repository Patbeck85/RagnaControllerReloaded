#!/usr/bin/env python3
"""
Update all locale files to match en.json entries.
This script reads en.json and updates all other .json files in the Locales directory.
"""

import json
import os
import glob

# Base directory for locales
LOCALES_DIR = "/mnt/c/RagnaController/src/RagnaController/Locales"

def load_en_json():
    """Load the en.json file as the source of truth."""
    en_path = os.path.join(LOCALES_DIR, "en.json")
    with open(en_path, 'r', encoding='utf-8') as f:
        return json.load(f)

def update_locale_file(locale_name, en_entries):
    """Update a locale file with entries from en.json."""
    locale_path = os.path.join(LOCALES_DIR, f"{locale_name}.json")
    
    # Load existing file
    with open(locale_path, 'r', encoding='utf-8') as f:
        existing = json.load(f)
    
    # Update entries from en.json
    for key, value in en_entries.items():
        if key not in existing:
            print(f"  Added missing entry: {key}")
        existing[key] = value
    
    # Save updated file
    with open(locale_path, 'w', encoding='utf-8') as f:
        json.dump(existing, f, ensure_ascii=False, indent=2)
    
    print(f"✓ Updated {locale_name}.json")

def main():
    print("=" * 60)
    print("Updating all locale files to match en.json...")
    print("=" * 60)
    
    # Load en.json as source of truth
    print("\nLoading en.json...")
    en_entries = load_en_json()
    print(f"Loaded {len(en_entries)} entries from en.json")
    
    # Get all .json files except en.json itself
    json_files = glob.glob(os.path.join(LOCALES_DIR, "*.json"))
    json_files = [f for f in json_files if os.path.basename(f) != "en.json"]
    
    print(f"\nFound {len(json_files)} locale files to update:")
    for f in sorted(json_files):
        print(f"  - {os.path.basename(f)}")
    
    # Update each file
    print("\n" + "-" * 60)
    print("Updating locale files...")
    print("-" * 60)
    
    updated_count = 0
    for json_file in sorted(json_files):
        locale_name = os.path.basename(json_file).replace('.json', '')
        try:
            update_locale_file(locale_name, en_entries)
            updated_count += 1
        except Exception as e:
            print(f"✗ Error updating {locale_name}.json: {e}")
    
    print("\n" + "=" * 60)
    print(f"✓ Successfully updated {updated_count} locale files!")
    print("=" * 60)

if __name__ == "__main__":
    main()
