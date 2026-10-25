#!/usr/bin/env python3
"""
Quick JSON fixer for en.json
Removes any trailing commas and fixes common JSON issues
"""

import json
import re

INPUT_FILE = "/mnt/c/RagnaController/src/RagnaController/Locales/en.json"
OUTPUT_FILE = INPUT_FILE  # Overwrite in place

def fix_json_file():
    """Read, fix, and write the JSON file"""
    print(f"Fixing {INPUT_FILE}...")
    
    try:
        with open(INPUT_FILE, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Try to parse it first to see the error
        try:
            data = json.loads(content)
            print("✅ JSON is valid!")
            return True
        except json.JSONDecodeError as e:
            print(f"❌ JSON Error: {e}")
            
            # Fix common issues
            print("\nFixing common JSON issues...")
            
            # Remove trailing commas before } or ]
            content = re.sub(r',(\s*[}\]])', r'\1', content)
            
            # Fix escaped quotes in strings
            content = re.sub(r'\"([^\"]*)\"', r'"\1"', content)
            
            # Write fixed content
            with open(INPUT_FILE, 'w', encoding='utf-8') as f:
                f.write(content)
            
            print("✅ Fixed and written")
            
            # Try to parse again
            try:
                data = json.loads(content)
                print(f"✅ JSON is now valid! ({len(data)} keys)")
                return True
            except json.JSONDecodeError as e2:
                print(f"❌ Still invalid: {e2}")
                return False
                
    except Exception as e:
        print(f"❌ Error reading file: {e}")
        return False

if __name__ == "__main__":
    fix_json_file()
