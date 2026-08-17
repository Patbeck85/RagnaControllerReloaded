#!/usr/bin/env python3
"""
RagnaController Performance Analyzer
Analyzes code for performance issues in the 125Hz tick loop.
Checks for Thread.Sleep, LINQ, allocations, and other anti-patterns.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict, Set

def analyze_file_for_performance(file_path: Path) -> Dict:
    """Analyze a single file for performance issues."""
    issues = {
        'file': str(file_path),
        'thread_sleep': [],
        'linq_in_tick': [],
        'allocations': [],
        'blocking_calls': [],
        'gc_pressure': []
    }
    
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        return {'error': str(e)}
    
    # Check for Thread.Sleep in tick/update methods
    tick_patterns = [
        r'(?:public\s+)?(?:void\s+)?(?:OnTick|Update|Tick)\s*\([^)]*\)\s*\{',
        r'(?:public\s+)?(?:void\s+)?(?:ProcessInput|HandleInput|Execute)\s*\([^)]*\)\s*\{'
    ]
    
    in_tick_method = False
    tick_content = []
    brace_count = 0
    
    for line in content.split('\n'):
        if any(re.search(pattern, line) for pattern in tick_patterns):
            in_tick_method = True
            brace_count = line.count('{') - line.count('}')
            continue
        
        if in_tick_method:
            tick_content.append(line)
            brace_count += line.count('{') - line.count('}')
            
            if brace_count <= 0 and '{' in content[content.find(line):]:
                # Check if we're still in the method
                method_start = content.rfind('{', 0, content.find(line))
                method_end = content.find('}', method_start)
                if method_end > -1:
                    in_tick_method = False
                    tick_content = []
        
        if in_tick_method:
            # Check for Thread.Sleep
            if re.search(r'Thread\.Sleep\s*\(', line):
                issues['thread_sleep'].append({
                    'line': tick_content.index(line) + 1,
                    'code': line.strip()
                })
            
            # Check for LINQ
            linq_methods = ['.Where(', '.Select(', '.ToList(', '.Any(', '.First(', '.Last(', '.Count(']
            for method in linq_methods:
                if method in line:
                    issues['linq_in_tick'].append({
                        'line': tick_content.index(line) + 1,
                        'code': line.strip()
                    })
            
            # Check for allocations
            alloc_patterns = [
                r'new\s+\w+',
                r'\.Clone\s*\(',
                r'\.CopyTo\s*\('
            ]
            for pattern in alloc_patterns:
                if re.search(pattern, line):
                    issues['allocations'].append({
                        'line': tick_content.index(line) + 1,
                        'code': line.strip()
                    })
            
            # Check for blocking calls
            blocking_patterns = [
                r'Task\.Run\s*\(',
                r'await\s+\w+\.Read',
                r'File\.Read',
                r'Http(?:Client|WebRequest)\.Get'
            ]
            for pattern in blocking_patterns:
                if re.search(pattern, line):
                    issues['blocking_calls'].append({
                        'line': tick_content.index(line) + 1,
                        'code': line.strip()
                    })
    
    # Check entire file for GC pressure patterns
    gc_patterns = [
        (r'new\s+Dictionary', 'Dictionary allocation'),
        (r'new\s+List', 'List allocation'),
        (r'new\s+HashSet', 'HashSet allocation'),
        (r'new\s+string\s*\[', 'Array allocation'),
        (r'\.ToArray\s*\(', 'Toarray allocation')
    ]
    
    for pattern, description in gc_patterns:
        matches = re.finditer(pattern, content)
        for match in matches:
            line_num = content[:match.start()].count('\n') + 1
            issues['gc_pressure'].append({
                'line': line_num,
                'code': content.split('\n')[line_num - 1].strip(),
                'description': description
            })
    
    return issues

def analyze_project(project_path: Path) -> Dict:
    """Analyze entire project for performance issues."""
    cs_files = list(project_path.rglob('*.cs'))
    
    all_issues = []
    file_results = {}
    
    for cs_file in cs_files:
        result = analyze_file_for_performance(cs_file)
        if 'error' not in result:
            file_results[str(cs_file)] = result
            all_issues.extend(result['thread_sleep'])
            all_issues.extend(result['linq_in_tick'])
            all_issues.extend(result['allocations'])
            all_issues.extend(result['blocking_calls'])
            all_issues.extend(result['gc_pressure'])
    
    return {
        'files_analyzed': len(cs_files),
        'issues_found': len(all_issues),
        'thread_sleep_count': len(all_issues) + sum(1 for i in all_issues if 'Thread.Sleep' in str(i)),
        'linq_count': len([i for i in all_issues if 'LINQ' in str(i)]),
        'allocation_count': len([i for i in all_issues if 'alloc' in str(i).lower()]),
        'blocking_call_count': len([i for i in all_issues if 'block' in str(i).lower()]),
        'gc_pressure_count': len([i for i in all_issues if 'gc' in str(i).lower()]),
        'file_results': file_results
    }

def generate_report(analysis: Dict) -> str:
    """Generate a performance analysis report."""
    report = []
    report.append("=" * 70)
    report.append("RAGNACONTROLLER PERFORMANCE ANALYSIS REPORT")
    report.append("=" * 70)
    report.append("")
    report.append(f"Files Analyzed: {analysis['files_analyzed']}")
    report.append(f"Total Issues Found: {analysis['issues_found']}")
    report.append("")
    
    if analysis['thread_sleep_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  THREAD.SLEEP ISSUES (Critical for 125Hz Loop)")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('thread_sleep'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['thread_sleep']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    if analysis['linq_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  LINQ IN TICK METHOD (Zero Allocation Violation)")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('linq_in_tick'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['linq_in_tick']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    if analysis['allocation_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  ALLOCATIONS IN TICK METHOD")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('allocations'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['allocations']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    if analysis['blocking_call_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  BLOCKING CALLS IN TICK METHOD")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('blocking_calls'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['blocking_calls']:
                    report.append(f"  Line {item['line']}: {item['code']}")
        report.append("")
    
    if analysis['gc_pressure_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  GC PRESSURE PATTERNS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('gc_pressure'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['gc_pressure']:
                    report.append(f"  Line {item['line']}: {item['code']} ({item['description']})")
        report.append("")
    
    report.append("=" * 70)
    report.append("RECOMMENDATIONS:")
    report.append("=" * 70)
    report.append("")
    report.append("1. Remove all Thread.Sleep() calls from tick/update methods")
    report.append("2. Replace LINQ with manual loops or object pooling")
    report.append("3. Use readonly record structs instead of classes for tick data")
    report.append("4. Move blocking operations to background threads")
    report.append("5. Use System.Random.Shared instead of new Random()")
    report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: analyze_performance.py <project_path>")
        print("Example: analyze_performance.py /mnt/c/RagnaController/src/RagnaController")
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
    report_file = project_path.parent / "performance_analysis_report.txt"
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())
