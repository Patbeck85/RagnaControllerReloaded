using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using RagnaController.Profiles;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// DefaultProfileLoader — Loads built-in default character profiles.
    /// Reads JSON profile templates from DefaultProfiles/ folder.
    /// </summary>
    public static class DefaultProfileLoader
    {
        public static List<Profile> Load()
        {
            var profiles = new List<Profile>();
            // WICHTIG for Single-File EXE:
            string folder = Path.Combine(AppContext.BaseDirectory, "DefaultProfiles");

            if (!Directory.Exists(folder)) return profiles;

            foreach (var file in Directory.GetFiles(folder, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var p = JsonSerializer.Deserialize<Profile>(json, AppJsonContext.Default.Profile);
                    if (p != null)
                    {
                        p.IsBuiltIn = true;
                        profiles.Add(p);
                    }
                }
                catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Profile] Ungültig: {ex.Message}"); }
            }
            profiles.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
            return profiles;
        }
    }
}