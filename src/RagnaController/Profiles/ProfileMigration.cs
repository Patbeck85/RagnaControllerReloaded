using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    /// <summary>
    /// TASK-2302: Profile Versioning & Migration.
    /// Verwaltet Schema-Versionen für Profile und führt automatische Migration
    /// älterer Versionen auf das aktuelle Schema durch.
    /// </summary>
    public static class ProfileMigration
    {
        /// <summary>Aktuelle Schema-Version. Wird bei jeder inkompatiblem Änderung inkrementiert.</summary>
        public const int CurrentSchemaVersion = 2;

        /// <summary>
        /// Migriert ein Profil auf das aktuelle Schema.
        /// Wenn SchemaVersion < CurrentSchemaVersion ist, werden alle
        /// Versionsübergänge nacheinander angewendet.
        /// </summary>
        /// <param name="profile">Das zu migrierende Profil (wird in-place modifiziert).</param>
        public static void Migrate(Profile profile)
        {
            if (profile == null) return;

            int currentVersion = profile.SchemaVersion;

            // Bereits aktuell — keine Migration nötig
            if (currentVersion >= CurrentSchemaVersion) return;

            // Version 1 → 2: Neue Properties hinzugefügt (LightbarColor, OverlayTheme,
            // EnableDynamicTickReduction, LongPress*, Stick*, etc.)
            // Da C# Default-Werte für Properties hat, reicht es hier,
            // die Versionsnummer zu aktualisieren. Spezifische Daten-Transformationen
            // würden hier ergänzt werden.
            if (currentVersion < 2)
            {
                MigrateV1ToV2(profile);
            }

            profile.SchemaVersion = CurrentSchemaVersion;
        }

        /// <summary>
        /// Migration von Schema-Version 1 auf 2.
        /// Füllt neu eingeführte Properties mit sinnvollen Defaults, falls sie
        /// im alten JSON nicht enthalten waren (was bei Deserialisierung bereits
        /// durch C# Default-Werte passiert, aber hier explizit dokumentiert wird).
        /// </summary>
        private static void MigrateV1ToV2(Profile profile)
        {
            // TASK-2302: Migration V1→V2
            // Alle neuen Properties haben bereits C#-Default-Werte, die
            // beim Deserialisieren automatisch übernommen werden.
            // Hier könnten bei Bedarf spezifische Transformationen ergänzt werden,
            // z.B. umbenannte Properties oder geänderte Defaults.

            // Beispiel: Wenn LightbarColor im alten JSON fehlte, ist es bereits
            // "#005AD2" (Default). Keine explizite Aktion nötig.

            // Wenn zukünftig Properties umbenannt werden, hier die Umleitung:
            // if (string.IsNullOrEmpty(profile.LightbarColor))
            //     profile.LightbarColor = "#005AD2";
        }

        /// <summary>
        /// Prüft, ob ein JSON-Profil-String eine Schema-Version enthält und gibt diese zurück.
        /// Gibt 0 zurück, wenn keine Version gefunden wird (sehr altes Format).
        /// </summary>
        public static int DetectSchemaVersion(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return 0;

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("SchemaVersion", out var versionProp))
                {
                    return versionProp.GetInt32();
                }
            }
            catch (JsonException)
            {
                // Ungültiges JSON → Version 0 annehmen
            }

            return 0;
        }

        /// <summary>
        /// Generiert einen Migrations-Report für ein Profil.
        /// Nützlich für UI-Anzeige und Logging.
        /// </summary>
        public static string GetMigrationReport(Profile profile)
        {
            if (profile == null) return "Kein Profil";

            if (profile.SchemaVersion >= CurrentSchemaVersion)
                return $"Schema v{profile.SchemaVersion} — aktuell, keine Migration nötig.";

            return $"Schema v{profile.SchemaVersion} → v{CurrentSchemaVersion} — Migration erforderlich.";
        }

        /// <summary>
        /// Führt Migration auf allen geladenen Profilen durch und speichert migrierte Profile.
        /// </summary>
        public static void MigrateAll(ProfileManager manager)
        {
            if (manager == null) return;

            foreach (var profile in manager.Profiles)
            {
                int before = profile.SchemaVersion;
                Migrate(profile);
                if (profile.SchemaVersion != before)
                {
                    manager.SaveProfile(profile);
                }
            }
        }
}
}
