using System;
using System.IO;
using RagnaController.Profiles;

namespace RagnaController.Core.Services.System {
    /// <summary>
    /// TASK-3303: PortableModeService — Vollständig portabler Betrieb von RagnaController.
    ///
    /// Unterstützt:
    ///   • Betrieb ohne Registry-Einträge
    ///   • AppData optional (alle Daten im Programmverzeichnis)
    ///   • Konfiguration, Profile, Logs im Programmverzeichnis
    ///   • USB-Stick-freundlich
    ///
    /// White-Hat: Nur Konfigurations-Steuerung, keine Spielautomatisierung.
    /// </summary>
    public sealed class PortableModeService
    {
        private readonly string _applicationDirectory;
        private readonly bool _isPortable;

        /// <summary>Ob der Portable-Modus aktiv ist.</summary>
        public bool IsPortable => _isPortable;

        /// <summary>Verzeichnis für Benutzerdaten (Portable: Programmverzeichnis, sonst AppData).</summary>
        public string UserDataDirectory { get; }

        /// <summary>Verzeichnis für Konfiguration.</summary>
        public string ConfigDirectory { get; }

        /// <summary>Verzeichnis für Profile.</summary>
        public string ProfilesDirectory { get; }

        /// <summary>Verzeichnis für Logs.</summary>
        public string LogsDirectory { get; }

        /// <summary>Verzeichnis für Cache.</summary>
        public string CacheDirectory { get; }

        public PortableModeService(string applicationDirectory, bool forcePortable = false)
        {
            _applicationDirectory = applicationDirectory ?? AppContext.BaseDirectory;
            _isPortable = forcePortable || DetectPortableMode();

            if (_isPortable)
            {
                // Im Portable-Modus: alles im Programmverzeichnis
                UserDataDirectory = Path.Combine(_applicationDirectory, "UserData");
                ConfigDirectory = Path.Combine(UserDataDirectory, "Config");
                ProfilesDirectory = Path.Combine(UserDataDirectory, "Profiles");
                LogsDirectory = Path.Combine(UserDataDirectory, "Logs");
                CacheDirectory = Path.Combine(UserDataDirectory, "Cache");
            }
            else
            {
                // Im Installer-Modus: Standard-Windows-Pfade (AppData)
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var appFolder = Path.Combine(appData, "RagnaController");
                UserDataDirectory = appFolder;
                ConfigDirectory = Path.Combine(appFolder, "Config");
                ProfilesDirectory = Path.Combine(appFolder, "Profiles");
                LogsDirectory = Path.Combine(appFolder, "Logs");
                CacheDirectory = Path.Combine(appFolder, "Cache");
            }
        }

        /// <summary>Erkennt ob der Portable-Modus aktiv sein sollte (Portable-Marker-Datei).</summary>
        private static bool DetectPortableMode()
        {
            var exeDir = AppContext.BaseDirectory;
            var portableMarker = Path.Combine(exeDir, "portable");
            var portableMarkerExe = Path.Combine(exeDir, "portable.txt");
            return File.Exists(portableMarker) || File.Exists(portableMarkerExe);
        }

        /// <summary>Erstellt alle benötigten Verzeichnisse.</summary>
        public void InitializeDirectories()
        {
            Directory.CreateDirectory(UserDataDirectory);
            Directory.CreateDirectory(ConfigDirectory);
            Directory.CreateDirectory(ProfilesDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(CacheDirectory);
        }

        /// <summary>Gibt den vollständigen Pfad für eine Datei im angegebenen Verzeichnis zurück.</summary>
        public string GetPath(string directory, string fileName)
        {
            return Path.Combine(directory, fileName);
        }

        /// <summary>Prüft ob eine Konfigurationsdatei existiert.</summary>
        public bool ConfigExists(string fileName)
        {
            return File.Exists(Path.Combine(ConfigDirectory, fileName));
        }

        /// <summary>Prüft ob eine Profil-Datei existiert.</summary>
        public bool ProfileExists(string fileName)
        {
            return File.Exists(Path.Combine(ProfilesDirectory, fileName));
        }
    }
}
