#!/usr/bin/env python3
"""
Prüfe, ob alle {core:Loc *} Keys aus XAML in en.json enthalten sind
"""

import json
import re
from pathlib import Path

xaml_dir = "/mnt/c/RagnaController/src/RagnaController"
locales_dir = "/mnt/c/RagnaController/Locales"

# Alle XAML-Dateien finden
xaml_files = list(Path(xaml_dir).glob("*.xaml"))

# Alle {core:Loc *} Keys sammeln
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

# en.json laden
en_file = Path(locales_dir) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

# Alle Keys aus en.json sammeln (flache Struktur)
def flatten_dict(d, parent_key='', sep='.'):
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.extend(flatten_dict(v, new_key, sep=sep).items())
        else:
            items.append((new_key, v))
    return dict(items)

en_keys_flat = set(flatten_dict(en_data).keys())

# Fehlende Keys finden
missing_keys = all_locale_keys - en_keys_flat
extra_keys = en_keys_flat - all_locale_keys

print("=" * 80)
print("VERGLEICH: XAML {core:Loc *} Keys vs en.json")
print("=" * 80)
print()

if missing_keys:
    print(f"⚠️  FEHLENDE KEYS in en.json ({len(missing_keys)}):")
    print("-" * 80)
    for key in sorted(missing_keys):
        print(f"  ✗ {key}")
else:
    print("✅ Alle {core:Loc *} Keys aus XAML sind in en.json enthalten!")

print()

if extra_keys:
    print(f"ℹ️  Zusätzliche KEYS in en.json (nicht in XAML verwendet): ({len(extra_keys)})")
    print("-" * 80)
    for key in sorted(extra_keys):
        print(f"  ℹ️  {key}")
else:
    print("ℹ️  Keine zusätzlichen Keys in en.json.")

print()
print("=" * 80)
print("ZUSAMMENFASSUNG:")
print("=" * 80)
print(f"  XAML {core:Loc *} Keys: {len(all_locale_keys)}")
print(f"  Keys in en.json: {len(en_keys_flat)}")
print(f"  Fehlende in en.json: {len(missing_keys)}")
print(f"  Extra in en.json: {len(extra_keys)}")
