using RagnaController;
using RagnaController.Core;
using System;
using System.IO;
using System.Text.Json;
using RagnaController.Core.Services.UI;

namespace RagnaController.Models
{
    /// <summary>
    /// Data model for application state.
    /// </summary>
    public enum RightStickPolicy
    {
        /// <summary>Right stick controls mouse/cursor (Action Combat)</summary>
                Camera,
                /// <summary>Right stick aims (Target Lock / Aim Assist)</summary>
        Aim,
        /// <summary>Hybrid: Target Lock active → Aim, sonst Camera</summary>
        Hybrid
    }

    public class Settings
        {
            // Path to settings.json in the AppData folder
            private static readonly object _fileLock = new();
            private static readonly string SettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RagnaController", "settings.json");

            // FEAT-031: Static Default instance for XAML bindings
            private static Settings? _default;
            public static Settings Default
            {
                get
                {
                    if (_default == null)
                    {
                        lock (_fileLock)
                        {
                            if (_default == null)
                                _default = Load();
                        }
                    }
                    return _default!;
                }
            }

            // FIX: Flag to mark if loading settings failed
            private static bool _loadFailed = false;

        // --- The eigentlichen Einstellungen ---
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
        /// Forces handheld mode regardless of detected device.
        /// Useful for desktop users with controller who prefer big-picture UI.
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

                // NEW: Skip Splash Screen on startup
                public bool SkipSplash { get; set; } = false;

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

        // ── TECH-063: Auto-Restart on Crash ─────────────────────────────────
        /// <summary>Automatically restart the app after a fatal crash (gaming UX: no dead controller mid-session).</summary>
        public bool AutoRestartOnCrash { get; set; } = false;

        // ── ROB-002: Input-Emulation-Failover (SendInput ↔ Kernel-Service) ───
        /// <summary>SendInput latency threshold in ms. Flushes above are considered "slow".</summary>
        public int FailoverLatencyThresholdMs { get; set; } = 5;
        /// <summary>N aufeinanderfolgende langsame Flushes → Auto-Switch on Kernel-Strategie.</summary>
        public int FailoverTriggerCount { get; set; } = 3;
        /// <summary>M stable (fast) flushes after switch → recovery back to SendInput.</summary>
        public int FailoverRecoveryCount { get; set; } = 2;

        /// <summary>Force kernel-mode input emulation (Interception driver) instead of SendInput. Requires driver installation.</summary>
        public bool EnableKernelInput { get; set; } = false;

        /// <summary>Whether the kernel-input restart dialog has already been shown (to avoid repeating it).</summary>
        public bool KernelInputFirstRunShown { get; set; } = false;

        // ── FEAT-014: Session-Replay (JSONL-Aufzeichnung for deterministische Wiedergabe) ──
        /// <summary>Session replay enabled? Recording runs per engine session, rotation at 50 MB.</summary>
        public bool EnableSessionRecording { get; set; } = false;

        // Fenster-Einstellungen (v1.6.0)
        public bool StartMinimized { get; set; } = false;
        public bool ShowControllerViz { get; set; } = true;
        /// <summary>Gespeicherte Fensterposition (X,Y). Null = zentriert.</summary>
        public double? WindowPositionX { get; set; } = null;
        public double? WindowPositionY { get; set; } = null;

        // ── UX-021: Overlay Position Memory ───────────────────────────────
        /// <summary>Gespeicherte Overlay-Position (X,Y). Null = standard (oben-rechts).</summary>
        public double? OverlayPositionX { get; set; } = null;
        public double? OverlayPositionY { get; set; } = null;

        // ── UX-022: Gyro Calibration ─────────────────────────────────────
        /// <summary>Gyro drift threshold (deadzone) in raw HID units. Determined by calibration.</summary>
        public int GyroDeadzone { get; set; } = 64;

        // ── FEAT-029: Automatic Updates ──────────────────────────────────
        /// <summary>Automatically check for updates on startup.</summary>
        public bool AutoUpdateEnabled { get; set; } = true;

        // ── FEAT-030: Multi-Monitor Support ──────────────────────────────
        /// <summary>Index of monitor where the overlay window should appear (0 = primary).</summary>
        public int OverlayMonitorIndex { get; set; } = 0;

        // ── Lifetime RSI Stats (v1.7.0) ───────────────────────────────
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

                // ── FEAT-031: Skill Cooldown Overlay ──────────────────────────────
                /// <summary>Skill-Cooldown-Balken im Overlay anzeigen (F1-F12).</summary>
                public bool OverlayCooldownsEnabled { get; set; } = true;
                /// <summary>Höhe jedes Cooldown-Balkens in Pixel.</summary>
                public double OverlayCooldownBarHeight { get; set; } = 6.0;
                /// <summary>Breite jedes Cooldown-Balkens in Pixel.</summary>
                public double OverlayCooldownBarWidth { get; set; } = 40.0;
                /// <summary>Opacity der Cooldown-Balken (0.0 = unsichtbar, 1.0 = deckend).</summary>
                public double OverlayCooldownOpacity { get; set; } = 0.8;

