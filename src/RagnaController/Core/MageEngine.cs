using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class MageEngine : IInputHandler
    {
        // --- Konfiguration (vom Profil geladen) ---
        public bool MageEnabled { get; set; }
        public VirtualKey MageBoltKeyVK { get; set; } = VirtualKey.F1;
        public int MageBoltCastDelayMs { get; set; } = 1200;
        public float BoltAimSensitivity { get; set; } = 20f;
        public float AimDeadzone { get; set; } = 0.15f;

        // --- Handheld / Gyro Support ---
        public bool GyroAimEnabled { get; set; } = false;
        /// <summary>Alias für GyroAimEnabled — wird von HybridEngine.LoadProfile gesetzt.</summary>
        public bool GyroEnabled
        {
            get => GyroAimEnabled;
            set => GyroAimEnabled = value;
        }
        public float GyroBlend { get; set; } = 0.6f;
        public float GyroSensitivity { get; set; } = 1.0f;  // Multiplikator für Gyro-Delta

        // --- Status ---
        public bool IsActive { get; private set; }
        public MagePhase Phase { get; private set; } = MagePhase.Idle;

        // NEW: Smart Grid UI properties
        public string Buffs { get; set; } = "";
        public string Cooldowns { get; set; } = "";

        // --- Interne Variablen ---
        private int _castCooldown;
        private readonly InputCommandQueue _queue;

        // FIX: Virtual Cursor Pattern - interne Cursor-Position im Engine-Thread (nie GetCursorPos verwenden)
        private bool _cursorInitialized = false;

        public MageEngine() : this(new InputCommandQueue())
        {
        }

        public MageEngine(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public int Priority => 40;

        /// <summary>
        /// Haupt-Eingabeverarbeitung.
        /// </summary>
        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!IsActive || !MageEnabled) return false;
            Update(input, deltaMs);
            return false; // CursorEngine handles the right stick aiming automatically!
        }

        public void ToggleMageMode()
        {
            IsActive = !IsActive;
            if (!IsActive)
            {
                Phase = MagePhase.Idle;
            }
        }

        /// <summary>
        /// Wird vom HandheldModeManager aufgerufen, um Gyroskop-Daten einzuspeisen.
        /// </summary>
        public void InjectGyroDelta(int dx, int dy)
        {
            if (!GyroAimEnabled) return;

            float vx = dx * GyroSensitivity;
            float vy = dy * GyroSensitivity;

            if (vx != 0 || vy != 0)
            {
                _queue.MoveMouseRelative((int)vx, (int)vy);
            }
        }

        private void Update(ParsedInput input, int ms)
        {
            _castCooldown = Math.Max(0, _castCooldown - ms);

            // FIX: Release-to-Cast Architecture - CombatEngine zentralisiert Ground Spell State
            // Wir melden den Cast-Zustand an die CombatEngine statt direkt zu casten

            // 2. Bolt-Spam Modus (Trigger R2 gehalten)
            if (input.R2)
            {
                if (_castCooldown <= 0)
                {
                    // FIX: Virtual Cursor Pattern - initialisiere Cursor-Position virtuell
                    if (!_cursorInitialized)
                    {
                        // TODO: Use SmartCursorService.GetVirtualCursorPosition() instead of GetCursorPos
                        _cursorInitialized = true;
                    }

                    _queue.TapKey(MageBoltKeyVK);
                    _castCooldown = JitterService.Apply(MageBoltCastDelayMs, 50);
                    Phase = MagePhase.BoltSpamming;
                }
            }
            else
            {
                Phase = MagePhase.Idle;
            }
        }

        // Reset: Alle Felder auf Initialwerte setzen (wichtig bei Profil-Reload!)
        public void Reset()
        {
            MageEnabled = false;
            IsActive = false;
            Phase = MagePhase.Idle;
            _castCooldown = 0;
        }
    }
}
