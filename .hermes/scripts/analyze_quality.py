#!/usr/bin/env python3
"""
RagnaController Code Quality Auditor
Analyzes code for quality issues, null reference risks, race conditions, and anti-patterns.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict, Set

def analyze_file_for_quality(file_path: Path) -> Dict:
    """Analyze a single file for quality issues."""
    issues = {
        'file': str(file_path),
        'null_reference_risks': [],
        'race_conditions': [],
        'unsafe_patterns': [],
        'magic_numbers': [],
        'hardcoded_strings': [],
        'complex_expressions': []
    }
    
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        return {'error': str(e)}
    
    lines = content.split('\n')
    
    for line_num, line in enumerate(lines, 1):
        # Check for NullReferenceException risks
        null_patterns = [
            (r'\.ToString\s*\(\s*\)', 'ToString() on potentially null'),
            (r'\.Length\s*\(\s*\)', 'Length() on potentially null'),
            (r'\.Count\s*\(\s*\)', 'Count() on potentially null'),
            (r'\.Contains\s*\(', 'Contains() on potentially null'),
            (r'\.IndexOf\s*\(', 'IndexOf() on potentially null'),
            (r'\.LastIndexOf\s*\(', 'LastIndexOf() on potentially null'),
            (r'\.Split\s*\(', 'Split() on potentially null'),
            (r'\.Substring\s*\(', 'Substring() on potentially null'),
            (r'\.Trim\s*\(', 'Trim() on potentially null'),
            (r'\.ToUpper\s*\(', 'ToUpper() on potentially null'),
            (r'\.ToLower\s*\(', 'ToLower() on potentially null'),
            (r'\.PadLeft\s*\(', 'PadLeft() on potentially null'),
            (r'\.PadRight\s*\(', 'PadRight() on potentially null'),
            (r'\.Insert\s*\(', 'Insert() on potentially null'),
            (r'\.Replace\s*\(', 'Replace() on potentially null'),
            (r'\.StartsWith\s*\(', 'StartsWith() on potentially null'),
            (r'\.EndsWith\s*\(', 'EndsWith() on potentially null'),
            (r'\.CompareTo\s*\(', 'CompareTo() on potentially null'),
            (r'\.Equals\s*\(', 'Equals() on potentially null'),
            (r'\.GetHashCode\s*\(\s*\)', 'GetHashCode() on potentially null'),
            (r'\.GetType\s*\(\s*\)', 'GetType() on potentially null'),
            (r'\.InvokeMember\s*\(', 'InvokeMember() on potentially null'),
            (r'\.Call\s*\(', 'Call() on potentially null'),
            (r'\.BeginInvoke\s*\(', 'BeginInvoke() on potentially null'),
            (r'\.EndInvoke\s*\(', 'EndInvoke() on potentially null'),
            (r'\.GetInterface\s*\(', 'GetInterface() on potentially null'),
            (r'\.GetMember\s*\(', 'GetMember() on potentially null'),
            (r'\.GetMethod\s*\(', 'GetMethod() on potentially null'),
            (r'\.GetType\s*\(\s*\)\.GetMethod\s*\(', 'GetType().GetMethod() on potentially null'),
            (r'\.GetProperty\s*\(', 'GetProperty() on potentially null'),
            (r'\.GetField\s*\(', 'GetField() on potentially null'),
            (r'\.GetEvent\s*\(', 'GetEvent() on potentially null'),
            (r'\.GetTypeInfo\s*\(', 'GetTypeInfo() on potentially null'),
            (r'\.GetProperties\s*\(', 'GetProperties() on potentially null'),
            (r'\.GetFields\s*\(', 'GetFields() on potentially null'),
            (r'\.GetMethods\s*\(', 'GetMethods() on potentially null'),
            (r'\.GetInterfaces\s*\(', 'GetInterfaces() on potentially null'),
            (r'\.GetBaseType\s*\(', 'GetBaseType() on potentially null'),
            (r'\.IsSubclassOf\s*\(', 'IsSubclassOf() on potentially null'),
            (r'\.IsInstanceOfType\s*\(', 'IsInstanceOfType() on potentially null'),
            (r'\.MakeGenericMethod\s*\(', 'MakeGenericMethod() on potentially null'),
            (r'\.CreateInstance\s*\(', 'CreateInstance() on potentially null'),
            (r'\.GetMethod\s*\(\s*\)\.Invoke\s*\(', 'GetMethod().Invoke() on potentially null'),
            (r'\.GetProperty\s*\(\s*\)\.GetValue\s*\(', 'GetProperty().GetValue() on potentially null'),
            (r'\.GetField\s*\(\s*\)\.GetValue\s*\(', 'GetField().GetValue() on potentially null'),
        ]
        
        for pattern, description in null_patterns:
            if re.search(pattern, line):
                # Skip if already protected by null check
                if not any(x in line for x in ['?.', '!= null', '== null', 'is not null', 'is null']):
                    issues['null_reference_risks'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description
                    })
        
        # Check for race conditions in multi-threaded code
        race_patterns = [
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Count', 'Static counter without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Index', 'Static index without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Position', 'Static position without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Offset', 'Static offset without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Value', 'Static value without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*State', 'Static state without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Mode', 'Static mode without lock'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?(?:int\s+)?_?\w*Flag', 'Static flag without lock'),
        ]
        
        for pattern, description in race_patterns:
            if re.search(pattern, line):
                # Check if there's a lock around it
                if not any(x in content[max(0, content.rfind('\n', 0, content.find(line))):content.find(line)+500] 
                          for x in ['lock (', 'Monitor.', 'Interlocked.']):
                    issues['race_conditions'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description
                    })
        
        # Check for unsafe patterns
        unsafe_patterns = [
            (r'unsafe\s*\{', 'Unsafe block'),
            (r'fixed\s*\(', 'Fixed statement'),
            (r'stackalloc\s+', 'Stack allocation'),
            (r'fixed\s*\(\s*(.+)\s+\^', 'Fixed pointer'),
        ]
        
        for pattern, description in unsafe_patterns:
            if re.search(pattern, line):
                issues['unsafe_patterns'].append({
                    'line': line_num,
                    'code': line.strip(),
                    'pattern': description
                })
        
        # Check for magic numbers
        magic_number_pattern = r'(?<!\d)(?:[0-9]{2,})(?!\d)'
        if re.search(magic_number_pattern, line):
            # Skip common constants
            if not any(x in line.lower() for x in ['version', 'max', 'min', 'size', 'capacity', 'count']):
                issues['magic_numbers'].append({
                    'line': line_num,
                    'code': line.strip()
                })
        
        # Check for hardcoded strings that should be localized
        if re.search(r'["\'][^"\']{20,}["\']', line):
            # Skip comments and known non-localized strings
            if not line.strip().startswith('//') and 'LocalizationManager' not in line:
                issues['hardcoded_strings'].append({
                    'line': line_num,
                    'code': line.strip()
                })
    
    return issues

def analyze_project(project_path: Path) -> Dict:
    """Analyze entire project for quality issues."""
    cs_files = list(project_path.rglob('*.cs'))
    
    all_issues = []
    file_results = {}
    
    for cs_file in cs_files:
        result = analyze_file_for_quality(cs_file)
        if 'error' not in result:
            file_results[str(cs_file)] = result
            all_issues.extend(result['null_reference_risks'])
            all_issues.extend(result['race_conditions'])
            all_issues.extend(result['unsafe_patterns'])
            all_issues.extend(result['magic_numbers'])
            all_issues.extend(result['hardcoded_strings'])
    
    return {
        'files_analyzed': len(cs_files),
        'issues_found': len(all_issues),
        'null_reference_count': len([i for i in all_issues if 'Null' in str(i)]),
        'race_condition_count': len([i for i in all_issues if 'Race' in str(i)]),
        'unsafe_pattern_count': len([i for i in all_issues if 'Unsafe' in str(i)]),
        'magic_number_count': len([i for i in all_issues if 'Magic' in str(i)]),
        'hardcoded_string_count': len([i for i in all_issues if 'Hardcoded' in str(i)]),
        'file_results': file_results
    }

def generate_report(analysis: Dict) -> str:
    """Generate a code quality report."""
    report = []
    report.append("=" * 70)
    report.append("RAGNACONTROLLER CODE QUALITY AUDIT REPORT")
    report.append("=" * 70)
    report.append("")
    report.append(f"Files Analyzed: {analysis['files_analyzed']}")
    report.append(f"Total Issues Found: {analysis['issues_found']}")
    report.append("")
    
    if analysis['null_reference_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  NULL REFERENCE RISKS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('null_reference_risks'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['null_reference_risks']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['race_condition_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  RACE CONDITIONS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('race_conditions'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['race_conditions']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['unsafe_pattern_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  UNSAFE PATTERNS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('unsafe_patterns'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['unsafe_patterns']:
                    report.append(f"  Line {item['line']}: {item['pattern']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['magic_number_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  MAGIC NUMBERS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('magic_numbers'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['magic_numbers']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    if analysis['hardcoded_string_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  HARDCODED STRINGS (Should be localized)")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('hardcoded_strings'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['hardcoded_strings']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    report.append("=" * 70)
    report.append("RECOMMENDATIONS:")
    report.append("=" * 70)
    report.append("")
    report.append("1. Add null checks before calling methods on potentially null objects")
    report.append("2. Use nullable reference types (?.) for safe navigation")
    report.append("3. Protect shared state with locks or use thread-safe collections")
    report.append("4. Replace magic numbers with named constants")
    report.append("5. Move hardcoded strings to localization files")
    report.append("6. Review unsafe blocks and ensure they're necessary")
    report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: analyze_quality.py <project_path>")
        print("Example: analyze_quality.py /mnt/c/RagnaController/src/RagnaController")
        sys.exit(1)
    
    project_path = Path(sys.argv[1])
    
    if not project_path.exists():
        print(f"Error: Path not found: {project_path}")
        sys.exit(1)
    
    print(f"Analyzing project: {project_path}")
    analysis = analyze_project(project_path)
    
    report = generate_report(analysis)
    print(report)
    
    # Save report to file
    report_file = project_path.parent / "code_quality_audit_report.txt"
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())
