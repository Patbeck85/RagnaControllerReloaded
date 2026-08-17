#!/usr/bin/env python3
"""
Update en.json with all new localization keys from the batch XAML update
"""

import json

# Read existing en.json
with open('/mnt/c/RagnaController/src/RagnaController/Locales/en.json', 'r', encoding='utf-8') as f:
    existing_keys = json.load(f)

# New keys from the batch update (keys that were added by the batch script)
new_keys = {
    # MainWindow.xaml keys
    "AdminWarning_Message": "Not running as Administrator — SendInput may not work correctly.",
    "Btn_RestartAsAdmin": "↑ Restart as Admin",
    "Btn_RestartAsAdmin_Tooltip": "Restart application as Administrator",
    "ClassBadge_Empty": "⚔ –",
    "GameMode_PreRenewal": "PRE-RENEWAL",
    "GameMode_Renewal": "RENEWAL",
    "ControllerName_NoController": "No Controller",
    "BatteryLevel_Empty": "–",
    
    # SettingsWindow.xaml keys
    "SettingsWindow_Title": "Settings",
    "SettingsWindow_Header": "GLOBAL SETTINGS",
    "Btn_Close_Tooltip": "Close settings window",
    "Settings_AutoStart": "Auto-start Engine on Launch",
    "Settings_AutoStart_Tooltip": "Enable engine automatically on client launch",
    "Settings_Sound": "Enable Sound Feedback",
    "Settings_Sound_Tooltip": "Enable click and skill sounds",
    "Settings_Rumble": "Enable Controller Rumble",
    "Settings_Rumble_Tooltip": "Enable controller vibration for feedback",
    "Settings_MiniMode": "Start in Mini-Mode",
    "Settings_MiniMode_Tooltip": "Start app in mini-overlay mode (compact)",
    "Settings_FocusLock": "Pause Engine when RO is in Background",
    "Settings_FocusLock_Tooltip": "Pause engine when RO is in background",
    "Settings_FocusLock_Description": "Prevents accidental input in Discord or browser when tabbed out.",
    "Settings_KernelInput": "Use Hardware Input (Anti-Cheat Bypass)",
    "Settings_KernelInput_Tooltip": "Requires Interception driver. Bypasses Gepard/Harmony blocks.",
    "Btn_InstallDriver": "Install Driver",
    "Btn_InstallDriver_Tooltip": "Installs required kernel driver (restart required)",
    "Settings_LogLevel_Label": "LOG LEVEL",
    "LogLevel_Debug": "Debug",
    "LogLevel_Info": "Info",
    "LogLevel_Warning": "Warning",
    "LogLevel_Error": "Error",
    "Btn_ReportBug": "🐛 Report a Bug / Request Feature",
    "Btn_ReportBug_Tooltip": "Opens the GitHub Issues page in your web browser",
    "Lbl_Version_Tooltip": "Current app version",
    "Lbl_SettingsPath_Tooltip": "Open settings directory (Windows Explorer)",
    "Btn_Reset": "RESET TO DEFAULT",
    "Btn_Reset_Tooltip": "Reset all settings to default values",
    "Btn_SaveAndClose": "SAVE & CLOSE",
    "Btn_SaveAndClose_Tooltip": "Save settings and close window",
    
    # ButtonRemappingWindow.xaml keys
    "Window_Remap_Title": "Button Remapping",
    "Window_Remap_Header": "BUTTON REMAPPING",
    "Layer_Title_1": "LAYER 1",
    "Layer_Title_2": "LAYER 2",
    "Layer_Title_3": "LAYER 3",
    "Layer_Title_4": "LAYER 4",
    "Layer_Title_5": "LAYER 5",
    
    # ComboEditorWindow.xaml keys
    "Window_Combo_Title": "Combo Editor",
    "Window_Combo_Header": "COMBO EDITOR",
    "Combo_Slot_Label_1": "SLOT 1",
    "Combo_Slot_Label_2": "SLOT 2",
    "Combo_Slot_Label_3": "SLOT 3",
    "Combo_Slot_Label_4": "SLOT 4",
    "Combo_Slot_Label_5": "SLOT 5",
    
    # CommunityBrowserWindow.xaml keys
    "Window_Community_Title": "Community Browser",
    "Window_Community_Header": "COMMUNITY BROWSER",
    "Search_Community_Placement": "Search for profiles...",
    "Community_NoResults": "No matching profiles found",
    "Btn_LoadMore": "LOAD MORE",
    "Btn_LoadMore_Tooltip": "Load more community profiles",
    "Btn_Close_Community": "CLOSE COMMUNITY",
    "Btn_Close_Community_Tooltip": "Close community browser",
    
    # DaisyWheelWindow.xaml keys
    "Window_DaisyWheel_Title": "Daisy Wheel",
    "Window_DaisyWheel_Header": "DAISY WHEEL",
    
    # HandheldWindow.xaml keys
    "Window_Handheld_Title": "Handheld Mode",
    "Window_Handheld_Header": "HANDHELD MODE",
    
    # InGameOverlayWindow.xaml keys
    "Window_Overlay_Title": "In-Game Overlay",
    "Window_Overlay_Header": "IN-GAME OVERLAY",
    "Btn_ToggleHUD": "TOGGLE HUD",
    "Btn_ToggleHUD_Tooltip": "Show/hide in-game HUD overlay",
    
    # MacroEditorWindow.xaml keys
    "Window_Macro_Title": "Macro Editor",
    "Window_Macro_Header": "MACRO EDITOR",
    
    # MacroRecorderWindow.xaml keys
    "Window_MacroRecorder_Title": "Macro Recorder",
    "Window_MacroRecorder_Header": "MACRO RECORDER",
    "Btn_StartRecording": "START RECORDING",
    "Btn_StartRecording_Tooltip": "Start recording macro actions",
    "Btn_StopRecording": "STOP RECORDING",
    "Btn_StopRecording_Tooltip": "Stop recording and save macro",
    
    # MacroTimelineWindow.xaml keys
    "Window_Timeline_Title": "Macro Timeline",
    "Window_Timeline_Header": "MACRO TIMELINE",
    
    # ProfileLibraryWindow.xaml keys
    "Window_Library_Title": "Profile Library",
    "Window_Library_Header": "PROFILE LIBRARY",
    
    # ProfileWizardWindow.xaml keys
    "Window_Wizard_Title": "Profile Wizard",
    "Window_Wizard_Header": "PROFILE WIZARD",
    "Btn_Next": "NEXT",
    "Btn_Next_Tooltip": "Proceed to next step",
    "Btn_Back": "BACK",
    "Btn_Back_Tooltip": "Go back to previous step",
    "Btn_Finish": "FINISH",
    "Btn_Finish_Tooltip": "Complete profile creation",
    
    # RadialSetupWindow.xaml keys
    "Window_Radial_Title": "Radial Menu Setup",
    "Window_Radial_Header": "RADIAL MENU SETUP",
    
    # SplashWindow.xaml keys
    "Splash_Loading": "Loading...",
    "Splash_Version": "Version {version}",
}

# Merge existing keys with new keys
merged_keys = {**existing_keys, **new_keys}

# Write updated en.json
with open('/mnt/c/RagnaController/src/RagnaController/Locales/en.json', 'w', encoding='utf-8') as f:
    json.dump(merged_keys, f, indent=2, ensure_ascii=False)
    f.write('\n')

print(f"✓ Updated en.json with {len(new_keys)} new keys")
print(f"  Total keys in en.json: {len(merged_keys)}")
print()
print("New keys added:")
for key in sorted(new_keys.keys()):
    print(f"  - {key}")
