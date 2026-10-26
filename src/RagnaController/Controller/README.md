# Controller — Hardware Interaction Layer

Handles all physical controller input: XInput polling, DualSense HID communication, gyro/lightbar, and controller state snapshots.

## Files

| File | Responsibility |
|------|----------------|
|| `ButtonState.cs` | Readonly Record-Struct mit Button-Status-Flags |
|| `ControllerSnapshot.cs` | Controller-Status-Snapshot für UI-Binding |
|| `DualSenseHardwareService.cs` | DualSense-Lightbar-Farben und Adaptive Triggers via Raw HID |
|| `SnapshotBuilder.cs` | Baut ControllerSnapshot aus Engine-Zustand |
|| `XInputFallbackService.cs` | XInput-Fallback-Controller-Dienst |

## Architecture

- **ControllerService** is the main entry point for controller input
- **DualSenseHardwareService** and **DualSenseLightbarService** use Raw HID (not XInput) for PS5-specific features
- **GyroService** reads raw gyro data and applies low-pass filtering for smooth input
- **ControllerSnapshot** is a data container passed to UI for display

## Namespace

`RagnaController.Controller`
