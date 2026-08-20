#!/usr/bin/env python3
"""
Scan XAML files for hardcoded strings that should be localized
"""

import re
from pathlib import Path

# Pfad zum Source-Verzeichnis
source_dir = Path("/mnt/c/RagnaController/src/RagnaController")

# Alle XAML-Dateien finden
xaml_files = sorted(source_dir.glob("*.xaml"))

print(f"🔍 Scanning {len(xaml_files)} XAML-Dateien für hardcoded Strings...\n")

# Muster für hardcoded Strings (nicht mit {core:Loc})
patterns = [
    r'Text="[^"]*"',  # TextBlock mit String
    r'Content="[^"]*"',  # Button mit Content
    r'ToolTip="[^"]*"',  # ToolTip
    r'Placeholder="[^"]*"',  # PlaceholderText
    r'Header="[^"]*"',  # Header
]

results = []

for xaml_file in xaml_files:
    try:
        with open(xaml_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Finde alle hardcoded Strings (nicht mit {core:Loc})
        hardcoded = []
        for pattern in patterns:
            matches = re.findall(pattern, content)
            for match in matches:
                # Filtere bereits lokalisierte Strings
                if '{core:Loc' not in match and 'StaticResource' not in match:
                    # Extrahiere den Text
                    text = match.strip('"').strip()
                    # Ignoriere UI-Elemente wie "Base", "L1", "R1", etc.
                    if not any(ui_term in text for ui_term in ['Base', 'L1', 'R1', 'L2', 'R2', 'Next', 'Back', 'Finish', 'Cancel', 'Close', 'Save', 'Delete', 'Load', 'Export', 'Import', 'Apply', 'Reset', 'OK', 'Yes', 'No', 'Error', 'Warning', 'Info', 'Loading', 'Running', 'Paused', 'Enable', 'Disable', 'Auto', 'Manual', 'Debug', 'Release', 'Build', 'Version', 'ms', 'ms', '×']):
                        if text and len(text) > 2:  # Nur sinnvolle Texte
                            hardcoded.append(text)
        
        if hardcoded:
            results.append({
                'file': xaml_file.name,
                'hardcoded': hardcoded[:20]  # Max 20 pro Datei
            })
    except Exception as e:
        print(f"❌ Fehler bei {xaml_file}: {e}")

# Ergebnisse anzeigen
print("=" * 80)
print("📋 ERGEBNISSE - Hardcoded Strings in XAML-Dateien:")
print("=" * 80)

for r in results:
    print(f"\n📄 {r['file']}:")
    for text in r['hardcoded']:
        print(f"   • {text}")

if not results:
    print("\n✅ Keine hardcoded Strings gefunden! Alle XAML-Dateien sind bereits lokalisiert.")

# Zusammenfassung
print("\n" + "=" * 80)
print("📊 ZUSAMMENFASSUNG:")
print("=" * 80)

all_strings = []
for r in results:
    all_strings.extend(r['hardcoded'])

print(f"🔍 Gefundene hardcoded Strings: {len(all_strings)}")
if all_strings:
    print(f"\n⚠️ ACHTUNG: {len(all_strings)} Strings müssen zu en.json hinzugefügt werden!")
else:
    print("\n✅ Alle XAML-Dateien verwenden bereits {core:Loc} Bindungen.")