                // ── FEAT-032: Buff/Debuff Timer Overlay ───────────────────────────
                /// <summary>Buff/Debuff-Timer im Overlay anzeigen.</summary>
                public bool OverlayBuffTimersEnabled { get; set; } = true;
                /// <summary>Warnung anzeigen wenn Buff innerhalb von X Sekunden abläuft.</summary>
                public int OverlayBuffWarnSec { get; set; } = 5;
                /// <summary>Maximale Anzahl sichtbarer Buffs im Overlay.</summary>
                public int OverlayBuffMaxVisible { get; set; } = 8;

                // ── FEAT-033: HP/SP Overlay (OCR) ──────────────────────────────
                /// <summary>HP/SP-Overlay via OCR aus RO-Client auslesen.</summary>
                public bool OverlayHpSpEnabled { get; set; } = true;
                /// <summary>OCR-Region X für HP-Wert (relativ zur Client-Fenster-Größe, 0-1).</summary>
                public double OverlayHpRegionX { get; set; } = 0.05;
                /// <summary>OCR-Region Y für HP-Wert (relativ zur Client-Fenster-Größe, 0-1).</summary>
                public double OverlayHpRegionY { get; set; } = 0.85;
                /// <summary>OCR-Region Breite für HP-Wert (relativ, 0-1).</summary>
                public double OverlayHpRegionW { get; set; } = 0.10;
                /// <summary>OCR-Region Höhe für HP-Wert (relativ, 0-1).</summary>
                public double OverlayHpRegionH { get; set; } = 0.05;
                /// <summary>OCR-Region X für SP-Wert (relativ zur Client-Fenster-Größe, 0-1).</summary>
                public double OverlaySpRegionX { get; set; } = 0.05;
                /// <summary>OCR-Region Y für SP-Wert (relativ zur Client-Fenster-Größe, 0-1).</summary>
                public double OverlaySpRegionY { get; set; } = 0.91;
                /// <summary>OCR-Region Breite für SP-Wert (relativ, 0-1).</summary>
                public double OverlaySpRegionW { get; set; } = 0.10;
                /// <summary>OCR-Region Höhe für SP-Wert (relativ, 0-1).</summary>
                public double OverlaySpRegionH { get; set; } = 0.05;

                // ── FEAT-034: Target-Info Overlay (OCR) ─────────────────────────
                /// <summary>Target-Info-Overlay via OCR aus RO-Client auslesen.</summary>
                public bool OverlayTargetInfoEnabled { get; set; } = true;
                /// <summary>OCR-Region X für Ziel-Name (relativ, 0-1).</summary>
                public double OverlayTargetNameRegionX { get; set; } = 0.35;
                /// <summary>OCR-Region Y für Ziel-Name (relativ, 0-1).</summary>
                public double OverlayTargetNameRegionY { get; set; } = 0.05;
                /// <summary>OCR-Region Breite für Ziel-Name (relativ, 0-1).</summary>
                public double OverlayTargetNameRegionW { get; set; } = 0.20;
                /// <summary>OCR-Region Höhe für Ziel-Name (relativ, 0-1).</summary>
                public double OverlayTargetNameRegionH { get; set; } = 0.04;
                /// <summary>OCR-Region X für Ziel-Level (relativ, 0-1).</summary>
                public double OverlayTargetLevelRegionX { get; set; } = 0.35;
                /// <summary>OCR-Region Y für Ziel-Level (relativ, 0-1).</summary>
                public double OverlayTargetLevelRegionY { get; set; } = 0.09;
                /// <summary>OCR-Region Breite für Ziel-Level (relativ, 0-1).</summary>
                public double OverlayTargetLevelRegionW { get; set; } = 0.08;
                /// <summary>OCR-Region Höhe für Ziel-Level (relativ, 0-1).</summary>
                public double OverlayTargetLevelRegionH { get; set; } = 0.04;
                /// <summary>OCR-Region X für Ziel-HP (relativ, 0-1).</summary>
                public double OverlayTargetHpRegionX { get; set; } = 0.35;
                /// <summary>OCR-Region Y für Ziel-HP (relativ, 0-1).</summary>
                public double OverlayTargetHpRegionY { get; set; } = 0.13;
                /// <summary>OCR-Region Breite für Ziel-HP (relativ, 0-1).</summary>
                public double OverlayTargetHpRegionW { get; set; } = 0.10;
                /// <summary>OCR-Region Höhe für Ziel-HP (relativ, 0-1).</summary>
                public double OverlayTargetHpRegionH { get; set; } = 0.04;
                /// <summary>OCR-Region X für Ziel-Distanz (relativ, 0-1).</summary>
                public double OverlayTargetDistRegionX { get; set; } = 0.35;
                /// <summary>OCR-Region Y für Ziel-Distanz (relativ, 0-1).</summary>
                public double OverlayTargetDistRegionY { get; set; } = 0.17;
                /// <summary>OCR-Region Breite für Ziel-Distanz (relativ, 0-1).</summary>
                public double OverlayTargetDistRegionW { get; set; } = 0.08;
                /// <summary>OCR-Region Höhe für Ziel-Distanz (relativ, 0-1).</summary>
                public double OverlayTargetDistRegionH { get; set; } = 0.04;

