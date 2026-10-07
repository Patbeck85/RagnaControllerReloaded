using RagnaController;
using System;
using System.IO;
using System.Text.Json;

namespace RagnaController.Models
{
    public enum RightStickPolicy
    {
        /// <summary>Right Stick steuert Maus/Cursor (Action Combat)</summary>
        Camera,
        /// <summary>Right Stick zielt (Target Lock / Aim Assist)</summary>
        Aim,
        /// <summary>Hybrid: Target Lock aktiv → Aim, sonst Camera</summary>
        Hybrid
    }

    public class Settings
    {
        // Path to settings.json in the AppData folder
        private static readonly object _fileLock = new();
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RagnaController", "settings.json");

        // FIX: Flag to mark if loading settings failed
        private static bool _loadFailed = false;

        // --- Die eigentlichen Einstellungen ---
        public string LastProfileName { get; set; } = "Novice";
        public string LastGameMode { get; set; } = "Ren";
        public bool SoundEnabled { get; set; } = true;
        public bool RumbleEnabled { get; set; } = true;
        public bool AutoStart { get; set; } = false;

        // ── Tray and Auto-Start Settings (v1.7.0) ──────────────────────────
                public bool MinimizeToTray { get; set; } = false;
                public bool StartWithWindows { get; set; } = false;
                public bool ShowLatency { get; set; } = false;

                /// <summary>
        /// Erzwingt Handheld-Modus unabhängig vom erkannten Gerät.
        /// Nützlich für Desktop-Nutzer mit Controller die das Big-Picture-UI bevorzugen.
        /// </summary>
        public bool ForceHandheldMode { get; set; } = false;
        public bool StartInMiniMode { get; set; } = false;

        // i18n: User's selected language (default: English)
        public string AppLanguage { get; set; } = "en";

        // NEW: Ragnarok Online .exe Path Selector
        public string RoExePath { get; set; } = "";

        // Focus Lock Einstellungen
        public bool FocusLockEnabled { get; set; } = true;
        public string FocusLockProcess { get; set; } = "ragexe"; // Standard RO Name

        public int LogLevel { get; set; } = 1; // 0=Debug, 1=Info, 2=Warning, 3=Error

        // NEW: Discord RPC
        public bool EnableDiscordRPC { get; set; } = true;

        // NEW: TTS Voice Announcements
        public bool EnableVoiceAnnouncements { get; set; } = true;

        // NEW: Haptic Metronome
        public bool EnableHapticMetronome { get; set; } = false;

        // NEW: Smart Standby (AFK Battery Saver)
        public bool EnableSmartStandby { get; set; } = true;
        public int StandbyTimeoutMinutes { get; set; } = 5;

        // NEW: Anonymous Telemetry & Crash Reporting (GDPR compliant, opt-in)
        public bool EnableTelemetry { get; set; } = false;
        public bool HasAskedForTelemetry { get; set; } = false;

        // ── ROB-002: Input-Emulation-Failover (SendInput ↔ Kernel-Service) ───
        /// <summary>SendInput-Latenz-Schwelle in ms. Flushes darüber gelten als "langsam".</summary>
        public int FailoverLatencyThresholdMs { get; set; } = 5;
        /// <summary>N aufeinanderfolgende langsame Flushes → Auto-Switch auf Kernel-Strategie.</summary>
        public int FailoverTriggerCount { get; set; } = 3;
        /// <summary>M stabile (schnelle) Flushes nach Switch → Recovery zurück zu SendInput.</summary>
        public int FailoverRecoveryCount { get; set; } = 2;

        // ── FEAT-014: Session-Replay (JSONL-Aufzeichnung für deterministische Wiedergabe) ──
        /// <summary>Session-Replay aktiviert? Aufzeichnung läuft pro Engine-Session, Rotation bei 50 MB.</summary>
        public bool EnableSessionRecording { get; set; } = false;

        // Fenster-Einstellungen (v1.6.0)
        public bool StartMinimized { get; set; } = false;
        public bool ShowControllerViz { get; set; } = true;
        /// <summary>Gespeicherte Fensterposition (X,Y). Null = zentriert.</summary>
        public double? WindowPositionX { get; set; } = null;
        public double? WindowPositionY { get; set; } = null;

        // ── Lifetime RSI Stats (v1.7.0) ───────────────────────────────────
        public long LifetimeSavedClicks { get; set; } = 0;
        public long LifetimeSavedKeystrokes { get; set; } = 0;

        /// <summary>
                /// Overlay theme variants: Neon (bright cyberpunk), Soft (subtle), Dark (minimal)
                /// </summary>
        public enum OverlayThemeType
        {
            Neon = 0,    // Bright cyberpunk colors with strong glows
            Soft = 1,    // Subtle colors with gentle glows
            Dark = 2     // Minimal, high contrast, low glow
        }

        // Overlay customization settings
        public OverlayThemeType OverlayTheme { get; set; } = OverlayThemeType.Neon;
        public double OverlayOpacity { get; set; } = 0.9;
        public double OverlayFontScale { get; set; } = 1.0;

        // ── Right Stick Policy (Action Combat) ──────────────────────────────
        /// <summary>Right Stick Policy: Camera (Action Combat), Aim (Target Lock), Hybrid (Lock=Aim, else Camera)</summary>
        public RightStickPolicy RightStickPolicy { get; set; } = RightStickPolicy.Hybrid;

        // --- Speicher- & Lade-Logik ---
        public static Settings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize(json, AppJsonContext.Default.Settings) ?? new Settings();
                }
            }
            catch (Exception ex) 
            { 
                System.Diagnostics.Debug.WriteLine($"[Settings] Load failed: {ex.Message}"); 
                _loadFailed = true;  // FIX: Mark that loading failed!
                return new Settings(); 
            }
            return new Settings();
        }

        public void Save()
        {
            // FIX: Wenn das Laden zuvor fehlgeschlagen hat, nicht überschreiben!
            if (_loadFailed)
            {
                System.Diagnostics.Debug.WriteLine("[Settings] Speichern abgebrochen - Einstellungen konnten nicht geladen werden!");
                return;
            }

            try
            {
                lock (_fileLock)
                {
                    string dir = Path.GetDirectoryName(SettingsPath)!;
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    // FileStream: kein großer string im LOH, direkt auf Disk
                    string tmp = SettingsPath + ".tmp";
                    using (var fs = new System.IO.FileStream(tmp, System.IO.FileMode.Create,
                        System.IO.FileAccess.Write, System.IO.FileShare.None))
                        JsonSerializer.Serialize(fs, this, AppJsonContext.Default.Settings);
                    File.Move(tmp, SettingsPath, overwrite: true);
                }
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[Settings] Save failed: {ex.Message}"); }
        }
    }
}