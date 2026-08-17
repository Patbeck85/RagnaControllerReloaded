#!/usr/bin/env python3
"""
Localization Synchronization Script
Synchronizes all locale JSON files with en.json as source of truth.
"""

import json
import os
from pathlib import Path

# Define the locales directory
LOCALES_DIR = "/mnt/c/RagnaController/src/RagnaController/Locales"

# Load en.json as source of truth
with open(Path(LOCALES_DIR) / "en.json", 'r', encoding='utf-8') as f:
    en_data = json.load(f)

print(f"Loaded en.json with {len(en_data)} keys")
print("=" * 60)

# Load all Designer.cs files for reference translations
designer_files = sorted(Path(LOCALES_DIR).glob("*.Designer.cs"))
print(f"Found {len(designer_files)} Designer.cs files")

# Extract translations from Designer.cs files (for languages that have them)
designer_translations = {}
for designer_file in designer_files:
    lang_code = designer_file.stem  # e.g., "de-DE", "ja-JP"
    try:
        with open(designer_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Extract key-value pairs from the Designer.cs file
        import re
        matches = re.findall(r'internal static string (\w+) = "([^"]*)";', content)
        for key, value in matches:
            if lang_code not in designer_translations:
                designer_translations[lang_code] = {}
            designer_translations[lang_code][key] = value
    except Exception as e:
        print(f"Error reading {designer_file}: {e}")

print(f"Extracted translations from {len(designer_translations)} Designer.cs files")

# List of target languages to synchronize (excluding en.json)
target_languages = [
    "ar", "bg", "bn", "cs", "da", "de", "es", "fa", "fr", "gu",
    "he", "hi", "hu", "id", "it", "ja", "kn", "ko", "ml", "mr",
    "ms", "nl", "no", "or", "pa", "pl", "pt", "ro", "ru", "sv",
    "ta", "te", "th", "tl", "tr", "uk", "ur", "vi", "zh-cn", "zh"
]

print(f"\nWill synchronize {len(target_languages)} target languages")
print("=" * 60)

# Process each target language
for lang_code in target_languages:
    json_file = Path(LOCALES_DIR) / f"{lang_code}.json"
    
    # Check if Designer.cs file exists for this language
    designer_file = Path(LOCALES_DIR) / f"{lang_code}.Designer.cs"
    has_designer = designer_file.exists()
    
    print(f"\nProcessing {lang_code}...")
    
    if has_designer:
        print(f"  ✓ Designer.cs file found, using existing translations where available")
        
        # Load existing translations from Designer.cs
        with open(designer_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        import re
        matches = re.findall(r'internal static string (\w+) = "([^"]*)";', content)
        existing_translations = {key: value for key, value in matches}
        
        print(f"  Found {len(existing_translations)} existing translations")
        
        # Create new JSON with all 268 keys
        new_data = {}
        for key, en_value in en_data.items():
            if key in existing_translations:
                new_data[key] = existing_translations[key]
            else:
                # For missing keys, we'll need to translate from English
                # For now, use English as placeholder (will be translated later)
                new_data[key] = en_value
        
        # Save the synchronized JSON file
        with open(json_file, 'w', encoding='utf-8') as f:
            json.dump(new_data, f, ensure_ascii=False, indent=2)
        
        print(f"  ✓ Created {lang_code}.json with {len(new_data)} keys")
    else:
        print(f"  ✗ No Designer.cs file, creating from English (needs translation)")
        
        # Create JSON with all keys using English values as placeholder
        new_data = {key: value for key, value in en_data.items()}
        
        # Save the JSON file
        with open(json_file, 'w', encoding='utf-8') as f:
            json.dump(new_data, f, ensure_ascii=False, indent=2)
        
        print(f"  ✓ Created {lang_code}.json with {len(new_data)} keys (English placeholder)")

print("\n" + "=" * 60)
print("Synchronization complete!")
print("=" * 60)

# Verify all files have the same number of keys
print("\nVerification:")
for lang_code in target_languages:
    json_file = Path(LOCALES_DIR) / f"{lang_code}.json"
    if json_file.exists():
        with open(json_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        print(f"  {lang_code}: {len(data)} keys ✓")
    else:
        print(f"  {lang_code}: File not found ✗")

print("\nNote: Some files may still have English placeholder values that need translation.")
print("Run this script again after manually translating the missing keys.")
