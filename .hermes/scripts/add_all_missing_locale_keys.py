#!/usr/bin/env python3
"""
Hinzufügen aller fehlenden {core:Loc *} Keys zu allen Sprachdateien
"""

import json
import re
from pathlib import Path

xaml_dir = "/mnt/c/RagnaController/src/RagnaController"
locales_dir = "/mnt/c/RagnaController/Locales"

# 1. Alle XAML-Dateien finden und {core:Loc *} Keys sammeln
print("=" * 80)
print("SCHRITT 1: Extrahiere {core:Loc *} Keys aus allen XAML-Dateien")
print("=" * 80)
print()

xaml_files = list(Path(xaml_dir).glob("*.xaml"))
print(f"Found {len(xaml_files)} XAML files")
print()

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

# 2. en.json laden und ihre Keys sammeln
print("=" * 80)
print("SCHRITT 2: Lade en.json und sammle ihre Keys")
print("=" * 80)
print()

en_file = Path(locales_dir) / "en.json"
with open(en_file, 'r', encoding='utf-8') as f:
    en_data = json.load(f)

# Funktion zum Flachen von nested dicts
def flatten_dict(d, parent_key='', sep='.'):
    items = []
    for k, v in d.items():
        new_key = f"{parent_key}{sep}{k}" if parent_key else k
        if isinstance(v, dict):
            items.extend(flatten_dict(v, new_key, sep=sep).items())
        else:
            items.append((new_key, v))
    return dict(items)

en_keys_flat = set(flatten_dict(en_data).keys())
print(f"Keys in en.json: {len(en_keys_flat)}")
print()

# 3. Finde fehlende Keys in en.json
print("=" * 80)
print("SCHRITT 3: Finde fehlende Keys in en.json")
print("=" * 80)
print()

missing_keys = all_locale_keys - en_keys_flat
extra_keys = en_keys_flat - all_locale_keys

print(f"Keys in XAML: {len(all_locale_keys)}")
print(f"Keys in en.json: {len(en_keys_flat)}")
print(f"Fehlende in en.json: {len(missing_keys)}")
print(f"Extra in en.json: {len(extra_keys)}")
print()

if missing_keys:
    print("FEHLENDE KEYS in en.json:")
    print("-" * 80)
    for key in sorted(missing_keys):
        print(f"  ✗ {key}")
else:
    print("✅ Alle {core:Loc *} Keys aus XAML sind in en.json enthalten!")

print()

# 4. Hinzufügen aller fehlenden Keys zu en.json
print("=" * 80)
print("SCHRITT 4: Hinzufügen aller fehlenden Keys zu en.json")
print("=" * 80)
print()

if missing_keys:
    # Dictionary mit Übersetzungen für alle fehlenden Keys
    translations = {
        "Btn.Calibrate": "Calibrate Stick",
        "Lbl.Minutes": "Minutes",
        "Tut.1_Title": "Getting Started",
        "Tut.1_Desc": "Welcome to RagnaController! This tutorial will help you get started.",
        "Tut.Btn_Next": "Next",
        "Tut.Btn_Prev": "Previous",
        "Tooltip.DiscordRPC": "Enables Discord Rich Presence to show your game status",
        "Tooltip.HapticMetronome": "Provides rhythmic haptic feedback during combat",
        "Tooltip.SmartStandby": "Automatically saves and suspends when idle",
        "Tooltip.VoiceAnnouncer": "Announces events via text-to-speech",
        "Settings.DiscordRPC": "Discord Rich Presence",
        "Settings.HapticMetronome": "Haptic Metronome",
        "Settings.SmartStandby": "Smart Standby",
        "Settings.StickCalibration": "Stick Calibration",
        "Settings.TutorialBtn": "Show Tutorial",
        "Settings.VoiceAnnouncer": "Voice Announcer",
        "Desc.StickCalibration": "Calibrate your controller stick for optimal responsiveness."
    }
    
    # Hinzufügen der fehlenden Keys zu en.json
    for key in sorted(missing_keys):
        if key not in en_keys_flat:
            value = translations.get(key, "")
            
            # Hinzufügen des Keys zu en.json
            parts = key.split('.')
            current = en_data
            
            # Navigiere zur richtigen Kategorie
            for i in range(len(parts) - 1):
                part = parts[i]
                if part not in current:
                    current[part] = {}
                current = current[part]
            
            # Füge den Key hinzu
            last_part = parts[-1]
            if last_part not in current:
                current[last_part] = value
            
            print(f"✓ Added {key} = '{value}'")

    # Speichere en.json
    with open(en_file, 'w', encoding='utf-8') as f:
        json.dump(en_data, f, ensure_ascii=False, indent=2)
    
    print()
    print("en.json gespeichert!")
