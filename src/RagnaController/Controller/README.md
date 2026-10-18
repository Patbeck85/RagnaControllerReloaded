# Controller — Hardware Interaction Layer

Handles all physical controller input: XInput polling, DualSense HID communication, gyro/lightbar, and controller state snapshots.

## Files

| File | Responsibility |
|------|----------------|
| `ControllerService.cs` | XInput polling, WMI brand detection, controller enumeration |
| `DualSenseHardwareService.cs` | DualSense Lightbar colors and Adaptive Triggers via Raw HID |
| `DualSenseLightbarService.cs` | PS5 LED color control via Raw HID USB reports |
| `GyroService.cs` | Raw HID gyro reading with low-pass filtering |
| `ControllerSnapshot.cs` | Controller state snapshot for UI binding |

## Architecture

- **ControllerService** is the main entry point for controller input
- **DualSenseHardwareService** and **DualSenseLightbarService** use Raw HID (not XInput) for PS5-specific features
- **GyroService** reads raw gyro data and applies low-pass filtering for smooth input
- **ControllerSnapshot** is a data container passed to UI for display

## Namespace

`RagnaController.Controller`
