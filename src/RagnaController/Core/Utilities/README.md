# Core/Utilities — Utility Classes & Helpers

General-purpose utilities, helpers, and shared infrastructure.

## Files

| File | Responsibility |
|------|----------------|
| `EngineOrchestrator.cs` | Main tick coordination, lifecycle management |
| `EngineOptimizationPool.cs` | String pooling for hot-path allocations |
| `AppVersion.cs` | Application version info |
| `Messages.cs` | App-wide message constants |
| `RoFeatures.cs` | RO feature flags & detection |
| `NativeMethods.cs` | Win32 P/Invoke declarations |
| `NativeMethodsImpl.cs` | Native methods implementation |
| `NativeStructs.cs` | Win32 struct definitions |
| `ProcessNameResolver.cs` | Process name resolution |
| `RagnaControllerETW.cs` | ETW event source |
| `ShareCodeValidator.cs` | Share code validation |
| `SnapshotBuilder.cs` | Builds ControllerSnapshot from engine state |
| `AdvancedLogger.cs` | High-performance logger |
| `FeedbackSystem.cs` | User feedback collection |
| `GestureRecognizer.cs` | Gesture recognition |
| `GpuOverlayProfiler.cs` | GPU overlay profiling |
| `KiteStates.cs` | Kite engine state definitions |
| `MainViewModel.cs` | Main WPF ViewModel (MVVM) |
| `ObjectPools.cs` | Object pooling utilities |
| `Pools.cs` | Generic pool implementations |
| `EngineWatchdog.cs` | Tick latency watchdog |

## Notes

- `EngineOrchestrator` is the heartbeat — coordinates all engines at 125Hz
- `EngineOptimizationPool` prevents string allocations in hot paths
- `SnapshotBuilder` constructs `ControllerSnapshot` from engine state for UI binding
- All utilities are in `RagnaController.Core` namespace
