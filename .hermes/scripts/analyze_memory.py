#!/usr/bin/env python3
"""
RagnaController Memory Analyzer
Analyzes code for memory leaks, GC pressure, and memory optimization opportunities.
"""

import re
import sys
from pathlib import Path
from typing import List, Dict, Set

def analyze_file_for_memory(file_path: Path) -> Dict:
    """Analyze a single file for memory issues."""
    issues = {
        'file': str(file_path),
        'memory_leaks': [],
        'gc_pressure': [],
        'event_subscription_leaks': [],
        'static_collection_growth': [],
        'cache_without_size_limit': [],
        'unnecessary_copies': []
    }
    
    try:
        content = file_path.read_text(encoding='utf-8')
    except Exception as e:
        return {'error': str(e)}
    
    lines = content.split('\n')
    
    for line_num, line in enumerate(lines, 1):
        # Check for event subscription leaks
        event_patterns = [
            (r'(?:this|me|self)\.Subscribe\s*\(', 'Event subscription without unsubscription'),
            (r'\.AddHandler\s*\(', 'Event handler without removal'),
            (r'\.RemoveHandler\s*\(', 'Event handler removal'),
            (r'Add_\w+\s*\(', 'Add event handler'),
            (r'Remove_\w+\s*\(', 'Remove event handler'),
        ]
        
        for pattern, description in event_patterns:
            if re.search(pattern, line):
                # Check if there's a corresponding RemoveHandler
                if 'RemoveHandler' not in line and 'AddHandler' not in line:
                    issues['event_subscription_leaks'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description
                    })
        
        # Check for static collections that grow
        static_collection_patterns = [
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?List<', 'Static List'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?Dictionary<', 'Static Dictionary'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?HashSet<', 'Static HashSet'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?Queue<', 'Static Queue'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?Stack<', 'Static Stack'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?PriorityQueue<', 'Static PriorityQueue'),
        ]
        
        for pattern, description in static_collection_patterns:
            if re.search(pattern, line):
                # Check if it's being added to
                context = content[max(0, content.rfind('\n', 0, content.find(line))):content.find(line)+500]
                if any(x in context for x in ['.Add(', '.AddRange(', '= new', 'new ']):
                    issues['static_collection_growth'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description + ' - grows indefinitely'
                    })
        
        # Check for cache without size limit
        cache_patterns = [
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?Dictionary<.*,\s*string>', 'Static Dictionary cache'),
            (r'(?:private\s+)?(?:static\s+)?(?:readonly\s+)?Dictionary<.*,\s*object>', 'Static Dictionary cache'),
        ]
        
        for pattern, description in cache_patterns:
            if re.search(pattern, line):
                # Check if there's a size limit
                if not any(x in content[max(0, content.rfind('\n', 0, content.find(line))):content.find(line)+500] 
                          for x in ['WithCapacity', 'Count <', 'Size <', 'MaxSize']):
                    issues['cache_without_size_limit'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description + ' - no size limit'
                    })
        
        # Check for unnecessary copies
        copy_patterns = [
            (r'\.Clone\s*\(', 'Array/Collection clone'),
            (r'\.ToArray\s*\(', 'Toarray conversion'),
            (r'\.ToList\s*\(', 'ToList conversion'),
            (r'\.CopyTo\s*\(', 'CopyTo operation'),
            (r'new\s+byte\s*\[\s*\]', 'New byte array'),
            (r'new\s+int\s*\[\s*\]', 'New int array'),
            (r'new\s+string\s*\[\s*\]', 'New string array'),
        ]
        
        for pattern, description in copy_patterns:
            if re.search(pattern, line):
                # Check if it's in a hot path (tick/update method)
                if any(x in content[max(0, content.rfind('\n', 0, content.find(line))):content.find(line)+500] 
                       for x in ['OnTick(', 'Update(', 'Tick(']):
                    issues['unnecessary_copies'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description + ' - in hot path'
                    })
        
        # Check for GC pressure patterns
        gc_patterns = [
            (r'new\s+Dictionary<', 'Dictionary allocation'),
            (r'new\s+List<', 'List allocation'),
            (r'new\s+HashSet<', 'HashSet allocation'),
            (r'new\s+Queue<', 'Queue allocation'),
            (r'new\s+Stack<', 'Stack allocation'),
            (r'new\s+PriorityQueue<', 'PriorityQueue allocation'),
            (r'new\s+byte\s*\[\s*\]', 'Byte array allocation'),
            (r'new\s+int\s*\[\s*\]', 'Int array allocation'),
            (r'new\s+string\s*\[\s*\]', 'String array allocation'),
            (r'new\s+char\s*\[\s*\]', 'Char array allocation'),
            (r'new\s+object\s*\[', 'Object array allocation'),
            (r'\.ToArray\s*\(', 'Toarray allocation'),
            (r'\.ToList\s*\(', 'ToList allocation'),
            (r'\.Clone\s*\(', 'Clone allocation'),
            (r'\.CopyTo\s*\(', 'CopyTo allocation'),
        ]
        
        for pattern, description in gc_patterns:
            if re.search(pattern, line):
                # Check if it's in a hot path
                context = content[max(0, content.rfind('\n', 0, content.find(line))):content.find(line)+500]
                if any(x in context for x in ['OnTick(', 'Update(', 'Tick(']):
                    issues['gc_pressure'].append({
                        'line': line_num,
                        'code': line.strip(),
                        'risk': description + ' - in 125Hz loop'
                    })
        
        # Check for memory leaks with anonymous delegates
        if re.search(r'new\s+Action\s*\(\s*[^)]*\)\s*\{', line):
            issues['memory_leaks'].append({
                'line': line_num,
                'code': line.strip(),
                'risk': 'Anonymous delegate - potential memory leak'
            })
        
        if re.search(r'new\s+Func\s*\(\s*[^)]*\)\s*\{', line):
            issues['memory_leaks'].append({
                'line': line_num,
                'code': line.strip(),
                'risk': 'Anonymous Func - potential memory leak'
            })
    
    return issues

