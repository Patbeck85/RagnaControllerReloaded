# Core/Services — Background Services & Managers

All background services, managers, and long-running components.

## Folder Structure

Services are organized by domain into subfolders:

| Subfolder | Services |
|-----------|----------|
| `Audio/` | `TtsAnnouncerService.cs`, `VoiceChatService.cs` |
| `Battery/` | `BatteryOptimizationService.cs` |
| `Config/` | `ConfigWatcher.cs`, `LocalizationManager.cs` |
| `Database/` | `MonsterDatabaseService.cs`, `MonsterDataLoader.cs` |
| `Developer/` | `ReplaySystem.cs`, `SessionRecorder.cs` |
| `Engine/` | `ControllerManager.cs`, `ControllerService.cs`, `EngineInstance.cs`, `MultiClientEngineOrchestrator.cs` |
| `FirstRun/` | `FirstRunService.cs` |
| `Gameplay/` | `BuffManager.cs`, `ClassDetector.cs`, `CooldownManager.cs`, `PartyManager.cs`, `RadialMenuService.cs`, `SmartCursorService.cs` |
| `Handheld/` | `HandheldDetector.cs`, `HandheldModeManager.cs` |
| `Hardware/` | `DualSenseLightbarService.cs`, `GyroCursorAdapter.cs`, `GyroService.cs`, `HapticFeedbackService.cs` |
| `Input/` | `ChordInputService.cs`, `FeedbackRumbleProvider.cs`, `LongPressTracker.cs`, `NullFeedbackProvider.cs` |
| `Monitor/` | `MonitorHelper.cs` |
| `Monitoring/` | `ActionLogService.cs`, `ETWProvider.cs`, `FrameBudgetMonitor.cs`, `MemoryAllocationTracker.cs`, `OcrService.cs`, `SystemMonitor.cs`, `TelemetryService.cs` |
| `Network/` | `DiscordRpcService.cs` |
| `Overlay/` | `OverlayRouter.cs`, `RoUiMenuService.cs` |
| `Power/` | `PowerModeService.cs`, `PowerMonitor.cs`, `SleepModeService.cs`, `StandbyManager.cs` |
| `Profile/` | `CloudSyncService.cs`, `DefaultProfileLoader.cs`, `ProfileApplier.cs`, `ProfileShareService.cs`, `ProfileValidator.cs` |
| `UI/` | `ColorBlindService.cs`, `AccessibilityService.cs`, `GamepadNavigationService.cs`, `AdvancedInputService.cs` |

## Notes

- Services are registered in the DI container in `App.xaml.cs`
- Most services implement `IDisposable` for proper cleanup
- `ClassDetector` and `BuffManager` are key features (FEAT-009, FEAT-008)
- `LocalizationManager` manages i18n language switching for all 48 supported languages
