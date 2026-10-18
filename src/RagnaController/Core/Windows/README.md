# Core/Windows — Window Management & Tracking

Win32 window tracking, multi-client support, and window switching.

## Files

| File | Responsibility |
|------|----------------|
| `WindowTracker.cs` | WinEventHook for game client bounds/DPI |
| `MultiClientWindowTracker.cs` | Multi-client window tracking |
| `WindowSwitcher.cs` | Instant multi-client switching |
| `DeviceNotificationWindow.cs` | Device change notifications |
| `IWindowTracker.cs` | Window tracker interface |
| `EnumWindowsProcDelegate.cs` | Win32 enum windows callback |

## Notes

- `WindowTracker` hooks into Win32 events to detect RO client windows
- `MultiClientWindowTracker` supports multiple RO client instances
- `WindowSwitcher` enables instant switching between clients
