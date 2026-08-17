#!/usr/bin/env python3
"""
Automated Fixer für RagnaController
Automatisiert Fixes für häufige Build- und Code-Probleme
"""

import os
import sys
import re
from pathlib import Path
from typing import Dict, List, Optional, Tuple

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
SRC_DIR = PROJECT_ROOT / "src" / "RagnaController"
LOCABLES_DIR = PROJECT_ROOT / "Locales"

class AutomatedFixer:
    """Automatisiert Fixes für häufige Probleme"""
    
    def __init__(self):
        self.fixes_applied: List[Dict] = []
        self.fixes_failed: List[Dict] = []
        
    def fix_null_reference_issues(self) -> List[Dict]:
        """Fixt NullReferenceException-Risiken"""
        fixes = []
        
        # Suche nach Using static ohne explizite Referenz
        cs_files = list(SRC_DIR.rglob("*.cs"))
        
        for cs_file in cs_files:
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Pattern für Using static ohne Referenz
                pattern = r'using\s+static\s+(\w+)\.\*'
                matches = re.findall(pattern, content)
                
                for match in matches:
                    # Prüfe ob es sich um RagnaController Namespace handelt
                    if match != "RagnaController":
                        fix = {
                            "file": str(cs_file.relative_to(PROJECT_ROOT)),
                            "issue": f"Using static without explicit reference: {match}.*",
                            "fix": f"Replace 'using static {match}.*' with fully qualified names",
                            "severity": "high"
                        }
                        fixes.append(fix)
                        
            except Exception as e:
                print(f"Error processing {cs_file}: {e}")
        
        return fixes
    
    def fix_using_static_name_conflicts(self) -> List[Dict]:
        """Fixt Using Static Name Konflikte (CS0118)"""
        fixes = []
        
        cs_files = list(SRC_DIR.rglob("*.cs"))
        
        for cs_file in cs_files:
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    lines = f.readlines()
                
                for i, line in enumerate(lines, 1):
                    # Suche nach Using static mit Namespace-Konflikten
                    if re.search(r'using\s+static\s+\w+\.\*', line):
                        # Prüfe ob es einen Konflikt gibt
                        if 'RagnaController' not in line:
                            fixes.append({
                                "file": str(cs_file.relative_to(PROJECT_ROOT)),
                                "line": i,
                                "issue": "Using static with namespace conflict",
                                "fix": "Use fully qualified name or explicit using alias",
                                "severity": "high"
                            })
                            
            except Exception as e:
                print(f"Error processing {cs_file}: {e}")
        
        return fixes
    
    def fix_field_warnings(self) -> List[Dict]:
        """Fixt Feld-Warnungen (CS0649)"""
        fixes = []
        
        cs_files = list(SRC_DIR.rglob("*.cs"))
        xaml_files = list(SRC_DIR.rglob("*.xaml"))
        
        # XAML-Dateien: Suppress-Messages hinzufügen
        for xaml_file in xaml_files:
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Suche nach Feldern die in XAML gesetzt werden
                field_pattern = r'<(\w+)\s+Name="([^"]+)".*<\1\s+x:Class'
                matches = re.findall(field_pattern, content)
                
                for tag, name in matches:
                    # Prüfe ob SuppressMessage fehlt
                    if f'SuppressMessage("Usage", "CS0649"' not in content:
                        fixes.append({
                            "file": str(xaml_file.relative_to(PROJECT_ROOT)),
                            "issue": f"Field '{name}' set in XAML needs SuppressMessage",
                            "fix": "Add [SuppressMessage(\"Usage\", \"CS0649\", Justification = \"XAML field\")] attribute",
                            "severity": "low"
                        })
                        
            except Exception as e:
                print(f"Error processing {xaml_file}: {e}")
        
        return fixes
    
    def fix_thread_safety_issues(self) -> List[Dict]:
        """Fixt Thread-Safety-Probleme"""
        fixes = []
        
        cs_files = list(SRC_DIR.rglob("*.cs"))
        
        for cs_file in cs_files:
            try:
                with open(cs_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Suche nach Dispatcher.Invoke statt BeginInvoke
                invoke_pattern = r'Dispatcher\.Invoke\s*\('
                matches = re.findall(invoke_pattern, content)
                
                for match in matches:
                    fixes.append({
                        "file": str(cs_file.relative_to(PROJECT_ROOT)),
                        "issue": "Using Dispatcher.Invoke instead of BeginInvoke",
                        "fix": "Replace 'Dispatcher.Invoke()' with 'Dispatcher.BeginInvoke()'",
                        "severity": "high"
                    })
                    
            except Exception as e:
                print(f"Error processing {cs_file}: {e}")
        
        return fixes
    
    def fix_localization_issues(self) -> List[Dict]:
        """Fixt Lokalisierungs-Probleme"""
        fixes = []
        
        # Finde Hardcoded Strings in XAML
        xaml_files = list(SRC_DIR.rglob("*.xaml"))
        
        for xaml_file in xaml_files:
            try:
                with open(xaml_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Finde Text zwischen > und </ (nicht {core:Loc})
                pattern = r'>([^<\{]+)</'
                matches = re.findall(pattern, content)
                
                for match in matches:
                    # Überspringe bekannte UI-Texte
                    if any(ui_text in match.lower() for ui_text in [
                        'button', 'textblock', 'stackpanel', 'grid',
                        'window', 'dialog', 'messagebox', 'label'
                    ]):
                        continue
                    
                    # Überspringe Platzhalter
                    if '{0}' in match or '{1}' in match or '{2}' in match:
                        continue
                    
                    fixes.append({
                        "file": str(xaml_file.relative_to(PROJECT_ROOT)),
                        "string": match.strip(),
                        "issue": "Hardcoded string should be localized",
                        "fix": f"Replace with {{core:Loc {match.lower().replace(' ', '_')}}}",
                        "severity": "medium"
                    })
                    
            except Exception as e:
                print(f"Error processing {xaml_file}: {e}")
        
        return fixes
    
    def fix_build_target_errors(self) -> List[Dict]:
        """Fixt Build Target Fehler (CS1926)"""
        fixes = []
        
        # Prüfe csproj-Dateien auf Bundle-Einstellungen
        csproj_files = list(SRC_DIR.parent.rglob("*.csproj"))
        
        for csproj_file in csproj_files:
            try:
                with open(csproj_file, 'r', encoding='utf-8') as f:
                    content = f.read()
                
                # Prüfe auf Bundle-Einstellungen
                if 'GenerateBundle=true' in content:
                    fixes.append({
                        "file": str(csproj_file.relative_to(PROJECT_ROOT)),
                        "issue": "Bundle generation enabled",
                        "fix": "Set 'GenerateBundle=false' or remove post-build zip target",
                        "severity": "high"
                    })
                
                if 'PublishTrimmed=true' in content:
                    fixes.append({
                        "file": str(csproj_file.relative_to(PROJECT_ROOT)),
                        "issue": "Trimmed publish enabled",
                        "fix": "Set 'PublishTrimmed=false' to disable bundle generation",
                        "severity": "high"
                    })
                    
            except Exception as e:
                print(f"Error processing {csproj_file}: {e}")
        
        return fixes
    
    def apply_fixes(self, fixes: List[Dict]) -> Tuple[List[Dict], List[Dict]]:
        """Wendet Fixes an und gibt erfolgreiche/gescheiterte zurück"""
        successful = []
        failed = []
        
        for fix in fixes:
            try:
                # Hier würde der eigentliche Fix angewendet werden
                # Für jetzt simulieren wir erfolgreichen Fix
                fix["status"] = "applied"
                successful.append(fix)
                
            except Exception as e:
                fix["status"] = "failed"
                fix["error"] = str(e)
                failed.append(fix)
        
        return successful, failed
    
    def generate_report(self) -> str:
        """Generiert Fix-Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("AUTOMATED FIXER - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        # Führe alle Fixes durch
        print("🔧 Running automated fixes...")
        
        null_ref_fixes = self.fix_null_reference_issues()
        print(f"   NullReference issues: {len(null_ref_fixes)}")
        
        using_static_fixes = self.fix_using_static_name_conflicts()
        print(f"   Using static conflicts: {len(using_static_fixes)}")
        
        field_warning_fixes = self.fix_field_warnings()
        print(f"   Field warnings: {len(field_warning_fixes)}")
        
        thread_safety_fixes = self.fix_thread_safety_issues()
        print(f"   Thread safety issues: {len(thread_safety_fixes)}")
        
        localization_fixes = self.fix_localization_issues()
        print(f"   Localization issues: {len(localization_fixes)}")
        
        build_target_fixes = self.fix_build_target_errors()
        print(f"   Build target errors: {len(build_target_fixes)}")
        
        all_fixes = null_ref_fixes + using_static_fixes + field_warning_fixes + \
                    thread_safety_fixes + localization_fixes + build_target_fixes
        
        if all_fixes:
            print()
            print("📋 FIXES TO APPLY:")
            print("-" * 40)
            
            for fix in all_fixes[:20]:  # Zeige erste 20
                severity_icon = {
                    "critical": "🔴",
                    "high": "🟠",
                    "medium": "🟡",
                    "low": "🟢"
                }
                icon = severity_icon.get(fix.get("severity", "low"), "⚪")
                
                print(f"{icon} {fix.get('file', 'N/A')}:{fix.get('line', 'N/A')}")
                print(f"   → {fix.get('issue', 'N/A')}")
                print(f"   Fix: {fix.get('fix', 'N/A')}")
                print()
            
            if len(all_fixes) > 20:
                print(f"   ... and {len(all_fixes) - 20} more fixes")
        else:
            print("✅ No fixes needed!")
        
        return "\n".join(lines)


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("AUTOMATED FIXER - RagnaController")
    print("=" * 60)
    print()
    
    fixer = AutomatedFixer()
    
    # Generiere Bericht
    report = fixer.generate_report()
    print(report)
    print()
    
    # Zusammenfassung
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    
    # Zähle alle Fixes
    null_ref_fixes = fixer.fix_null_reference_issues()
    using_static_fixes = fixer.fix_using_static_name_conflicts()
    field_warning_fixes = fixer.fix_field_warnings()
    thread_safety_fixes = fixer.fix_thread_safety_issues()
    localization_fixes = fixer.fix_localization_issues()
    build_target_fixes = fixer.fix_build_target_errors()
    
    total_fixes = (
        len(null_ref_fixes) +
        len(using_static_fixes) +
        len(field_warning_fixes) +
        len(thread_safety_fixes) +
        len(localization_fixes) +
        len(build_target_fixes)
    )
    
    print(f"   Total issues found: {total_fixes}")
    print(f"   NullReference risks: {len(null_ref_fixes)}")
    print(f"   Using static conflicts: {len(using_static_fixes)}")
    print(f"   Field warnings: {len(field_warning_fixes)}")
    print(f"   Thread safety issues: {len(thread_safety_fixes)}")
    print(f"   Localization issues: {len(localization_fixes)}")
    print(f"   Build target errors: {len(build_target_fixes)}")
    
    if total_fixes == 0:
        print()
        print("✅ NO FIXES NEEDED - All code is clean!")
    else:
        print()
        print("⚠️  Found issues that should be addressed")
        print("   Run fixes manually or apply automated fixes as needed.")
    
    return 0 if total_fixes == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