                // ── FEAT-036: Damage Numbers Overlay (Optional) ───────────────
                /// <summary>Schadenszahlen-Overlay über Ziel anzeigen.</summary>
                public bool OverlayDamageNumbersEnabled { get; set; } = true;
                /// <summary>Schriftgröße der Schadenszahlen.</summary>
                public double OverlayDamageFontSize { get; set; } = 16.0;
                /// <summary>Farbe der Schadenszahlen (Hex).</summary>
                public string OverlayDamageColor { get; set; } = "#FF5252";
                /// <summary>Anzeigedauer einer Schadenszahl in Millisekunden.</summary>
                public int OverlayDamageDurationMs { get; set; } = 1200;
                /// <summary>Maximale Anzahl gleichzeitiger Schadenszahlen.</summary>
                public int OverlayDamageMaxVisible { get; set; } = 10;

        // ── UX-017: Accessibility — High-Contrast & Font Scaling ─────────────
        /// <summary>High-Contrast-Modus aktivieren (für Sehbehinderte, Steam Deck im Sonnenlicht).</summary>
        public bool HighContrastEnabled { get; set; } = false;
        /// <summary>UI-Schrift-Skalierung: 0.85 = klein, 1.00 = normal, 1.25 = groß, 1.50 = sehr groß.</summary>
        public double FontScale { get; set; } = 1.0;
        /// <summary>Screen-Reader-Modus aktivieren (NVDA, JAWS, Narrator Unterstützung).</summary>
        public bool AccessibilityScreenReaderMode { get; set; } = false;
        /// <summary>Reduzierte Bewegung — Animationen deaktivieren (Respektiert Windows-Einstellung).</summary>
        public bool ReducedMotionEnabled { get; set; } = false;
        /// <summary>Farbblindheits-Modus: None, Deuteranopia, Protanopia, Tritanopia.</summary>
        public ColorBlindMode ColorBlindMode { get; set; } = ColorBlindMode.None;

        // ── Gamepad-Navigation (GamepadNavigationService) ───────────────────
        /// <summary>Gamepad-Navigation für alle Fenster aktiviert (D-Pad, Buttons, Sticks).</summary>
        public bool GamepadNavigationEnabled { get; set; } = false;

        // ── Radial Menu Preset (RoUiMenuService) ────────────────────────────
        /// <summary>RO UI Radial-Menü-Preset aktiviert (Inventar, Status, Skills, etc.).</summary>
        public bool RoUiMenuEnabled { get; set; } = false;

        // ── Skill-Empfehlungen (SkillRecommendationEngine) ──────────────────
        /// <summary>Skill-Build-Empfehlungen anzeigen.</summary>
        public bool SkillRecommendationsEnabled { get; set; } = false;

        // ── Auto-Stat-Optimierung (AutoBuildOptimizer) ──────────────────────
        /// <summary>Automatische Stat-Allokations-Empfehlungen anzeigen.</summary>
        public bool AutoBuildOptimizerEnabled { get; set; } = false;

        // ── UITheme Enum (für HighContrastService) ──────────────────────────
        /// <summary>Verfügbare UI-Themes.</summary>
        public enum UITheme
        {
            /// <summary>Dunkles Theme (Standard).</summary>
            Dark,
            /// <summary>Helles Theme.</summary>
            Light,
            /// <summary>Hochkontrast-Theme.</summary>
            HighContrast,
            /// <summary>Sepia-Theme.</summary>
            Sepia,
        }

        /// <summary>UI-Theme: Dark, Light, HighContrast, Sepia.</summary>
        public UITheme SelectedTheme { get; set; } = UITheme.Dark;

        // ── Auto-Update (TASK-3701) ─────────────────────────────────────────
        /// <summary>Update-Channel: stable oder beta.</summary>
        public string UpdateChannel { get; set; } = "stable";
        /// <summary>Intervall für automatische Update-Prüfung in Stunden.</summary>
        public int UpdateCheckIntervalHours { get; set; } = 24;
        /// <summary>Datum des letzten Update-Checks.</summary>
        public DateTime? LastUpdateCheckAt { get; set; }

