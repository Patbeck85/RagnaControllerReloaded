#!/usr/bin/env python3
"""
UI Inspection Tool für RagnaController
Überprüft XAML-Elemente, Accessibility und Layout-Probleme
"""

import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Tuple, Optional

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
XAML_DIR = PROJECT_ROOT / "src" / "RagnaController" / "Views"
CS_CODE_DIR = PROJECT_ROOT / "src" / "RagnaController" / "ViewModels"
LOCALES_DIR = PROJECT_ROOT / "Locales"

class UIInspectionReport:
    """Bericht über UI-Inspektionsergebnisse"""
    
    def __init__(self):
        self.xaml_files: List[Dict] = []
        self.accessibility_issues: List[Dict] = []
        self.layout_problems: List[Dict] = []
        self.missing_localizations: List[Dict] = []
        self.code_behind_issues: List[Dict] = []
        
    def add_xaml_file(self, path: str, size: int, elements: int):
        self.xaml_files.append({
            "path": path,
            "size": size,
            "elements": elements
        })
    
    def add_accessibility_issue(self, file: str, line: int, issue: str, severity: str = "medium"):
        self.accessibility_issues.append({
            "file": file,
            "line": line,
            "issue": issue,
            "severity": severity
        })
    
    def add_layout_problem(self, file: str, line: int, problem: str):
        self.layout_problems.append({
            "file": file,
            "line": line,
            "problem": problem
        })
    
    def add_missing_localization(self, file: str, key: str, context: str = ""):
        self.missing_localizations.append({
            "file": file,
            "key": key,
            "context": context
        })
    
    def add_code_behind_issue(self, file: str, line: int, issue: str):
        self.code_behind_issues.append({
            "file": file,
            "line": line,
            "issue": issue
        })
    
    def generate_report(self) -> str:
        """Generiert einen formatierten Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("UI INSPECTION REPORT - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        # XAML-Dateien
        lines.append(f"📁 XAML Files Analyzed: {len(self.xaml_files)}")
        for x in self.xaml_files:
            lines.append(f"   - {x['path']} ({x['elements']} elements)")
        lines.append("")
        
        # Accessibility Issues
        if self.accessibility_issues:
            lines.append(f"⚠️  Accessibility Issues: {len(self.accessibility_issues)}")
            for issue in self.accessibility_issues:
                severity_icon = {"critical": "🔴", "high": "🟠", "medium": "🟡", "low": "🟢"}
                lines.append(f"   {severity_icon.get(issue['severity'], '⚪')} {issue['file']}:{issue['line']}")
                lines.append(f"      → {issue['issue']}")
        else:
            lines.append("✅ No accessibility issues found")
        lines.append("")
        
        # Layout Probleme
        if self.layout_problems:
            lines.append(f"📐 Layout Problems: {len(self.layout_problems)}")
            for problem in self.layout_problems:
                lines.append(f"   - {problem['file']}:{problem['line']}")
                lines.append(f"      → {problem['problem']}")
        else:
            lines.append("✅ No layout problems found")
        lines.append("")
        
        # Fehlende Lokalisierungen
        if self.missing_localizations:
            lines.append(f"🌐 Missing Localizations: {len(self.missing_localizations)}")
            for loc in self.missing_localizations:
                lines.append(f"   - {loc['file']}: {loc['key']}")
                if loc['context']:
                    lines.append(f"      → Context: {loc['context']}")
        else:
            lines.append("✅ All localizations present")
        lines.append("")
        
        # Code-Behind Issues
        if self.code_behind_issues:
            lines.append(f"💻 Code-Behind Issues: {len(self.code_behind_issues)}")
            for issue in self.code_behind_issues:
                lines.append(f"   - {issue['file']}:{issue['line']}")
                lines.append(f"      → {issue['issue']}")
        else:
            lines.append("✅ No code-behind issues found")
        
        return "\n".join(lines)


def analyze_xaml_file(filepath: Path) -> Tuple[int, int]:
    """Analysiert eine XAML-Datei und zählt Elemente"""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        # Zähle UI-Elemente (basierend auf Tag-Namen)
        element_pattern = r'<(\w+)'
        elements = len(re.findall(element_pattern, content))
        
        # Überprüfe auf Accessibility-Attribute
        accessibility_issues = []
        
        # Fehlende AutomationProperties.Name
        name_pattern = r'<(\w+)(?!.*AutomationProperties\.Name)'
        elements_without_name = len(re.findall(name_pattern, content))
        if elements_without_name > 0:
            accessibility_issues.append(f"Elements without AutomationProperties.Name: {elements_without_name}")
        
        # Fehlende IsEnabled für interaktive Elemente
        button_pattern = r'<Button[^>]*>(?!.*IsEnabled)'
        buttons_without_enabled = len(re.findall(button_pattern, content))
        if buttons_without_enabled > 0:
            accessibility_issues.append(f"Buttons without IsEnabled: {buttons_without_enabled}")
        
        # Hardcoded Strings (sollten lokalisiert sein)
        hardcoded_strings = re.findall(r'>([^<]+)</', content)
        localized_strings = re.findall(r'>\{core:Loc\s+(\w+)\}</', content)
        non_localized = len(hardcoded_strings) - len(localized_strings)
        
        return elements, accessibility_issues
        
    except Exception as e:
        print(f"Error analyzing {filepath}: {e}")
        return 0, []


def scan_xaml_files() -> UIInspectionReport:
    """Scannt alle XAML-Dateien im Projekt"""
    report = UIInspectionReport()
    
    xaml_files = list(XAML_DIR.rglob("*.xaml"))
    
    for xaml_file in xaml_files:
        rel_path = str(xaml_file.relative_to(PROJECT_ROOT))
        elements, issues = analyze_xaml_file(xaml_file)
        
        report.add_xaml_file(rel_path, xaml_file.stat().st_size, elements)
        
        for issue in issues:
            # Extrahiere grobe Zeilennummer (vereinfacht)
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    lines = f.readlines()
                    for i, line in enumerate(lines):
                        if any(issue in line for issue in issues):
                            report.add_accessibility_issue(rel_path, i + 1, issue)
                            break
            except:
                pass
    
    return report


def scan_code_behind_files() -> List[Dict]:
    """Scannt Code-Behind Dateien auf häufige Probleme"""
    issues = []
    
    cs_files = list(CS_CODE_DIR.rglob("*.cs"))
    
    for cs_file in cs_files:
        rel_path = str(cs_file.relative_to(PROJECT_ROOT))
        
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                content = f.read()
                lines = content.split('\n')
            
            # Suche nach häufigen Problemen
            for i, line in enumerate(lines, 1):
                # Using static ohne explizite Referenz
                if re.search(r'using\s+static\s+\w+\.\*', line) and 'RagnaController' not in line:
                    issues.append({
                        "file": rel_path,
                        "line": i,
                        "issue": "Using static without explicit reference"
                    })
                
                # Feld-Warnungen (CS0649)
                if re.search(r'\.Field\([^)]*\)', line):
                    issues.append({
                        "file": rel_path,
                        "line": i,
                        "issue": "Potential field warning (CS0649)"
                    })
                    
        except Exception as e:
            print(f"Error reading {cs_file}: {e}")
    
    return issues


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("UI INSPECTION TOOL - RagnaController")
    print("=" * 60)
    print()
    
    # XAML-Inspektion
    print("📋 Analyzing XAML files...")
    xaml_report = scan_xaml_files()
    
    print(f"   Found {len(xaml_report.xaml_files)} XAML files")
    print(f"   Accessibility issues: {len(xaml_report.accessibility_issues)}")
    print(f"   Layout problems: {len(xaml_report.layout_problems)}")
    print(f"   Missing localizations: {len(xaml_report.missing_localizations)}")
    print()
    
    # Code-Behind-Inspektion
    print("📝 Analyzing Code-Behind files...")
    code_issues = scan_code_behind_files()
    print(f"   Found {len(code_issues)} potential issues")
    print()
    
    # Bericht generieren
    print("=" * 60)
    print(xaml_report.generate_report())
    print("=" * 60)
    print()
    
    # Zusammenfassung
    total_issues = (
        len(xaml_report.accessibility_issues) +
        len(xaml_report.layout_problems) +
        len(xaml_report.missing_localizations) +
        len(code_issues)
    )
    
    print(f"📊 SUMMARY:")
    print(f"   Total issues found: {total_issues}")
    print(f"   XAML files analyzed: {len(xaml_report.xaml_files)}")
    
    if total_issues == 0:
        print()
        print("✅ UI INSPECTION COMPLETE - No issues found!")
    else:
        print()
        print("⚠️  Please review the issues above and fix them.")
    
    return 0 if total_issues == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
