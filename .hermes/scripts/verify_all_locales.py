#!/usr/bin/env python3
"""
Verifikation: Prüfe ob alle {core:Loc *} Keys in allen Sprachdateien vorhanden sind (verschachtelt)
"""

import json
import re
from pathlib import Path

xaml_dir = "/mnt/c/RagnaController/src/RagnaController"
locales_dir = "/mnt/c/RagnaController/Locales"

# 1. Alle XAML-Dateien finden und {core:Loc *} Keys sammeln
print("=" * 80)
print("VERIFIZIERUNG: Prüfe ob alle {core:Loc *} Keys in allen Sprachdateien vorhanden sind")
print("=" * 80)
print()

xaml_files = list(Path(xaml_dir).glob("*.xaml"))
all_locale_keys = set()
for xaml_file in xaml_files:
    try:
        with open(xaml_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        matches = re.findall(r'\{core:Loc\s+(\w+)\}', content)
        for match in matches:
            all_locale_keys.add(match)
            
    except Exception as e:
        print(f"Error reading {xaml_file.name}: {e}")

print(f"Total unique {{core:Loc *}} keys found in XAML files: {len(all_locale_keys)}")
print()

# 2. Prüfe jede Sprachdatei
print("=" * 80)
print("SCHRITT 2: Prüfe jede Sprachdatei")
print("=" * 80)
print()

locales_files = list(Path(locales_dir).glob("*.json"))
all_ok = True

for locale_file in locales_files:
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
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
        
        flat_keys = set(flatten_dict(data).keys())
        missing = all_locale_keys - flat_keys
        
        if missing:
            lang = locale_file.stem
            print(f"✗ {lang}.json - {len(missing)} fehlende Keys:")
            for key in sorted(missing):
                print(f"    ✗ {key}")
            all_ok = False
        else:
            lang = locale_file.stem
            print(f"✓ {lang}.json - Alle {len(all_locale_keys)} Keys vorhanden!")
            
    except Exception as e:
        print(f"✗ Error reading {locale_file.name}: {e}")
        all_ok = False

print()

# 3. Zusammenfassung
print("=" * 80)
print("ZUSAMMENFASSUNG:")
print("=" * 80)
if all_ok:
    print("✅ ALLE Sprachdateien enthalten alle {core:Loc *} Keys!")
else:
    print("❌ Einige Sprachdateien haben fehlende Keys!")