else:
    print("✅ Keine fehlenden Keys in en.json!")

print()

# 5. Hinzufügen aller fehlenden Keys zu allen anderen Sprachdateien
print("=" * 80)
print("SCHRITT 5: Hinzufügen aller fehlenden Keys zu allen anderen Sprachdateien")
print("=" * 80)
print()

locales_files = list(Path(locales_dir).glob("*.json"))
for locale_file in locales_files:
    if locale_file.name == "en.json":
        continue
    
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        flat_keys = flatten_dict(data)
        missing = all_locale_keys - set(flat_keys.keys())
        
        if missing:
            lang = locale_file.stem
            print(f"Processing {lang}.json - {len(missing)} missing keys")
            
            # Dictionary mit Übersetzungen für alle Sprachen (DE als Beispiel)
            de_translations = {
                "Btn.Calibrate": "Stick kalibrieren",
                "Lbl.Minutes": "Minuten",
                "Tut.1_Title": "Erste Schritte",
                "Tut.1_Desc": "Willkommen bei RagnaController! Dieser Tutorial hilft Ihnen beim Start.",
                "Tut.Btn_Next": "Weiter",
                "Tut.Btn_Prev": "Zurück",
                "Tooltip.DiscordRPC": "Aktiviert Discord Rich Presence zum Anzeigen Ihres Spielstatus",
                "Tooltip.HapticMetronome": "Bietet rhythmische haptische Rückmeldung während des Kampfes",
                "Tooltip.SmartStandby": "Speichert und pausiert automatisch bei Inaktivität",
                "Tooltip.VoiceAnnouncer": "Gibt Ereignisse über Text-zu-Sprache bekannt",
                "Settings.DiscordRPC": "Discord Rich Presence",
                "Settings.HapticMetronome": "Haptischer Metronom",
                "Settings.SmartStandby": "Intelligenter Standby",
                "Settings.StickCalibration": "Stick-Kalibrierung",
                "Settings.TutorialBtn": "Tutorial anzeigen",
                "Settings.VoiceAnnouncer": "Sprach-Ankündiger",
                "Desc.StickCalibration": "Kalibrieren Sie Ihren Controller-Stick für optimale Reaktionsfähigkeit."
            }
            
            for key in sorted(missing):
                # Bestimme die Übersetzung basierend auf der Sprache
                if lang == "de":
                    value = de_translations.get(key, "")
                else:
                    # Für andere Sprachen verwenden wir die englische Übersetzung
                    value = translations.get(key, "")
                
                # Hinzufügen des Keys basierend auf der Sprache
                parts = key.split('.')
                current = data
                
                # Navigiere zur richtigen Kategorie
                for i in range(len(parts) - 1):
                    part = parts[i]
                    if part not in current:
                        current[part] = {}
                    current = current[part]
                
                # Füge den Key hinzu
                last_part = parts[-1]
                if last_part not in current:
                    current[last_part] = value
                
                print(f"  ✓ Added {key} = '{value}'")
            
            # Speichere die Sprachdatei
            with open(locale_file, 'w', encoding='utf-8') as f:
                json.dump(data, f, ensure_ascii=False, indent=2)
            
            print(f"✓ {lang}.json gespeichert!")
        else:
            print(f"✅ {lang}.json - Alle Keys vorhanden!")
            
    except Exception as e:
        print(f"✗ Error reading {locale_file.name}: {e}")

print()
print("=" * 80)
print("ZUSAMMENFASSUNG:")
print("=" * 80)
print(f"  XAML {{core:Loc *}} Keys: {len(all_locale_keys)}")
print(f"  en.json Keys: {len(en_keys_flat)}")
print(f"  Fehlende in en.json: {len(missing_keys)}")
print(f"  Sprachdateien mit fehlenden Keys: {len([f for f in locales_files if f.name != 'en.json'])}")
