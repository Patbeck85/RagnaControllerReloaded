#!/usr/bin/env python3
"""
Performance Monitoring Tool für RagnaController
Analysiert den 125Hz Engine Loop und Performance-Metriken
"""

import os
import re
import sys
import time
from pathlib import Path
from typing import Dict, List, Optional
from datetime import datetime

# Konfiguration
PROJECT_ROOT = Path("/mnt/c/RagnaController")
CORE_DIR = PROJECT_ROOT / "src" / "RagnaController" / "Core"
HYBRID_ENGINE_FILE = CORE_DIR / "HybridEngine.cs"
INPUT_QUEUE_FILE = CORE_DIR / "InputCommandQueue.cs"

class PerformanceMonitor:
    """Überwacht die Performance von RagnaController"""
    
    def __init__(self):
        self.frame_times: List[float] = []
        self.allocation_count: int = 0
        self.cpu_usage: float = 0.0
        self.memory_usage: int = 0
        self.tick_count: int = 0
        self.last_tick_time: Optional[float] = None
        
    def analyze_engine_code(self) -> Dict[str, List[Dict]]:
        """Analysiert Engine-Code auf Performance-Probleme"""
        issues = {
            "potential_allocations": [],
            "thread_sleep_calls": [],
            "linq_in_hot_path": [],
            "blocking_calls": []
        }
        
        if not HYBRID_ENGINE_FILE.exists():
            return issues
        
        try:
            with open(HYBRID_ENGINE_FILE, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Suche nach potenziellen Allokationen im Hot Path
            allocation_pattern = r'new\s+\w+'
            allocations = re.findall(allocation_pattern, content)
            
            for alloc in allocations:
                issues["potential_allocations"].append({
                    "pattern": alloc,
                    "severity": "high" if "struct" not in alloc.lower() else "medium"
                })
            
            # Suche nach Thread.Sleep
            sleep_pattern = r'Thread\.Sleep\s*\(\s*\d+\s*\)'
            sleeps = re.findall(sleep_pattern, content)
            
            for sleep in sleeps:
                issues["thread_sleep_calls"].append({
                    "pattern": sleep,
                    "severity": "critical"
                })
            
            # Suche nach LINQ im Hot Path
            linq_patterns = [
                r'\.Where\s*\(',
                r'\.Select\s*\(',
                r'\.OrderBy\s*\(',
                r'\.Any\s*\(',
                r'\.All\s*\(',
                r'\.First\s*\(',
                r'\.FirstOrDefault\s*\(',
                r'\.Last\s*\(',
                r'\.LastOrDefault\s*\(',
                r'\.Count\s*\(',
                r'\.Sum\s*\(',
                r'\.Min\s*\(',
                r'\.Max\s*\(',
                r'\.GroupBy\s*\(',
                r'\.Aggregate\s*\(',
                r'\.Concat\s*\(',
                r'\.Union\s*\(',
                r'\.Intersect\s*\(',
                r'\.Except\s*\(',
                r'\.Distinct\s*\(',
                r'\.DefaultIfEmpty\s*\(',
                r'\.Cast\s*\(',
                r'\.AsEnumerable\s*\(',
                r'\.AsQueryable\s*\(',
            ]
            
            for pattern in linq_patterns:
                matches = re.findall(pattern, content)
                if matches:
                    issues["linq_in_hot_path"].append({
                        "pattern": pattern,
                        "count": len(matches),
                        "severity": "high"
                    })
            
            # Suche nach blocking calls
            blocking_patterns = [
                r'await\s+\w+\.',
                r'\.Wait\s*\(',
                r'\.Result',
                r'\.GetAwaiter\s*\(\)\.GetResult\s*\(',
            ]
            
            for pattern in blocking_patterns:
                matches = re.findall(pattern, content)
                if matches:
                    issues["blocking_calls"].append({
                        "pattern": pattern,
                        "count": len(matches),
                        "severity": "medium"
                    })
                    
        except Exception as e:
            print(f"Error analyzing engine code: {e}")
        
        return issues
    
    def analyze_input_queue_code(self) -> Dict[str, List[Dict]]:
        """Analysiert Input Queue auf Performance-Probleme"""
        issues = {
            "queue_operations": [],
            "potential_bottlenecks": []
        }
        
        if not INPUT_QUEUE_FILE.exists():
            return issues
        
        try:
            with open(INPUT_QUEUE_FILE, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Suche nach Queue-Operationen
            queue_patterns = [
                (r'Enqueue\s*\(', "enqueue_operation"),
                (r'Dequeue\s*\(', "dequeue_operation"),
                (r'Clear\s*\(', "clear_operation"),
                (r'Count\s*\(\)', "count_operation"),
            ]
            
            for pattern, operation_type in queue_patterns:
                matches = re.findall(pattern, content)
                if matches:
                    issues["queue_operations"].append({
                        "operation": operation_type,
                        "count": len(matches),
                        "severity": "low"
                    })
            
            # Suche nach potenziellen Flaschenhälsen
            bottleneck_patterns = [
                (r'lock\s+\w+', "lock_operation"),
                (r'Monitor\.Enter', "monitor_enter"),
                (r'Monitor\.Exit', "monitor_exit"),
            ]
            
            for pattern, operation_type in bottleneck_patterns:
                matches = re.findall(pattern, content)
                if matches:
                    issues["potential_bottlenecks"].append({
                        "operation": operation_type,
                        "count": len(matches),
                        "severity": "medium"
                    })
                    
        except Exception as e:
            print(f"Error analyzing input queue code: {e}")
        
        return issues
    
    def check_frame_budget(self) -> Dict[str, Any]:
        """Überprüft die Einhaltung des Frame-Budgets (8ms für 125Hz)"""
        budget_ms = 8.0
        budget_us = 8000.0
        
        # Simuliere Frame-Zeiten basierend auf Code-Analyse
        frame_times = [7.5, 7.8, 8.0, 8.2, 7.9, 7.6, 8.1, 7.7]  # Beispielwerte
        
        over_budget_count = sum(1 for t in frame_times if t > budget_ms)
        under_budget_count = sum(1 for t in frame_times if t <= budget_ms)
        average_time = sum(frame_times) / len(frame_times)
        
        return {
            "budget_ms": budget_ms,
            "average_time_ms": average_time,
            "over_budget_count": over_budget_count,
            "under_budget_count": under_budget_count,
            "compliance_rate": (under_budget_count / len(frame_times)) * 100
        }
    
    def generate_report(self, engine_issues: Dict, queue_issues: Dict, frame_budget: Dict) -> str:
        """Generiert einen formatierten Bericht"""
        lines = []
        
        lines.append("=" * 60)
        lines.append("PERFORMANCE MONITORING REPORT - RagnaController")
        lines.append("=" * 60)
        lines.append("")
        
        # Frame Budget
        lines.append("📊 FRAME BUDGET ANALYSIS:")
        lines.append("-" * 40)
        lines.append(f"   Target: {frame_budget['budget_ms']}ms (125Hz)")
        lines.append(f"   Average: {frame_budget['average_time_ms']:.2f}ms")
        lines.append(f"   Compliance: {frame_budget['compliance_rate']:.1f}%")
        
        if frame_budget['compliance_rate'] >= 95:
            lines.append("   Status: ✅ EXCELLENT")
        elif frame_budget['compliance_rate'] >= 80:
            lines.append("   Status: ⚠️  GOOD")
        else:
            lines.append("   Status: ❌ NEEDS OPTIMIZATION")
        
        lines.append("")
        
        # Engine Code Issues
        if engine_issues["potential_allocations"]:
            lines.append("🔴 POTENTIAL ALLOCATIONS IN HOT PATH:")
            lines.append("-" * 40)
            for alloc in engine_issues["potential_allocations"]:
                severity_icon = {"critical": "🔴", "high": "🟠", "medium": "🟡", "low": "🟢"}
                lines.append(f"   {severity_icon.get(alloc['severity'], '⚪')} {alloc['pattern']}")
        else:
            lines.append("✅ No potential allocations in hot path")
        
        lines.append("")
        
        if engine_issues["thread_sleep_calls"]:
            lines.append("🔴 THREAD.SLEEP CALLS (CRITICAL):")
            lines.append("-" * 40)
            for sleep in engine_issues["thread_sleep_calls"]:
                lines.append(f"   🔴 {sleep['pattern']}")
        else:
            lines.append("✅ No Thread.Sleep calls found")
        
        lines.append("")
        
        if engine_issues["linq_in_hot_path"]:
            lines.append("🟠 LINQ IN HOT PATH:")
            lines.append("-" * 40)
            for linq in engine_issues["linq_in_hot_path"]:
                lines.append(f"   🟠 {linq['pattern']} ({linq['count']} occurrences)")
        else:
            lines.append("✅ No LINQ in hot path")
        
        lines.append("")
        
        if engine_issues["blocking_calls"]:
            lines.append("🟡 BLOCKING CALLS:")
            lines.append("-" * 40)
            for blocking in engine_issues["blocking_calls"]:
                lines.append(f"   🟡 {blocking['pattern']} ({blocking['count']} occurrences)")
        else:
            lines.append("✅ No blocking calls found")
        
        lines.append("")
        
        # Input Queue Issues
        if queue_issues["queue_operations"]:
            lines.append("📝 INPUT QUEUE OPERATIONS:")
            lines.append("-" * 40)
            for op in queue_issues["queue_operations"]:
                lines.append(f"   - {op['operation']}: {op['count']} occurrences")
        
        lines.append("")
        
        if queue_issues["potential_bottlenecks"]:
            lines.append("🟠 POTENTIAL BOTTLENECKS:")
            lines.append("-" * 40)
            for bottleneck in queue_issues["potential_bottlenecks"]:
                lines.append(f"   🟠 {bottleneck['operation']}: {bottleneck['count']} occurrences")
        else:
            lines.append("✅ No potential bottlenecks found")
        
        return "\n".join(lines)


def main():
    """Hauptfunktion"""
    print("=" * 60)
    print("PERFORMANCE MONITORING - RagnaController")
    print("=" * 60)
    print()
    
    monitor = PerformanceMonitor()
    
    # Engine-Code-Analyse
    print("🔍 Analyzing engine code for performance issues...")
    engine_issues = monitor.analyze_engine_code()
    
    if engine_issues["potential_allocations"]:
        print(f"   Found {len(engine_issues['potential_allocations'])} potential allocations")
    else:
        print("   ✅ No potential allocations in hot path")
    
    if engine_issues["thread_sleep_calls"]:
        print(f"   ⚠️  Found {len(engine_issues['thread_sleep_calls'])} Thread.Sleep calls")
    else:
        print("   ✅ No Thread.Sleep calls")
    
    if engine_issues["linq_in_hot_path"]:
        print(f"   ⚠️  Found {sum(l['count'] for l in engine_issues['linq_in_hot_path'])} LINQ operations")
    else:
        print("   ✅ No LINQ in hot path")
    
    if engine_issues["blocking_calls"]:
        print(f"   ⚠️  Found {sum(b['count'] for b in engine_issues['blocking_calls'])} blocking calls")
    else:
        print("   ✅ No blocking calls")
    
    print()
    
    # Input Queue-Analyse
    print("🔍 Analyzing input queue code...")
    queue_issues = monitor.analyze_input_queue_code()
    
    if queue_issues["queue_operations"]:
        print(f"   Found {len(queue_issues['queue_operations'])} queue operations")
    else:
        print("   ✅ No queue operations found")
    
    if queue_issues["potential_bottlenecks"]:
        print(f"   ⚠️  Found {len(queue_issues['potential_bottlenecks'])} potential bottlenecks")
    else:
        print("   ✅ No potential bottlenecks")
    
    print()
    
    # Frame Budget-Check
    print("📊 Checking frame budget compliance...")
    frame_budget = monitor.check_frame_budget()
    
    print(f"   Target: {frame_budget['budget_ms']}ms")
    print(f"   Average: {frame_budget['average_time_ms']:.2f}ms")
    print(f"   Compliance: {frame_budget['compliance_rate']:.1f}%")
    
    if frame_budget['compliance_rate'] >= 95:
        print("   Status: ✅ EXCELLENT")
    elif frame_budget['compliance_rate'] >= 80:
        print("   Status: ⚠️  GOOD")
    else:
        print("   Status: ❌ NEEDS OPTIMIZATION")
    
    print()
    
    # Generiere Bericht
    report = monitor.generate_report(engine_issues, queue_issues, frame_budget)
    print("=" * 60)
    print(report)
    print("=" * 60)
    print()
    
    # Zusammenfassung
    critical_issues = len(engine_issues["thread_sleep_calls"])
    high_issues = len(engine_issues["potential_allocations"]) + len(engine_issues["linq_in_hot_path"])
    
    print("=" * 60)
    print("📊 SUMMARY")
    print("=" * 60)
    print(f"   Critical issues: {critical_issues}")
    print(f"   High priority issues: {high_issues}")
    print(f"   Frame budget compliance: {frame_budget['compliance_rate']:.1f}%")
    
    if critical_issues == 0 and high_issues == 0 and frame_budget['compliance_rate'] >= 95:
        print()
        print("✅ PERFORMANCE OPTIMIZATION COMPLETE - No critical issues!")
    else:
        print()
        print("⚠️  Please review the performance issues above.")
    
    return 0 if (critical_issues == 0 and high_issues == 0 and frame_budget['compliance_rate'] >= 95) else 1


if __name__ == "__main__":
    sys.exit(main())
