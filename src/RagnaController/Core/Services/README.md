# Core/Services — Background Services & Manager

Alle Hintergrunddienste, Manager und langlaufenden Komponenten.

## Ordnerstruktur

Dienste sind nach Domänen in Unterordnern organisiert:

| Unterordner | Dienste |
|-------------|---------|
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

## Hinweise

- Dienste sind im DI-Container in `App.xaml.cs` registriert
- Die meisten Dienste implementieren `IDisposable` für ordnungsgemäße Bereinigung
- `ClassDetector` und `BuffManager` sind Schlüssel-Features (FEAT-009, FEAT-008)
- `LocalizationManager` verwaltet die i18n-Sprachumschaltung für alle 48 unterstützten Sprachen
