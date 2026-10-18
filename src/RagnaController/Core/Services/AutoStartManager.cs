using System;
using Microsoft.Win32;

namespace RagnaController.Core
{
    /// <summary>
    /// Windows Registry Integration für Auto-Start beim Systemstart.
    /// </summary>
    public static class AutoStartManager
    {
        private const string AppName = "RagnaController";

        /// <summary>
        /// Aktiviert oder deaktiviert den automatischen Start beim Windows-Boot.
        /// </summary>
        /// <param name="enable">true, um Auto-Start zu aktivieren; false, um zu deaktivieren.</param>
        public static void SetAutoStart(bool enable)
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", 
                    writable: true);

                if (key == null) return;

                if (enable)
                {
                    // Pfad zur aktuellen Executable-Datei ermitteln und in Anführungszeichen einhüllen
                    string exePath = $"\"{Environment.ProcessPath}\"";
                    key.SetValue(AppName, exePath);
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoStart] Error toggling registry key: {ex.Message}");
            }
        }
    }
}
