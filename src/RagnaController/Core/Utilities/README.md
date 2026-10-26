# Core/Utilities — Utility Classes & Helpers

General-purpose utilities, helpers, and shared infrastructure.

## Files

| File | Responsibility |
|------|----------------|
|| `AdvancedLogger.cs` | Hochperformanter Logger |
|| `AppVersion.cs` | Anwendungs-Versions-Info |
|| `EngineOptimizationPool.cs` | String-Pooling für Hot-Path-Zuweisungen |
|| `EngineOrchestrator.cs` | Haupt-Tick-Koordinierung, Lebenszyklus-Verwaltung |
|| `EngineWatchdog.cs` | Tick-Latenz-Watchdog |
|| `FeedbackSystem.cs` | Benutzer-Feedback-Sammlung |
|| `GestureRecognizer.cs` | Gestenerkennung |
|| `GpuOverlayProfiler.cs` | GPU-Overlay-Profiling |
|| `KiteStates.cs` | Kite-Engine-Status-Definitionen |
|| `MainViewModel.cs` | Haupt-WPF ViewModel (MVVM) |
|| `Messages.cs` | App-weite Nachrichten-Konstanten |
|| `NativeMethods.cs` | Win32 P/Invoke-Deklarationen |
|| `NativeMethodsImpl.cs` | Native-Methoden-Implementierung |
|| `NativeStructs.cs` | Win32-Struct-Definitionen |
|| `ObjectPools.cs` | Objekt-Pooling-Dienstprogramme |
|| `PerformanceProfiler.cs` | Performance-Profiling-Dienstprogramme |
|| `Pools.cs` | Generische Pool-Implementierungen |
|| `ProcessNameResolver.cs` | Prozessnamen-Auflösung |
|| `RagnaControllerETW.cs` | ETW-Ereignisquelle |
|| `RoFeatures.cs` | RO-Feature-Flags & -Erkennung |
|| `ShareCodeValidator.cs` | Share-Code-Validierung |

## Notes

- `EngineOrchestrator` is the heartbeat — coordinates all engines at 125Hz
- `EngineOptimizationPool` prevents string allocations in hot paths
- `SnapshotBuilder` constructs `ControllerSnapshot` from engine state for UI binding
- All utilities are in `RagnaController.Core` namespace
