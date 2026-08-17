#!/usr/bin/env python3
"""
Korrektur: Entferne falsch hinzugefügte flache Keys und füge sie korrekt verschachtelt hinzu
"""

import json
from pathlib import Path

locales_dir = "/mnt/c/RagnaController/Locales"

# Dictionary mit korrekten Übersetzungen für alle Sprachen
translations = {
    "en": {
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
    },
    "de": {
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
    },
    "ar": {
        "Btn.Calibrate": "معايرة العصا",
        "Lbl.Minutes": "دقائق",
        "Tut.1_Title": "البدء",
        "Tut.1_Desc": "مرحباً بك في RagnaController! سيساعدك هذا الدليل على البدء.",
        "Tut.Btn_Next": "التالي",
        "Tut.Btn_Prev": "السابق",
        "Tooltip.DiscordRPC": "ينشط Discord Rich Presence لعرض حالة لعبتك",
        "Tooltip.HapticMetronome": "يوفر رد فعل هابتي إيقاعي أثناء القتال",
        "Tooltip.SmartStandby": "يحفظ ويقف تلقائياً عند الخمول",
        "Tooltip.VoiceAnnouncer": "ينotify عن الأحداث عبر تحويل النص إلى كلام",
        "Settings.DiscordRPC": "Discord Rich Presence",
        "Settings.HapticMetronome": "المترونوم الهابتي",
        "Settings.SmartStandby": "وضع الانتظار الذكي",
        "Settings.StickCalibration": "معايرة العصا",
        "Settings.TutorialBtn": "إظهار الدليل",
        "Settings.VoiceAnnouncer": "المعلن الصوتي",
        "Desc.StickCalibration": "قم بمعايرة عصا التحكم الخاصة بك لرد فعل مثالي."
    },
    # Für alle anderen Sprachen verwenden wir die englische Übersetzung
}

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

# Funktion zum Entfernen von flachen Keys
def remove_flat_keys(data):
    keys_to_remove = []
    for key in data.keys():
        if '.' not in key and key != "":
            # Prüfe ob es ein bekannter Key ist, der verschachtelt sein sollte
            known_flat_keys = [
                "Btn_Calibrate", "Desc_StickCalibration", "Lbl_Minutes",
                "Settings_DiscordRPC", "Settings_HapticMetronome",
                "Settings_SmartStandby", "Settings_StickCalibration",
                "Settings_TutorialBtn", "Settings_VoiceAnnouncer",
                "Tooltip_DiscordRPC", "Tooltip_HapticMetronome",
                "Tooltip_SmartStandby", "Tooltip_VoiceAnnouncer",
                "Tut_1_Desc", "Tut_1_Title", "Tut_Btn_Next", "Tut_Btn_Prev"
            ]
            if key in known_flat_keys:
                keys_to_remove.append(key)
    
    for key in keys_to_remove:
        del data[key]
    
    return data

# Funktion zum Hinzufügen von verschachtelten Keys
def add_nested_keys(data, nested_keys):
    for key, value in nested_keys.items():
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

# Verarbeite alle Sprachdateien
locales_files = list(Path(locales_dir).glob("*.json"))
print("=" * 80)
print("Korrektur: Entferne falsch hinzugefügte flache Keys und füge verschachtelte hinzu")
print("=" * 80)
print()

for locale_file in locales_files:
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        lang = locale_file.stem
        
        # Entferne falsch hinzugefügte flache Keys
        print(f"Processing {lang}.json...")
        data = remove_flat_keys(data)
        
        # Wähle die richtige Übersetzung basierend auf der Sprache
        if lang in translations:
            selected_translations = translations[lang]
        else:
            selected_translations = translations["en"]
        
        # Füge verschachtelte Keys hinzu
        add_nested_keys(data, selected_translations)
        
        # Speichere die Sprachdatei
        with open(locale_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ {lang}.json gespeichert!")
        
    except Exception as e:
        print(f"✗ Error reading {locale_file.name}: {e}")

print()
print("=" * 80)
print("Korrektur abgeschlossen!")
print("=" * 80)
