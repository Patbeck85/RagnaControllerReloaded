#!/usr/bin/env python3
"""
RagnaController UI Inspector
Inspects XAML files for UI/UX issues, accessibility problems, and layout validation.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict

def analyze_xaml_file(file_path: Path) -> Dict:
    """Analyze a single XAML file for UI/UX issues."""
    issues = {
        'file': str(file_path),
        'missing_accessibility': [],
        'layout_issues': [],
        'hardcoded_strings': [],
        'missing_names': [],
        'nested_grid_issues': [],
        'visual_state_missing': []
    }
    
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        return {'error': str(e)}
    
    lines = content.split('\n')
    
    for line_num, line in enumerate(lines, 1):
        # Check for missing accessibility (AutomationProperties)
        if re.search(r'<(TextBlock|Button|TextBox|ListBoxItem)', line):
            # Check for AutomationProperties.Name or Header
            if not any(x in line for x in ['AutomationProperties.Name', 'Header=', 'x:Uid']):
                issues['missing_accessibility'].append({
                    'line': line_num,
                    'element': line.strip()[:80],
                    'issue': 'Missing accessibility name/label'
                })
        
        # Check for hardcoded strings in UI elements
        string_pattern = r'["\'][^"\']{15,}["\']'
        matches = re.finditer(string_pattern, line)
        for match in matches:
            string_content = match.group(0)[1:-1]
            # Skip if it looks like a variable or method call
            if not any(x in string_content for x in ['@', '.', '$', '{', '}']):
                issues['hardcoded_strings'].append({
                    'line': line_num,
                    'string': string_content[:80],
                    'issue': 'Hardcoded UI string - should be localized'
                })
        
        # Check for missing x:Name on interactive elements
        interactive_elements = ['Button', 'CheckBox', 'RadioButton', 'ComboBox', 'ListBox']
        for element in interactive_elements:
            if f'<{element}' in line and 'x:Name=' not in line:
                issues['missing_names'].append({
                    'line': line_num,
                    'element': element,
                    'issue': 'Missing x:Name attribute'
                })
        
        # Check for nested Grid without RowDefinitions/ColumnDefinitions
        if re.search(r'<Grid[^>]*>', line):
            # Look ahead to see if RowDefinitions or ColumnDefinitions exist
            context = '\n'.join(lines[line_num-1:line_num+20])
            if '<Grid' in context and 'RowDefinitions' not in context and 'ColumnDefinitions' not in context:
                # Check if it has explicit Width/Height which might indicate layout issues
                if re.search(r'(Width|Height)\s*=\s*["\'][^"\']{10,}["\']', context):
                    issues['nested_grid_issues'].append({
                        'line': line_num,
                        'issue': 'Nested Grid without Row/ColumnDefinitions'
                    })
        
        # Check for missing VisualStateManager or TransitioningContentGroup
        if re.search(r'<(TextBlock|Button|TextBox)', line):
            context = '\n'.join(lines[line_num-1:line_num+30])
            if 'VisualStateManager' not in context and 'TransitioningContentGroup' not in context:
                # This might be OK for simple UI, but worth noting for complex states
                pass  # Don't flag as issue unless it's clearly needed
        
        # Check for potential layout issues with absolute positioning
        if re.search(r'<Canvas', line):
            issues['layout_issues'].append({
                'line': line_num,
                'element': 'Canvas',
                'issue': 'Canvas used - consider Grid with Row/ColumnDefinitions'
            })
        
        # Check for DockPanel which can cause layout issues
        if re.search(r'<DockPanel', line):
            issues['layout_issues'].append({
                'line': line_num,
                'element': 'DockPanel',
                'issue': 'DockPanel - consider Grid for better layout control'
            })
    
    return issues

def analyze_project(project_path: Path) -> Dict:
    """Analyze entire project for UI/UX issues."""
    xaml_files = list(project_path.rglob('*.xaml'))
    
    all_issues = []
    file_results = {}
    
    for xaml_file in xaml_files:
        result = analyze_xaml_file(xaml_file)
        if 'error' not in result:
            file_results[str(xaml_file)] = result
            all_issues.extend(result['missing_accessibility'])
            all_issues.extend(result['hardcoded_strings'])
            all_issues.extend(result['missing_names'])
            all_issues.extend(result['nested_grid_issues'])
            all_issues.extend(result['layout_issues'])
    
    return {
        'xaml_files_analyzed': len(xaml_files),
        'issues_found': len(all_issues),
        'missing_accessibility_count': len([i for i in all_issues if 'Accessibility' in str(i)]),
        'hardcoded_string_count': len([i for i in all_issues if 'Hardcoded' in str(i)]),
        'missing_name_count': len([i for i in all_issues if 'Name' in str(i)]),
        'layout_issue_count': len([i for i in all_issues if 'Layout' in str(i)]),
        'file_results': file_results
    }

def generate_report(analysis: Dict) -> str:
    """Generate a UI/UX inspection report."""
    report = []
    report.append("=" * 70)
    report.append("RAGNACONTROLLER UI/UX INSPECTION REPORT")
    report.append("=" * 70)
    report.append("")
    report.append(f"XAML Files Analyzed: {analysis['xaml_files_analyzed']}")
    report.append(f"Total Issues Found: {analysis['issues_found']}")
    report.append("")
    
    if analysis['missing_accessibility_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  MISSING ACCESSIBILITY LABELS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('missing_accessibility'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['missing_accessibility']:
                    report.append(f"  Line {item['line']}: {item['issue']}")
                    report.append(f"    {item['element']}")
        report.append("")
    
    if analysis['hardcoded_string_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  HARDCODED STRINGS (Should be localized)")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('hardcoded_strings'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['hardcoded_strings'][:10]:  # Limit to first 10
                    truncated = item['string'] if len(item['string']) <= 50 else item['string'][:47] + "..."
                    report.append(f"  Line {item['line']}: \"{truncated}\"")
        report.append("")
    
    if analysis['missing_name_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  MISSING X:NAME ATTRIBUTES")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('missing_names'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['missing_names']:
                    report.append(f"  Line {item['line']}: {item['element']} - {item['issue']}")
        report.append("")
    
    if analysis['layout_issue_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  LAYOUT ISSUES")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('nested_grid_issues'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['nested_grid_issues']:
                    report.append(f"  Line {item['line']}: {item['issue']}")
            if issue.get('layout_issues'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['layout_issues']:
                    report.append(f"  Line {item['line']}: {item['element']} - {item['issue']}")
        report.append("")
    
    report.append("=" * 70)
    report.append("RECOMMENDATIONS:")
    report.append("=" * 70)
    report.append("")
    report.append("1. Add AutomationProperties.Name to all interactive elements")
    report.append("2. Use {core:Loc KeyName} for all UI strings")
    report.append("3. Add x:Name to all Button, CheckBox, ComboBox, ListBox elements")
    report.append("4. Use Grid with RowDefinitions/ColumnDefinitions instead of Canvas/DockPanel")
    report.append("5. Consider VisualStateManager for complex UI states")
    report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: inspect_ui.py <project_path>")
        print("Example: inspect_ui.py /mnt/c/RagnaController/src/RagnaController")
        sys.exit(1)
    
    project_path = Path(sys.argv[1])
    
    if not project_path.exists():
        print(f"Error: Path not found: {project_path}")
        sys.exit(1)
    
    print(f"Inspecting UI in: {project_path}")
    analysis = analyze_project(project_path)
    
    report = generate_report(analysis)
    print(report)
    
    # Save report to file
    report_file = project_path.parent / "ui_inspection_report.txt"
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())
