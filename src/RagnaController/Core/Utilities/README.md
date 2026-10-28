# Core/Utilities — Utility Classes & Helpers

General-purpose utilities, helpers, and shared infrastructure.

## Files

| File | Responsibility |
|------|----------------|
| `AdvancedLogger.cs` | High-performance logger |
| `AppVersion.cs` | Application version info |
| `EngineOptimizationPool.cs` | String pooling for hot-path allocations |
| `EngineOrchestrator.cs` | Main tick coordination, lifecycle management |
| `EngineWatchdog.cs` | Tick latency watchdog |
| `FeedbackSystem.cs` | User feedback collection |
| `GestureRecognizer.cs` | Gesture recognition |
| `GpuOverlayProfiler.cs` | GPU overlay profiling |
| `KiteStates.cs` | Kite engine state definitions |
| `MainViewModel.cs` | Main WPF ViewModel (MVVM) |
| `Messages.cs` | App-wide message constants |
| `NativeMethods.cs` | Win32 P/Invoke declarations |
| `NativeMethodsImpl.cs` | Native methods implementation |
| `NativeStructs.cs` | Win32 struct definitions |
| `ObjectPools.cs` | Object pooling utilities |
| `PerformanceProfiler.cs` | Performance profiling utilities |
| `Pools.cs` | Generic pool implementations |
| `ProcessNameResolver.cs` | Process name resolution |
| `RagnaControllerETW.cs` | ETW event source |
| `RoFeatures.cs` | RO feature flags & detection |
| `ShareCodeValidator.cs` | Share code validation |

## Notes

- `EngineOrchestrator` is the heartbeat — coordinates all engines at 125Hz
- `EngineOptimizationPool` prevents string allocations in hot paths
- `SnapshotBuilder` constructs `ControllerSnapshot` from engine state for UI binding
- All utilities are in `RagnaController.Core` namespace
