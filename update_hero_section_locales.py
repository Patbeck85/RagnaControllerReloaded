#!/usr/bin/env python3
"""
Update all 40 locale files with new UI/UX Overhaul strings for Hero Section & Dashboard.
This script adds new localization keys to all non-English locale JSON files.
"""

import json
import os
import glob

# New localization strings to add to all locales
NEW_STRINGS = {
    "Hero_Image_Fallback": "/Assets/Classes/unknown.png",
    "StatusTextDisplay_Running": "RUNNING",
    "StatusTextDisplay_Paused": "PAUSED",
    "StatusProfile_Empty": "No Profile",
    "InfoClassName_Empty": "No Class Selected",
    "InfoClassType_Empty": "",
    "BatteryLevelText2_Full": "100%",
    "BatteryLevelText2_Empty": "–",
    "ControllerNameText2_Empty": "No Controller Connected",
    "VoiceStatusText_Ready": "READY",
    "VoiceStatusText_Active": "ACTIVE",
    "LatencyText_Empty": "0ms",
    "EngineStateText_Running": "Running",
    "EngineStateText_Paused": "Paused",
    "EngineStateText_Error": "Error"
}

# Base locale directory
LOCALE_DIR = "/mnt/c/RagnaController/src/RagnaController/Locales"

def update_locale_file(locale_path):
    """Update a single locale JSON file with new strings."""
    try:
        # Read the file
        with open(locale_path, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Parse JSON
        data = json.loads(content)
        
        # Add new strings (maintain order by appending at end)
        for key, value in NEW_STRINGS.items():
            if key not in data:
                data[key] = value
        
        # Write back with proper formatting
        with open(locale_path, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Updated: {os.path.basename(locale_path)}")
        return True
        
    except Exception as e:
        print(f"✗ Error updating {os.path.basename(locale_path)}: {e}")
        return False

def main():
    """Main function to update all locale files."""
    # Find all JSON files in Locales directory (excluding en.json)
    locale_files = glob.glob(os.path.join(LOCALE_DIR, "*.json"))
    
    # Filter out en.json and any other non-locale files
    locale_files = [f for f in locale_files if os.path.basename(f).lower() not in ['en.json', 'README.md']]
    
    print(f"Found {len(locale_files)} locale files to update")
    print("-" * 60)
    
    # Update each file
    success_count = 0
    for locale_file in sorted(locale_files):
        if update_locale_file(locale_file):
            success_count += 1
    
    print("-" * 60)
    print(f"Completed: {success_count}/{len(locale_files)} files updated successfully")
    
    # Also verify en.json is valid
    en_path = os.path.join(LOCALE_DIR, "en.json")
    if os.path.exists(en_path):
        try:
            with open(en_path, 'r', encoding='utf-8') as f:
                json.load(f)
            print("✓ en.json is valid JSON")
        except Exception as e:
            print(f"✗ en.json has errors: {e}")

if __name__ == "__main__":
    main()
