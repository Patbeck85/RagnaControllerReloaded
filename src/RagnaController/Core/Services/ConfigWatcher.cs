using System;
using System.IO;

namespace RagnaController.Core
{
    /// <summary>
    /// TECH-059: Überwacht settings.json and profiles.json on Änderungen
    /// and feuert ein Event, damit das Profil neu geladen are kann,
    /// ohne die Anwendung neu starten to müssen (Config Hot-Reload).
    /// </summary>
    public class ConfigWatcher : IDisposable
    {
        private readonly FileSystemWatcher? _settingsWatcher;
        private readonly FileSystemWatcher? _profilesWatcher;
        private readonly object _reloadLock = new();
        private DateTime _lastReload = DateTime.MinValue;
        private readonly TimeSpan _debounce = TimeSpan.FromMilliseconds(500);

        public event Action<string>? ConfigChanged;

        public ConfigWatcher(string settingsPath, string profilesPath)
        {
            if (File.Exists(settingsPath))
            {
                _settingsWatcher = new FileSystemWatcher(
                    Path.GetDirectoryName(settingsPath)!,
                    Path.GetFileName(settingsPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _settingsWatcher.Changed += (s, e) => OnChanged("settings.json");
            }

            if (File.Exists(profilesPath))
            {
                _profilesWatcher = new FileSystemWatcher(
                    Path.GetDirectoryName(profilesPath)!,
                    Path.GetFileName(profilesPath))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _profilesWatcher.Changed += (s, e) => OnChanged("profiles.json");
            }
        }

        private void OnChanged(string fileName)
        {
            lock (_reloadLock)
            {
                var now = DateTime.Now;
                if ((now - _lastReload) < _debounce) return;
                _lastReload = now;
            }
            ConfigChanged?.Invoke(fileName);
        }

        public void Dispose()
        {
            _settingsWatcher?.Dispose();
            _profilesWatcher?.Dispose();
        }
    }
}
