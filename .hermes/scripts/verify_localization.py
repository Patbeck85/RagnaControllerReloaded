#!/usr/bin/env python3
"""
RagnaController Localization Verifier
Verifies that all UI strings are properly localized and no hardcoded strings exist.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict, Set

def find_xaml_localization_markers(xaml_content: str) -> List[Dict]:
    """Find XAML localization markers."""
    markers = []
    
    # Pattern for {core:Loc KeyName}
    loc_pattern = r'\{core:Loc\s+(\w+)\}'
    
    for match in re.finditer(loc_pattern, xaml_content):
        key_name = match.group(1)
        line_num = xaml_content[:match.start()].count('\n') + 1
        markers.append({
            'line': line_num,
            'key': key_name,
            'pattern': match.group(0)
        })
    
    return markers

def find_hardcoded_strings(content: str) -> List[Dict]:
    """Find hardcoded strings that should be localized."""
    hardcoded = []
    
    # Pattern for double-quoted strings (excluding comments and known patterns)
    string_pattern = r'["\'][^"\']{10,}["\']'
    
    lines = content.split('\n')
    for line_num, line in enumerate(lines, 1):
        # Skip comments
        if line.strip().startswith('//'):
            continue
        
        # Skip known non-localized strings
        if any(x in line.lower() for x in [
            'xmlns:', '<', '>', '=', '(', ')', '[', ']',
            'x:Class', 'x:Type', 'x:Uid',
            'Window', 'Button', 'TextBlock', 'TextBox',
            'Grid', 'StackPanel', 'Canvas',
            'HorizontalAlignment', 'VerticalAlignment',
            'Margin', 'Padding', 'Width', 'Height',
            'FontSize', 'FontFamily', 'Foreground', 'Background',
            'Opacity', 'CornerRadius', 'BorderThickness',
            'Visibility', 'IsEnabled', 'IsVisible',
            'Source', 'Command', 'DataContext',
            'x:Name', 'x:FieldModifier', 'x:DeferLoadStrategy'
        ]):
            continue
        
        # Check for long strings that might be hardcoded UI text
        matches = re.finditer(string_pattern, line)
        for match in matches:
            string_content = match.group(0)[1:-1]  # Remove quotes
            
            # Skip if it looks like a variable or method call
            if any(x in string_content for x in ['@', '.', '$', '{', '}']):
                continue
            
            # Skip very short strings (likely not UI text)
            if len(string_content) < 15:
                continue
            
            hardcoded.append({
                'line': line_num,
                'string': string_content[:100],  # Truncate long strings
                'full_match': match.group(0)
            })
    
    return hardcoded

def analyze_file_for_localization(file_path: Path) -> Dict:
    """Analyze a single file for localization issues."""
    issues = {
        'file': str(file_path),
        'xaml_markers': [],
        'hardcoded_strings': [],
        'missing_localizations': []
    }
    
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        return {'error': str(e)}
    
    if file_path.suffix == '.xaml':
        issues['xaml_markers'] = find_xaml_localization_markers(content)
    
    issues['hardcoded_strings'] = find_hardcoded_strings(content)
    
    return issues

def analyze_project(project_path: Path) -> Dict:
    """Analyze entire project for localization issues."""
    xaml_files = list(project_path.rglob('*.xaml'))
    cs_files = list(project_path.rglob('*.cs'))
    
    all_issues = []
    file_results = {}
    
    for xaml_file in xaml_files:
        result = analyze_file_for_localization(xaml_file)
        if 'error' not in result:
            file_results[str(xaml_file)] = result
            all_issues.extend(result['xaml_markers'])
            all_issues.extend(result['hardcoded_strings'])
    
    for cs_file in cs_files:
        result = analyze_file_for_localization(cs_file)
        if 'error' not in result:
            file_results[str(cs_file)] = result
            all_issues.extend(result['hardcoded_strings'])
    
    return {
        'xaml_files_analyzed': len(xaml_files),
        'cs_files_analyzed': len(cs_files),
        'total_files_analyzed': len(xaml_files) + len(cs_files),
        'loc_markers_found': len([i for i in all_issues if 'core:Loc' in str(i)]),
        'hardcoded_strings_count': len([i for i in all_issues if 'Hardcoded' in str(i)]),
        'file_results': file_results
    }

def generate_report(analysis: Dict) -> str:
    """Generate a localization report."""
    report = []
    report.append("=" * 70)
    report.append("RAGNACONTROLLER LOCALIZATION VERIFICATION REPORT")
    report.append("=" * 70)
    report.append("")
    report.append(f"XAML Files Analyzed: {analysis['xaml_files_analyzed']}")
    report.append(f"C# Files Analyzed: {analysis['cs_files_analyzed']}")
    report.append(f"Total Files Analyzed: {analysis['total_files_analyzed']}")
    report.append("")
    
    loc_markers = [i for i in analysis['file_results'].values() if i.get('xaml_markers')]
    if loc_markers:
        report.append("-" * 70)
        report.append("✅ XAML LOCALIZATION MARKERS FOUND")
        report.append("-" * 70)
        for file_result in loc_markers:
            if file_result['xaml_markers']:
                report.append(f"\nFile: {file_result['file']}")
                for marker in file_result['xaml_markers'][:10]:  # Limit to first 10
                    report.append(f"  Line {marker['line']}: {marker['pattern']} (Key: {marker['key']})")
        report.append(f"\n... and {len([m for f in loc_markers for m in f['xaml_markers']]) - 10} more markers")
        report.append("")
    
    hardcoded = [i for i in analysis['file_results'].values() if i.get('hardcoded_strings')]
    if hardcoded:
        report.append("-" * 70)
        report.append("⚠️  HARDCODED STRINGS FOUND (Should be localized)")
        report.append("-" * 70)
        for file_result in hardcoded:
            if file_result['hardcoded_strings']:
                report.append(f"\nFile: {file_result['file']}")
                for item in file_result['hardcoded_strings'][:5]:  # Limit to first 5
                    truncated = item['string'] if len(item['string']) <= 50 else item['string'][:47] + "..."
                    report.append(f"  Line {item['line']}: \"{truncated}\"")
        report.append("")
    
    report.append("=" * 70)
    report.append("RECOMMENDATIONS:")
    report.append("=" * 70)
    report.append("")
    report.append("1. All UI strings should use {core:Loc KeyName} pattern in XAML")
    report.append("2. C# code should use LocalizationManager.Instance[\"KeyName\"]")
    report.append("3. Add all new English strings to Locales/en.json")
    report.append("4. Create translations for other languages (de.json, th.json, etc.)")
    report.append("5. Use string interpolation with Loc keys: {core:Loc Key_Arg1={0}}")
    report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: verify_localization.py <project_path>")
        print("Example: verify_localization.py /mnt/c/RagnaController/src/RagnaController")
        sys.exit(1)
    
    project_path = Path(sys.argv[1])
    
    if not project_path.exists():
        print(f"Error: Path not found: {project_path}")
        sys.exit(1)
    
    print(f"Verifying localization in: {project_path}")
    analysis = analyze_project(project_path)
    
    report = generate_report(analysis)
    print(report)
    
    # Save report to file
    report_file = project_path.parent / "localization_verification_report.txt"
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())
