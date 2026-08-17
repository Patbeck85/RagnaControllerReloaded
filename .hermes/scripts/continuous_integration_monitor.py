#!/usr/bin/env python3
"""
Continuous Integration Monitor für RagnaController
Überwacht Build-Status und meldet Probleme automatisch
"""

import os
import sys
from pathlib import Path
from typing import Dict, List, Optional
from datetime import datetime, timedelta

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
BUILD_LOG_FILE = PROJECT_ROOT / "build.log"
CI_STATUS_FILE = PROJECT_ROOT / ".ci_status.json"
NOTIFICATION_LOG = PROJECT_ROOT / ".ci_notifications.log"

class ContinuousIntegrationMonitor:
    """Überwacht Build-Status und meldet Probleme"""
    
    def __init__(self):
        self.last_build_time: Optional[datetime] = None
        self.last_build_status: str = "unknown"
        self.build_history: List[Dict] = []
        self.notification_count: int = 0
        
    def check_build_status(self) -> Dict[str, Any]:
        """Überprüft aktuellen Build-Status"""
        
        # Prüfe Build-Log auf Existenz
        if not BUILD_LOG_FILE.exists():
            return {
                "status": "no_build_log",
                "message": "No build log found. Run 'dotnet build' first.",
                "timestamp": datetime.now().isoformat()
            }
        
        try:
            with open(BUILD_LOG_FILE, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Analysiere Build-Ergebnis
            if "Build succeeded" in content or "Build completed" in content:
                status = "success"
                errors = 0
                warnings = 0
            else:
                status = "failed"
                # Zähle Fehler
                error_count = content.count("error CS")
                warning_count = content.count("warning CS")
                errors = error_count
                warnings = warning_count
            
            # Extrahiere Build-Zeit
            build_time = self._extract_build_time(content)
            
            return {
                "status": status,
                "errors": errors,
                "warnings": warnings,
                "build_time_seconds": build_time,
                "timestamp": datetime.now().isoformat()
            }
            
        except Exception as e:
            return {
                "status": "error",
                "message": str(e),
                "timestamp": datetime.now().isoformat()
            }
    
    def _extract_build_time(self, content: str) -> float:
        """Extrahiert Build-Zeit aus Log"""
        import re
        
        # Suche nach Build-Zeit Pattern
        time_patterns = [
            r'(\d+)\.(\d+)s',  # "12.5s"
            r'Build time:\s*(\d+\.?\d*)s',
            r'Elapsed time:\s*(\d+\.?\d*)s',
        ]
        
        for pattern in time_patterns:
            match = re.search(pattern, content)
            if match:
                return float(f"{match.group(1)}.{match.group(2)}" if len(match.groups()) > 1 else match.group(1))
        
        return 0.0
    
    def check_for_regressions(self) -> List[Dict]:
        """Überprüft auf Regressionen im Vergleich zur Historie"""
        regressions = []
        
        if not self.build_history:
            return regressions
        
        # Hole letzten erfolgreichen Build
        last_success = None
        for build in reversed(self.build_history):
            if build["status"] == "success":
                last_success = build
                break
        
        if not last_success:
            return regressions
        
        # Prüfe aktuellen Build gegen letzte Erfolg
        current = self.check_build_status()
        
        if current["status"] != "success":
            regressions.append({
                "type": "build_failure",
                "severity": "critical",
                "message": f"Build failed with {current.get('errors', 0)} errors",
                "current_errors": current.get("errors", 0),
                "previous_errors": 0
            })
        else:
            # Prüfe auf Performance-Regression
            current_time = current.get("build_time_seconds", 0)
            previous_time = last_success.get("build_time_seconds", 0)
            
            if previous_time > 0:
                time_increase = ((current_time - previous_time) / previous_time) * 100
                
                if time_increase > 20:
                    regressions.append({
                        "type": "build_time_regression",
                        "severity": "medium",
                        "message": f"Build time increased by {time_increase:.1f}%",
                        "current_time": current_time,
                        "previous_time": previous_time,
                        "increase_percent": time_increase
                    })
        
        return regressions
    
    def should_notify(self) -> bool:
        """Bestimmt ob Benachrichtigung gesendet werden sollte"""
        if self.notification_count >= 3:
            return False
        
        # Prüfe seit letzter Benachrichtigung
        if CI_STATUS_FILE.exists():
            try:
                with open(CI_STATUS_FILE, 'r', encoding='utf-8') as f:
                    status_data = json.load(f)
                
                last_notification = datetime.fromisoformat(status_data.get("last_notification", "1970-01-01"))
                hours_since_notification = (datetime.now() - last_notification).total_seconds() / 3600
                
                # Benachrichtige nur wenn mehr als 24 Stunden vergangen sind
                if hours_since_notification < 24:
                    return False
                    
            except Exception as e:
                print(f"Error reading CI status: {e}")
        
        return True
    
    def record_status(self, status_data: Dict) -> None:
        """Speichert Build-Status"""
        try:
            with open(CI_STATUS_FILE, 'w', encoding='utf-8') as f:
                json.dump(status_data, f, indent=2)
        except Exception as e:
            print(f"Error recording status: {e}")
    
    def send_notification(self, message: str, severity: str = "info") -> None:
        """Sendet Benachrichtigung"""
        self.notification_count += 1
        
        timestamp = datetime.now().isoformat()
        
        notification = f"[{timestamp}] [{severity.upper()}] {message}\n"
        
        with open(NOTIFICATION_LOG, 'a', encoding='utf-8') as f:
            f.write(notification)
        
        print(notification.strip())
    
    def generate_report(self) -> str:
        """Generiert CI-Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("CONTINUOUS INTEGRATION MONITOR - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        # Aktueller Status
        status = self.check_build_status()
        
        if status["status"] == "success":
            lines.append("✅ BUILD STATUS: SUCCESS")
        elif status["status"] == "failed":
            lines.append(f"❌ BUILD STATUS: FAILED ({status.get('errors', 0)} errors)")
        else:
            lines.append(f"⚠️  {status.get('message', 'Unknown status')}")
        
        lines.append(f"   Timestamp: {status.get('timestamp', 'N/A')}")
        
        if "build_time_seconds" in status:
            lines.append(f"   Build time: {status['build_time_seconds']:.2f}s")
        
        lines.append("")
        
        # Regressionen
        regressions = self.check_for_regressions()
        
        if regressions:
            lines.append("📉 REGRESSIONS DETECTED:")
            lines.append("-" * 40)
            for regression in regressions:
                severity_icon = {
                    "critical": "🔴",
                    "high": "🟠",
                    "medium": "🟡",
                    "low": "🟢"
                }
                icon = severity_icon.get(regression["severity"], "⚪")
                lines.append(f"   {icon} {regression['type'].replace('_', ' ').title()}")
                lines.append(f"      → {regression['message']}")
        else:
            lines.append("✅ No regressions detected")
        
        lines.append("")
        
        # Historie
        if self.build_history:
            lines.append("📊 BUILD HISTORY (Last 5):")
            lines.append("-" * 40)
            for build in self.build_history[-5:]:
                status_icon = "✅" if build["status"] == "success" else "❌"
                lines.append(f"   {status_icon} {build['timestamp'][:19]} - {build['status']}")
        else:
            lines.append("📊 BUILD HISTORY: No history available")
        
        return "\n".join(lines)


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("CONTINUOUS INTEGRATION MONITOR - RagnaController")
    print("=" * 60)
    print()
    
    monitor = ContinuousIntegrationMonitor()
    
    # Generiere Bericht
    report = monitor.generate_report()
    print(report)
    print()
    
    # Prüfe ob Benachrichtigung gesendet werden sollte
    if monitor.should_notify():
        status = monitor.check_build_status()
        
        if status["status"] == "failed":
            message = f"Build failed with {status.get('errors', 0)} errors"
            monitor.send_notification(message, "critical")
            
            # Speichere Status
            monitor.record_status({
                "last_status": "failed",
                "last_notification": datetime.now().isoformat(),
                "error_count": status.get("errors", 0)
            })
        else:
            message = "Build successful - no regressions detected"
            monitor.send_notification(message, "success")
            
            # Speichere Status
            monitor.record_status({
                "last_status": "success",
                "last_notification": datetime.now().isoformat(),
                "error_count": 0
            })
    
    print()
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"   Notification count: {monitor.notification_count}")
    print(f"   Max notifications per day: 3")
    print(f"   Next notification in: 24 hours (if build fails)")
    
    return 0


if __name__ == "__main__":
    import json
    sys.exit(main())
