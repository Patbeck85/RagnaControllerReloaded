using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// MageEngine — Ground-spell aiming engine using right stick + gyro input.
    /// Handles bolt casting with aim sensitivity, deadzone, and cast delay timing.
    /// Supports gyro blending for handheld device aim assistance.
    /// </summary>
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
        /// <summary>Alias for GyroAimEnabled — is from HybridEngine.LoadProfile gesetzt.</summary>
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
        private readonly SmartCursorService _smartCursorService;

        // FIX: Virtual Cursor Pattern - interne Cursor-Position im Engine-Thread (nie GetCursorPos verwenden)
        private bool _cursorInitialized = false;
        private (int X, int Y) _virtualCursorPos;

        public MageEngine() : this(new InputCommandQueue(), null!)
                {
                }

        public MageEngine(InputCommandQueue queue, SmartCursorService smartCursorService)
        {
            _queue = queue;
            _smartCursorService = smartCursorService;
        }

        public int Priority => 40;

        /// <summary>
        /// Haupt-Eingabeverarbeitung.
        /// </summary>
        /// <param name="deltaMs">Milliseconds elapsed since the last tick.</param>
        /// <returns>True if input was consumed by the mage engine; false otherwise.</returns>
        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!IsActive || !MageEnabled) return false;
            Update(input, deltaMs);
            // Engine active → Input konsumiert (Priority-Chain: Support bekommt then keinen Durchgang mehr).
            // CursorEngine handles the right stick aiming automatically!
            return true;
        }

        /// <summary>
        /// Toggles mage mode on or off.
        /// </summary>
        public void ToggleMageMode()
        {
            IsActive = !IsActive;
            if (!IsActive)
            {
                Phase = MagePhase.Idle;
            }
        }

        /// <summary>
        /// Is vom HandheldModeManager aufgerufen, um Gyroskop-Daten einzuspeisen.
        /// </summary>
        /// <param name="dy">Gyro Y-axis delta.</param>
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

            // FIX: Release-to-Cast Architecture - CombatEngine zentralisiert Groand Spell State
            // Wir melden den Cast-Zustand an die CombatEngine statt direkt to casten

            // 2. Bolt-Spam Modus (Trigger R2 gehalten) — digital ODER analog
            if (input.R2 || input.TriggerRight > 0.5f)
            {
                if (_castCooldown <= 0)
                {
                    // FIX: Virtual Cursor Pattern - initialisiere Cursor-Position virtuell
                    if (!_cursorInitialized)
                    {
                        // Use SmartCursorService to get virtual cursor position
                        _virtualCursorPos = _smartCursorService?.GetVirtualCursorPosition() ?? (0, 0);
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
                _cursorInitialized = false; // Reset on release
            }
        }

        // Reset: All Felder on Initialwerte setzen (important bei Profil-Reload!)
        /// <summary>
        /// Resets the mage engine state to idle.
        /// </summary>
        public void Reset()
        {
            MageEnabled = false;
            IsActive = false;
            Phase = MagePhase.Idle;
            _castCooldown = 0;
        }
    }
}