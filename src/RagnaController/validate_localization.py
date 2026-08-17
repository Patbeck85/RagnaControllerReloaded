#!/usr/bin/env python3
"""
Localization JSON Validator
Validates all language JSON files for syntax errors and missing keys
"""

import json
import os
import sys
from pathlib import Path

# Base directory for the project
BASE_DIR = Path("/mnt/c/RagnaController/src/RagnaController/Locales")

# English (fallback) file to check for required keys
FALLBACK_FILE = BASE_DIR / "en.json"

def load_json_file(filepath):
    """Load and validate JSON file syntax"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
            return data, None
    except json.JSONDecodeError as e:
        return None, f"JSON Syntax Error: {e}"
    except Exception as e:
        return None, f"Error: {e}"

def validate_json_structure(data, filename):
    """Validate JSON structure and check for common issues"""
    errors = []
    
    if not isinstance(data, dict):
        errors.append(f"Root element must be an object, got {type(data).__name__}")
        return errors
    
    # Check for duplicate keys (JSON5 allows them, but standard JSON doesn't)
    seen_keys = set()
    for key in data.keys():
        if key in seen_keys:
            errors.append(f"Duplicate key: '{key}'")
        seen_keys.add(key)
    
    return errors

def validate_all_languages():
    """Validate all language JSON files"""
    print("=" * 80)
    print("LOCALIZATION JSON VALIDATOR")
    print("=" * 80)
    print()
    
    # Find all JSON files
    json_files = list(BASE_DIR.glob("*.json"))
    json_files.sort(key=lambda x: x.name)
    
    print(f"Found {len(json_files)} language files in '{BASE_DIR}':")
    print("-" * 80)
    
    # Load fallback (English) first
    if not FALLBACK_FILE.exists():
        print(f"\n⚠️  Warning: Fallback file '{FALLBACK_FILE}' not found!")
        print("   All languages will be validated but cannot check for missing keys.\n")
        fallback_data = None
    else:
        print(f"\n✅ Loading fallback language from '{FALLBACK_FILE}'...")
        fallback_data, error = load_json_file(FALLBACK_FILE)
        if error:
            print(f"   ❌ Error: {error}")
            print("   Cannot validate other languages without fallback.\n")
            return
        else:
            print(f"   ✅ Loaded {len(fallback_data)} keys from fallback\n")
    
    # Validate each language file
    for json_file in json_files:
        print(f"\n📄 {json_file.name}")
        print("-" * 80)
        
        data, error = load_json_file(json_file)
        
        if error:
            print(f"   ❌ {error}")
            continue
        
        # Validate structure
        structure_errors = validate_json_structure(data, json_file.name)
        if structure_errors:
            for err in structure_errors:
                print(f"   ⚠️  {err}")
            continue
        
        print(f"   ✅ Valid JSON structure ({len(data)} keys)")
        
        # Check for missing keys (if fallback is available)
        if fallback_data:
            missing_keys = []
            for key in fallback_data.keys():
                if key not in data:
                    missing_keys.append(key)
            
            if missing_keys:
                print(f"   ⚠️  Missing {len(missing_keys)} keys compared to English:")
                for key in missing_keys[:10]:  # Show first 10
                    print(f"      - {key}")
                if len(missing_keys) > 10:
                    print(f"      ... and {len(missing_keys) - 10} more")
            else:
                print(f"   ✅ All keys from fallback are present")
        
        # Check for empty values
        empty_values = [k for k, v in data.items() if not v or v.strip() == ""]
        if empty_values:
            print(f"   ⚠️  {len(empty_values)} empty/blank values found:")
            for key in empty_values[:5]:
                print(f"      - '{key}' is empty")
        
        # Check for placeholder formatting
        import re
        placeholders = [k for k, v in data.items() if re.search(r'\{[0-9]+\}', v)]
        if placeholders:
            print(f"   ℹ️  {len(placeholders)} keys with number placeholders:")
            for key in placeholders[:5]:
                print(f"      - '{key}': {data[key]}")
    
    # Summary
    print("\n" + "=" * 80)
    print("VALIDATION SUMMARY")
    print("=" * 80)
    
    valid_files = [f for f in json_files if not load_json_file(f)[1]]
    invalid_files = [f for f in json_files if load_json_file(f)[1]]
    
    print(f"\n✅ Valid files: {len(valid_files)}")
    print(f"❌ Invalid files: {len(invalid_files)}")
    
    if invalid_files:
        print("\nInvalid files:")
        for f in invalid_files:
            print(f"   - {f.name}")
    
    print("\n" + "=" * 80)
    print("VALIDATION COMPLETE")
    print("=" * 80)

if __name__ == "__main__":
    validate_all_languages()
