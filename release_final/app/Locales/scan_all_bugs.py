#!/usr/bin/env python3
"""
Comprehensive Bug Hunter for RagnaController project.
Scans all .cs, .xaml, .json, .csproj files for common bugs and issues.
"""

import os
import re
import sys
from pathlib import Path
from collections import defaultdict

# Common bug patterns to detect
BUG_PATTERNS = {
    'NullReferenceException': [
        (r'\.ToString\(\)', 'ToString() without null check'),
        (r'\.Length\b', 'Accessing .Length without null check'),
        (r'\.Count\b', 'Accessing .Count without null check'),
        (r'\.First\(\)', '.First() without Any() check'),
        (r'\.Single\(\)', '.Single() without Any() check'),
        (r'using\s+static\s+System\.Linq;', 'Using static LINQ (allocation risk)'),
    ],
    'MemoryLeak': [
        (r'(?:event|delegate)\s+\w+\s*=\s*new', 'Event subscription without unsubscribe'),
        (r'new\s+System\.Threading\.Timer\(', 'Timer created but may not be disposed'),
        (r'new\s+HttpClient\(\)', 'HttpClient created outside of using block'),
    ],
    'RaceCondition': [
        (r'volatile\s+static\s+\w+', 'Using volatile (consider lock or ConcurrentCollection)'),
        (r'lock\s*\(\s*[^)]*\)\s*\{', 'Lock usage - check if necessary'),
    ],
    'BuildWarning': [
        (r'CS0168|local variable never used', 'Unused local variable'),
        (r'CS0169|field never used', 'Unused field'),
        (r'CS0649|non-readonly field never assigned', 'Non-readonly field never assigned'),
        (r'CS0219|unused class member', 'Unused member'),
    ],
    'UsingStaticName': [
        (r'using\s+static\s+System\.(?:Linq|Collections|IO|Threading|Globalization|Reflection|Diagnostics);', 'Using static directive'),
    ],
    'FieldWarning': [
        (r'\[System\.ComponentModel\.DesignerGenerated\]', 'Designer generated code'),
        (r'private\s+\w+\s+\w+\s*=\s*null;', 'Private field initialized to null'),
    ],
}

def scan_file(filepath):
    """Scan a single file for bugs."""
    results = []
    
    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()
            lines = content.split('\n')
            
            for bug_type, patterns in BUG_PATTERNS.items():
                for pattern, description in patterns:
                    matches = re.finditer(pattern, content, re.MULTILINE)
                    for match in matches:
                        line_num = content[:match.start()].count('\n') + 1
                        results.append({
                            'type': bug_type,
                            'pattern': description,
                            'line': line_num,
                            'file': filepath,
                            'match': match.group()
                        })
    except Exception as e:
        results.append({
            'type': 'ERROR',
            'pattern': f'File read error: {str(e)}',
            'line': 0,
            'file': filepath,
            'match': ''
        })
    
    return results

def main():
    """Main function to scan the entire project."""
    project_root = '/mnt/c/RagnaController'
    
    # Find all relevant files
    cs_files = []
    xaml_files = []
    json_files = []
    csproj_files = []
    
    for root, dirs, files in os.walk(project_root):
        # Skip build directories
        if 'bin' in root or 'obj' in root:
            continue
        
        for file in files:
            filepath = os.path.join(root, file)
            
            if file.endswith('.cs'):
                cs_files.append(filepath)
            elif file.endswith('.xaml'):
                xaml_files.append(filepath)
            elif file.endswith('.json'):
                json_files.append(filepath)
            elif file.endswith('.csproj'):
                csproj_files.append(filepath)
    
    print(f"Scanning project...")
    print(f"  .cs files: {len(cs_files)}")
    print(f"  .xaml files: {len(xaml_files)}")
    print(f"  .json files: {len(json_files)}")
    print(f"  .csproj files: {len(csproj_files)}")
    print("=" * 80)
    
    all_results = []
    
    # Scan C# files
    print("\nScanning C# files...")
    for filepath in cs_files:
        results = scan_file(filepath)
        all_results.extend(results)
    
    # Scan XAML files
    print("Scanning XAML files...")
    for filepath in xaml_files:
        results = scan_file(filepath)
        all_results.extend(results)
    
    # Scan JSON files
    print("Scanning JSON files...")
    for filepath in json_files:
        results = scan_file(filepath)
        all_results.extend(results)
    
    # Scan CSProj files
    print("Scanning CSProj files...")
    for filepath in csproj_files:
        results = scan_file(filepath)
        all_results.extend(results)
    
    # Group results by file
    issues_by_file = defaultdict(list)
    for result in all_results:
        issues_by_file[result['file']].append(result)
    
    # Print results
    print("\n" + "=" * 80)
    print("BUG REPORT")
    print("=" * 80)
    
    if not issues_by_file:
        print("\n✓ No bugs found! Project is clean.")
        return 0
    
    total_issues = 0
    for filepath, issues in sorted(issues_by_file.items()):
        print(f"\n{filepath}")
        print("-" * 80)
        for issue in issues:
            line = issue['line']
            bug_type = issue['type']
            pattern = issue['pattern']
            match = issue['match']
            
            print(f"  Line {line}: [{bug_type}] {pattern}")
            if match:
                print(f"    Match: {match[:100]}")
            total_issues += 1
    
    print("\n" + "=" * 80)
    print(f"SUMMARY: Found {total_issues} potential issues")
    print("=" * 80)
    
    return 1 if total_issues > 0 else 0

if __name__ == '__main__':
    sys.exit(main())
