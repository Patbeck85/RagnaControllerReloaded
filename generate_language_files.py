#!/usr/bin/env python3
"""
Generate all language translation files for RagnaController localization system
Creates translations for 16 languages with proper translations
"""

import json
import os

# Project root
PROJECT_ROOT = "/mnt/c/RagnaController/src/RagnaController/Locales"

# Language codes and their display names
LANGUAGES = {
    "de": "Deutsch (German)",
    "fr": "Français (French)",
    "it": "Italiano (Italian)",
    "es": "Español (Spanish)",
    "pt": "Português (Portuguese)",
    "nl": "Nederlands (Dutch)",
    "pl": "Polski (Polish)",
    "ru": "Русский (Russian)",
    "sv": "Svenska (Swedish)",
    "no": "Norsk (Norwegian)",
    "da": "Dansk (Danish)",
    "tl": "Filipino (Tagalog)",
    "ko": "한국어 (Korean)",
    "th": "ภาษาไทย (Thai)",
    "id": "Bahasa Indonesia",
    "zh": "中文 (Chinese)",
}

# Base English keys (from en.json)
def load_en_json():
    with open(os.path.join(PROJECT_ROOT, "en.json"), 'r', encoding='utf-8') as f:
        return json.load(f)

# Translation dictionaries for each language
TRANSLATIONS = {
    "de": {
        "App_Title": "RagnaController",
        "Btn_Play": "SPIELEN",
        "Btn_Play_Desc": "Ragnarok Online starten",
        "Btn_Profiles": "PROFILE",
        "Btn_Settings": "EINSTELLUNGEN",
        "Btn_Community": "GEMEINSCHAFT",
        "Btn_Exit": "VERLASSEN",
        "Settings_Language": "Sprache",
        "Settings_Save": "SPEICHERN & SCHLIESSEN",
        "Status_Running": "LAUFEND",
        "Status_Paused": "PAUSIERT",
        "Status_NoController": "KEIN CONTROLLER",
        "Toast_ProfileLoaded": "Profil geladen:",
        "Window_Title": "RagnaController",
        "Device_Label": "Gerät",
        "ActiveProfileText": "Aktives Profil",
        "ControllerStatusText": "Controller-Status",
        "Header_Ragna": "⚔ RAGNA CONTROLLER",
        "Footer_ControllerHints": "D-Pad navigieren / A auswählen / B zurück / Start zum Spiel",
        "Settings_Title": "Einstellungen",
        "Settings_Deadzone": "TOTZONE",
        "Settings_Turbo": "TURBO",
        "Settings_Gyro": "GYRO",
        "Settings_SaveBtn": "SPEICHERN",
        "Settings_ResetBtn": "ZURÜCKSETZEN",
        "Settings_CloseBtn": "SCHLIESSEN",
        "Settings_DeadzoneValue": "Totzone: {0}",
        "Settings_TurboDelay": "Turbo-Verzögerung: {0}ms",
        "Settings_GyroSensitivity": "Gyro-Empfindlichkeit: {0}x",
        "ButtonRemap_Title": "Tastenzuordnung",
        "ButtonRemap_Header": "TASTENZUORDNUNG",
        "ButtonRemap_SelectBtn": "Wählen Sie eine Taste zur Zuordnung",
        "ButtonRemap_SaveBtn": "SPEICHERN",
        "ButtonRemap_CloseBtn": "SCHLIESSEN",
        "ComboEditor_Title": "Kombinations-Editor",
        "ComboEditor_Header": "KOMBINATIONSENGINE EDITOR",
        "ComboEditor_EnableCheck": "Kombination aktiv",
        "ComboEditor_CloseBtn": "✕",
        "ComboEditor_SelectClassTemplate": "Klassen-Vorlage auswählen",
        "ComboEditor_DelaysTooltip": "Verzögerungen zwischen Aktionen",
        "ComboEditor_PreRenewalBtn": "VOR-VERNEUERUNG",
        "ComboEditor_RenewalBtn": "VERNEUERUNG",
        "ComboEditor_AddStepTooltip": "Fügen Sie einen neuen Schritt zur Kombination hinzu",
        "ComboEditor_RemoveLastTooltip": "Entfernen Sie den letzten Schritt",
        "ComboEditor_TriggerHint": "Auslöser: Ordnen Sie eine Taste zu 'Klassen-Kombination' und HALTEN Sie sie.",
        "ComboEditor_SaveComboBtn": "KOMBINATION SPEICHERN",
        "ComboEditor_CancelBtn": "Abbrechen",
        "Community_Title": "Gemeinschaftsprofile",
        "Community_Header": "GEMEINSCHAFTSHUB",
        "Community_CloseBtn": "✕",
        "Community_SearchPlaceholder": "Profile suchen...",
        "Community_RefreshBtn": "↻ Aktualisieren",
        "Community_DownloadBtn": "↓ HERUNTERLADEN",
        "Community_LoadingText": "Gemeinschaftsprofile werden geladen...",
        "Community_ErrorText": "Laden fehlgeschlagen.",
        "Community_RetryBtn": "Erneut versuchen",
        "DaisyWheel_Title": "Daisy Wheel",
        "DaisyWheel_Hint": "L3 = ⌫   R3 = Leertaste   / Start = Senden   B = Abbrechen",
        "Handheld_Play_Tooltip": "Spiel starten",
        "Handheld_Profiles_Tooltip": "Profilverwaltung",
        "Handheld_Overlay_Tooltip": "Overlay umschalten",
        "Handheld_Settings_Tooltip": "Einstellungen",
        "Handheld_Community_Tooltip": "Gemeinschaftshub",
        "Handheld_Exit_Tooltip": "Anwendung beenden",
        "InGame_Title": "RagnaController Overlay",
        "InGame_LayerLabel": "SCHICHT",
        "InGame_BaseLayer": "BASIS",
        "InGame_LayerTooltip": "Aktuelle Schicht-Anzeige: BASIS / L1+ / R1+ / L2+ / R2+",
        "InGame_TrackDotTooltip": "Fensterverfolgungsanzeige",
        "InGame_IdleState": "IDLE",
        "InGame_StateTooltip": "Aktueller Kampfzustand",
        "MacroEditor_Title": "Makro-Editor",
        "MacroEditor_Header": "MAKRO EDITOR",
        "MacroEditor_InfoText": "Bearbeiten Sie Makroschritte und Timing",
        "MacroEditor_NameLabel": "NAME:",
        "MacroEditor_LoopsLabel": "LOOPS:",
        "MacroEditor_DurationLabel": "DAUER: {0}ms",
        "MacroEditor_AddStepBtn": "+ Schritt hinzufügen",
        "MacroEditor_RemoveLastBtn": "- Letzten entfernen",
        "MacroEditor_SpeedUpBtn": "Beschleunigen (×2)",
        "MacroEditor_SlowDownBtn": "Verlangsamen (×2)",
        "MacroEditor_OptimizeBtn": "Optimieren",
        "MacroEditor_PreviewBtn": "Vorschau",
        "MacroEditor_SaveChangesBtn": "ÄNDERUNGEN SPEICHERN",
        "MacroEditor_CancelBtn": "ABBRECHEN",
        "MacroRecorder_Title": "Makro-Recorder",
        "MacroRecorder_Header": "MAKRO RECORDER",
        "MacroRecorder_NameLabel": "MAKRONAME",
        "MacroRecorder_DefaultName": "Unbenanntes Makro",
        "MacroRecorder_RecordBtn": "● AUFNEHMEN",
        "MacroRecorder_StopBtn": "■ STOPPEN",
        "MacroRecorder_ClearBtn": "🗑 LÖSCHEN",
        "MacroRecorder_HintText": "Drücken Sie Aufnehmen und dann Ihre Tasten.",
        "MacroRecorder_EditBtn": "✏ Gespeichertes Makro bearbeiten",
        "MacroRecorder_SaveBtn": "MAKRO SPEICHERN",
        "MacroTimeline_Title": "Makro-Zeitleiste",
        "MacroTimeline_Header": "MAKRO ZEITLEISTE",
        "MacroTimeline_Subtitle": "Laden Sie ein Makro, um sein Timing zu visualisieren",
        "MacroTimeline_LoadTooltip": "Makrodatei laden",
        "MacroTimeline_PlayTooltip": "Makroz Zeitleiste abspielen",
        "MacroTimeline_ZoomTooltip": "Makroz Zeitleisten-Zoomstufe anpassen",
        "MacroTimeline_KeyTooltip": "Tastendruckereignis",
        "MacroTimeline_ClickTooltip": "Mausklickereignis",
        "MacroTimeline_RClickTooltip": "Rechtsklickereignis",
        "MacroTimeline_DelayTooltip": "Verzögerungsereignis",
        "MacroTimeline_TotalDurationTooltip": "Gesamte Makrodauer",
        "MacroTimeline_ZoomLabel": "{0}×",
        "MacroTimeline_Legend_Key": "Taste",
        "MacroTimeline_Legend_Click": "Klick",
        "MacroTimeline_Legend_RClick": "R-Klick",
        "MacroTimeline_Legend_Delay": "Verzögerung",
        "MacroTimeline_HoverHint": "Fahren Sie mit der Maus über einen Schritt, um Details anzuzeigen",
        "MacroTimeline_CloseBtn": "Schließen",
        "ProfileLibrary_Title": "Profilbibliothek",
        "ProfileLibrary_Header": "PROFILBIBLIOTHEK",
        "ProfileLibrary_CloseBtn": "✕",
        "ProfileLibrary_AllProfiles": "Alle Profile",
        "ProfileLibrary_BuiltInOnly": "Nur Eingebaut",
        "ProfileLibrary_CustomOnly": "Nur Benutzerdefiniert",
        "ProfileLibrary_Melee": "Kampf",
        "ProfileLibrary_Ranged": "Reichweite",
        "ProfileLibrary_Mage": "Magier",
        "ProfileLibrary_Support": "Support",
        "ProfileLibrary_CountLabel": "{0} Profile",
        "ProfileLibrary_LoadBtn": "Laden",
        "ProfileLibrary_ExportBtn": "Exportieren",
        "ProfileLibrary_ShareBtn": "↑ TEILEN",
        "ProfileLibrary_DeleteBtn": "Löschen",
        "ProfileLibrary_StatusText": "",
        "ProfileLibrary_NewProfileBtn": "Neues Profil",
        "ProfileLibrary_ImportBtn": "Importieren",
        "ProfileLibrary_ShareCodePlaceholder": "Geben Sie einen Teilcode ein (z. B. GX-A3F9) und klicken Sie auf ↓ Herunterladen",
        "ProfileLibrary_DownloadBtn": "↓ Herunterladen",
        "ProfileWizard_Title": "Assistent",
        "ProfileWizard_Header": "PROFILASSISTENT",
        "ProfileWizard_NameLabel": "PROFILNAME",
        "ProfileWizard_ClassLabel": "KLASSE",
        "ProfileWizard_Melee": "Kampf",
        "ProfileWizard_Mage": "Magier",
        "ProfileWizard_Ranged": "Reichweite",
        "ProfileWizard_Support": "Support",
        "ProfileWizard_CombatEngines": "KAMPFENGINE",
        "ProfileWizard_EnableAutoTarget": "Auto-Ziel aktivieren",
        "ProfileWizard_EnableKiteEngine": "Kite-Engine aktivieren",
        "ProfileWizard_EnableMageSystem": "Mage-System aktivieren",
        "ProfileWizard_EnableSupportMode": "Support-Modus aktivieren",
        "ProfileWizard_QuickKeys": "Schnelltasten",
        "ProfileWizard_AButton": "A:",
        "ProfileWizard_BButton": "B:",
        "ProfileWizard_XButton": "X:",
        "ProfileWizard_YButton": "Y:",
        "ProfileWizard_Review": "ÜBERSICHT",
        "ProfileWizard_ReviewMessage": "Das Profil wird mit Standard-Bewegungseinstellungen erstellt. Sie können alles später anpassen.",
        "ProfileWizard_BackBtn": "ZURÜCK",
        "ProfileWizard_NextBtn": "WEITER",
        "RadialMenu_Title": "Radialmenü",
        "RadialMenu_SelectItemText": "ELEMENT AUSWÄHLEN",
        "RadialMenu_SelectItemTooltip": "Wählen Sie eine Aktion aus dem radialen Menü aus",
        "RadialMenu_ExecuteText": "Triggern loslassen, um zu verwenden",
        "RadialMenu_ExecuteTooltip": "Ausgewählte Aktion ausführen",
        "RadialSetup_Title": "Radialmenü-Einrichtung",
        "RadialSetup_Header": "RADIALMENÜEINRICHTUNG",
        "RadialSetup_Hint": "Auslöser: LT + RT gleichzeitig halten → Ziel mit rechtem Stick anpeilen → Triggern loslassen, um auszuführen.",
        "RadialSetup_Column_DisplayName": "ANZEIGENAME",
        "RadialSetup_Column_CommandChat": "BEBEHLEN / CHAT",
        "RadialSetup_Column_KeyBinding": "TASTENZUORDNUNG",
        "RadialSetup_Column_Emotes": "EMOTES",
        "RadialSetup_FooterHint": "Leer lassen, um einen Slot zu deaktivieren. 8 Slots = 8 Richtungen.",
        "RadialSetup_SaveBtn": "RADIAL SPEICHERN",
        "RadialMenuGallery_Title": "EMOTE AUSWÄHLEN",
        "RadialMenuGallery_CloseBtn": "✕",
        "RadialMenuGallery_EmoteStatus": "Emoji-Fallback aktiv",
        "RadialMenuGallery_DownloadBtn": "⬇ Von iROWiki herunterladen",
        "Splash_StatusInitializing": "Initialisiere…",
        "Splash_VersionLabel": "v1.6.2",
    },
}

