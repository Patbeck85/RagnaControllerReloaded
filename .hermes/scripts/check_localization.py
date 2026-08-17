#!/usr/bin/env python3
"""
Übersetzungs-Prüfer für RagnaController Lokalisierung
Vergleicht alle Sprachdateien mit en.json und identifiziert fehlende Übersetzungen
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

# Alle anderen Sprachdateien analysieren
missing_translations = []
translation_coverage = {}

for lang_file in json_files:
    if lang_file.name == "en.json":
        continue
    
    try:
        with open(lang_file, 'r', encoding='utf-8') as f:
            lang_data = json.load(f)
        
        # Alle Keys aus en.json sammeln
        en_keys = set()
        for category in en_data.values():
            if isinstance(category, dict):
                en_keys.update(category.keys())
        
        # Für jede Sprache prüfen
        missing_in_lang = []
        found_keys = 0
        
        for key in sorted(en_keys):
            if key not in lang_data:
                missing_in_lang.append(key)
            else:
                found_keys += 1
        
        coverage = (found_keys / len(en_keys) * 100) if en_keys else 0
        translation_coverage[lang_file.name] = {
            'total_keys': len(en_keys),
            'found_keys': found_keys,
            'missing_keys': len(missing_in_lang),
            'coverage_percent': round(coverage, 1)
        }
        
        if missing_in_lang:
            missing_translations.append({
                'file': lang_file.name,
                'missing': missing_in_lang[:20]  # Max 20 anzeigen
            })
            
    except Exception as e:
        print(f"Error reading {lang_file.name}: {e}")

# Ergebnisse ausgeben
print("=" * 80)
print("Übersetzungsstatus aller Sprachdateien")
print("=" * 80)
print()

# Coverage nach Prozent sortiert
sorted_coverage = sorted(translation_coverage.items(), key=lambda x: x[1]['coverage_percent'], reverse=True)

for lang, stats in sorted_coverage:
    status = "✓" if stats['missing_keys'] == 0 else "✗"
    print(f"{status} {lang:3s}: {stats['found_keys']:2d}/{stats['total_keys']:2d}] ({stats['coverage_percent']:5.1f}%) - {stats['missing_keys']} fehlende Übersetzungen")

print()
print("=" * 80)
print("Sprachen mit fehlenden Übersetzungen:")
print("=" * 80)

if missing_translations:
    for item in missing_translations:
        print(f"\n{item['file']}:")
        for key in item['missing']:
            print(f"  - {key}")
else:
    print("Alle Sprachen sind vollständig übersetzt! ✓")
