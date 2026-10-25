using System;
using System.IO;
using System.Runtime.InteropServices;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-3304: LinuxSupportService — Linux/Wine Unterstützung für RagnaController.
    ///
    /// Bietet Detektion und Konfiguration für den Betrieb unter Linux/Wine:
    ///   • Betriebssystem-Detektion (Windows vs. Linux/Wine)
    ///   • SDL2-Kompatibilitätsprüfung
    ///   • Pfad-Konvertierung (Windows ↔ Linux)
    ///   • Bekannte Probleme und Workarounds dokumentieren
    ///
    /// White-Hat: Nur System-Informationsdienste, keine Spielautomatisierung.
    /// </summary>
    public sealed class LinuxSupportService
    {
        /// <summary>Ob das Betriebssystem Linux ist.</summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

        /// <summary>Ob das Betriebssystem Windows ist.</summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>Ob Wine erkannt wird (Linux mit Wine).</summary>
        public static bool IsWine =>
            IsLinux && File.Exists("/usr/bin/wine") ||
            Environment.GetEnvironmentVariable("WINEPREFIX") != null;

        /// <summary>Ob .NET 8 verfügbar ist.</summary>
        public static bool IsDotNet8Available =>
            Environment.Version.Major >= 8;

        /// <summary>SDL2-Bibliothek verfügbar unter Linux.</summary>
        public static bool IsSdl2Available =>
            IsLinux && (File.Exists("/usr/lib/x86_64-linux-gnu/libSDL2-2.0.so.0") ||
                        File.Exists("/usr/lib/libSDL2-2.0.so.0") ||
                        File.Exists("/usr/local/lib/libSDL2-2.0.so.0"));

        /// <summary>Gibt bekannte Linux/Wine Probleme zurück.</summary>
        public static string[] GetKnownIssues()
        {
            return new[]
            {
                "SDL2 unter Wine kann gelegentlich Eingabeverzögerungen verursachen",
                "Overlay-Fenster können unter bestimmten Wine-Versionen nicht transparent sein",
                "Dateipfade mit Backslash müssen unter Linux umgewandelt werden",
                "Autostart-Registrierung funktioniert nicht unter Linux (Desktop-Datei erforderlich)",
            };
        }

        /// <summary>Gibt Empfehlungen für Linux/Wine Betrieb zurück.</summary>
        public static string[] GetRecommendations()
        {
            return new[]
            {
                "Verwenden Sie Wine 7.0 oder neuer für beste SDL2-Kompatibilität",
                "Setzen Sie WINEPREFIX auf ein dediziertes Verzeichnis",
                "Aktivieren Sie DXVK für bessere Grafikleistung unter Wine",
                "Erstellen Sie eine .desktop-Datei für den Anwendungsstarter",
            };
        }

        /// <summary>Konvertiert einen Windows-Pfad in einen Linux-Pfad (für Wine).</summary>
        public static string? WindowsPathToLinux(string windowsPath)
        {
            if (string.IsNullOrWhiteSpace(windowsPath))
                return null;

            var path = windowsPath.Trim();

            // C:\Pfad\Zur\Datei -> /pfad/zur/datei
            if (path.Length >= 2 && path[1] == ':')
            {
                var drive = char.ToLowerInvariant(path[0]);
                var rest = path.Substring(2).Replace('\\', '/');
                return $"/mnt/{drive}{rest}";
            }

            return path.Replace('\\', '/');
        }

        /// <summary>Konvertiert einen Linux-Pfad in einen Windows-Pfad (für Wine).</summary>
        public static string? LinuxPathToWindows(string linuxPath)
        {
            if (string.IsNullOrWhiteSpace(linuxPath))
                return null;

            var path = linuxPath.Trim();

            // /mnt/c/Pfad/Zur/Datei -> C:\Pfad\Zur\Datei
            if (path.StartsWith("/mnt/") && path.Length > 5 && path[5] >= 'a' && path[5] <= 'z' && path[6] == '/')
            {
                var drive = char.ToUpperInvariant(path[5]);
                var rest = path.Substring(7).Replace('/', '\\');
                return $"{drive}:\\{rest}";
            }

            return path.Replace('/', '\\');
        }
    }
}
