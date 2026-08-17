#!/usr/bin/env python3
import os
import re
import sys

def check_csharp_syntax(file_path):
    """Check C# file for common syntax errors."""
    errors = []
    warnings = []
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.split('\n')
            
            for i, line in enumerate(lines, 1):
                # Check for missing semicolons at end of statements (common issues)
                stripped = line.strip()
                
                # Skip empty lines, comments, and braces
                if not stripped or stripped.startswith('//') or stripped.startswith('/*'):
                    continue
                
                # Check for incomplete statements (missing semicolon before closing brace)
                if re.search(r'\b(if|while|for|foreach|catch)\s*\{', line):
                    # Block start, check next lines for missing semicolons
                    pass
                
                # Check for common syntax issues
                if re.search(r'\breturn\s+[^;]*$', stripped) and not stripped.endswith('//'):
                    if not stripped.endswith('{') and not stripped.endswith('}'):
                        errors.append((i, "Missing semicolon after return statement", line))
                
                if re.search(r'\bthrow\s+[^;]*$', stripped) and not stripped.endswith('//'):
                    if not stripped.endswith('{') and not stripped.endswith('}'):
                        errors.append((i, "Missing semicolon after throw statement", line))
                
                # Check for incomplete property access
                if re.search(r'\.\w+\s*$', stripped) and not stripped.endswith(';') and not stripped.endswith('//'):
                    if not stripped.endswith('{') and not stripped.endswith('}'):
                        errors.append((i, "Possible missing semicolon after property access", line))
                
                # Check for incomplete method calls
                if re.search(r'\)\s*$', stripped) and not stripped.endswith(';') and not stripped.endswith('//'):
                    if not stripped.endswith('{') and not stripped.endswith('}'):
                        errors.append((i, "Possible missing semicolon after method call", line))
                
            # Check for using statement issues
            if 'using' in content.lower():
                # Basic check - would need more sophisticated parsing for full analysis
                pass
                
    except Exception as e:
        errors.append((0, f"Error reading file: {str(e)}", ""))
    
    return errors, warnings

# Find all C# files in the project (excluding bin/obj)
csharp_files = []
for root, dirs, files in os.walk('/mnt/c/RagnaController/src/RagnaController'):
    # Skip bin and obj directories
    if 'bin' in root or 'obj' in root:
        continue
    for file in files:
        if file.endswith('.cs'):
            csharp_files.append(os.path.join(root, file))

print(f"Found {len(csharp_files)} C# files to check")

# Check each C# file for syntax errors
all_errors = []
for file_path in csharp_files:
    errors, warnings = check_csharp_syntax(file_path)
    for line_num, message, code in errors:
        all_errors.append({
            'file': file_path,
            'line': line_num,
            'message': message,
            'code': code
        })

if all_errors:
    print(f"\n\nTotal C# syntax errors found: {len(all_errors)}")
    for error in all_errors[:20]:  # Show first 20 errors
        print(f"\nERROR in {error['file']} at line {error['line']}:")
        print(f"  {error['message']}")
        if error['code']:
            print(f"  {error['code'][:100]}...")
else:
    print("\nNo C# syntax errors found in any files!")
