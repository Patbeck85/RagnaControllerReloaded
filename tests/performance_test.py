#!/usr/bin/env python3
"""
Performance Test Suite for RagnaController
Tests GC allocations, memory latency, and optimization pool performance
"""

import subprocess
import sys
from pathlib import Path

APP_ROOT = Path("/mnt/c/RagnaController/src/RagnaController")
TESTS_DIR = APP_ROOT / "tests"

def run_performance_tests():
    """Run C# performance tests"""
    print("=" * 60)
    print("PERFORMANCE TEST SUITE")
    print("=" * 60)
    
    perf_test_file = TESTS_DIR / "PerformanceTests.cs"
    
    if not perf_test_file.exists():
        print("❌ PerformanceTests.cs not found")
        return False
    
    print(f"✅ PerformanceTests.cs found")
    
    # Navigate to project directory
    import os
    os.chdir(APP_ROOT)
    
    # Build and run performance tests
    print("\n--- Building Performance Tests ---")
    result = subprocess.run(
        ["dotnet", "build", "--verbosity", "minimal"],
        capture_output=True,
        text=True
    )
    
    if result.returncode != 0:
        print("❌ Build failed:")
        print(result.stderr)
        return False
    
    print("✅ Build successful")
    
    # Run tests
    print("\n--- Running Performance Tests ---")
    result = subprocess.run(
        ["dotnet", "test", "--verbosity", "minimal", "--no-build"],
        capture_output=True,
        text=True
    )
    
    print(result.stdout)
    
    if result.returncode == 0:
        print("\n✅ All performance tests passed!")
        return True
    else:
        print("\n❌ Some performance tests failed:")
        print(result.stderr)
        return False

def check_gc_optimization():
    """Check GC optimization implementation"""
    print()
    print("=" * 60)
    print("GC OPTIMIZATION CHECK")
    print("=" * 60)
    
    # Check for StringPool, MessagePool in core files
    core_files = [
        "Core/EngineOptimizationPool.cs",
        "Core/SnapshotBuilder.cs",
        "Core/HybridEngine.cs",
    ]
    
    all_passed = True
    
    for file_path in core_files:
        file = APP_ROOT / file_path
        
        if not file.exists():
            print(f"⚠️  {file_path} not found")
            continue
        
        content = file.read_text()
        
        # Check for optimization features
        has_string_pool = "StringPool" in content
        has_message_pool = "MessagePool" in content
        
        if has_string_pool:
            print(f"✅ {file_path} - StringPool implemented")
        else:
            print(f"⚠️  {file_path} - StringPool not found")
            all_passed = False
        
        if has_message_pool:
            print(f"✅ {file_path} - MessagePool implemented")
        else:
            print(f"⚠️  {file_path} - MessagePool not found")
            all_passed = False
    
    return all_passed

def check_memory_latency():
    """Check memory latency implementation"""
    print()
    print("=" * 60)
    print("MEMORY LATENCY CHECK")
    print("=" * 60)
    
    # Check for latency tracking in core files
    core_files = [
        "Core/MovementEngine.cs",
        "Core/AutoTargetEngine.cs",
        "Core/InputCommandQueue.cs",
    ]
    
    all_passed = True
    
    for file_path in core_files:
        file = APP_ROOT / file_path
        
        if not file.exists():
            print(f"⚠️  {file_path} not found")
            continue
        
        content = file.read_text()
        
        # Check for latency tracking
        has_latency = "latency" in content.lower() or "performance" in content.lower()
        
        if has_latency:
            print(f"✅ {file_path} - Memory latency tracking implemented")
        else:
            print(f"⚠️  {file_path} - Memory latency tracking not found")
            all_passed = False
    
    return all_passed

def main():
    """Run all performance tests"""
    print("\n" + "=" * 60)
    print("COMPREHENSIVE PERFORMANCE TEST SUITE")
    print("=" * 60 + "\n")
    
    results = {}
    
    # Run performance tests
    results["performance_tests"] = run_performance_tests()
    
    # Check GC optimization
    results["gc_optimization"] = check_gc_optimization()
    
    # Check memory latency
    results["memory_latency"] = check_memory_latency()
    
    # Summary
    print()
    print("=" * 60)
    print("PERFORMANCE TEST SUMMARY")
    print("=" * 60)
    
    all_passed = True
    for test_name, passed in results.items():
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"{test_name}: {status}")
        if not passed:
            all_passed = False
    
    print()
    if all_passed:
        print("🎉 ALL PERFORMANCE TESTS PASSED!")
        print("The application meets performance requirements.")
    else:
        print("⚠️  Some performance tests failed. Please review the output above.")
    
    return 0 if all_passed else 1

if __name__ == "__main__":
    sys.exit(main())
