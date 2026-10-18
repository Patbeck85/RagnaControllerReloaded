# Core/Services — Background Services & Managers

All background services, managers, and long-running components.

## Files

| File | Responsibility |
|------|----------------|
| `TelemetryService.cs` | Usage telemetry & analytics |
| `ProfileService.cs` | Profile CRUD operations |
| `ControllerService.cs` | High-level controller orchestration |
| `GyroService.cs` | Gyro service wrapper |
| `SystemMonitor.cs` | Focus Lock evaluation (500ms polling) |
| `PowerMonitor.cs` | Windows Sleep/Resume & Battery state |
| `ProfileValidator.cs` | Profile validation logic |
| `ConfigWatcher.cs` | Configuration file watcher |
| `ClassDetector.cs` | Weighted heuristic auto-class detection (FEAT-009) |
| `BuffManager.cs` | Active buff/debuff tracking (FEAT-008) |
| `SessionRecorder.cs` | JSONL session replay recorder |
| `StandbyManager.cs` | Smart Standby AFK detection |
| `SmartCursorService.cs` | D-Pad grid-hopping & precision damping |
| `ActionLogService.cs` | Event tracking & Channel-based logging |
| `EngineWatchdog.cs` | Tick latency monitoring |
| `LocalizationManager.cs` | i18n language switching |
| `ProfileApplier.cs` | Profile live parameter updates |
| `ProfileShareService.cs` | GitHub Gists API for community profiles |
| `DefaultProfileLoader.cs` | Built-in profile loading |
| `DiscordRpcService.cs` | Discord Rich Presence integration |
| `RoUiMenuService.cs` | RO in-game menu integration |
| `RadialMenuService.cs` | Radial menu service |
| `TtsAnnouncerService.cs` | Text-to-speech announcements |
| `HapticFeedbackService.cs` | Haptic feedback control |
| `GamepadUiNavigator.cs` | Gamepad UI navigation |
| `FeedbackRumbleProvider.cs` | Rumble feedback provider |
| `NullFeedbackProvider.cs` | Null feedback provider (disabled) |
| `ETWProvider.cs` | Event Tracing for Windows provider |
| `BatteryOptimizationService.cs` | Battery optimization |
| `AutoStartManager.cs` | Auto-start on Windows boot |
| `BackgroundTickProvider.cs` | Background tick timing |
| `WpfTickProvider.cs` | WPF tick provider |
| `HandheldDetector.cs` | Handheld device detection |
| `HandheldModeManager.cs` | PC Handheld integration |
| `LongPressTracker.cs` | Long-press detection |
| `MemoryAllocationTracker.cs` | Memory allocation tracking |
| `MonitorHelper.cs` | Monitor/DPI helper |
| `UpdateChecker.cs` | Update check service |
| `VoiceChatService.cs` | Windows Speech Recognition (Voice-to-Chat) |
| `OverlayRouter.cs` | Overlay window routing |
| `PartyManager.cs` | Party management |
| `PowerModeService.cs` | Windows power mode service |
| `CooldownManager.cs` | Cooldown tracking |

## Notes

- Services are registered in the DI container in `App.xaml.cs`
- Most services implement `IDisposable` for proper cleanup
- `ClassDetector` and `BuffManager` are key features (FEAT-009, FEAT-008)
