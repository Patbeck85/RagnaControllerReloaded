import json
import os

# New Smart Standby keys to add
new_keys = {
    "Settings_SmartStandby": "Smart Standby (AFK Battery Saver)",
    "Tooltip_SmartStandby": "Turns off the controller lightbar and reduces CPU usage after being AFK.",
    "Lbl_Minutes": "Minutes:"
}

locales_dir = "/mnt/c/RagnaController/src/RagnaController/Locales"

# List all locale files except en.json
locale_files = [f for f in os.listdir(locales_dir) if f.endswith('.json') and f != 'en.json']
print(f"Found {len(locale_files)} locale files to update")

# Process each locale file
for locale_file in sorted(locale_files):
    filepath = os.path.join(locales_dir, locale_file)
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        
        # Add new keys if they don't exist
        for key, value in new_keys.items():
            if key not in data:
                data[key] = value
        
        # Save back
        with open(filepath, 'w', encoding='utf-8') as f:
            json.dump(data, f, ensure_ascii=False, indent=2)
        
        print(f"✓ Updated {locale_file}")
        
    except Exception as e:
        print(f"✗ Error updating {locale_file}: {e}")

print("\nDone!")
