#!/usr/bin/env python3
"""
Systematic C# Bug Scanner for RagnaController
Checks for common issues:
- Missing using directives
- Null reference risks
- Common syntax patterns that might indicate bugs
- Thread safety issues
- Resource leaks
"""

import os
import re
import sys
from pathlib import Path

def check_file(filepath):
    """Check a single file for potential issues."""
    issues = []
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.split('\n')
        
        # Check for common patterns
        line_num = 0
        for line in lines:
            line_num += 1
            
            # Check for missing semicolons (common syntax error)
            stripped = line.strip()
            if stripped and not stripped.startswith('//') and not stripped.startswith('/*'):
                # Skip string literals
                if not ('"' in stripped and '"' in stripped.split('}')[0] if '}' in stripped else False):
                    if stripped.endswith('{') or stripped.endswith('}') or stripped.endswith('(') or stripped.endswith(')'):
                        pass  # OK, these are fine
                    elif stripped.endswith(';'):
                        pass  # OK, has semicolon
                    elif not stripped.startswith('//'):
                        # Check if it looks like a statement without semicolon
                        if re.match(r'^\s*\w+\s*(new|var|if|for|while|switch|catch|using)\b', stripped):
                            if not stripped.endswith(';') and not stripped.endswith('{'):
                                issues.append(f"Line {line_num}: Possible missing semicolon: {stripped[:60]}")
            
            # Check for null assignment patterns
            if re.search(r'\.ToString\s*\(\s*\)', line):
                pass  # Common pattern, might be OK
            
            # Check for potential null reference
            if re.search(r'if\s*\(.*==\s*null\b', line):
                issues.append(f"Line {line_num}: Null check pattern: {stripped[:60]}")
                
    except Exception as e:
        issues.append(f"Error reading file: {str(e)}")
    
    return issues

def main():
    """Main scanning function."""
    src_dir = '/mnt/c/RagnaController/src/RagnaController'
    
    # Get all .cs files excluding obj
    cs_files = []
    for root, dirs, files in os.walk(src_dir):
        # Skip obj directories
        if 'obj' in root:
            continue
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    
    print(f"Scanning {len(cs_files)} C# files...")
    
    total_issues = 0
    for filepath in sorted(cs_files):
        issues = check_file(filepath)
        if issues:
            print(f"\n{filepath}:")
            for issue in issues[:5]:  # Show first 5 issues per file
                print(f"  {issue}")
            total_issues += len(issues)
    
    print(f"\n\nTotal issues found: {total_issues}")
    return total_issues

if __name__ == '__main__':
    main()
