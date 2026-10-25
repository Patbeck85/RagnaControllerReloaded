using System;
using Microsoft.Win32;

namespace RagnaController.Core.Services.System
{
    /// <summary>
    /// Windows Registry Integration for Auto-Start beim Systemstart.
    /// </summary>
    public static class AutoStartManager
    {
        private const string AppName = "RagnaController";

        /// <summary>
        /// Enabled or disabled den automatischen Start beim Windows-Boot.
        /// </summary>
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
                    // Determine path to current executable and wrap in quotes
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
                global::System.Diagnostics.Debug.WriteLine($"[AutoStart] Error toggling registry key: {ex.Message}");
            }
        }
    }
}