def analyze_project(project_path: Path) -> Dict:
    """Analyze entire project for memory issues."""
    cs_files = list(project_path.rglob('*.cs'))
    
    all_issues = []
    file_results = {}
    
    for cs_file in cs_files:
        result = analyze_file_for_memory(cs_file)
        if 'error' not in result:
            file_results[str(cs_file)] = result
            all_issues.extend(result['memory_leaks'])
            all_issues.extend(result['gc_pressure'])
            all_issues.extend(result['event_subscription_leaks'])
            all_issues.extend(result['static_collection_growth'])
            all_issues.extend(result['cache_without_size_limit'])
            all_issues.extend(result['unnecessary_copies'])
    
    return {
        'files_analyzed': len(cs_files),
        'issues_found': len(all_issues),
        'memory_leak_count': len([i for i in all_issues if 'Memory' in str(i)]),
        'gc_pressure_count': len([i for i in all_issues if 'GC' in str(i)]),
        'event_subscription_leak_count': len([i for i in all_issues if 'Event' in str(i)]),
        'static_collection_growth_count': len([i for i in all_issues if 'Static' in str(i)]),
        'cache_without_size_limit_count': len([i for i in all_issues if 'Cache' in str(i)]),
        'unnecessary_copies_count': len([i for i in all_issues if 'Copy' in str(i)]),
        'file_results': file_results
    }

def generate_report(analysis: Dict) -> str:
    """Generate a memory analysis report."""
    report = []
    report.append("=" * 70)
    report.append("RAGNACONTROLLER MEMORY ANALYSIS REPORT")
    report.append("=" * 70)
    report.append("")
    report.append(f"Files Analyzed: {analysis['files_analyzed']}")
    report.append(f"Total Issues Found: {analysis['issues_found']}")
    report.append("")
    
    if analysis['memory_leak_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  MEMORY LEAKS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('memory_leaks'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['memory_leaks']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['gc_pressure_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  GC PRESSURE IN HOT PATH")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('gc_pressure'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['gc_pressure']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['event_subscription_leak_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  EVENT SUBSCRIPTION LEAKS")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('event_subscription_leaks'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['event_subscription_leaks']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['static_collection_growth_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  STATIC COLLECTIONS THAT GROW")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('static_collection_growth'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['static_collection_growth']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['cache_without_size_limit_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  CACHES WITHOUT SIZE LIMIT")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('cache_without_size_limit'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['cache_without_size_limit']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    if analysis['unnecessary_copies_count'] > 0:
        report.append("-" * 70)
        report.append("⚠️  UNNECESSARY COPIES IN HOT PATH")
        report.append("-" * 70)
        for issue in analysis['file_results'].values():
            if issue.get('unnecessary_copies'):
                report.append(f"\nFile: {issue['file']}")
                for item in issue['unnecessary_copies']:
                    report.append(f"  Line {item['line']}: {item['risk']}")
                    report.append(f"    {item['code']}")
        report.append("")
    
    report.append("=" * 70)
    report.append("RECOMMENDATIONS:")
    report.append("=" * 70)
    report.append("")
    report.append("1. Always unsubscribe from events when objects are disposed")
    report.append("2. Use object pooling for frequently allocated objects")
    report.append("3. Implement size limits on caches and collections")
    report.append("4. Move allocations out of the 125Hz tick loop")
    report.append("5. Use Span<T> and Memory<T> for zero-allocation operations")
    report.append("6. Consider using struct instead of class for small value types")
    report.append("7. Use WeakReference for caches that should not prevent GC")
    report.append("")
    
    return '\n'.join(report)

def main():
    """Main entry point."""
    if len(sys.argv) < 2:
        print("Usage: analyze_memory.py <project_path>")
        print("Example: analyze_memory.py /mnt/c/RagnaController/src/RagnaController")
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
    report_file = project_path.parent / "memory_analysis_report.txt"
    with open(report_file, 'w', encoding='utf-8') as f:
        f.write(report)
    
    print(f"\nReport saved to: {report_file}")
    return 0

if __name__ == '__main__':
    sys.exit(main())
