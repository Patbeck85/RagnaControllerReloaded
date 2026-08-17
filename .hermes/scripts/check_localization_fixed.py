#!/usr/bin/env python3
"""
Übersetzungs-Prüfer für RagnaController Lokalisierung - KORREKTURIERT
Vergleicht alle Sprachdateien mit en.json und identifiziert fehlende Übersetzungen (inklusive verschachtelter Keys)
"""

import json
from pathlib import Path

# Pfad zu den Locales
locales_path = "/mnt/c/RagnaController/Locales"

# Alle JSON-Dateien finden
json_files = sorted(Path(locales_path).glob("*.json"))

print(f"Found {len(json_files)} JSON files in {locales_path}\n")

# en.json als Referenz laden
en_file = Path(locales_path) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

# Alle verschachtelten Keys aus en.json sammeln (flache Struktur)
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
print(f"Total unique keys in en.json: {len(en_keys_flat)}")
print()

# Alle anderen Sprachdateien analysieren
missing_translations = []
translation_coverage = {}

for lang_file in json_files:
    if lang_file.name == "en.json":
        continue
    
    try:
        with open(lang_file, 'r', encoding='utf-8') as f:
            lang_data = json.load(f)
        
        # Flache Struktur der Sprachdatei erstellen
        lang_keys_flat = set(flatten_dict(lang_data).keys())
        
        # Fehlende Keys finden
        missing_in_lang = en_keys_flat - lang_keys_flat
        
        coverage = (len(lang_keys_flat) / len(en_keys_flat) * 100) if en_keys_flat else 0
        translation_coverage[lang_file.name] = {
            'total_keys': len(en_keys_flat),
            'found_keys': len(lang_keys_flat),
            'missing_keys': len(missing_in_lang),
            'coverage_percent': round(coverage, 1)
        }
        
        if missing_in_lang:
            missing_translations.append({
                'file': lang_file.name,
                'missing': sorted(missing_in_lang)
            })
            
    except Exception as e:
        print(f"Error reading {lang_file.name}: {e}")

# Ergebnisse ausgeben
print("=" * 80)
print("Übersetzungsstatus aller Sprachdateien (korrekt)")
print("=" * 80)
print()

# Coverage nach Prozent sortiert
sorted_coverage = sorted(translation_coverage.items(), key=lambda x: x[1]['coverage_percent'], reverse=True)

for lang, stats in sorted_coverage:
    status = "✓" if stats['missing_keys'] == 0 else "✗"
    print(f"{status} {lang:3s}: {stats['found_keys']:3d}/{stats['total_keys']:3d}] ({stats['coverage_percent']:5.1f}%) - {stats['missing_keys']} fehlende Übersetzungen")

print()
print("=" * 80)
print("Sprachen mit fehlenden Übersetzungen:")
print("=" * 80)

if missing_translations:
    for item in missing_translations:
        lang = item['file']
        missing = item['missing']
        
        print(f"\n{lang}:")
        # Gruppieren nach Kategorie
        categories = {}
        for key in missing:
            # Extrahiere die Kategorie (z.B. "Application.Name" -> "Application")
            category = key.split('.')[0]
            if category not in categories:
                categories[category] = []
            categories[category].append(key)
        
        for category, keys in sorted(categories.items()):
            print(f"  {category}:")
            for key in sorted(keys):
                print(f"    - {key}")
else:
    print("Alle Sprachen sind vollständig übersetzt! ✓")
