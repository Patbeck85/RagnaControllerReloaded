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
    # Prüfe alle JSON-Dateien im Projekt (außer Locales und .hermes)
    project_root = '/mnt/c/RagnaController'
    
    # Dateien zu prüfen
    files_to_check = [
        '/mnt/c/RagnaController/src/RagnaController/BugReport_NullReference.json',
        '/mnt/c/RagnaController/src/RagnaController/bug_audit_report.json',
    ]
    
    # DefaultProfiles JSON-Dateien
    default_profiles_dir = '/mnt/c/RagnaController/src/RagnaController/DefaultProfiles'
    if os.path.exists(default_profiles_dir):
        for filename in sorted(os.listdir(default_profiles_dir)):
            if filename.endswith('.json'):
                files_to_check.append(os.path.join(default_profiles_dir, filename))
    
    errors = []
    for filepath in sorted(files_to_check):
        if os.path.exists(filepath):
            valid, error = validate_json_file(filepath)
            if not valid:
                errors.append(f"{filepath}: {error}")
                print(f"FEHLER: {filepath}")
                print(f"  {error}")
    
    if errors:
        print(f"\n{len(errors)} Fehler gefunden!")
        return 1
    else:
        print("Alle geprüften JSON-Dateien sind gültig!")
        return 0

if __name__ == '__main__':
    sys.exit(main())
