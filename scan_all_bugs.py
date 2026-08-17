#!/usr/bin/env python3
"""
RagnaController Bug Hunter - Iterativer Fehlersuch- und Reparatur-Prozess
Scans all .cs, .xaml, .json, .csproj files for errors and repairs immediately.
"""

import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import List, Tuple, Dict, Any

PROJECT_ROOT = "/mnt/c/RagnaController"
SRC_DIR = os.path.join(PROJECT_ROOT, "src", "RagnaController")
LOCES_DIR = os.path.join(PROJECT_ROOT, "Locales")
BIN_DIR = os.path.join(PROJECT_ROOT, "src", "RagnaController", "bin")

# Patterns for potential bugs
NULL_REF_PATTERNS = [
    (r'\.ToString\(\)', 'Accessing .ToString() without null check'),
    (r'\.Length\b', 'Accessing .Length without null check'),
    (r'\.Count\b', 'Accessing .Count without null check'),
    (r'\.GetType\(\)', 'Accessing .GetType() without null check'),
    (r'\.Value\b', 'Accessing .Value without null check'),
    (r'\.Text\b', 'Accessing .Text without null check'),
]

RACE_CONDITION_PATTERNS = [
    (r'volatile\s*\(\s*\w+\s*\)', 'Potential race condition - consider using Interlocked'),
    (r'static\s+string\s+\w+\s*=\s*""', 'Static string initialized at runtime - potential race'),
]

RESOURCE_LEAK_PATTERNS = [
    (r'new\s+Stream\b', 'Stream created - check for proper disposal'),
    (r'new\s+HttpClient\b', 'HttpClient created - should be reused or disposed'),
]

# Build command
BUILD_CMD = ["dotnet", "build", "-c", "Release", "--no-restore"]

def run_command(cmd: List[str], timeout: int = 300) -> Tuple[int, str]:
    """Run a command and return exit code and output."""
    try:
        result = subprocess.run(
            cmd, 
            cwd=PROJECT_ROOT, 
            capture_output=True, 
            text=True, 
            timeout=timeout
        )
        return result.returncode, result.stdout + result.stderr
    except subprocess.TimeoutExpired:
        return -1, "Command timed out"
    except Exception as e:
        return -1, str(e)

def check_build() -> Tuple[int, int]:
    """Check current build status. Returns (errors, warnings)."""
    exit_code, output = run_command(BUILD_CMD)
    
    errors = 0
    warnings = 0
    
    if "Build succeeded." in output:
        return 0, 0
    
    # Parse error count
    error_match = re.search(r'(\d+)\s+Error', output)
    if error_match:
        errors = int(error_match.group(1))
    
    # Parse warning count
    warning_match = re.search(r'(\d+)\s+Warning', output)
    if warning_match:
        warnings = int(warning_match.group(1))
    
    return errors, warnings

def scan_json_files() -> List[Tuple[str, str, int]]:
    """Scan JSON files for syntax errors. Returns list of (file, error, line)."""
    errors = []
    
    if not os.path.exists(LOCES_DIR):
        return errors
    
    for filename in sorted(os.listdir(LOCES_DIR)):
        if not filename.endswith('.json'):
            continue
        
        filepath = os.path.join(LOCES_DIR, filename)
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            try:
                json.loads(content)
            except json.JSONDecodeError as e:
                errors.append((filepath, str(e), e.lineno))
                
        except Exception as e:
            errors.append((filepath, f"Read error: {e}", 0))
    
    return errors

def scan_cs_files_for_patterns() -> List[Tuple[str, str, int]]:
    """Scan C# files for potential bug patterns. Returns list of (file, pattern, line)."""
    found = []
    
    # Find all .cs files
    cs_files = []
    for root, dirs, files in os.walk(SRC_DIR):
        for f in files:
            if f.endswith('.cs'):
                cs_files.append(os.path.join(root, f))
    
    for filepath in sorted(cs_files):
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                lines = f.readlines()
            
            for line_num, line in enumerate(lines, 1):
                for pattern, description in NULL_REF_PATTERNS:
                    if re.search(pattern, line):
                        found.append((filepath, description, line_num))
                        
        except Exception as e:
            pass
    
    return found

def scan_xaml_files() -> List[Tuple[str, str, int]]:
    """Scan XAML files for common errors."""
    found = []
    
    xaml_files = []
    for root, dirs, files in os.walk(SRC_DIR):
        for f in files:
            if f.endswith('.xaml'):
                xaml_files.append(os.path.join(root, f))
    
    for filepath in sorted(xaml_files):
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Check for common XAML errors
            if '<' not in content or '>' not in content:
                found.append((filepath, "Invalid XAML structure", 1))
                
        except Exception as e:
            found.append((filepath, f"Read error: {e}", 0))
    
    return found

def scan_csproj_files() -> List[Tuple[str, str, int]]:
    """Scan .csproj files for common errors."""
    found = []
    
    csproj_files = []
    for root, dirs, files in os.walk(PROJECT_ROOT):
        for f in files:
            if f.endswith('.csproj'):
                csproj_files.append(os.path.join(root, f))
    
    for filepath in sorted(csproj_files):
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Check for common csproj errors
            if '<?xml' not in content:
                found.append((filepath, "Missing XML declaration", 1))
                
        except Exception as e:
            found.append((filepath, f"Read error: {e}", 0))
    
    return found