        // ── Chorded Input (TASK-3601) ─────────────────────────────────────
        /// <summary>Chorded Input aktiviert (gleichzeitige Tastenkombinationen, Sequenzen, Hold+Tap).</summary>
        public bool ChordedInputEnabled { get; set; } = true;
        /// <summary>Maximales Zeitfenster für Chord-Erkennung in Millisekunden.</summary>
        public int ChordMaxIntervalMs { get; set; } = 150;
        /// <summary>Maximales Zeitfenster für Sequenz-Erkennung in Millisekunden.</summary>
        public int SequenceMaxIntervalMs { get; set; } = 500;
        /// <summary>Hold+Tap Schwelle in Millisekunden (ab wann als Halten gewertet).</summary>
        public int HoldTapThresholdMs { get; set; } = 300;

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
            // FIX: if loading previously failed, do not overwrite!
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
                    // FileStream: no large string in LOH, directly on disk
                    string tmp = SettingsPath + ".tmp";
                    using (var fs = new System.IO.FileStream(tmp, System.IO.FileMode.Create,
                        System.IO.FileAccess.Write, System.IO.FileShare.None))
                        JsonSerializer.Serialize(fs, this, AppJsonContext.Default.Settings);
                    File.Move(tmp, SettingsPath, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($@"[Settings] Save failed: {ex.Message}");
                // TASK-1701: Benutzer sichtbarer Fehler via Toast
                try { RagnaController.App.ToastService?.Show($@"Einstellungen konnten nicht gespeichert werden: {ex.Message}", isError: true); } catch (Exception toastEx) { System.Diagnostics.Debug.WriteLine($@"[Settings] Toast notification failed: {toastEx.Message}"); }
            }
        }

        // TASK-2002: Reset all settings to defaults
        public void ResetToDefaults()
        {
            var defaults = new Settings();
            LastProfileName = defaults.LastProfileName;
            LastGameMode = defaults.LastGameMode;
            SoundEnabled = defaults.SoundEnabled;
            RumbleEnabled = defaults.RumbleEnabled;
            AutoStart = defaults.AutoStart;
            MinimizeToTray = defaults.MinimizeToTray;
            StartWithWindows = defaults.StartWithWindows;
            ShowLatency = defaults.ShowLatency;
            ForceHandheldMode = defaults.ForceHandheldMode;
            StartInMiniMode = defaults.StartInMiniMode;
            AppLanguage = defaults.AppLanguage;
            RoExePath = defaults.RoExePath;
            FocusLockEnabled = defaults.FocusLockEnabled;
            FocusLockProcess = defaults.FocusLockProcess;
            LogLevel = defaults.LogLevel;
            EnableDiscordRPC = defaults.EnableDiscordRPC;
            SkipSplash = defaults.SkipSplash;
            EnableVoiceAnnouncements = defaults.EnableVoiceAnnouncements;
            EnableHapticMetronome = defaults.EnableHapticMetronome;
            EnableSmartStandby = defaults.EnableSmartStandby;
            StandbyTimeoutMinutes = defaults.StandbyTimeoutMinutes;
            EnableTelemetry = defaults.EnableTelemetry;
            HasAskedForTelemetry = defaults.HasAskedForTelemetry;
            AutoRestartOnCrash = defaults.AutoRestartOnCrash;
            FailoverLatencyThresholdMs = defaults.FailoverLatencyThresholdMs;
            FailoverTriggerCount = defaults.FailoverTriggerCount;
            FailoverRecoveryCount = defaults.FailoverRecoveryCount;
            EnableKernelInput = defaults.EnableKernelInput;
            KernelInputFirstRunShown = defaults.KernelInputFirstRunShown;
            EnableSessionRecording = defaults.EnableSessionRecording;
            StartMinimized = defaults.StartMinimized;
            ShowControllerViz = defaults.ShowControllerViz;
            WindowPositionX = defaults.WindowPositionX;
            WindowPositionY = defaults.WindowPositionY;
            OverlayPositionX = defaults.OverlayPositionX;
            OverlayPositionY = defaults.OverlayPositionY;
            GyroDeadzone = defaults.GyroDeadzone;
            AutoUpdateEnabled = defaults.AutoUpdateEnabled;
            OverlayMonitorIndex = defaults.OverlayMonitorIndex;
            LifetimeSavedClicks = defaults.LifetimeSavedClicks;
            LifetimeSavedKeystrokes = defaults.LifetimeSavedKeystrokes;
            OverlayTheme = defaults.OverlayTheme;
            OverlayOpacity = defaults.OverlayOpacity;
            OverlayFontScale = defaults.OverlayFontScale;
            HighContrastEnabled = defaults.HighContrastEnabled;
            FontScale = defaults.FontScale;
            RightStickPolicy = defaults.RightStickPolicy;
        }
    }
}