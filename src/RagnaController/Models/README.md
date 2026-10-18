# Models — Data Models & DTOs

Data transfer objects and record structs used across all layers.

## Files

| File | Responsibility |
|------|----------------|
| `ParsedInput.cs` | Readonly record struct (current/prev frame input) |
| `ControllerState.cs` | Readonly record struct for UI binding |
| `VirtualKey.cs` | Enum matching Win32 VK codes |
| `Settings.cs` | Global app settings (AppData) |
| *(additional model classes)* | Various data models |

## Notes

- All models are immutable where possible (`readonly record struct`)
- `VirtualKey` is defined here (NOT `System.Windows.Input.VirtualKey`)
- Settings are persisted to AppData
