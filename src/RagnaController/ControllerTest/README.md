# ControllerTest — Test Harness

Test window and diagnostic tools for controller hardware testing and validation.

## Files

| File | Responsibility |
|------|----------------|
| `ControllerTestWindow.xaml` / `.cs` | WPF window showing all button states and thumbstick positions for testing |
| `ControllerDiagnosticRunner.cs` | Runs 16 mandatory controller checks (face buttons, L1/R1, Start/Back, etc.) |
| `ControllerSessionRecorder.cs` | Records controller sessions for replay testing |

## Notes

- Used for hardware validation during development
- Not part of the release build (test-only)
