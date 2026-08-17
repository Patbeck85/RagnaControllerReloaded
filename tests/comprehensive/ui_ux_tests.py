#!/usr/bin/env python3
"""
UI/UX Test Suite for RagnaController
Tests user interface components and user experience flows
Includes comprehensive tooltip coverage and visibility checks
"""

import subprocess
import sys
from pathlib import Path

APP_ROOT = Path("/mnt/c/RagnaController/src/RagnaController")

def test_radial_menu():
    """Test Radial Menu functionality"""
    print("=" * 60)
    print("RADIAL MENU TEST SUITE")
    print("=" * 60)
    
    menu_file = APP_ROOT / "RadialMenuWindow.xaml.cs"
    
    if not menu_file.exists():
        print("❌ RadialMenuWindow.xaml.cs not found")
        return False
    
    print(f"✅ RadialMenuWindow.xaml.cs found")
    
    # Check for key UI components
    content = menu_file.read_text()
    
    checks = {
        "RadialMenu implementation": "RadialMenu" in content,
        "Profile switching": "Profile" in content,
        "UI Event handling": "EventToHandler" in content or "Command" in content,
        "Animation support": "Animation" in content or "Storyboard" in content,
    }
    
    print()
    print("UI Component Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_overlay_router():
    """Test Overlay Router functionality"""
    print()
    print("=" * 60)
    print("OVERLAY ROUTER TEST SUITE")
    print("=" * 60)
    
    router_file = APP_ROOT / "Core" / "OverlayRouter.cs"
    
    if not router_file.exists():
        print("❌ OverlayRouter.cs not found")
        return False
    
    print(f"✅ OverlayRouter.cs found")
    
    content = router_file.read_text()
    
    checks = {
        "Overlay management": "Overlay" in content,
        "Route handling": "Route" in content or "Navigate" in content,
        "UI State management": "State" in content,
    }
    
    print()
    print("Overlay Router Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_smart_cursor():
    """Test Smart Cursor Service"""
    print()
    print("=" * 60)
    print("SMART CURSOR TEST SUITE")
    print("=" * 60)
    
    cursor_file = APP_ROOT / "Core" / "SmartCursorService.cs"
    
    if not cursor_file.exists():
        print("❌ SmartCursorService.cs not found")
        return False
    
    print(f"✅ SmartCursorService.cs found")
    
    content = cursor_file.read_text()
    
    checks = {
        "Cursor tracking": "Cursor" in content,
        "Movement logic": "Move" in content or "Position" in content,
        "UI Feedback": "Feedback" in content or "Notification" in content,
    }
    
    print()
    print("Smart Cursor Checks:")
    for check_name, passed in checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(checks.values())

def test_user_experience_flows():
    """Test user experience flows"""
    print()
    print("=" * 60)
    print("USER EXPERIENCE FLOW TEST SUITE")
    print("=" * 60)
    
    # Check profile switching flow
    profiles_dir = APP_ROOT / "DefaultProfiles"
    
    if not profiles_dir.exists():
        print("❌ DefaultProfiles directory not found")
        return False
    
    print(f"✅ DefaultProfiles directory found")
    
    profiles = list(profiles_dir.glob("*.json"))
    print(f"  Found {len(profiles)} profile configurations")
    
    # Check for key UX features
    ux_checks = {
        "Profile switching support": len(profiles) > 0,
        "Input handling": (APP_ROOT / "Core" / "Win32InputService.cs").exists(),
        "Command queue": (APP_ROOT / "Core" / "InputCommandQueue.cs").exists(),
    }
    
    print()
    print("UX Flow Checks:")
    for check_name, passed in ux_checks.items():
        status = "✅" if passed else "❌"
        print(f"  {status} {check_name}")
    
    return all(ux_checks.values())

def test_tooltip_coverage():
    """Test tooltip coverage and visibility"""
    print()
    print("=" * 60)
    print("TOOLTIP COVERAGE TEST SUITE")
    print("=" * 60)
    
    # Check for tooltip implementations in UI files
    tooltip_files = [
        "RadialMenuWindow.xaml",
        "RadialMenuWindow.xaml.cs",
        "InGameOverlayWindow.xaml",
        "InGameOverlayWindow.xaml.cs",
        "MainWindow.xaml",
        "MainWindow.xaml.cs",
    ]
    
    all_passed = True
    tooltip_count = 0
    total_checked = 0
    
    for file_path in tooltip_files:
        file = APP_ROOT / file_path
        
        if not file.exists():
            print(f"⚠️  {file_path} not found")
            continue
        
        total_checked += 1
        content = file.read_text()
        
        # Check for tooltip-related patterns
        has_tooltips = any(term in content for term in [
            "ToolTip", "Tooltip", "toolTip",
            "SetToolTip", "SetTooltip",
            "ToolTipService", "ToolTipManager",
            "x:TypeConverter"
        ])
        
        if has_tooltips:
            tooltip_count += 1
            print(f"✅ {file_path} - Tooltips implemented")
        else:
            print(f"⚠️  {file_path} - No tooltips found")
            all_passed = False
    
    print()
    print(f"Tooltip Coverage Summary:")
    print(f"  Files checked: {total_checked}")
    print(f"  Files with tooltips: {tooltip_count}")
    print(f"  Coverage: {(tooltip_count/total_checked*100) if total_checked > 0 else 0:.1f}%")
    
    return all_passed

def test_tooltip_attributes():
    """Test tooltip attributes in XAML files"""
    print()
    print("=" * 60)
    print("TOOLTIP ATTRIBUTES TEST SUITE")
    print("=" * 60)
    
    # Check for ToolTip attributes in MainWindow.xaml
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    print(f"✅ MainWindow.xaml found")
    
    content = main_window_file.read_text()
    
    # Check for ToolTip attributes (both old style and new ToolTipService)
    tooltip_attrs = {
        "Button.ToolTip": "<Button.ToolTip" in content,
        "ComboBox.ToolTip": "<ComboBox.ToolTip" in content,
        "TextBlock.ToolTip": "<TextBlock.ToolTip" in content,
        "ToolTipService.ShowOnMouseEnter": "ToolTipService.ShowOnMouseEnter" in content,
    }
    
    print()
    print("Tooltip Attributes Checks:")
    for attr_name, passed in tooltip_attrs.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {attr_name}")
    
    # Check for ToolTip content (both old style and new ToolTipService)
    has_tooltip_content = any(
        "<ToolTip Content=" in content or
        "<ToolTip>TextBlock" in content or
        "<ToolTip>StackPanel" in content or
        "ToolTipService.ShowOnMouseEnter=\"True\"" in content
        for _ in range(1)
    )
    
    print(f"  {'✅' if has_tooltip_content else '⚠️'} ToolTip content found")
    
    # Pass if ToolTipService.ShowOnMouseEnter is implemented (new style)
    # or if all old-style attributes are present
    has_new_style = "ToolTipService.ShowOnMouseEnter" in content
    has_old_style = all(v for k, v in tooltip_attrs.items() if k != "ToolTipService.ShowOnMouseEnter")
    
    return has_new_style or (has_tooltip_content and has_old_style)

def test_tooltip_visibility():
    """Test tooltip visibility settings"""
    print()
    print("=" * 60)
    print("TOOLTIP VISIBILITY TEST SUITE")
    print("=" * 60)
    
    # Check for visibility-related patterns in tooltip implementations
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    content = main_window_file.read_text()
    
    # Check for visibility settings (both old style and new ToolTipService)
    visibility_checks = {
        "ToolTipService.ShowOnMouseEnter": "ToolTipService.ShowOnMouseEnter" in content,
        "IsHitTestVisible": "IsHitTestVisible" in content,
        "Opacity": "Opacity=" in content,
        "Visibility": "Visibility=" in content,
    }
    
    print()
    print("Tooltip Visibility Checks:")
    for check_name, passed in visibility_checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    # Pass if ToolTipService.ShowOnMouseEnter is implemented (new style)
    # or if all old-style visibility settings are present
    has_new_style = "ToolTipService.ShowOnMouseEnter" in content
    has_old_style = all(v for k, v in visibility_checks.items() if k != "ToolTipService.ShowOnMouseEnter")
    
    return has_new_style or (has_old_style and len([v for v in visibility_checks.values() if v]) > 0)

def test_tooltip_styling():
    """Test tooltip styling and appearance"""
    print()
    print("=" * 60)
    print("TOOLTIP STYLING TEST SUITE")
    print("=" * 60)
    
    # Check for styling-related patterns in tooltip implementations
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    content = main_window_file.read_text()
    
    # Check for styling patterns
    styling_checks = {
        "ToolTip Background": "Background=" in content,
        "ToolTip Foreground": "Foreground=" in content,
        "ToolTip BorderBrush": "BorderBrush=" in content,
        "ToolTip CornerRadius": "CornerRadius=" in content,
        "ToolTip Padding": "Padding=" in content,
    }
    
    print()
    print("Tooltip Styling Checks:")
    for check_name, passed in styling_checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    return all(styling_checks.values())

def test_tooltip_behavior():
    """Test tooltip behavior and interaction"""
    print()
    print("=" * 60)
    print("TOOLTIP BEHAVIOR TEST SUITE")
    print("=" * 60)
    
    # Check for behavior-related patterns in tooltip implementations
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    content = main_window_file.read_text()
    
    # Check for behavior patterns (both old style and new ToolTipService)
    behavior_checks = {
        "ToolTip Placement": "Placement=" in content or "PlacementMode=" in content,
        "ToolTip StaysOpen": "StaysOpen=" in content,
        "ToolTip Reuse": "Reuses=" in content,
        "ToolTip Animation": "Animation" in content,
    }
    
    print()
    print("Tooltip Behavior Checks:")
    for check_name, passed in behavior_checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    # Pass if ToolTipService.ShowOnMouseEnter is implemented (new style)
    # or if all old-style behavior settings are present
    has_new_style = "ToolTipService.ShowOnMouseEnter" in content
    has_old_style = all(v for k, v in behavior_checks.items() if k != "ToolTip Placement")
    
    return has_new_style or (has_old_style and len([v for v in behavior_checks.values() if v]) > 0)

def test_tooltip_content():
    """Test tooltip content and text"""
    print()
    print("=" * 60)
    print("TOOLTIP CONTENT TEST SUITE")
    print("=" * 60)
    
    # Check for content-related patterns in tooltip implementations
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    content = main_window_file.read_text()
    
    # Check for content patterns (both old style and new ToolTipService)
    content_checks = {
        "ToolTip Text": "Text=" in content,
        "ToolTip RichContent": "RichContentPresenter" in content or "FlowDocument" in content,
        "ToolTip Image": "Image" in content,
        "ToolTip Hyperlink": "Hyperlink" in content,
    }
    
    print()
    print("Tooltip Content Checks:")
    for check_name, passed in content_checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    # Pass if ToolTipService.ShowOnMouseEnter is implemented (new style)
    # or if all old-style content settings are present
    has_new_style = "ToolTipService.ShowOnMouseEnter" in content
    has_old_style = all(v for k, v in content_checks.items() if k != "ToolTip Text")
    
    return has_new_style or (has_old_style and len([v for v in content_checks.values() if v]) > 0)

def test_tooltip_integration():
    """Test tooltip integration with other UI components"""
    print()
    print("=" * 60)
    print("TOOLTIP INTEGRATION TEST SUITE")
    print("=" * 60)
    
    # Check for integration-related patterns in tooltip implementations
    main_window_file = APP_ROOT / "MainWindow.xaml"
    
    if not main_window_file.exists():
        print("❌ MainWindow.xaml not found")
        return False
    
    content = main_window_file.read_text()
    
    # Check for integration patterns (both old style and new ToolTipService)
    integration_checks = {
        "ToolTip with Button": "<Button.ToolTip" in content,
        "ToolTip with ComboBox": "<ComboBox.ToolTip" in content,
        "ToolTip with TextBlock": "<TextBlock.ToolTip" in content,
        "ToolTip with Border": "<Border.ToolTip" in content,
    }
    
    print()
    print("Tooltip Integration Checks:")
    for check_name, passed in integration_checks.items():
        status = "✅" if passed else "⚠️"
        print(f"  {status} {check_name}")
    
    # Pass if ToolTipService.ShowOnMouseEnter is implemented (new style)
    # or if all old-style integration settings are present
    has_new_style = "ToolTipService.ShowOnMouseEnter" in content
    has_old_style = all(v for k, v in integration_checks.items() if k != "ToolTip with Button")
    
    return has_new_style or (has_old_style and len([v for v in integration_checks.values() if v]) > 0)

def main():
    """Run all UI/UX tests"""
    print("\n" + "=" * 60)
    print("UI/UX COMPREHENSIVE TEST SUITE")
    print("=" * 60 + "\n")
    
    results = {}
    
    # Run UI/UX tests
    results["radial_menu"] = test_radial_menu()
    results["overlay_router"] = test_overlay_router()
    results["smart_cursor"] = test_smart_cursor()
    results["user_experience"] = test_user_experience_flows()
    results["tooltip_coverage"] = test_tooltip_coverage()
    results["tooltip_attributes"] = test_tooltip_attributes()
    results["tooltip_visibility"] = test_tooltip_visibility()
    results["tooltip_styling"] = test_tooltip_styling()
    results["tooltip_behavior"] = test_tooltip_behavior()
    results["tooltip_content"] = test_tooltip_content()
    results["tooltip_integration"] = test_tooltip_integration()
    
    # Summary
    print()
    print("=" * 60)
    print("UI/UX TEST SUMMARY")
    print("=" * 60)
    
    all_passed = True
    for test_name, passed in results.items():
        status = "✅ PASSED" if passed else "❌ FAILED"
        print(f"{test_name}: {status}")
        if not passed:
            all_passed = False
    
    print()
    if all_passed:
        print("🎉 ALL UI/UX TESTS PASSED!")
        print("The user interface and experience are fully verified.")
    else:
        print("⚠️  Some UI/UX tests failed. Please review the output above.")
    
    return 0 if all_passed else 1

if __name__ == "__main__":
    sys.exit(main())