# Models — Data Models & DTOs

Data transfer objects and record structs used across all layers.

## Files

| File | Responsibility |
|------|----------------|
|| `AdaptiveTriggerMode.cs` | Enum für DualSense Adaptive-Trigger-Modi |
|| `BuffRegistry.cs` | Registry für aktive Buff/Debuff-Verfolgung |
|| `ButtonAction.cs` | Enum für Button-Action-Mappings |
|| `ButtonKey.cs` | Enum passend zu Win32 VK-Codes mit Button-Key-Mappings |
|| `ChordConfig.cs` | Konfiguration für Akkord-/Button-Kombinationen |
|| `CommunityEntry.cs` | Datenmodell für Community-Hub-Einträge |
|| `ItemConfig.cs` | Konfiguration für Spielgegenstände |
|| `MobDatabase.cs` | Monster-Datenbank-Modell |
|| `Monster.cs` | Einzelnes Monster-Datenmodell |
|| `ParsedInput.cs` | Readonly Record-Struct (aktueller/vorheriger Frame-Input) |
|| `Settings.cs` | Globale App-Einstellungen (AppData) |
|| `StorageDropPlanner.cs` | Storage-Drop-Organisations-Planer |
|| `UpdateInfo.cs` | Update-Prüfungs-Informationen-Modell |
|| `VirtualKey.cs` | Enum passend zu Win32 VK-Codes |

## Notes

- All models are immutable where possible (`readonly record struct`)
- `VirtualKey` is defined here (NOT `System.Windows.Input.VirtualKey`)
- Settings are persisted to AppData
