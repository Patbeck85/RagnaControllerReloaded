#!/usr/bin/env python3
"""
Suche nach {core:Loc *} Pattern in allen XAML-Dateien
"""

import re
from pathlib import Path

xaml_dir = "/mnt/c/RagnaController/src/RagnaController"
locales_dir = "/mnt/c/RagnaController/Locales"

# Alle XAML-Dateien finden
xaml_files = list(Path(xaml_dir).glob("*.xaml"))

print(f"Found {len(xaml_files)} XAML files in {xaml_dir}\n")

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

# Alle Keys alphabetisch sortiert anzeigen
print("Alle gefundenen Locales-Keys:")
print("=" * 80)
for key in sorted(all_locale_keys):
    print(f"  - {key}")
