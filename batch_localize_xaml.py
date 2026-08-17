#!/usr/bin/env python3
"""
Batch Localization Script for RagnaController XAML Files
Updates all remaining hardcoded strings with {core:Loc KeyName} bindings
"""

import os
from pathlib import Path

# Project root
PROJECT_ROOT = "/mnt/c/RagnaController/src/RagnaController"

# Localization keys mapping (old_string -> new_string with localization)
LOCALIZATION_MAPPINGS = {
    # MainWindow.xaml - remaining strings
    ("MainWindow.xaml", "Title"): ('RagnaController', '{core:Loc Header_Ragna}'),
    ("MainWindow.xaml", "Admin Warning"): ('Not running as Administrator — SendInput may not work correctly.', '{core:Loc AdminWarning_Message}'),
    ("MainWindow.xaml", "Restart Button"): ('↑ Restart as Admin', '{core:Loc Btn_RestartAsAdmin}'),
    ("MainWindow.xaml", "Restart Tooltip"): ('Restart application as Administrator', '{core:Loc Btn_RestartAsAdmin_Tooltip}'),
    ("MainWindow.xaml", "Class Badge Empty"): ('⚔ –', '{core:Loc ClassBadge_Empty}'),
    ("MainWindow.xaml", "Game Mode Pre-Renewal"): ('PRE-RENEWAL', '{core:Loc GameMode_PreRenewal}'),
    ("MainWindow.xaml", "Game Mode Renewal"): ('RENEWAL', '{core:Loc GameMode_Renewal}'),
    ("MainWindow.xaml", "Controller Name No Controller"): ('No Controller', '{core:Loc ControllerName_NoController}'),
    ("MainWindow.xaml", "Battery Level Empty"): ('–', '{core:Loc BatteryLevel_Empty}'),
    
    # SettingsWindow.xaml - already done, skip
    
    # ButtonRemappingWindow.xaml
    ("ButtonRemappingWindow.xaml", "Title"): ('Button Remapping', '{core:Loc Window_Remap_Title}'),
    ("ButtonRemappingWindow.xaml", "Header"): ('BUTTON REMAPPING', '{core:Loc Window_Remap_Header}'),
    ("ButtonRemappingWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("ButtonRemappingWindow.xaml", "Close Tooltip"): ('Close remapping window', '{core:Loc Btn_Close_Tooltip}'),
    ("ButtonRemappingWindow.xaml", "Layer Title"): ('LAYER 1', '{core:Loc Layer_Title_1}'),
    ("ButtonRemappingWindow.xaml", "Layer Title 2"): ('LAYER 2', '{core:Loc Layer_Title_2}'),
    ("ButtonRemappingWindow.xaml", "Layer Title 3"): ('LAYER 3', '{core:Loc Layer_Title_3}'),
    ("ButtonRemappingWindow.xaml", "Layer Title 4"): ('LAYER 4', '{core:Loc Layer_Title_4}'),
    ("ButtonRemappingWindow.xaml", "Layer Title 5"): ('LAYER 5', '{core:Loc Layer_Title_5}'),
    ("ButtonRemappingWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("ButtonRemappingWindow.xaml", "Save Tooltip"): ('Save remapping and close window', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("ButtonRemappingWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("ButtonRemappingWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # ComboEditorWindow.xaml
    ("ComboEditorWindow.xaml", "Title"): ('Combo Editor', '{core:Loc Window_Combo_Title}'),
    ("ComboEditorWindow.xaml", "Header"): ('COMBO EDITOR', '{core:Loc Window_Combo_Header}'),
    ("ComboEditorWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("ComboEditorWindow.xaml", "Close Tooltip"): ('Close editor window', '{core:Loc Btn_Close_Tooltip}'),
    ("ComboEditorWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("ComboEditorWindow.xaml", "Save Tooltip"): ('Save combo chain and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("ComboEditorWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("ComboEditorWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    ("ComboEditorWindow.xaml", "Slot Label 1"): ('SLOT 1', '{core:Loc Combo_Slot_Label_1}'),
    ("ComboEditorWindow.xaml", "Slot Label 2"): ('SLOT 2', '{core:Loc Combo_Slot_Label_2}'),
    ("ComboEditorWindow.xaml", "Slot Label 3"): ('SLOT 3', '{core:Loc Combo_Slot_Label_3}'),
    ("ComboEditorWindow.xaml", "Slot Label 4"): ('SLOT 4', '{core:Loc Combo_Slot_Label_4}'),
    ("ComboEditorWindow.xaml", "Slot Label 5"): ('SLOT 5', '{core:Loc Combo_Slot_Label_5}'),
    
    # CommunityBrowserWindow.xaml
    ("CommunityBrowserWindow.xaml", "Title"): ('Community Browser', '{core:Loc Window_Community_Title}'),
    ("CommunityBrowserWindow.xaml", "Header"): ('COMMUNITY BROWSER', '{core:Loc Window_Community_Header}'),
    ("CommunityBrowserWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("CommunityBrowserWindow.xaml", "Close Tooltip"): ('Close browser window', '{core:Loc Btn_Close_Tooltip}'),
    ("CommunityBrowserWindow.xaml", "Search Placeholder"): ('Search for profiles...', '{core:Loc Search_Community_Placement}'),
    ("CommunityBrowserWindow.xaml", "No Results"): ('No matching profiles found', '{core:Loc Community_NoResults}'),
    ("CommunityBrowserWindow.xaml", "Load More"): ('LOAD MORE', '{core:Loc Btn_LoadMore}'),
    ("CommunityBrowserWindow.xaml", "Load More Tooltip"): ('Load more community profiles', '{core:Loc Btn_LoadMore_Tooltip}'),
    ("CommunityBrowserWindow.xaml", "Close Community"): ('CLOSE COMMUNITY', '{core:Loc Btn_Close_Community}'),
    ("CommunityBrowserWindow.xaml", "Close Community Tooltip"): ('Close community browser', '{core:Loc Btn_Close_Community_Tooltip}'),
    
    # DaisyWheelWindow.xaml
    ("DaisyWheelWindow.xaml", "Title"): ('Daisy Wheel', '{core:Loc Window_DaisyWheel_Title}'),
    ("DaisyWheelWindow.xaml", "Header"): ('DAISY WHEEL', '{core:Loc Window_DaisyWheel_Header}'),
    ("DaisyWheelWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("DaisyWheelWindow.xaml", "Close Tooltip"): ('Close daisy wheel window', '{core:Loc Btn_Close_Tooltip}'),
    ("DaisyWheelWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("DaisyWheelWindow.xaml", "Save Tooltip"): ('Save combo chain and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("DaisyWheelWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("DaisyWheelWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # HandheldWindow.xaml
    ("HandheldWindow.xaml", "Title"): ('Handheld Mode', '{core:Loc Window_Handheld_Title}'),
    ("HandheldWindow.xaml", "Header"): ('HANDHELD MODE', '{core:Loc Window_Handheld_Header}'),
    ("HandheldWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("HandheldWindow.xaml", "Close Tooltip"): ('Close handheld mode window', '{core:Loc Btn_Close_Tooltip}'),
    ("HandheldWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("HandheldWindow.xaml", "Save Tooltip"): ('Save settings and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("HandheldWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("HandheldWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # InGameOverlayWindow.xaml
    ("InGameOverlayWindow.xaml", "Title"): ('In-Game Overlay', '{core:Loc Window_Overlay_Title}'),
    ("InGameOverlayWindow.xaml", "Header"): ('IN-GAME OVERLAY', '{core:Loc Window_Overlay_Header}'),
    ("InGameOverlayWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("InGameOverlayWindow.xaml", "Close Tooltip"): ('Close overlay window', '{core:Loc Btn_Close_Tooltip}'),
    ("InGameOverlayWindow.xaml", "Toggle HUD"): ('TOGGLE HUD', '{core:Loc Btn_ToggleHUD}'),
    ("InGameOverlayWindow.xaml", "Toggle HUD Tooltip"): ('Show/hide in-game HUD overlay', '{core:Loc Btn_ToggleHUD_Tooltip}'),
    ("InGameOverlayWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("InGameOverlayWindow.xaml", "Save Tooltip"): ('Save settings and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    
    # MacroEditorWindow.xaml
    ("MacroEditorWindow.xaml", "Title"): ('Macro Editor', '{core:Loc Window_Macro_Title}'),
    ("MacroEditorWindow.xaml", "Header"): ('MACRO EDITOR', '{core:Loc Window_Macro_Header}'),
    ("MacroEditorWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("MacroEditorWindow.xaml", "Close Tooltip"): ('Close macro editor window', '{core:Loc Btn_Close_Tooltip}'),
    ("MacroEditorWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("MacroEditorWindow.xaml", "Save Tooltip"): ('Save macro and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("MacroEditorWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("MacroEditorWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # MacroRecorderWindow.xaml
    ("MacroRecorderWindow.xaml", "Title"): ('Macro Recorder', '{core:Loc Window_MacroRecorder_Title}'),
    ("MacroRecorderWindow.xaml", "Header"): ('MACRO RECORDER', '{core:Loc Window_MacroRecorder_Header}'),
    ("MacroRecorderWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("MacroRecorderWindow.xaml", "Close Tooltip"): ('Close recorder window', '{core:Loc Btn_Close_Tooltip}'),
    ("MacroRecorderWindow.xaml", "Start Recording"): ('START RECORDING', '{core:Loc Btn_StartRecording}'),
    ("MacroRecorderWindow.xaml", "Start Recording Tooltip"): ('Start recording macro actions', '{core:Loc Btn_StartRecording_Tooltip}'),
    ("MacroRecorderWindow.xaml", "Stop Recording"): ('STOP RECORDING', '{core:Loc Btn_StopRecording}'),
    ("MacroRecorderWindow.xaml", "Stop Recording Tooltip"): ('Stop recording and save macro', '{core:Loc Btn_StopRecording_Tooltip}'),
    ("MacroRecorderWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("MacroRecorderWindow.xaml", "Save Tooltip"): ('Save macro and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    
    # MacroTimelineWindow.xaml
    ("MacroTimelineWindow.xaml", "Title"): ('Macro Timeline', '{core:Loc Window_Timeline_Title}'),
    ("MacroTimelineWindow.xaml", "Header"): ('MACRO TIMELINE', '{core:Loc Window_Timeline_Header}'),
    ("MacroTimelineWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("MacroTimelineWindow.xaml", "Close Tooltip"): ('Close timeline window', '{core:Loc Btn_Close_Tooltip}'),
    ("MacroTimelineWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("MacroTimelineWindow.xaml", "Save Tooltip"): ('Save macro and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("MacroTimelineWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("MacroTimelineWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # ProfileLibraryWindow.xaml
    ("ProfileLibraryWindow.xaml", "Title"): ('Profile Library', '{core:Loc Window_Library_Title}'),
    ("ProfileLibraryWindow.xaml", "Header"): ('PROFILE LIBRARY', '{core:Loc Window_Library_Header}'),
    ("ProfileLibraryWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("ProfileLibraryWindow.xaml", "Close Tooltip"): ('Close library window', '{core:Loc Btn_Close_Tooltip}'),
    ("ProfileLibraryWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("ProfileLibraryWindow.xaml", "Save Tooltip"): ('Save profile and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("ProfileLibraryWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("ProfileLibraryWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # ProfileWizardWindow.xaml
    ("ProfileWizardWindow.xaml", "Title"): ('Profile Wizard', '{core:Loc Window_Wizard_Title}'),
    ("ProfileWizardWindow.xaml", "Header"): ('PROFILE WIZARD', '{core:Loc Window_Wizard_Header}'),
    ("ProfileWizardWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("ProfileWizardWindow.xaml", "Close Tooltip"): ('Close wizard window', '{core:Loc Btn_Close_Tooltip}'),
    ("ProfileWizardWindow.xaml", "Next Button"): ('NEXT', '{core:Loc Btn_Next}'),
    ("ProfileWizardWindow.xaml", "Next Tooltip"): ('Proceed to next step', '{core:Loc Btn_Next_Tooltip}'),
    ("ProfileWizardWindow.xaml", "Back Button"): ('BACK', '{core:Loc Btn_Back}'),
    ("ProfileWizardWindow.xaml", "Back Tooltip"): ('Go back to previous step', '{core:Loc Btn_Back_Tooltip}'),
    ("ProfileWizardWindow.xaml", "Finish Button"): ('FINISH', '{core:Loc Btn_Finish}'),
    ("ProfileWizardWindow.xaml", "Finish Tooltip"): ('Complete profile creation', '{core:Loc Btn_Finish_Tooltip}'),
    
    # RadialSetupWindow.xaml
    ("RadialSetupWindow.xaml", "Title"): ('Radial Menu Setup', '{core:Loc Window_Radial_Title}'),
    ("RadialSetupWindow.xaml", "Header"): ('RADIAL MENU SETUP', '{core:Loc Window_Radial_Header}'),
    ("RadialSetupWindow.xaml", "Close Button"): ('✕', '{core:Loc Btn_Close}'),
    ("RadialSetupWindow.xaml", "Close Tooltip"): ('Close setup window', '{core:Loc Btn_Close_Tooltip}'),
    ("RadialSetupWindow.xaml", "Save Button"): ('SAVE & CLOSE', '{core:Loc Btn_SaveAndClose}'),
    ("RadialSetupWindow.xaml", "Save Tooltip"): ('Save radial menu and close', '{core:Loc Btn_SaveAndClose_Tooltip}'),
    ("RadialSetupWindow.xaml", "Cancel Button"): ('CANCEL', '{core:Loc Btn_Cancel}'),
    ("RadialSetupWindow.xaml", "Cancel Tooltip"): ('Cancel without saving', '{core:Loc Btn_Cancel_Tooltip}'),
    
    # SplashWindow.xaml
    ("SplashWindow.xaml", "Title"): ('RagnaController', '{core:Loc Header_Ragna}'),
    ("SplashWindow.xaml", "Loading Text"): ('Loading...', '{core:Loc Splash_Loading}'),
    ("SplashWindow.xaml", "Version Text"): ('Version {version}', '{core:Loc Splash_Version}'),
}

def update_xaml_file(filepath, mappings):
    """Update a single XAML file with localization bindings."""
    if not os.path.exists(filepath):
        print(f"  ⚠ File not found: {filepath}")
        return False
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original_content = content
        file_modified = False
        
        for key_name, (old_string, new_string) in mappings.items():
            if old_string in content:
                # Escape special characters in new_string for XAML
                escaped_new_string = new_string.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
                content = content.replace(old_string, escaped_new_string)
                file_modified = True
                print(f"    ✓ Updated: {old_string[:50]}... -> {new_string}")
        
        if file_modified:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"  ✓ Modified: {filepath}")
            return True
        
        return False
    
    except Exception as e:
        print(f"  ✗ Error updating {filepath}: {e}")
        return False

def main():
    """Main function to update all XAML files."""
    print("=" * 60)
    print("RagnaController XAML Localization Batch Update")
    print("=" * 60)
    print()
    
    # Filter mappings for existing files only
    existing_files = set()
    for (filepath, key_name) in LOCALIZATION_MAPPINGS.keys():
        full_path = os.path.join(PROJECT_ROOT, filepath)
        if os.path.exists(full_path):
            existing_files.add(filepath)
    
    print(f"Found {len(existing_files)} existing XAML files")
    print()
    
    # Group mappings by file
    file_mappings = {}
    for (filepath, key_name), (old_string, new_string) in LOCALIZATION_MAPPINGS.items():
        if filepath in existing_files:
            if filepath not in file_mappings:
                file_mappings[filepath] = {}
            file_mappings[filepath][key_name] = (old_string, new_string)
    
    # Update each file
    total_modified = 0
    for filepath, mappings in sorted(file_mappings.items()):
        print(f"Processing: {filepath}")
        if update_xaml_file(os.path.join(PROJECT_ROOT, filepath), mappings):
            total_modified += 1
        print()
    
    print("=" * 60)
    print(f"Batch Update Complete!")
    print(f"Files modified: {total_modified}/{len(existing_files)}")
    print("=" * 60)

if __name__ == "__main__":
    main()
