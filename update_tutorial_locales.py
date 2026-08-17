#!/usr/bin/env python3
"""
Batch-Übersetzungsskript: Fügt 12 neue Tutorial-Lokalisierungskeys in alle Sprachdateien hinzu.
Die neuen Keys werden mit dem englischen Originaltext als Platzhalter eingefügt.
"""

import json
import os
import glob

# Die 12 neuen Tutorial-Lokalisierungskeys
NEW_KEYS = {
    "Settings_TutorialBtn": "View Interactive Tutorials",
    "Tut_Btn_Next": "NEXT →",
    "Tut_Btn_Prev": "← BACK",
    "Tut_Btn_Finish": "LET'S PLAY",
    "Tut_1_Title": "Welcome to RagnaController",
    "Tut_1_Desc": "Before you start, make sure to select your Ragnarok Online executable in the settings. The 'Focus Lock' feature ensures your controller only works when the game window is active, protecting you from accidental clicks in Windows.",
    "Tut_2_Title": "Smart Grid (Inventory Mode)",
    "Tut_2_Desc": "Tired of missing tiny items? When you open your inventory, use the D-Pad to snap the cursor perfectly from slot to slot. Press 'A' to double-click and equip items instantly.",
    "Tut_3_Title": "Release-to-Cast (AoE Magic)",
    "Tut_3_Desc": "Casting Storm Gust has never been easier. Hold down your skill button (e.g., L1+A). The targeting circle appears. Aim with the right stick. Simply RELEASE the button to cast the spell!",
    "Tut_4_Title": "Smart Aim Assist",
    "Tut_4_Desc": "Having trouble clicking on a fast-moving Poring? Just tilt the Right Stick slightly towards the monster and click R3. The engine will fire a micro-spiral click, guaranteeing a hit on the hitbox.",
    "Tut_5_Title": "Visual Macro Editor",
    "Tut_5_Desc": "Need complex combos? Record your key presses, then open the Visual Timeline Editor. You can drag and drop the delays perfectly to match the server's lag and your character's ASPD."
}

# Pfad zum Locales-Verzeichnis
LOCALES_DIR = "/mnt/c/RagnaController/src/RagnaController/Locales"

# Alle .json-Dateien außer en.json finden
json_files = glob.glob(os.path.join(LOCALES_DIR, "*.json"))
json_files.remove(os.path.join(LOCALES_DIR, "en.json"))

print(f"📁 Gefunden: {len(json_files)} Sprachdateien (außer en.json)")
print(f"🔑 Neue Keys zu übernehmen: {len(NEW_KEYS)}")
print()

for json_file in json_files:
    filename = os.path.basename(json_file)
    print(f"📝 Verarbeite: {filename}...", end=" ")
    
    try:
        # Datei laden
        with open(json_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # Neue Keys hinzufügen (mit englischen Originaltext als Platzhalter)
        for key, value in NEW_KEYS.items():
            if key not in data:
                data[key] = value  # Englischer Text als Platzhalter
        
        # Datei speichern
        with open(json_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✅ Hinzugefügt")
        
    except Exception as e:
        print(f"❌ Fehler: {e}")

print()
print("🎉 Fertig! Alle Sprachdateien wurden aktualisiert.")
print("💡 Hinweis: Die neuen Keys enthalten den englischen Originaltext als Platzhalter.")
print("   Übersetze diese Texte später in die jeweilige Sprache!")
