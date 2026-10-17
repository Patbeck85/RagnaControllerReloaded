#!/usr/bin/env python3
import json
import os
import sys

locales_dir = "/mnt/c/RagnaController/Locales"
errors_found = []

for filename in sorted(os.listdir(locales_dir)):
    if filename.endswith('.json'):
        filepath = os.path.join(locales_dir, filename)
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                json.load(f)
        except json.JSONDecodeError as e:
            errors_found.append(f"{filename}: {e.msg} (Zeile {e.lineno})")
        except Exception as e:
            errors_found.append(f"{filename}: {str(e)}")

if errors_found:
    print("JSON-Fehler gefunden:")
    for error in errors_found:
        print(error)
else:
    print("Alle JSON-Dateien sind gültig!")
