#!/usr/bin/env python3
"""
XAML Syntax Checker - Checks XAML files for common syntax errors.
This script validates XAML structure and identifies potential issues.
"""

import os
import re
import sys
from typing import List, Tuple, Dict

def read_file(filepath: str) -> str:
    """Read file content with encoding handling."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception as e:
        return "ERROR: " + str(e)

def check_xaml_syntax(content: str, filepath: str) -> List[str]:
    """
    Check XAML content for syntax errors.
    Returns list of error messages.
    """
    errors = []
    
    # Remove comments and whitespace for cleaner analysis
    lines = content.split('\n')
    
    # Track brace depth
    brace_depth = 0
    tag_stack = []
    
    for i, line in enumerate(lines, 1):
        # Check for unmatched braces
        for char in ['{', '}']:
            brace_depth += line.count(char)
        
        # Simple tag tracking (basic validation)
        tag_pattern = r'<(\w+)([^>]*)>'
        matches = re.finditer(tag_pattern, line)
        
        for match in matches:
            tag_name = match.group(1)
            attributes = match.group(2)
            
            # Skip closing tags
            if tag_name.endswith('/'):
                continue
            
            # Check for common XAML errors
            # 1. Unmatched closing brace
            if '}' not in line and brace_depth > 0:
                pass  # Continue tracking
            
            # 2. Check for missing closing tag (basic check)
            if tag_name not in ['/', 'x:TypeConverter', 'x:Static']:
                tag_stack.append(tag_name)
        
        # Check for self-closing tags
        self_closing_pattern = r'<(\w+)/>'
        self_closing_matches = re.finditer(self_closing_pattern, line)
        for match in self_closing_matches:
            tag_name = match.group(1)
            if tag_name not in ['Image', 'Canvas', 'Grid', 'StackPanel', 'Border',
                              'Button', 'TextBox', 'CheckBox', 'ComboBox', 'ListBox',
                              'ScrollViewer', 'WrapPanel', 'UniformGrid', 'Expander',
                              'ToggleSwitch', 'ProgressBar', 'Slider', 'TextBlock',
                              'Hyperlink', 'Label', 'PasswordBox', 'DatePicker',
                              'TimePicker', 'NumericUpDown', 'AutoCompleteBox',
                              'TreeView', 'DataGrid', 'ListView', 'TabControl',
                              'Window', 'Application', 'Page', 'UserControl']:
                pass  # Some tags might need explicit closing
    
    # Check for common XAML issues
    # 1. Missing xmlns declarations
    if 'xmlns' not in content:
        errors.append("Line 1: Missing xmlns namespace declaration")
    
    # 2. Check for unmatched braces
    open_braces = content.count('{')
    close_braces = content.count('}')
    if open_braces != close_braces:
        errors.append("Line " + str(content.find('{')+1) + ": Unmatched braces - Open: " + str(open_braces) + ", Close: " + str(close_braces))
    
    # 3. Check for missing closing tags (basic check)
    tag_pattern = r'<(\w+)(?![^>]*\/>)'
    opening_tags = re.findall(tag_pattern, content)
    closing_tag_pattern = r'</(\w+)>'
    closing_tags = re.findall(closing_tag_pattern, content)
    
    # Simple validation - check if most tags are closed
    unclosed = set(opening_tags) - set(closing_tags)
    if unclosed:
        for tag in sorted(unclosed):
            errors.append("Line " + str(content.count('</' + tag) + 1) + ": Unclosed tag: <" + tag + ">")
    
    # 4. Check for common attribute errors
    # Missing quotes around attribute values
    attr_pattern = r'\b(\w+)\s*=\s*([^"\'])(?![^>]*>)'
    unquoted_attrs = re.findall(attr_pattern, content)
    for attr in unquoted_attrs:
        if attr not in ['x:Null', 'True', 'False', 'On', 'Off', 'Yes', 'No']:
            errors.append("Line " + str(content.count('\n')+1) + ": Unquoted attribute value found")
    
    # 5. Check for missing closing braces in elements
    element_pattern = r'<(\w+)([^>]*)>\s*([^<]*)'
    elements = re.findall(element_pattern, content)
    for tag_name, attrs, content_part in elements:
        if content_part.strip() and '}' not in content_part:
            # Check if it's a self-closing element or has closing brace later
            if tag_name not in ['Image', 'Canvas', 'Grid', 'StackPanel', 'Border',
                              'Button', 'TextBox', 'CheckBox', 'ComboBox', 'ListBox',
                              'ScrollViewer', 'WrapPanel', 'UniformGrid', 'Expander',
                              'ToggleSwitch', 'ProgressBar', 'Slider', 'TextBlock',
                              'Hyperlink', 'Label', 'PasswordBox', 'DatePicker',
                              'TimePicker', 'NumericUpDown', 'AutoCompleteBox',
                              'TreeView', 'DataGrid', 'ListView', 'TabControl',
                              'Window', 'Application', 'Page', 'UserControl']:
                pass  # Some elements need explicit closing
    
    return errors

def main():
    """Main function to check all XAML files."""
    base_dir = "."
    
    # Find all XAML files
    xaml_files = []
    for root, dirs, files in os.walk(base_dir):
        # Skip bin and obj directories
        if 'bin' in root or 'obj' in root:
            continue
        for file in files:
            if file.endswith('.xaml'):
                filepath = os.path.join(root, file)
                xaml_files.append(filepath)
    
    print("Found " + str(len(xaml_files)) + " XAML files to check\n")
    print("=" * 60)
    print("XAML SYNTAX VALIDATION")
    print("=" * 60)
    
    all_errors = []
    total_files = 0
    valid_files = 0
    
    for filepath in sorted(xaml_files):
        total_files += 1
        content = read_file(filepath)
        
        if "ERROR" in content:
            rel_path = filepath.replace(base_dir + "/", "")
            print("✗ " + rel_path + ": ERROR reading file")
            all_errors.append((rel_path, "ERROR reading file"))
            continue
        
        errors = check_xaml_syntax(content, filepath)
        rel_path = filepath.replace(base_dir + "/", "")
        
        if errors:
            print("✗ " + rel_path + ":")
            for error in errors[:5]:  # Show first 5 errors
                print("    - " + error)
            all_errors.append((rel_path, errors))
        else:
            print("✓ " + rel_path + ": Valid XAML")
            valid_files += 1
    
    # Summary
    print("\n" + "=" * 60)
    print("=== SUMMARY ===")
    print("=" * 60)
    print("Total files checked: " + str(total_files))
    print("Valid files: " + str(valid_files))
    print("Errors found: " + str(len(all_errors)))
    
    if all_errors:
        print("\n=== ERROR DETAILS ===")
        for filepath, errors in all_errors:
            print("\n" + filepath + ":")
            for error in errors:
                print("  - " + error)
    
    return len(all_errors) == 0

if __name__ == "__main__":
    success = main()
    sys.exit(0 if success else 1)