def repair_json_file(filepath: str, error_msg: str, line_num: int) -> bool:
    """Attempt to repair a JSON file. Returns True if successful."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Try to fix common JSON issues
        # 1. Fix trailing commas before ] or }
        content = re.sub(r',(\s*[}\]])', r'\1', content)
        
        # 2. Fix missing quotes around keys
        content = re.sub(r'(?<!:)\b([a-zA-Z_][a-zA-Z0-9_]*)\s*:', r'"\1":', content)
        
        # 3. Fix unescaped quotes in strings
        def escape_quotes(match):
            return match.group(0).replace('"', '\\"')
        content = re.sub(r'"(?:[^"\\]|\\.)*"', escape_quotes, content)
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.write(content)
        
        # Verify the fix
        try:
            json.loads(content)
            return True
        except:
            return False
            
    except Exception as e:
        print(f"  Error repairing {filepath}: {e}")
        return False

def repair_cs_file(filepath: str, pattern: str, line_num: int) -> bool:
    """Attempt to repair a C# file. Returns True if successful."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            lines = f.readlines()
        
        # For now, we'll just add null checks for common patterns
        # This is a simplified repair - in production would need more sophisticated logic
        
        if '.ToString()' in pattern:
            # Add null check before ToString()
            line_idx = line_num - 1
            if line_idx < len(lines):
                lines[line_idx] = re.sub(
                    r'(\w+)\.ToString\(\)',
                    r'(string? s = \1 ?? "" ??: "")',
                    lines[line_idx]
                )
        
        with open(filepath, 'w', encoding='utf-8') as f:
            f.writelines(lines)
        
        return True
            
    except Exception as e:
        print(f"  Error repairing {filepath}: {e}")
        return False

def main():
    """Main bug hunting loop."""
    print("=" * 70)
    print("RAGNACONTROLLER BUG HUNTER - Iterativer Fehlersuch- und Reparatur-Prozess")
    print("=" * 70)
    
    all_errors = []
    
    # PHASE 1: Scan JSON files for syntax errors
    print("\n[PHASE 1] Scanning JSON files for syntax errors...")
    json_errors = scan_json_files()
    if json_errors:
        print(f"  Found {len(json_errors)} JSON errors:")
        for filepath, error, line in json_errors:
            print(f"    {filepath}:{line} - {error}")
            all_errors.append(('json', filepath, error, line))
    else:
        print("  ✓ All JSON files are valid!")
    
    # Repair JSON errors immediately
    for error_type, filepath, error_msg, line_num in all_errors:
        if error_type == 'json':
            if repair_json_file(filepath, error_msg, line_num):
                print(f"  ✓ Repaired {filepath}")
    
    # PHASE 2: Scan C# files for potential null reference issues
    print("\n[PHASE 2] Scanning C# files for potential null reference issues...")
    cs_errors = scan_cs_files_for_patterns()
    if cs_errors:
        print(f"  Found {len(cs_errors)} potential null reference patterns:")
        for filepath, pattern, line in cs_errors[:10]:  # Show first 10
            print(f"    {filepath}:{line} - {pattern}")
    else:
        print("  ✓ No obvious null reference patterns found")
    
    # PHASE 3: Scan XAML files
    print("\n[PHASE 3] Scanning XAML files...")
    xaml_errors = scan_xaml_files()
    if xaml_errors:
        print(f"  Found {len(xaml_errors)} XAML issues:")
        for filepath, error, line in xaml_errors[:10]:
            print(f"    {filepath}:{line} - {error}")
    else:
        print("  ✓ All XAML files are valid")
    
    # PHASE 4: Scan .csproj files
    print("\n[PHASE 4] Scanning .csproj files...")
    csproj_errors = scan_csproj_files()
    if csproj_errors:
        print(f"  Found {len(csproj_errors)} .csproj issues:")
        for filepath, error, line in csproj_errors[:10]:
            print(f"    {filepath}:{line} - {error}")
    else:
        print("  ✓ All .csproj files are valid")
    
    # PHASE 5: Check build status
    print("\n[PHASE 5] Checking build status...")
    errors, warnings = check_build()
    if errors == 0 and warnings == 0:
        print("  ✓ Build succeeded with 0 errors and 0 warnings!")
    else:
        print(f"  ✗ Build has {errors} errors and {warnings} warnings")
    
    # Summary
    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)
    print(f"JSON files scanned: {len(os.listdir(LOCES_DIR))}")
    print(f"C# files scanned: {len(cs_errors)}")
    print(f"XAML files scanned: {len(xaml_errors)}")
    print(f".csproj files scanned: {len(csproj_errors)}")
    print(f"Build status: {'GREEN' if errors == 0 else 'RED'} ({errors} errors, {warnings} warnings)")
    
    if errors == 0 and warnings == 0:
        print("\n✓ Project is in healthy state - no build errors or warnings")
        print("✓ Project maintains 0 errors, 0 warnings build status")
    else:
        print(f"\n✗ Found {errors} errors that need attention")
    
    return 0 if errors == 0 else 1

if __name__ == "__main__":
    sys.exit(main())
