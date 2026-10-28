# Controller — Hardware Interaction Layer

Handles all physical controller input: XInput polling, DualSense HID communication, gyro/lightbar, and controller state snapshots.

## Files

| File | Responsibility |
|------|----------------|
| `ButtonState.cs` | Readonly Record-Struct with button status flags |
| `ControllerSnapshot.cs` | Controller status snapshot for UI binding |
| `DualSenseHardwareService.cs` | DualSense Lightbar colors and Adaptive Triggers via Raw HID |
| `SnapshotBuilder.cs` | Builds ControllerSnapshot from engine state |
| `XInputFallbackService.cs` | XInput fallback controller service |

## Architecture

- **ControllerService** is the main entry point for controller input
- **DualSenseHardwareService** and **DualSenseLightbarService** use Raw HID (not XInput) for PS5-specific features
- **GyroService** reads raw gyro data and applies low-pass filtering for smooth input
- **ControllerSnapshot** is a data container passed to UI for display

## Namespace

`RagnaController.Controller`
