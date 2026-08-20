#!/usr/bin/env python3
"""
JSON Syntax Validator for all locale files in RagnaController project.
Scans all .json files and reports syntax errors.
"""

import json
import os
import sys
from pathlib import Path

def validate_json_file(filepath):
    """Validate a single JSON file and return error info."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            json.loads(content)
        return {
            'file': filepath,
            'status': 'OK',
            'error': None,
            'line': None
        }
    except json.JSONDecodeError as e:
        return {
            'file': filepath,
            'status': 'ERROR',
            'error': str(e),
            'line': e.lineno
        }
    except Exception as e:
        return {
            'file': filepath,
            'status': 'ERROR',
            'error': f'{type(e).__name__}: {str(e)}',
            'line': None
        }

def main():
    """Main function to scan all JSON files."""
    # Find all JSON files in the project (excluding bin/obj directories)
    json_files = []
    
    for root, dirs, files in os.walk('/mnt/c/RagnaController'):
        # Skip build directories
        if 'bin' in root or 'obj' in root:
            continue
        
        for file in files:
            if file.endswith('.json'):
                json_files.append(os.path.join(root, file))
    
    print(f"Found {len(json_files)} JSON files to validate...")
    print("=" * 80)
    
    errors = []
    ok_count = 0
    
    for filepath in sorted(json_files):
        result = validate_json_file(filepath)
        
        if result['status'] == 'OK':
            ok_count += 1
            # Only show files that are OK if we want to track progress
            # print(f"✓ {filepath}")
        else:
            errors.append(result)
            print(f"✗ {result['file']}")
            print(f"  Error at line {result['line']}: {result['error']}")
    
    print("=" * 80)
    print(f"\nSummary:")
    print(f"  Total files: {len(json_files)}")
    print(f"  Valid JSON: {ok_count}")
    print(f"  Errors found: {len(errors)}")
    
    if errors:
        print("\nError details:")
        for err in errors:
            print(f"  - {err['file']} (line {err['line']})")
            print(f"    {err['error']}")
        
        return 1
    else:
        print("\n✓ All JSON files are valid!")
        return 0

if __name__ == '__main__':
    sys.exit(main())
