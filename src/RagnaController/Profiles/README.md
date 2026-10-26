# Profiles — Profile Management

Character profile loading, saving, and JSON serialization.

## Files

| File | Responsibility |
|------|----------------|
|| `AppJsonContext.cs` | AOT-freundlicher System.Text.Json-Kontext |
|| `BaseProfile.cs` | Basisklasse für Profiltypen |
|| `CharacterProfileResolver.cs` | Löst Charakterprofile aus Spieldaten auf |
|| `CharProfile.cs` | Charakter-spezifisches Profil-Datenmodell |
|| `ClassProfile.cs` | Klasse-spezifische Profil-Konfiguration |
|| `Profile.cs` | JSON-Struktur eines Charakterprofils |
|| `ProfileCollection.cs` | Sammlung von Profilen mit Bulk-Operationen |
|| `ProfileManager.cs` | Load/Save/Import/Export/Backup |
|| `ProfileMigration.cs` | Profildaten-Migration zwischen Versionen |
|| `ProfilePreset.cs` | Profil-Preset-Definition |
|| `ProfilePresetManager.cs` | Verwaltet Profil-Presets |
|| `ProfileTemplate.cs` | Profil-Vorlagenmodell |
|| `ProfileTemplateManager.cs` | Verwaltet Profil-Vorlagen |

## Notes

- Profiles are JSON files stored in user data directory
- `AppJsonContext` enables high-performance AOT serialization
- ProfileManager handles backup and restore
