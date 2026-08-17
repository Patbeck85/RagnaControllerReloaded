#!/usr/bin/env python3
"""
RagnaController Build Error Analyzer
Analyzes build output and provides structured error reports with fix suggestions.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict, Tuple

def parse_dotnet_build_output(output: str) -> Tuple[List[Dict], List[Dict]]:
    """Parse dotnet build output and extract errors/warnings."""
    errors = []
    warnings = []
    
    # Error pattern: error CSxxxxx : [filename](line,col) - [message]
    error_pattern = r'error\s+CS\d+:\s+(.+?)\((\d+),(\d+)\)\s+-\s+(.+?)(?:\s+\-\-\>\s+.+)?(?:\s+in\s+.+)?'
    
    # Warning pattern: warning CSxxxxx : [filename](line,col) - [message]
    warning_pattern = r'warning\s+CS\d+:\s+(.+?)\((\d+),(\d+)\)\s+-\s+(.+?)(?:\s+\-\-\>\s+.+)?(?:\s+in\s+.+)?'
    
    for match in re.finditer(error_pattern, output, re.MULTILINE):
        errors.append({
            'code': match.group(1),
            'file': match.group(2),
            'line': int(match.group(3)),
            'column': int(match.group(4)),
            'message': match.group(5).strip()
        })
    
    for match in re.finditer(warning_pattern, output, re.MULTILINE):
        warnings.append({
            'code': match.group(1),
            'file': match.group(2),
            'line': int(match.group(3)),
            'column': int(match.group(4)),
            'message': match.group(5).strip()
        })
    
    return errors, warnings

def categorize_errors(errors: List[Dict]) -> Dict[str, List[Dict]]:
    """Categorize errors by type."""
    categories = {
        'NullReference': [],
        'UsingStaticName': [],
        'FieldWarnings': [],
        'BuildTarget': [],
        'Other': []
    }
    
    for error in errors:
        msg = error['message'].lower()
        if 'using static' in msg and 'name' in msg:
            categories['UsingStaticName'].append(error)
        elif 'cs0649' in error.get('code', '') or 'field is never assigned' in msg:
            categories['FieldWarnings'].append(error)
        elif 'build target' in msg or 'bundle' in msg:
            categories['BuildTarget'].append(error)
        elif 'null reference' in msg:
            categories['NullReference'].append(error)
        else:
            categories['Other'].append(error)
    
    return categories

def generate_fix_suggestions(errors: List[Dict]) -> str:
    """Generate fix suggestions for errors."""
    suggestions = []
    
    for error in errors:
        msg = error['message']
        file_hint = error.get('file', '').split('/')[-1].replace('.cs', '')
        
        if 'using static' in msg.lower() and 'name' in msg.lower():
            suggestions.append("### " + file_hint + " - Using Static Name Conflict\n")
            suggestions.append("**Error:** " + error['message'] + "\n")
            suggestions.append("**Fix:** Rename the conflicting static member or use fully qualified name.\n")
            suggestions.append("Example:\n")
            suggestions.append("```csharp\n")
            suggestions.append("// Instead of:\n")
            suggestions.append("MyNamespace.SomeClass.SomeProperty\n")
            suggestions.append("\n")
            suggestions.append("// Use:\n")
            suggestions.append("MyNamespace.SomeClass.SomeProperty // with explicit namespace\n")
            suggestions.append("or\n")
            suggestions.append("SomeOtherNamespace.SomeClass.SomeProperty // if in different namespace\n")
            suggestions.append("```\n")
            
        elif 'field is never assigned' in msg.lower() or 'cs0649' in error.get('code', ''):
            suggestions.append("### " + file_hint + " - Unassigned Field Warning\n")
            suggestions.append("**Error:** " + error['message'] + "\n")
            suggestions.append("**Fix:** Either assign the field or suppress the warning if it's expected (e.g., XAML fields).\n")
            suggestions.append("Example:\n")
            suggestions.append("```csharp\n")
            suggestions.append("// Option 1: Assign in constructor\n")
            suggestions.append("private string _field;\n")
            suggestions.append("public MyView()\n")
            suggestions.append("{\n")
            suggestions.append("    _field = \"value\";\n")
            suggestions.append("}\n")
            suggestions.append("\n")
            suggestions.append("// Option 2: Suppress for XAML fields\n")
            suggestions.append("[SuppressMessage(\"Usage\", \"CS0649\", Justification = \"XAML field\")]\n")
            suggestions.append("private string _field;\n")
            suggestions.append("```\n")
            
        elif 'build target' in msg.lower() or 'bundle' in msg.lower():
            suggestions.append("### " + file_hint + " - Build Target Error\n")
            suggestions.append("**Error:** " + error['message'] + "\n")
            suggestions.append("**Fix:** Remove post-build zip targets or disable bundle generation.\n")
            suggestions.append("In .csproj:\n")
            suggestions.append("```xml\n")
            suggestions.append("<PropertyGroup>\n")
            suggestions.append("  <PublishTrimmed>false</PublishTrimmed>\n")
            suggestions.append("  <GenerateBundle>false</GenerateBundle>\n")
            suggestions.append("</PropertyGroup>\n")
            suggestions.append("```\n")
    
    return '\n\n'.join(suggestions)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: analyze_build_errors.py <build_output.txt>")
        sys.exit(1)
    
    output_file = Path(sys.argv[1])
    
    if not output_file.exists():
        print("Error: File not found: " + str(output_file))
        sys.exit(1)
    
    with open(output_file, 'r', encoding='utf-8') as f:
        output = f.read()
    
    errors, warnings = parse_dotnet_build_output(output)
    
    if not errors and not warnings:
        print("No errors or warnings found in build output.")
        sys.exit(0)
    
    print("Found " + str(len(errors)) + " errors and " + str(len(warnings)) + " warnings\n")
    
    # Categorize errors
    categorized = categorize_errors(errors)
    
    for category, error_list in categorized.items():
        if error_list:
            print("\n=== " + category + " (" + str(len(error_list)) + ") ===\n")
            for error in error_list:
                print("File: " + error['file'])
                print("Line: " + str(error['line']) + ", Column: " + str(error['column']))
                print("Code: " + error['code'])
                print("Message: " + error['message'] + "\n")
    
    # Generate fix suggestions
    if categorized['UsingStaticName'] or categorized['FieldWarnings'] or categorized['BuildTarget']:
        print("\n" + "="*60)
        print("FIX SUGGESTIONS:")
        print("="*60)
        print(generate_fix_suggestions(errors))
    
    return 0

if __name__ == '__main__':
    sys.exit(main())
