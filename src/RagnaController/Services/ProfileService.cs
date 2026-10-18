using System;
using System.Linq;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;
using RagnaController.Views.Dialogs;

namespace RagnaController.Services
{
    /// <summary>
    /// TECH-062: Extracted from MainWindow — Profile management logic.
    ///
    /// Centralizes profile selection, persistence (LastProfileName), and application.
    /// Reduces MainWindow.xaml.cs God Class surface (~60 lines removed).
    /// </summary>
    public class ProfileService
    {
        private readonly ProfileManager _manager;
        private readonly Settings _settings;
        private readonly HybridEngine _engine;
        private readonly Action<string> _onToast;

        public ProfileService(ProfileManager manager, Settings settings, HybridEngine engine, Action<string> onToast)
        {
            _manager = manager;
            _settings = settings;
            _engine = engine;
            _onToast = onToast;
        }

        /// <summary>Gibt die Liste aller verfügbaren Profile zurück.</summary>
        public System.Collections.Generic.IEnumerable<Profile> GetAllProfiles() => _manager.Profiles;

        /// <summary>Lädt das zuletzt verwendete Profil oder das erste verfügbare.</summary>
        /// <returns>Das geladene Profil oder null, wenn keine Profile vorhanden.</summary>
        public Profile? LoadLastOrFirst()
        {
            var lastProfile = _manager.Profiles.FirstOrDefault(p => p.Name == _settings.LastProfileName)
                           ?? _manager.Profiles.FirstOrDefault();
            return lastProfile;
        }

        /// <summary>
        /// Wendet ein Profil an: lädt es in die Engine, speichert LastProfileName,
        /// und zeigt eine Toast-Benachrichtigung.
        /// </summary>
        public void ApplyProfile(Profile profile)
        {
            if (profile == null) return;

            _engine.LoadProfile(profile);
            _settings.LastProfileName = profile.Name;
            _settings.Save();
            _onToast($"Profile loaded: {profile.Name}");
        }

        /// <summary>
        /// Wechselt zum nächsten oder vorherigen Profil in der Liste (z. B. via D-Pad Quick-Switch).
        /// </summary>
        public void SwitchProfile(int delta)
        {
            if (_manager.Profiles.Count == 0) return;
            var current = _manager.ActiveProfile;
            int idx = current != null ? _manager.Profiles.IndexOf(current) : 0;
            int next = (idx + delta + _manager.Profiles.Count) % _manager.Profiles.Count;
            var nextProfile = _manager.Profiles[next];
            ApplyProfile(nextProfile);
        }

        /// <summary>Öffnet das Profile Library Fenster (modal).</summary>
        public void OpenProfileLibrary(Action onClosed)
        {
            var w = new ProfileLibraryWindow(_manager) { Owner = null };
            w.Closed += (_, _) => onClosed();
            w.ShowDialog();
        }

        /// <summary>Gibt den Namen des aktuell aktiven Profils zurück.</summary>
        public string? ActiveProfileName => _manager.ActiveProfile?.Name;
    }
}
