# Profiles — Profile Management

Character profile loading, saving, and JSON serialization.

## Files

| File | Responsibility |
|------|----------------|
| `Profile.cs` | JSON structure of character profile |
| `ProfileManager.cs` | Load/Save/Import/Export/Backup |
| `AppJsonContext.cs` | AOT-friendly System.Text.Json context |

## Notes

- Profiles are JSON files stored in user data directory
- `AppJsonContext` enables high-performance AOT serialization
- ProfileManager handles backup and restore
