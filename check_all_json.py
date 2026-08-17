#!/usr/bin/env python3
import json
import os
import sys

def check_json_file(filepath):
    """Check a single JSON file for syntax errors."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            data = json.load(f)
        return True, None
    except json.JSONDecodeError as e:
        return False, str(e)
    except Exception as e:
        return False, f"{type(e).__name__}: {e}"

def main():
    base_dir = "."
    
    # Define directories to check (excluding bin/obj)
    dirs_to_check = [
        ".hermes",
        "Community",
        "Locales",
        "src/RagnaController",
        "tests/RagnaController.Tests"
    ]
    
    all_errors = []
    total_files = 0
    valid_files = 0
    
    for dir_name in dirs_to_check:
        dir_path = os.path.join(base_dir, dir_name)
        if not os.path.isdir(dir_path):
            continue
        
        for filename in sorted(os.listdir(dir_path)):
            filepath = os.path.join(dir_path, filename)
            
            # Skip directories and non-JSON files
            if os.path.isdir(filepath):
                continue
            if not filename.endswith('.json'):
                continue
            
            total_files += 1
            is_valid, error = check_json_file(filepath)
            
            if is_valid:
                valid_files += 1
                rel_path = filepath.replace(base_dir + "/", "")
                print(f"✓ {rel_path}: Valid JSON")
            else:
                rel_path = filepath.replace(base_dir + "/", "")
                print(f"✗ {rel_path}: ERROR - {error}")
                all_errors.append((rel_path, error))
    
    # Summary
    print(f"\n{'='*60}")
    print(f"=== JSON VALIDATION SUMMARY ===")
    print(f"{'='*60}")
    print(f"Total files checked: {total_files}")
    print(f"Valid files: {valid_files}")
    print(f"Errors found: {len(all_errors)}")
    
    if all_errors:
        print(f"\n{'='*60}")
        print(f"=== ERROR DETAILS ===")
        print(f"{'='*60}")
        for filepath, error in all_errors:
            print(f"\n{filepath}:")
            print(f"  Error: {error}")
    
    return len(all_errors) == 0

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
