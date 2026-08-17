#!/usr/bin/env python3
import json
import os
import sys

def validate_json_file(filepath):
    """Prüft eine JSON-Datei auf Syntaxfehler"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            json.loads(content)
        return True, None
    except json.JSONDecodeError as e:
        return False, str(e)

def main():
    # Prüfe alle JSON-Dateien im Locales Verzeichnis
    locales_dir = '/mnt/c/RagnaController/Locales'
    
    if not os.path.exists(locales_dir):
        print(f"Verzeichnis nicht gefunden: {locales_dir}")
        return 1
    
    json_files = [f for f in os.listdir(locales_dir) if f.endswith('.json')]
    
    errors = []
    for filename in sorted(json_files):
        filepath = os.path.join(locales_dir, filename)
        valid, error = validate_json_file(filepath)
        if not valid:
            errors.append(f"{filepath}: {error}")
            print(f"FEHLER: {filepath}")
            print(f"  {error}")
    
    if errors:
        print(f"\n{len(errors)} Fehler gefunden!")
        return 1
    else:
        print(f"Alle {len(json_files)} JSON-Dateien im Locales Verzeichnis sind gültig!")
        return 0

if __name__ == '__main__':
    sys.exit(main())
