#!/usr/bin/env python3
"""
Key-Analyse für alle Lokalisierungsdateien
"""

import json
from pathlib import Path

locales_path = "/mnt/c/RagnaController/Locales"

# en.json laden
en_file = Path(locales_path) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

# Alle verschachtelten Keys zählen
def count_keys(d):
    count = 0
    for k, v in d.items():
        if isinstance(v, dict):
            count += 1 + count_keys(v)
        else:
            count += 1
    return count

en_key_count = count_keys(en_data)
print(f"Total keys in en.json: {en_key_count}")

# Alle Sprachdateien prüfen
json_files = list(Path(locales_path).glob("*.json"))
for lang_file in json_files:
    if lang_file.name == "en.json":
        continue
    with open(lang_file, 'r', encoding='utf-8') as f:
        lang_data = json.load(f)
    lang_key_count = count_keys(lang_data)
    print(f"{lang_file.name}: {lang_key_count} keys")
