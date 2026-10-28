# Models — Data Models & DTOs

Data transfer objects and record structs used across all layers.

## Files

| File | Responsibility |
|------|----------------|
| `AdaptiveTriggerMode.cs` | Enum for DualSense Adaptive Trigger modes |
| `BuffRegistry.cs` | Registry for active buff/debuff tracking |
| `ButtonAction.cs` | Enum for button action mappings |
| `ButtonKey.cs` | Enum matching Win32 VK codes with button key mappings |
| `ChordConfig.cs` | Configuration for chord/button combinations |
| `CommunityEntry.cs` | Data model for community hub entries |
| `ItemConfig.cs` | Configuration for game items |
| `MobDatabase.cs` | Monster database model |
| `Monster.cs` | Single monster data model |
| `ParsedInput.cs` | Readonly Record-Struct (current/previous frame input) |
| `Settings.cs` | Global app settings (AppData) |
| `StorageDropPlanner.cs` | Storage drop organization planner |
| `UpdateInfo.cs` | Update check information model |
| `VirtualKey.cs` | Enum matching Win32 VK codes |

## Notes

- All models are immutable where possible (`readonly record struct`)
- `VirtualKey` is defined here (NOT `System.Windows.Input.VirtualKey`)
- Settings are persisted to AppData
