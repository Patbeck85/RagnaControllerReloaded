#!/usr/bin/env python3
"""
Build Status Dashboard für RagnaController
Analyziert Build-Ausgaben und zeigt Probleme an
"""

import os
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional
from datetime import datetime

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
BUILD_OUTPUT_FILE = PROJECT_ROOT / "build.log"
LAST_BUILD_TIME_FILE = PROJECT_ROOT / ".last_build_time"

class BuildStatusDashboard:
    """Zeigt den Build-Status von RagnaController an"""
    
    def __init__(self):
        self.build_output: str = ""
        self.build_time: Optional[datetime] = None
        self.errors: List[Dict] = []
        self.warnings: List[Dict] = []
        self.successful: bool = False
        
    def parse_build_output(self, output: str) -> None:
        """Parsen von Build-Ausgaben"""
        self.build_output = output
        self.errors = []
        self.warnings = []
        
        lines = output.split('\n')
        
        for line in lines:
            # Fehler
            error_patterns = [
                r'error\s+\d+',
                r'CS\d+',
                r'Build FAILED',
                r'error\[\d+\]',
            ]
            
            for pattern in error_patterns:
                if re.search(pattern, line, re.IGNORECASE):
                    error_info = {
                        "line": line.strip(),
                        "type": "error",
                        "timestamp": datetime.now().isoformat()
                    }
                    
                    # Extrahiere Fehler-Details
                    match = re.search(r'CS\d+\s+(.+)', line)
                    if match:
                        error_info["code"] = match.group(1).strip()
                    
                    self.errors.append(error_info)
                    break
            
            # Warnungen
            warning_patterns = [
                r'warning\s+\d+',
                r'CS\d+',
                r'warning\[\d+\]',
            ]
            
            for pattern in warning_patterns:
                if re.search(pattern, line, re.IGNORECASE):
                    # Prüfe ob es kein Fehler ist
                    is_error = any(re.search(p, line, re.IGNORECASE) for p in error_patterns)
                    
                    if not is_error:
                        warning_info = {
                            "line": line.strip(),
                            "type": "warning",
                            "timestamp": datetime.now().isoformat()
                        }
                        
                        # Extrahiere Warnung-Details
                        match = re.search(r'CS\d+\s+(.+)', line)
                        if match:
                            warning_info["code"] = match.group(1).strip()
                        
                        self.warnings.append(warning_info)
                    break
        
        # Prüfe auf Erfolg
        if "Build succeeded" in output or "Build completed" in output:
            self.successful = True
            
            # Extrahiere Build-Zeit
            time_match = re.search(r'(\d+)\.(\d+)s', output)
            if time_match:
                seconds = float(f"{time_match.group(1)}.{time_match.group(2)}")
                minutes = seconds / 60
                self.build_time = datetime.now() - timedelta(seconds=seconds)
    
    def get_error_count(self) -> int:
        """Zählt Fehler"""
        return len(self.errors)
    
    def get_warning_count(self) -> int:
        """Zählt Warnungen"""
        return len(self.warnings)
    
    def generate_report(self) -> str:
        """Generiert einen formatierten Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("BUILD STATUS DASHBOARD - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        if self.successful:
            lines.append("✅ BUILD SUCCESSFUL")
        else:
            lines.append("❌ BUILD FAILED")
        
        lines.append(f"   Errors: {self.get_error_count()}")
        lines.append(f"   Warnings: {self.get_warning_count()}")
        
        if self.build_time:
            elapsed = (datetime.now() - self.build_time).total_seconds()
            lines.append(f"   Build time: {elapsed:.2f}s")
        
        lines.append("")
        
        # Fehler-Details
        if self.errors:
            lines.append("🔴 ERRORS:")
            lines.append("-" * 40)
            for error in self.errors[:10]:  # Zeige erste 10
                lines.append(f"   {error['line']}")
                if 'code' in error:
                    lines.append(f"      → Code: {error['code']}")
            if len(self.errors) > 10:
                lines.append(f"   ... and {len(self.errors) - 10} more errors")
            lines.append("")
        
        # Warnung-Details
        if self.warnings:
            lines.append("🟠 WARNINGS:")
            lines.append("-" * 40)
            for warning in self.warnings[:10]:  # Zeige erste 10
                lines.append(f"   {warning['line']}")
                if 'code' in warning:
                    lines.append(f"      → Code: {warning['code']}")
            if len(self.warnings) > 10:
                lines.append(f"   ... and {len(self.warnings) - 10} more warnings")
            lines.append("")
        
        return "\n".join(lines)
    
    def categorize_errors(self) -> Dict[str, List[Dict]]:
        """Kategorisiert Fehler nach Typ"""
        categories = {
            "NullReference": [],
            "UsingStaticName": [],
            "FieldWarnings": [],
            "BuildTarget": [],
            "Other": []
        }
        
        for error in self.errors:
            line = error.get('line', '').lower()
            
            if 'nullreference' in line or 'cs0118' in line:
                categories["NullReference"].append(error)
            elif 'using static' in line or 'cs0115' in line:
                categories["UsingStaticName"].append(error)
            elif 'field' in line or 'cs0649' in line:
                categories["FieldWarnings"].append(error)
            elif 'build target' in line or 'cs1926' in line:
                categories["BuildTarget"].append(error)
            else:
                categories["Other"].append(error)
        
        return categories


def parse_build_log(log_file: Path) -> str:
    """Liest Build-Log-Datei"""
    if not log_file.exists():
        return ""
    
    try:
        with open(log_file, 'r', encoding='utf-8') as f:
            return f.read()
    except Exception as e:
        print(f"Error reading build log: {e}")
        return ""


def run_build_analysis() -> BuildStatusDashboard:
    """Führt Build-Analyse durch"""
    dashboard = BuildStatusDashboard()
    
    # Versuche, Build-Log zu lesen
    build_output = parse_build_log(BUILD_OUTPUT_FILE)
    
    if build_output:
        print("📄 Analyzing build output...")
        dashboard.parse_build_output(build_output)
        
        if dashboard.successful:
            print("✅ Build successful!")
        else:
            print(f"❌ Build failed with {dashboard.get_error_count()} errors")
    else:
        print("⚠️  No build log found. Run 'dotnet build' first.")
    
    return dashboard


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("BUILD STATUS DASHBOARD - RagnaController")
    print("=" * 60)
    print()
    
    # Build-Analyse
    dashboard = run_build_analysis()
    
    if not dashboard.successful:
        print()
        print(dashboard.generate_report())
        
        # Kategorisiere Fehler
        categories = dashboard.categorize_errors()
        
        print()
        print("📊 ERROR CATEGORIES:")
        for category, errors in categories.items():
            if errors:
                print(f"   {category}: {len(errors)}")
        
        print()
        print("=" * 60)
        print("⚠️  BUILD FAILED - Please review errors above")
        print("=" * 60)
        return 1
    
    else:
        print()
        print(dashboard.generate_report())
        
        print()
        print("=" * 60)
        print("✅ BUILD SUCCESSFUL - No critical issues")
        print("=" * 60)
        return 0


if __name__ == "__main__":
    sys.exit(main())
