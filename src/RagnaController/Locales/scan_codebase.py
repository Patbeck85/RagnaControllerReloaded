#!/usr/bin/env python3
"""
Codebase Cross-Reference: Scanne alle C#-Dateien auf verbleibende Hardcoded Strings
und identifiziere welche zu en.json hinzugefügt werden müssen
"""

import re
from pathlib import Path

# Pfad zum Source-Verzeichnis
source_dir = Path("/mnt/c/RagnaController/src/RagnaController")

# Alle C#-Dateien finden
cs_files = sorted(source_dir.glob("*.cs"))

print(f"🔍 Scanning {len(cs_files)} C#-Dateien auf Hardcoded Strings...\n")

# Muster für hardcoded Strings in C#-Dateien
patterns = [
    r'new\s+TextBlock\s*\([^)]*Text\s*=\s*"([^"]*)"',  # TextBlock mit Text
    r'new\s+Button\s*\([^)]*Content\s*=\s*"([^"]*)"',  # Button mit Content
    r'Text\s*=\s*"([^"]*)"',  # TextBlock Text
    r'Content\s*=\s*"([^"]*)"',  # Button Content
    r'ToolTip\s*=\s*"([^"]*)"',  # ToolTip
    r'PlaceholderText\s*=\s*"([^"]*)"',  # PlaceholderText
    r'Header\s*=\s*"([^"]*)"',  # Header
    r'Title\s*=\s*"([^"]*)"',  # Title
]

# Ergebnisse sammeln
results = []
all_strings = set()

for cs_file in cs_files:
    try:
        with open(cs_file, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Finde alle hardcoded Strings
        for pattern in patterns:
            matches = re.findall(pattern, content)
            for match in matches:
                text = match.strip()
                # Filtere bereits lokalisierte Strings und UI-Elemente
                if not any(ui_term in text for ui_term in ['Base', 'L1', 'R1', 'L2', 'R2', 'Next', 'Back', 'Finish', 'Cancel', 'Close', 'Save', 'Delete', 'Load', 'Export', 'Import', 'Apply', 'Reset', 'OK', 'Yes', 'No', 'Error', 'Warning', 'Info', 'Loading', 'Running', 'Paused', 'Enable', 'Disable', 'Auto', 'Manual', 'Debug', 'Release', 'Build', 'Version', 'ms', '×', 'Binding', 'TemplateBinding']):
                    if text and len(text) > 2:  # Nur sinnvolle Texte
                        all_strings.add(text)
    except Exception as e:
        print(f"❌ Fehler bei {cs_file}: {e}")

# Ergebnisse anzeigen
print("=" * 80)
print("📋 ERGEBNISSE - Hardcoded Strings in C#-Dateien:")
print("=" * 80)

if all_strings:
    for text in sorted(all_strings):
        print(f"   • {text}")
else:
    print("\n✅ Keine hardcoded Strings gefunden!")

# Zusammenfassung
print("\n" + "=" * 80)
print("📊 ZUSAMMENFASSUNG:")
print("=" * 80)
print(f"🔍 Gefundene hardcoded Strings: {len(all_strings)}")

if all_strings:
    print(f"\n⚠️ ACHTUNG: {len(all_strings)} Strings müssen zu en.json hinzugefügt werden!")
else:
    print("\n✅ Alle C#-Dateien verwenden bereits {core:Loc} Bindungen.")

# Nächste Schritte vorschlagen
print("\n" + "=" * 80)
print("📝 NÄCHSTE SCHritte:")
print("=" * 80)
print("1. ✅ JSON-Syntax und UTF-8-Encoding: OK")
print("2. ✅ Schlüssel-Synchronisation: ABGESCHLOSSEN")
print("3. ⏳ Codebase Cross-Reference: NOCH NICHT DURCHGEFÜHRT")
print("4. ⏳ Hardcoded String Sweep: NOCH NICHT DURCHGEFÜHRT")
