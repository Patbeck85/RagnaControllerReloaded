#!/usr/bin/env python3
"""
Systematische Überprüfung aller XAML-Dateien auf {core:Loc *} Keys
und Vergleich mit allen JSON-Lokalisierungsdateien
"""

import json
import re
from pathlib import Path

xaml_dir = "/mnt/c/RagnaController/src/RagnaController"
locales_dir = "/mnt/c/RagnaController/Locales"

# 1. Alle XAML-Dateien finden und {core:Loc *} Keys sammeln
print("=" * 80)
print("SCHRITT 1: Extrahiere {core:Loc *} Keys aus allen XAML-Dateien")
print("=" * 80)
print()

xaml_files = list(Path(xaml_dir).glob("*.xaml"))
print(f"Found {len(xaml_files)} XAML files")
print()

all_locale_keys = set()
for xaml_file in xaml_files:
    try:
        with open(xaml_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Suche nach {core:Loc *} Pattern
        matches = re.findall(r'\{core:Loc\s+(\w+)\}', content)
        for match in matches:
            all_locale_keys.add(match)
            
    except Exception as e:
        print(f"Error reading {xaml_file.name}: {e}")

print(f"Total unique {{core:Loc *}} keys found in XAML files: {len(all_locale_keys)}")
print()

# 2. Alle JSON-Dateien laden und ihre Keys sammeln
print("=" * 80)
print("SCHRITT 2: Lade alle JSON-Lokalisierungsdateien")
print("=" * 80)
print()

locales_files = list(Path(locales_dir).glob("*.json"))
print(f"Found {len(locales_files)} locale files")
print()

# Funktion zum Flachen von nested dicts
def flatten_dict(d, parent_key='', sep='.'):
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.extend(flatten_dict(v, new_key, sep=sep).items())
        else:
            items.append((new_key, v))
    return dict(items)

# Sammle alle Keys aus allen Sprachdateien
all_json_keys = set()
for locale_file in locales_files:
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        flat_keys = flatten_dict(data)
        all_json_keys.update(flat_keys.keys())
        
    except Exception as e:
        print(f"Error reading {locale_file.name}: {e}")

print(f"Total unique keys in all JSON files: {len(all_json_keys)}")
print()

# 3. Finde fehlende Keys in en.json
print("=" * 80)
print("SCHRITT 3: Finde fehlende Keys in en.json")
print("=" * 80)
print()

en_file = Path(locales_dir) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

en_keys_flat = set(flatten_dict(en_data).keys())

# Fehlende Keys finden
missing_keys = all_locale_keys - en_keys_flat
extra_keys = en_keys_flat - all_locale_keys

print(f"Keys in XAML: {len(all_locale_keys)}")
print(f"Keys in en.json: {len(en_keys_flat)}")
print(f"Fehlende in en.json: {len(missing_keys)}")
print(f"Extra in en.json: {len(extra_keys)}")
print()

if missing_keys:
    print("FEHLENDE KEYS in en.json:")
    print("-" * 80)
    for key in sorted(missing_keys):
        print(f"  ✗ {key}")
else:
    print("✅ Alle {core:Loc *} Keys aus XAML sind in en.json enthalten!")

print()

# 4. Finde Keys, die in anderen Sprachen fehlen
print("=" * 80)
print("SCHRITT 4: Prüfe andere Sprachdateien auf fehlende Keys")
print("=" * 80)
print()

missing_in_other_languages = {}
for locale_file in locales_files:
    if locale_file.name == "en.json":
        continue
    
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        flat_keys = flatten_dict(data)
        missing = all_locale_keys - set(flat_keys.keys())
        
        if missing:
            lang = locale_file.stem
            if lang not in missing_in_other_languages:
                missing_in_other_languages[lang] = []
            missing_in_other_languages[lang].extend(sorted(missing))
            
    except Exception as e:
        print(f"Error reading {locale_file.name}: {e}")

if missing_in_other_languages:
    print("Fehlende Keys in anderen Sprachdateien:")
    print("-" * 80)
    for lang, keys in sorted(missing_in_other_languages.items()):
        print(f"\n{lang.upper()}: {len(keys)} fehlende Keys")
        for key in keys:
            print(f"  ✗ {key}")
else:
    print("✅ Alle Sprachdateien enthalten alle {core:Loc *} Keys!")

print()
print("=" * 80)
print("ZUSAMMENFASSUNG:")
print("=" * 80)
print(f"  XAML {{core:Loc *}} Keys: {len(all_locale_keys)}")
print(f"  Keys in en.json: {len(en_keys_flat)}")
print(f"  Fehlende in en.json: {len(missing_keys)}")
print(f"  Sprachdateien mit fehlenden Keys: {len(missing_in_other_languages)}")
