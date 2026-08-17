#!/usr/bin/env python3
import os
import sys
import re

def find_potential_issues(filepath):
    """Sucht nach potenziellen Problemen in C#-Code"""
    issues = []
    
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
            lines = content.split('\n')
            
            for i, line in enumerate(lines, 1):
                # NullReferenceException Risiken - Feldzugriffe ohne Prüfung
                if re.search(r'\.(?!(null|IsNullOrEmpty|Length|Count))\w+', line) and 'using System;' in content:
                    # Nur wenn es sich um Feldzugriffe handelt (keine Properties)
                    pass
                
                # Memory Leaks - Event Subscription ohne Unsubscribe
                if '+= ' in line and ('Event' in line or 'EventHandler' in line):
                    issues.append((i, "Event Subscription - prüfe Unsubscribe", line.strip()))
                
                # Race Conditions - Shared State ohne Lock
                if re.search(r'(static\s+)?\w+\s*=\s*\w+', line) and 'lock' not in line:
                    pass  # Zu viele False Positives
                
                # Null checks missing
                if re.search(r'if\s*\(\s*\w+\.\w+\)', line):
                    issues.append((i, "Null check missing - prüfe Target", line.strip()))
                    
    except Exception as e:
        print(f"Error reading {filepath}: {e}")
    
    return issues

def main():
    # Prüfe alle C#-Dateien im Projekt
    project_root = '/mnt/c/RagnaController'
    
    csharp_files = []
    
    # Hauptprojekt
    main_proj = os.path.join(project_root, 'src', 'RagnaController')
    if os.path.exists(main_proj):
        for root, dirs, files in os.walk(main_proj):
            for f in files:
                if f.endswith('.cs'):
                    csharp_files.append(os.path.join(root, f))
    
    # Tests
    tests_proj = os.path.join(project_root, 'tests')
    if os.path.exists(tests_proj):
        for root, dirs, files in os.walk(tests_proj):
            for f in files:
                if f.endswith('.cs'):
                    csharp_files.append(os.path.join(root, f))
    
    # Benchmarks
    bench_proj = os.path.join(project_root, 'benchmarks')
    if os.path.exists(bench_proj):
        for root, dirs, files in os.walk(bench_proj):
            for f in files:
                if f.endswith('.cs'):
                    csharp_files.append(os.path.join(root, f))
    
    print(f"Prüfe {len(csharp_files)} C#-Dateien...")
    
    issues = []
    for filepath in sorted(csharp_files):
        file_issues = find_potential_issues(filepath)
        if file_issues:
            issues.extend(file_issues)
    
    if issues:
        print(f"\n{len(issues)} potenzielle Probleme gefunden:")
        for i, (line_num, desc, code) in enumerate(sorted(issues), 1):
            print(f"{i}. {filepath}:{line_num} - {desc}")
            print(f"   {code}")
    else:
        print("Keine offensichtlichen Probleme gefunden!")
    
    return 0 if not issues else 1

if __name__ == '__main__':
    sys.exit(main())
