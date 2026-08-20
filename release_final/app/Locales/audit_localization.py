#!/usr/bin/env python3
"""
Lokalisierungs-Integritätsprüfung für RagnaController
Prüft JSON-Syntax, UTF-8-Encoding und Schlüssel-Synchronisation
"""

import json
import os
import sys
from pathlib import Path

# Pfad zum Locales-Verzeichnis
locales_dir = Path("/mnt/c/RagnaController/src/RagnaController/Locales")

# Alle JSON-Dateien finden (ohne update_ und validate_ Dateien)
json_files = sorted([f for f in locales_dir.glob("*.json") if not f.name.startswith("update_") and not f.name.startswith("validate_")])

print(f"📊 Lokalisierungs-Integritätsprüfung\n")
print("=" * 80)
print(f"Gefundene JSON-Dateien: {len(json_files)}\n")

# en.json als Referenz laden
en_path = json_files[6]  # en.json ist der 7. Eintrag (Index 6)
with open(en_path, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

en_keys = set(en_data.keys())
print(f"✅ en.json (Referenz): {len(en_keys)} Schlüssel")

# Alle anderen Dateien prüfen
results = []
for i, file_path in enumerate(json_files):
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        keys = set(data.keys())
        key_count = len(keys)
        
        # Vergleich mit en.json
        missing = en_keys - keys
        extra = keys - en_keys
        
        if missing:
            status = f"❌ FEHLER: {len(missing)} fehlende Schlüssel, {len(extra)} überschüssige"
        elif extra:
            status = f"⚠️ WARNUNG: {len(extra)} überschüssige Schlüssel"
        else:
            status = "✅ OK: Synchronisiert"
        
        results.append({
            'file': file_path.name,
            'keys': key_count,
            'missing': len(missing),
            'extra': len(extra),
            'status': status
        })
    except json.JSONDecodeError as e:
        results.append({
            'file': file_path.name,
            'keys': 0,
            'missing': 'JSON-Fehler',
            'extra': None,
            'status': f'❌ JSON-Fehler: {str(e)[:50]}'
        })
    except Exception as e:
        results.append({
            'file': file_path.name,
            'keys': 0,
            'missing': 'Fehler',
            'extra': None,
            'status': f'❌ Fehler: {str(e)[:50]}'
        })

# Ergebnisse anzeigen
print("\n" + "=" * 80)
print("📋 ERGEBNISSE:")
print("=" * 80)

for r in results:
    status_icon = "✅" if "OK" in r['status'] else "❌" if "FEHLER" in r['status'] or "Fehler" in r['status'] else "⚠️"
    print(f"{status_icon} {r['file']:25} | {r['keys']:4} Schlüssel | {r['status']}")

# Zusammenfassung
print("\n" + "=" * 80)
print("📊 ZUSAMMENFASSUNG:")
print("=" * 80)

ok_count = sum(1 for r in results if "OK" in r['status'])
error_count = sum(1 for r in results if "FEHLER" in r['status'] or "Fehler" in r['status'])
warning_count = sum(1 for r in results if "WARNUNG" in r['status'])

print(f"✅ Synchronisiert: {ok_count}/{len(results)}")
print(f"❌ Fehler (fehlende Schlüssel): {error_count}/{len(results)}")
print(f"⚠️ Warnung (überschüssige Schlüssel): {warning_count}/{len(results)}")

if error_count > 0:
    print(f"\n⚠️ ACHTUNG: {error_count} Dateien haben fehlende Schlüssel und müssen aktualisiert werden!")
    
# Dateien mit Fehlern auflisten
print("\n" + "=" * 80)
print("🔍 DETAILLE FÜR FEHLERHAFTEN DATEIEN:")
print("=" * 80)

for r in results:
    if "FEHLER" in r['status'] or "Fehler" in r['status']:
        print(f"\n❌ {r['file']}:")
        print(f"   Schlüssel-Anzahl: {r['keys']} (erwartet: {len(en_keys)})")
        if isinstance(r['missing'], int):
            missing_list = sorted([en_keys - set(json.loads(open(fp, 'r', encoding='utf-8').read()).keys()) for fp in [locales_dir / r['file']]])
            print(f"   Fehlende Schlüssel: {len(missing_list)}")