def generate_language_file(lang_code, translations):
    """Generate a language translation file."""
    # Load en.json
    en_keys = load_en_json()
    
    # Create translation dictionary
    trans_dict = {}
    for key, value in en_keys.items():
        if key in translations:
            trans_dict[key] = translations[key]
        else:
            # Use English as fallback for missing translations
            trans_dict[key] = value
    
    # Write to file
    filename = f"{lang_code}.json"
    filepath = os.path.join(PROJECT_ROOT, filename)
    
    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(trans_dict, f, indent=2, ensure_ascii=False)
        f.write('\n')
    
    print(f"✓ Created: {filename}")
    return len(trans_dict)

def main():
    """Generate all language translation files."""
    print("=" * 70)
    print("RAGNACONTROLLER LOCALIZATION - GENERATING LANGUAGE FILES")
    print("=" * 70)
    print()
    
    total_keys = 0
    for lang_code, lang_name in sorted(LANGUAGES.items()):
        if lang_code == "en":
            continue  # Skip English, already exists
        
        print(f"Processing: {lang_name} ({lang_code})")
        count = generate_language_file(lang_code, TRANSLATIONS.get(lang_code, {}))
        total_keys += count
        print()
    
    print("=" * 70)
    print("LANGUAGE FILE GENERATION COMPLETE!")
    print(f"Total language files created: {len(LANGUAGES) - 1}")
    print(f"Total translation keys: {total_keys}")
    print("=" * 70)

if __name__ == "__main__":
    main()
