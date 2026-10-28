# Profiles — Profile Management

Character profile loading, saving, and JSON serialization.

## Files

| File | Responsibility |
|------|----------------|
| `AppJsonContext.cs` | AOT-friendly System.Text.Json context |
| `BaseProfile.cs` | Base class for profile types |
| `CharacterProfileResolver.cs` | Resolves character profiles from game data |
| `CharProfile.cs` | Character-specific profile data model |
| `ClassProfile.cs` | Class-specific profile configuration |
| `Profile.cs` | JSON structure of a character profile |
| `ProfileCollection.cs` | Collection of profiles with bulk operations |
| `ProfileManager.cs` | Load/Save/Import/Export/Backup |
| `ProfileMigration.cs` | Profile data migration between versions |
| `ProfilePreset.cs` | Profile preset definition |
| `ProfilePresetManager.cs` | Manages profile presets |
| `ProfileTemplate.cs` | Profile template model |
| `ProfileTemplateManager.cs` | Manages profile templates |

## Notes

- Profiles are JSON files stored in the user data directory
- `AppJsonContext` enables high-performance AOT serialization
- ProfileManager handles backup and restore
