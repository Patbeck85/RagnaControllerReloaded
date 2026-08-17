using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class SupportEngine : IInputHandler
    {
        public bool        SupportEnabled  { get; set; }
        public bool        IsActive        { get; private set; }
        public SupportPhase Phase          { get; private set; } = SupportPhase.Idle;

        public VirtualKey  HealKeyVK       { get; set; } = VirtualKey.F1;
        public bool        PartyTabCycle   { get; set; } = false;

        private int  _tabCooldown;
        private int  _healCooldown;
        private bool _wasY;
        private bool _wasRB;
        private readonly InputCommandQueue _queue;

        // Cooldowns für Tests zugänglich machen
        public int TabCooldown => _tabCooldown;
        public int HealCooldown => _healCooldown;

        private const int TAB_COOLDOWN_MS  = 300;
        private const int HEAL_COOLDOWN_MS = 800;

        public int Priority => 50;

        public SupportEngine(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!IsActive || !SupportEnabled) return false;

            _tabCooldown  = Math.Max(0, _tabCooldown  - deltaMs);
            _healCooldown = Math.Max(0, _healCooldown - deltaMs);

            // ── Party-Tab (R1 = Tab durch Party) ──────────────────────────
            bool rb = input.R1 && !input.L1 && !input.L2 && !input.R2;
            if (rb && !_wasRB && _tabCooldown == 0)
            {
                if (PartyTabCycle)
                    _queue.TapKeyWithModifier(VirtualKey.ControlLeft, VirtualKey.Tab);
                else
                    _queue.TapKey(VirtualKey.Tab);
                _tabCooldown = TAB_COOLDOWN_MS;
                Phase = SupportPhase.TargetingParty;
            }
            _wasRB = rb;

            // ── Heal (Y = Heal-Key) ────────────────────────────────────────
            if (input.BtnY && !_wasY && _healCooldown == 0)
            {
                _queue.TapKey(HealKeyVK);
                _healCooldown = HEAL_COOLDOWN_MS;
                Phase = SupportPhase.Healing;
            }
            _wasY = input.BtnY;

            // Return to Idle when no action
            if (_tabCooldown == 0 && _healCooldown == 0)
                Phase = SupportPhase.Idle;

            // Gibt false zurück — blockiert nachfolgende Engines nicht
            return false;
        }

        public void ToggleSupportMode()
        {
            IsActive = !IsActive;
            if (!IsActive) Phase = SupportPhase.Idle;
        }

        // Reset: Alle Felder auf Initialwerte setzen (wichtig bei Profil-Reload!)
        public void Reset()
        {
            SupportEnabled = false;
            IsActive = false;
            Phase = SupportPhase.Idle;
            _tabCooldown = 0;
            _healCooldown = 0;
            _wasY = false;
            _wasRB = false;
        }
    }
}
