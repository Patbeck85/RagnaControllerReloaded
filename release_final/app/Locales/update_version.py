import json
import os
from pathlib import Path

# Pfad zum Locales-Ordner
locales_dir = Path("/mnt/c/RagnaController/src/RagnaController/Locales")

# Alle .json-Dateien im Locales-Ordner finden
json_files = list(locales_dir.glob("*.json"))

print(f"Found {len(json_files)} JSON files to update")

# Version aktualisieren: v1.6.2 -> v2.0 (bereits durchgeführt)
version_old = "v1.6.2"
version_new = "v2.0"

updated_count = 0
for json_file in json_files:
    try:
        # Datei lesen
        with open(json_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Version ersetzen
        if version_old in content:
            new_content = content.replace(version_old, version_new)
            
            # Datei schreiben
            with open(json_file, 'w', encoding='utf-8') as f:
                f.write(new_content)
            
            updated_count += 1
            print(f"✓ Updated {json_file.name}: {version_old} -> {version_new}")
        else:
            print(f"⚠ {json_file.name}: No version string found")
            
    except Exception as e:
        print(f"✗ Error processing {json_file.name}: {e}")

print(f"\nTotal updated: {updated_count}/{len(json_files)} files")
