#!/usr/bin/env python3
"""
Fügt fehlende Locale-Keys allen Sprachdateien hinzu
"""

import json
from pathlib import Path

locales_dir = "/mnt/c/RagnaController/Locales"

# Fehlende Keys mit deutschen Übersetzungen
missing_keys_de = {
    "Btn": {
        "Calibrate": "Stick kalibrieren"
    },
    "Lbl": {
        "Minutes": "Minuten"
    },
    "Tut": {
        "1_Title": "Erste Schritte",
        "1_Desc": "Willkommen bei RagnaController! Dieser Tutorial hilft Ihnen beim Start.",
        "Btn_Next": "Weiter",
        "Btn_Prev": "Zurück"
    },
    "Tooltip": {
        "DiscordRPC": "Aktiviert Discord Rich Presence zum Anzeigen Ihres Spielstatus",
        "HapticMetronome": "Bietet rhythmischen haptischen Feedback während des Kampfes",
        "SmartStandby": "Speichert und pausiert automatisch bei Inaktivität",
        "VoiceAnnouncer": "Gibt Ereignisse über Text-zu-Sprache bekannt"
    },
    "Settings": {
        "DiscordRPC": "Discord Rich Presence",
        "HapticMetronome": "Haptischer Metronom",
        "SmartStandby": "Intelligenter Standby",
        "StickCalibration": "Stick-Kalibrierung",
        "TutorialBtn": "Tutorial anzeigen",
        "VoiceAnnouncer": "Sprach-Ankündiger"
    }
}

# Fehlende Keys mit englischen Übersetzungen (für en.json)
missing_keys_en = {
    "Btn": {
        "Calibrate": "Calibrate Stick"
    },
    "Lbl": {
        "Minutes": "Minutes"
    },
    "Tut": {
        "1_Title": "Getting Started",
        "1_Desc": "Welcome to RagnaController! This tutorial will help you get started.",
        "Btn_Next": "Next",
        "Btn_Prev": "Previous"
    },
    "Tooltip": {
        "DiscordRPC": "Enables Discord Rich Presence to show your game status",
        "HapticMetronome": "Provides rhythmic haptic feedback during combat",
        "SmartStandby": "Automatically saves and suspends when idle",
        "VoiceAnnouncer": "Announces events via text-to-speech"
    },
    "Settings": {
        "DiscordRPC": "Discord Rich Presence",
        "HapticMetronome": "Haptic Metronome",
        "SmartStandby": "Smart Standby",
        "StickCalibration": "Stick Calibration",
        "TutorialBtn": "Show Tutorial",
        "VoiceAnnouncer": "Voice Announcer"
    }
}

# Fehlende Keys mit arabischen Übersetzungen
missing_keys_ar = {
    "Btn": {
        "Calibrate": "معايرة العصا"
    },
    "Lbl": {
        "Minutes": "دقائق"
    },
    "Tut": {
        "1_Title": "البدء",
        "1_Desc": "مرحباً بك في RagnaController! سيساعدك هذا الدليل على البدء.",
        "Btn_Next": "التالي",
        "Btn_Prev": "السابق"
    },
    "Tooltip": {
        "DiscordRPC": "تفعيل Discord Rich Presence لعرض حالة اللعبة",
        "HapticMetronome": "يوفر تغذية راجعة هaptic إيقاعي أثناء القتال",
        "SmartStandby": "يحفظ ويقيد تلقائياً عند الخمول",
        "VoiceAnnouncer": "ينشر الأحداث عبر تحويل النص إلى كلام"
    },
    "Settings": {
        "DiscordRPC": "Discord Rich Presence",
        "HapticMetronome": "المترونوم اللمسي",
        "SmartStandby": "وضع الانتظار الذكي",
        "StickCalibration": "معايرة العصا",
        "TutorialBtn": "إظهار الدليل",
        "VoiceAnnouncer": "المعلن الصوتي"
    }
}

# Fehlende Keys mit weiteren Sprachübersetzungen (vereinfacht)
missing_keys_other = {
    "Btn": {
        "Calibrate": "Calibrate Stick"
    },
    "Lbl": {
        "Minutes": "Minutes"
    },
    "Tut": {
        "1_Title": "Getting Started",
        "1_Desc": "Welcome to RagnaController! This tutorial will help you get started.",
        "Btn_Next": "Next",
        "Btn_Prev": "Previous"
    },
    "Tooltip": {
        "DiscordRPC": "Enables Discord Rich Presence to show your game status",
        "HapticMetronome": "Provides rhythmic haptic feedback during combat",
        "SmartStandby": "Automatically saves and suspends when idle",
        "VoiceAnnouncer": "Announces events via text-to-speech"
    },
    "Settings": {
        "DiscordRPC": "Discord Rich Presence",
        "HapticMetronome": "Haptic Metronome",
        "SmartStandby": "Smart Standby",
        "StickCalibration": "Stick Calibration",
        "TutorialBtn": "Show Tutorial",
        "VoiceAnnouncer": "Voice Announcer"
    }
}

# Alle Sprachdateien finden
locales_files = list(Path(locales_dir).glob("*.json"))

print(f"Found {len(locales_files)} locale files")
print()

for locale_file in locales_files:
    try:
        with open(locale_file, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # Bestimme die Sprache basierend auf dem Dateinamen
        lang = locale_file.stem
        
        # Wähle die richtigen Übersetzungen
        if lang == "en":
            missing_keys = missing_keys_en
        elif lang == "de":
            missing_keys = missing_keys_de
        elif lang == "ar":
            missing_keys = missing_keys_ar
        else:
            missing_keys = missing_keys_other
        
        # Füge die fehlenden Keys hinzu
        for category, keys in missing_keys.items():
            if category not in data:
                data[category] = {}
            data[category].update(keys)
        
        # Speichere die aktualisierte Datei
        with open(locale_file, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Updated {locale_file.name}")
        
    except Exception as e:
        print(f"✗ Error updating {locale_file.name}: {e}")

print()
print("All locale files updated!")
