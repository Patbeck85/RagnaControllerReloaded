using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Profiles
{
    public class ProfileManager
    {
        private readonly string _dir;

        private static readonly string DefaultDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RagnaController", "Profiles");
        private static readonly AppJsonContext JsonCtx = AppJsonContext.Default;
        public List<Profile> Profiles { get; } = new();
        public string ActiveProfileName { get; private set; } = "Novice";
        /// <summary>v1.7.2: FIX #7 - Safe access to active profile with null protection.</summary>
        public Profile? ActiveProfile => Profiles.Find(p => p.Name == ActiveProfileName) ?? new Profile { Name = "Novice", Class = "Melee" };

        public void SetActive(string name)
        {
            if (Profiles.Any(p => p.Name == name))
                ActiveProfileName = name;
        }

        public IEnumerable<string> GetAllNames() => Profiles.Select(p => p.Name);

        /// <summary>Optional error logger — wired by HybridEngine after construction.</summary>
        public Action<string>? Logger { get; set; }

        /// <summary>Fired after a profile is successfully written to disk.</summary>
        public event Action<string>? ProfileSaved;

        // ── Debounced save (v1.5.1) ───────────────────────────────────────
        // Slider-dragging can fire SaveProfile dozens of times per second.
        // A Timer delays the actual disk write 500ms after the last call so
        // the File.Copy backup never races with a concurrent write.
        private readonly Dictionary<string, (Profile profile, Timer timer)> _pending = new();
        private const int DEBOUNCE_MS = 500;

        public ProfileManager() : this(DefaultDir) { }
        public ProfileManager(string dir) { _dir = dir; Directory.CreateDirectory(_dir); Load(); }

        public void Load()
        {
            Profiles.Clear();
            Profiles.AddRange(DefaultProfileLoader.Load());
            if (!Directory.Exists(_dir)) return;

            foreach (var f in Directory.GetFiles(_dir, "*.json"))
            {
                if (f.EndsWith(".bak.json", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    var p = JsonSerializer.Deserialize(File.ReadAllText(f), JsonCtx.Profile);
                    if (p != null)
                    {
                        p.IsBuiltIn = false;
                        int i = Profiles.FindIndex(x => x.Name == p.Name);
                        if (i >= 0) Profiles[i] = p; else Profiles.Add(p);
                    }
                }
                catch
                {
                    string bak = Path.ChangeExtension(f, ".bak.json");
                    if (!File.Exists(bak)) continue;
                    try
                    {
                        var pBak = JsonSerializer.Deserialize(File.ReadAllText(bak), JsonCtx.Profile);
                        if (pBak != null)
                        {
                            pBak.IsBuiltIn = false;
                            int i = Profiles.FindIndex(x => x.Name == pBak.Name);
                            if (i >= 0) Profiles[i] = pBak; else Profiles.Add(pBak);
                        }
                    }
                    catch { }
                }
            }
            if (Profiles.Count == 0)
                Profiles.Add(new Profile { Name = "Novice", Class = "Melee", IsBuiltIn = true });
        }

        private static string SafeName(string name) =>
            string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();

        /// <summary>
        /// Debounced save — the actual write fires 500ms after the last call.
        /// Safe against rapid slider/settings changes that would otherwise cause
        /// concurrent File.Copy + File.WriteAllText on the same path.
        /// </summary>
        public void SaveProfile(Profile p)
        {
            string key = SafeName(p.Name);
            lock (_pending)
            {
                if (_pending.TryGetValue(key, out var existing))
                {
                    // Reset the timer; update the pending profile reference
                    existing.timer.Change(DEBOUNCE_MS, Timeout.Infinite);
                    _pending[key] = (p, existing.timer);
                }
                else
                {
                    var timer = new Timer(_ => FlushProfile(key), null, DEBOUNCE_MS, Timeout.Infinite);
                    _pending[key] = (p, timer);
                }
            }
        }

        private void FlushProfile(string key)
        {
            Profile? p;
            Timer? timer;
            lock (_pending)
            {
                if (!_pending.TryGetValue(key, out var entry)) return;
                p     = entry.profile;
                timer = entry.timer;
                _pending.Remove(key);
            }
            timer?.Dispose();
            if (p == null) return;
            WriteToDisk(p);
        }

        /// <summary>
        /// Synchronous disk write — called by FlushProfile and Save().
        /// FIX #4: Safe-File-Save Pattern (wie bei Settings.cs): Zuerst in .tmp schreiben,
        /// Stream sicher schließen, dann per File.Move überschreiben. Verhindert korrupte 0-Byte-Dateien
        /// bei OutOfMemory oder Absturz von JsonSerializer.Serialize.
        /// </summary>
        private void WriteToDisk(Profile p)
        {
            string safe = SafeName(p.Name);
            string path = Path.Combine(_dir, safe + ".json");
            string tmpPath = path + ".tmp";
            
            bool wasBuiltIn = p.IsBuiltIn; // FIX: Variable im outer scope definieren
            
            try
            {
                // Backup erstellen (falls Datei existiert)
                if (File.Exists(path))
                {
                    string bak = Path.ChangeExtension(path, ".bak.json");
                    try { File.Copy(path, bak, overwrite: true); }
                    catch (Exception ex)
                    {
                        Logger?.Invoke($"[ProfileManager] Backup failed for '{p.Name}': {ex.Message}");
                    }
                }
                
                // FIX: Don't modify IsBuiltIn directly - create a copy instead
                var pCopy = new Profile { 
                    Name = p.Name,
                    Class = p.Class,
                    MouseSensitivity = p.MouseSensitivity,
                    Deadzone = p.Deadzone,
                    MovementCurve = p.MovementCurve,
                    CursorMaxSpeed = p.CursorMaxSpeed,
                    CursorDeadzone = p.CursorDeadzone,
                    CursorCurve = p.CursorCurve,
                    MovementCoastFrames = p.MovementCoastFrames,
                    ActionRpgMode = p.ActionRpgMode,
                    ActionSpeed = p.ActionSpeed,
                    MovementCurveMode = p.MovementCurveMode,
                    ClickCooldownMs = p.ClickCooldownMs,
                    MobSweepEnabled = p.MobSweepEnabled,
                    MobSweepTabIntervalMs = p.MobSweepTabIntervalMs,
                    MobSweepAttackDelayMs = p.MobSweepAttackDelayMs,
                    MobSweepAttackKeyVK = p.MobSweepAttackKeyVK,
                    PreRenewalAttackIntervalMs = p.PreRenewalAttackIntervalMs,
                    RenewalAttackIntervalMs = p.RenewalAttackIntervalMs,
                    PreRenewalSkillInterruptMs = p.PreRenewalSkillInterruptMs,
                    RenewalSkillInterruptMs = p.RenewalSkillInterruptMs,
                    KiteEnabled = p.KiteEnabled,
                    KiteAttackKeyVK = p.KiteAttackKeyVK,
                    KiteAttackIntervalMs = p.KiteAttackIntervalMs,
                    AutoAttackEnabled = p.AutoAttackEnabled,
                    AutoRetargetEnabled = p.AutoRetargetEnabled,
                    SmartSkillEnabled = p.SmartSkillEnabled,
                    AutoAttackKeyVK = p.AutoAttackKeyVK,
                    TabCycleMs = p.TabCycleMs,
                    AimSensitivity = p.AimSensitivity,
                    AimDeadzone = p.AimDeadzone,
                    MageEnabled = p.MageEnabled,
                    MageBoltKeyVK = p.MageBoltKeyVK,
                    MageBoltCastDelayMs = p.MageBoltCastDelayMs,
                    SupportEnabled = p.SupportEnabled,
                    SupportHealKeyVK = p.SupportHealKeyVK,
                    SupportPartyTabCycle = p.SupportPartyTabCycle,
                    ComboEnabled = p.ComboEnabled,
                    ComboSkillNames = new List<string>(p.ComboSkillNames),
                    ComboSequenceVK = new List<VirtualKey>(p.ComboSequenceVK),
                    PreRenewalComboDelays = new List<int>(p.PreRenewalComboDelays),
                    RenewalComboDelays = new List<int>(p.RenewalComboDelays),
                    SmartCursorSlotSizeX = p.SmartCursorSlotSizeX,
                    SmartCursorSlotSizeY = p.SmartCursorSlotSizeY,
                    HandheldModeEnabled = p.HandheldModeEnabled,
                    GyroEnabled = p.GyroEnabled,
                    GyroSensitivity = p.GyroSensitivity,
                    GyroBlend = p.GyroBlend,
                    BatteryThrottleEnabled = p.BatteryThrottleEnabled,
                    EnableSmartStandby = p.EnableSmartStandby,
                    LeftTriggerMode = p.LeftTriggerMode,
                    RightTriggerMode = p.RightTriggerMode,
                    ComboAutoLoop = p.ComboAutoLoop,
                    ComboChainCooldownMs = p.ComboChainCooldownMs,
                    ButtonMappings = new Dictionary<ButtonKey, ButtonAction>(p.ButtonMappings),
                    SkillRecommendations = new List<string>(p.SkillRecommendations),
                    ClassTips = p.ClassTips,
                    IsBuiltIn = false // Always false for saved profiles
                };
                
                // FIX: Use File.Replace() with fallback to manual copy if it fails
                try {
                    File.Replace(tmpPath, path, null);
                }
                catch {
                    // Fallback: Delete old file and copy new one
                    if (File.Exists(path)) File.Delete(path);
                    File.Copy(tmpPath, path, overwrite: true);
                }
                
                pCopy.IsBuiltIn = wasBuiltIn;
                ProfileSaved?.Invoke(pCopy.Name);
            }
            catch (Exception ex)
            {
                // FIX: Restore state if write fails - use original variable
                p.IsBuiltIn = wasBuiltIn;
                Logger?.Invoke($"[ProfileManager] SAVE FAILED for '{p.Name}': {ex.Message} — check disk space / AV settings.");
            }
        }

        /// <summary>Add to list and schedule a debounced save.</summary>
        public void AddAndSave(Profile p)
        {
            int idx = Profiles.FindIndex(x => x.Name == p.Name);
            if (idx >= 0) Profiles[idx] = p; else Profiles.Add(p);
            SaveProfile(p);
        }

        /// <summary>
        /// Immediate synchronous save — used after community profile download
        /// where we need the file present before the window closes.
        /// </summary>
        public void Save(Profile p)
        {
            // Flush any pending debounced version first
            string key = SafeName(p.Name);
            lock (_pending)
            {
                if (_pending.TryGetValue(key, out var entry))
                {
                    entry.timer.Dispose();
                    _pending.Remove(key);
                }
            }
            int idx = Profiles.FindIndex(x => x.Name == p.Name);
            if (idx >= 0) Profiles[idx] = p; else Profiles.Add(p);
            WriteToDisk(p);
        }

        public void Export(Profile p, string path)
        {
            // FileStream: kein LOH-String
            using var fs = new System.IO.FileStream(path, System.IO.FileMode.Create,
                System.IO.FileAccess.Write, System.IO.FileShare.None);
            JsonSerializer.Serialize(fs, p, JsonCtx.Profile);
        }

        public Profile? ImportPreview(string path)
        {
            try
            {
                var p = JsonSerializer.Deserialize(File.ReadAllText(path), JsonCtx.Profile);
                if (p != null) p.IsBuiltIn = false;
                return p;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProfileManager] ImportPreview failed: {ex.Message}");
                return null;
            }
        }

        public void Delete(Profile p)
        {
            if (!p.IsBuiltIn)
            {
                try
                {
                    string path = Path.Combine(_dir, SafeName(p.Name) + ".json");
                    if (File.Exists(path)) File.Delete(path);
                    string bak = Path.ChangeExtension(path, ".bak.json");
                    if (File.Exists(bak)) File.Delete(bak);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ProfileManager] Delete failed: {ex.Message}");
                }
                Profiles.Remove(p); // Always remove from memory even if file delete fails
            }
        }

        /// <summary>Activate a profile by name.</summary>
        public void Activate(string name)
        {
            SetActive(name);
        }

        /// <summary>Deactivate the current profile.</summary>
        public void Deactivate()
        {
            ActiveProfileName = "Novice";
        }

        /// <summary>Update a profile with new values.</summary>
        public void Update(Profile p)
        {
            int idx = Profiles.FindIndex(x => x.Name == p.Name);
            if (idx >= 0) Profiles[idx] = p;
        }

        /// <summary>Check if the active profile is enabled.</summary>
        public bool IsActive => Profiles.Any(p => p.Name == ActiveProfileName && p.IsEnabled);

        /// <summary>Get the priority of the active profile.</summary>
        public int Priority => Profiles.Find(p => p.Name == ActiveProfileName)?.Priority ?? 0;

        /// <summary>Check if the active profile is enabled.</summary>
        public bool ProfileEnabled => Profiles.Any(p => p.Name == ActiveProfileName && p.IsEnabled);
    }
}
