#!/usr/bin/env python3
import json
import os

def validate_json_file(filepath):
    """Validate a JSON file and return any errors."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            json.load(f)
        return True, None
    except json.JSONDecodeError as e:
        return False, str(e)

def main():
    profiles_dir = '/mnt/c/RagnaController/src/RagnaController/DefaultProfiles'
    errors_found = 0
    
    # Get all JSON files in the profiles directory
    json_files = [f for f in os.listdir(profiles_dir) if f.endswith('.json')]
    
    print(f"Checking {len(json_files)} profile files...")
    print("=" * 60)
    
    for filename in sorted(json_files):
        filepath = os.path.join(profiles_dir, filename)
        valid, error = validate_json_file(filepath)
        
        if valid:
            print(f"✓ {filename}: Valid JSON")
        else:
            print(f"✗ {filename}: INVALID JSON")
            print(f"  Error: {error}")
            errors_found += 1
    
    print("=" * 60)
    
    if errors_found == 0:
        print("All profile files are valid!")
        return 0
    else:
        print(f"Found {errors_found} invalid JSON file(s)")
        return 1

if __name__ == '__main__':
    import sys
    sys.exit(main())
