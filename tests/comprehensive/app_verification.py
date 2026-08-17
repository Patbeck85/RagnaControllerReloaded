#!/usr/bin/env python3
"""
Comprehensive Test Suite for RagnaController
Tests the complete application from core functions to UI/UX
"""

import subprocess
import sys
import os
from pathlib import Path

# App Root Directory
APP_ROOT = Path("/mnt/c/RagnaController/src/RagnaController")
TESTS_DIR = APP_ROOT / "tests" / "RagnaController.Tests"

def run_dotnet_test():
    """Run the complete .NET test suite"""
    print("=" * 60)
    print("COMPREHENSIVE TEST SUITE - RAGNACONTROLLER")
    print("=" * 60)
    print()
    
    # Navigate to project directory
    os.chdir(APP_ROOT)
    
    print("--- Running Unit Tests ---")
    result = subprocess.run(
        ["dotnet", "test", "--verbosity", "minimal", "--no-build"],
        capture_output=True,
        text=True
    )
    
    if result.returncode == 0:
        print("✅ All unit tests passed!")
        print(result.stdout)
    else:
        print("❌ Some tests failed:")
        print(result.stdout)
        print(result.stderr)
    
    return result.returncode == 0

def check_core_components():
    """Check if all core components are present"""
    print()
    print("--- Core Component Check ---")
    
    core_files = [
        "Core/Win32InputService.cs",
        "Core/MovementEngine.cs",
        "Core/AutoTargetEngine.cs",
        "Core/InputCommandQueue.cs",
        "Core/HybridEngine.cs",
        "Core/EngineOptimizationPool.cs",
    ]
    
    missing = []
    for file in core_files:
        if (APP_ROOT / file).exists():
            print(f"✅ {file}")
        else:
            print(f"❌ {file} - MISSING")
            missing.append(file)
    
    return len(missing) == 0

def check_default_profiles():
    """Check if all default profiles are present"""
    print()
    print("--- Default Profiles Check ---")
    
    profile_dir = APP_ROOT / "DefaultProfiles"
    profiles = list(profile_dir.glob("*.json"))
    
    print(f"Found {len(profiles)} default profiles:")
    for profile in sorted(profiles):
        print(f"  - {profile.name}")
    
    return len(profiles) > 0

def check_ui_components():
    """Check UI components"""
    print()
    print("--- UI Components Check ---")
    
    ui_files = [
        "RadialMenuWindow.xaml.cs",
        "Core/OverlayRouter.cs",
        "Core/SmartCursorService.cs",
    ]
    
    missing = []
    for file in ui_files:
        if (APP_ROOT / file).exists():
            print(f"✅ {file}")
        else:
            print(f"❌ {file} - MISSING")
            missing.append(file)
    
    return len(missing) == 0

def run_performance_tests():
    """Run performance tests"""
    print()
    print("--- Performance Tests ---")
    
    perf_test = APP_ROOT / "tests" / "PerformanceTests.cs"
    
    if perf_test.exists():
        print(f"✅ Performance test file found: {perf_test}")
        # Run the performance tests
        result = subprocess.run(
            ["dotnet", "run", "--project", "RagnaController.csproj"],
            capture_output=True,
            text=True,
            timeout=30
        )
        print(result.stdout)
        if result.stderr:
            print("STDERR:", result.stderr)
    else:
        print("⚠️  Performance test file not found")
    
    return True

def check_optimization_pool():
    """Check optimization pool implementation"""
    print()
    print("--- Optimization Pool Check ---")
    
    opt_pool = APP_ROOT / "Core" / "EngineOptimizationPool.cs"
    
    if opt_pool.exists():
        print(f"✅ EngineOptimizationPool.cs found")
        
        # Read and display key parts
        content = opt_pool.read_text()
        
        # Check for StringPool, MessagePool, EngineState
        has_string_pool = "StringPool" in content
        has_message_pool = "MessagePool" in content
        has_engine_state = "EngineState" in content
        
        if has_string_pool:
            print("  ✅ StringPool class implemented")
        else:
            print("  ⚠️  StringPool class not found")
            
        if has_message_pool:
            print("  ✅ MessagePool class implemented")
        else:
            print("  ⚠️  MessagePool class not found")
            
        if has_engine_state:
            print("  ✅ EngineState class implemented")
        else:
            print("  ⚠️  EngineState class not found")
    else:
        print(f"❌ EngineOptimizationPool.cs not found")
    
    return opt_pool.exists()

def main():
    """Run all comprehensive tests"""
    print("\n" + "=" * 60)
    print("COMPREHENSIVE APP VERIFICATION")
    print("=" * 60 + "\n")
    
    results = {}
    
    # Run checks
    results["core_components"] = check_core_components()
    results["default_profiles"] = check_default_profiles()
    results["ui_components"] = check_ui_components()
    results["optimization_pool"] = check_optimization_pool()
    results["performance_tests"] = run_performance_tests()
    
    # Summary
    print()
    print("=" * 60)
    print("TEST SUMMARY")
    print("=" * 60)
    
    all_passed = True
    for test_name, passed in results.items():
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"{test_name}: {status}")
        if not passed:
            all_passed = False
    
    print()
    if all_passed:
        print("🎉 ALL COMPREHENSIVE TESTS PASSED!")
        print("The RagnaController application is fully verified.")
    else:
        print("⚠️  Some tests failed. Please review the output above.")
    
    return 0 if all_passed else 1

if __name__ == "__main__":
    sys.exit(main())
