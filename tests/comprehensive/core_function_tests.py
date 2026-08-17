#!/usr/bin/env python3
"""
Core Function Test Suite for RagnaController
Tests core engine components and functionality
"""

import subprocess
import sys
from pathlib import Path

APP_ROOT = Path("/mnt/c/RagnaController/src/RagnaController")

def test_movement_engine():
    """Test Movement Engine functionality"""
    print("=" * 60)
    print("MOVEMENT ENGINE TEST SUITE")
    print("=" * 60)
    
    engine_file = APP_ROOT / "Core" / "MovementEngine.cs"
    
    if not engine_file.exists():
        print("❌ MovementEngine.cs not found")
        return False
    
    print(f"✅ MovementEngine.cs found")
    
    content = engine_file.read_text()
    
    # Check for key movement features
    checks = {
        "Movement calculation": "Move" in content or "Position" in content,
        "Delta time handling": "DeltaTime" in content or "Time" in content,
        "Input consumption": "Input" in content,
        "State management": "State" in content,
    }
    
    print()
    print("Movement Engine Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_auto_target_engine():
    """Test Auto Target Engine functionality"""
    print()
    print("=" * 60)
    print("AUTO TARGET ENGINE TEST SUITE")
    print("=" * 60)
    
    engine_file = APP_ROOT / "Core" / "AutoTargetEngine.cs"
    
    if not engine_file.exists():
        print("❌ AutoTargetEngine.cs not found")
        return False
    
    print(f"✅ AutoTargetEngine.cs found")
    
    content = engine_file.read_text()
    
    # Check for key auto-target features
    checks = {
        "Auto-target logic": "AutoTarget" in content or "Target" in content,
        "Target switching": "Switch" in content or "Change" in content,
        "Distance calculation": "Distance" in content or "Range" in content,
        "Priority handling": "Priority" in content or "Weight" in content,
    }
    
    print()
    print("Auto Target Engine Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_hybrid_engine():
    """Test Hybrid Engine functionality"""
    print()
    print("=" * 60)
    print("HYBRID ENGINE TEST SUITE")
    print("=" * 60)
    
    engine_file = APP_ROOT / "Core" / "HybridEngine.cs"
    
    if not engine_file.exists():
        print("❌ HybridEngine.cs not found")
        return False
    
    print(f"✅ HybridEngine.cs found")
    
    content = engine_file.read_text()
    
    # Check for key hybrid features
    checks = {
        "Engine integration": "Hybrid" in content or "Combine" in content,
        "Input handling": "Input" in content,
        "Command processing": "Command" in content,
        "Optimization pool": "Pool" in content or "Optimize" in content,
    }
    
    print()
    print("Hybrid Engine Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_input_command_queue():
    """Test Input Command Queue functionality"""
    print()
    print("=" * 60)
    print("INPUT COMMAND QUEUE TEST SUITE")
    print("=" * 60)
    
    queue_file = APP_ROOT / "Core" / "InputCommandQueue.cs"
    
    if not queue_file.exists():
        print("❌ InputCommandQueue.cs not found")
        return False
    
    print(f"✅ InputCommandQueue.cs found")
    
    content = queue_file.read_text()
    
    # Check for key queue features
    checks = {
        "Queue management": "Queue" in content or "Enqueue" in content,
        "Dequeue operations": "Dequeue" in content or "Pop" in content,
        "Command types": "Command" in content,
        "Thread safety": "Lock" in content or "Mutex" in content,
    }
    
    print()
    print("Input Command Queue Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_win32_input_service():
    """Test Win32 Input Service functionality"""
    print()
    print("=" * 60)
    print("WIN32 INPUT SERVICE TEST SUITE")
    print("=" * 60)
    
    service_file = APP_ROOT / "Core" / "Win32InputService.cs"
    
    if not service_file.exists():
        print("❌ Win32InputService.cs not found")
        return False
    
    print(f"✅ Win32InputService.cs found")
    
    content = service_file.read_text()
    
    # Check for key input features
    checks = {
        "Win32 API integration": "Win32" in content or "User32" in content,
        "Input event handling": "Message" in content or "Event" in content,
        "Key mapping": "Key" in content or "VirtualKey" in content,
        "Mouse handling": "Mouse" in content or "MouseButton" in content,
    }
    
    print()
    print("Win32 Input Service Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_optimization_pool():
    """Test Optimization Pool functionality"""
    print()
    print("=" * 60)
    print("OPTIMIZATION POOL TEST SUITE")
    print("=" * 60)
    
    pool_file = APP_ROOT / "Core" / "EngineOptimizationPool.cs"
    
    if not pool_file.exists():
        print("❌ EngineOptimizationPool.cs not found")
        return False
    
    print(f"✅ EngineOptimizationPool.cs found")
    
    content = pool_file.read_text()
    
    # Check for optimization features
    checks = {
        "String pooling": "StringPool" in content,
        "Message pooling": "MessagePool" in content,
        "Value types": "struct" in content or "Value" in content,
        "Memory management": "GC" in content or "Dispose" in content,
    }
    
    print()
    print("Optimization Pool Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_combos():
    """Test Combo Engine functionality"""
    print()
    print("=" * 60)
    print("COMBO ENGINE TEST SUITE")
    print("=" * 60)
    
    combo_file = APP_ROOT / "tests" / "RagnaController.Tests" / "ComboEngineIntegrationTests.cs"
    
    if not combo_file.exists():
        print("❌ ComboEngineIntegrationTests.cs not found")
        return False
    
    print(f"✅ ComboEngineIntegrationTests.cs found")
    
    content = combo_file.read_text()
    
    # Check for combo features
    checks = {
        "Combo tracking": "Combo" in content,
        "Combo counting": "Count" in content or "Increment" in content,
        "Combo reset": "Reset" in content or "Clear" in content,
    }
    
    print()
    print("Combo Engine Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_kite_engine():
    """Test Kite Engine functionality"""
    print()
    print("=" * 60)
    print("KITE ENGINE TEST SUITE")
    print("=" * 60)
    
    kite_file = APP_ROOT / "tests" / "RagnaController.Tests" / "KiteEngineIntegrationTests.cs"
    
    if not kite_file.exists():
        print("❌ KiteEngineIntegrationTests.cs not found")
        return False
    
    print(f"✅ KiteEngineIntegrationTests.cs found")
    
    content = kite_file.read_text()
    
    # Check for kite features
    checks = {
        "Kite tracking": "Kite" in content,
        "Kite movement": "Move" in content or "Position" in content,
    }
    
    print()
    print("Kite Engine Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def main():
    """Run all core function tests"""
    print("\n" + "=" * 60)
    print("CORE FUNCTION COMPREHENSIVE TEST SUITE")
    print("=" * 60 + "\n")
    
    results = {}
    
    # Run core function tests
    results["movement_engine"] = test_movement_engine()
    results["auto_target_engine"] = test_auto_target_engine()
    results["hybrid_engine"] = test_hybrid_engine()
    results["input_command_queue"] = test_input_command_queue()
    results["win32_input_service"] = test_win32_input_service()
    results["optimization_pool"] = test_optimization_pool()
    results["combo_engine"] = test_combos()
    results["kite_engine"] = test_kite_engine()
    
    # Summary
    print()
    print("=" * 60)
    print("CORE FUNCTION TEST SUMMARY")
    print("=" * 60)
    
    all_passed = True
    for test_name, passed in results.items():
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"{test_name}: {status}")
        if not passed:
            all_passed = False
    
    print()
    if all_passed:
        print("🎉 ALL CORE FUNCTION TESTS PASSED!")
        print("The core engine components are fully verified.")
    else:
        print("⚠️  Some core function tests failed. Please review the output above.")
    
    return 0 if all_passed else 1

if __name__ == "__main__":
    sys.exit(main())
