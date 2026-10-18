using System;
using System.IO;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-1631: First-Run Service.
    /// Erkennt ob die App zum ersten Mal gestartet wird (keine Einstellungen
    /// oder kein RO-Client-Pfad konfiguriert) und koordiniert den Wizard-Flow.
    /// </summary>
    public sealed class FirstRunService
    {
        private readonly Settings _settings;

        /// <summary>
        /// Erstellt eine neue FirstRunService-Instanz.
        /// </summary>
        /// <param name="settings">Die geladenen Anwendungseinstellungen.</param>
        public FirstRunService(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Gibt zurück, ob ein First-Run Wizard angezeigt werden muss.
        /// True wenn: keine settings.json existiert ODER RoExePath leer ist.
        /// </summary>
        public bool IsFirstRun =>
            string.IsNullOrWhiteSpace(_settings.RoExePath);

        /// <summary>
        /// Speichert den RO-Client-Pfad und markiert den First-Run als abgeschlossen.
        /// </summary>
        /// <param name="roExePath">Vollständiger Pfad zur RO-Client-Executable.</param>
        public void CompleteFirstRun(string roExePath)
        {
            if (string.IsNullOrWhiteSpace(roExePath))
                throw new ArgumentException("RO-Client-Pfad darf nicht leer sein.", nameof(roExePath));

            _settings.RoExePath = roExePath.Trim();
            _settings.Save();
        }

        /// <summary>
        /// Prüft ob eine angegebene Datei eine valide RO-Client-Executable ist.
        /// Akzeptiert ragexe.exe, sakexe.exe und gängige Varianten.
        /// </summary>
        /// <param name="filePath">Pfad zur zu prüfenden Datei.</param>
        /// <returns>True wenn die Datei existiert und ein bekannter RO-Client-Name ist.</returns>
        public static bool IsValidRoClient(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            var fileName = Path.GetFileName(filePath)?.ToLowerInvariant();
            return fileName is "ragexe.exe" or "sakexe.exe" or "ragexe_5th.exe" or "sakexe_5th.exe";
        }
    }
}
