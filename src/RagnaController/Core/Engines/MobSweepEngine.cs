using System;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class MobSweepEngine
    {
        public bool SweepEnabled   { get; set; }
        public bool PartyTabCycle  { get; set; }
        public int  Priority       { get; set; } = 50;
        public bool MobSweepEnabled   { get; set; }
        public int  AttackKeyVK       { get; set; } = (int)VirtualKey.F1;
        public int  AttackDelayMs     { get; set; } = 300;
        public int  TabIntervalMs     { get; set; } = 500;

        public bool IsActive => MobSweepEnabled;
        public SupportPhase Phase { get; private set; } = SupportPhase.Idle;

        // Cooldowns für Tests zugänglich machen
        public int TabCooldown => _tabCooldown;
        public int HealCooldown => _healCooldown;

        private int _tabCooldown;
        private int _healCooldown;
        private readonly InputCommandQueue _queue;

        // Wird aus dem CombatRouter heraus gerufen wenn L2+R2 gehalten werden
        public MobSweepEngine(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public void Activate()   => MobSweepEnabled = true;
        public void Deactivate() { MobSweepEnabled = false; Phase = SupportPhase.Idle; }

        public void Update(int deltaMs)
        {
            if (!MobSweepEnabled) return;

            _tabCooldown = Math.Max(0, _tabCooldown - deltaMs);
            _healCooldown = Math.Max(0, _healCooldown - deltaMs);
        }

        /// <summary>
        /// Handles input for MobSweepEngine.
        /// Returns false to indicate this engine handled the input (prevents blocking).
        /// </summary>
        public bool Handle(ParsedInput input, int deltaMs)
        {
            if (!MobSweepEnabled) return false;

            // Check cooldowns first
            if (Phase == SupportPhase.TargetingParty && TabIntervalMs > 0)
            {
                _tabCooldown = Math.Max(0, _tabCooldown - deltaMs);
                if (_tabCooldown == 0)
                {
                    _queue.TapKey(VirtualKey.Tab);
                    Phase = SupportPhase.Idle;
                }
            }

            if (input.R1)
            {
                // Mob Sweep activation
                if (Phase != SupportPhase.TargetingParty && _healCooldown == 0)
                {
                    _queue.TapKey((VirtualKey)AttackKeyVK);
                    Phase = SupportPhase.TargetingParty;
                    _healCooldown = AttackDelayMs;
                }
            }
            else if (input.BtnY)
            {
                // Healing activation
                if (Phase != SupportPhase.Healing && _tabCooldown == 0)
                {
                    _queue.TapKey((VirtualKey)32); // Space key for heal
                    Phase = SupportPhase.Healing;
                    _tabCooldown = TabIntervalMs;
                }
            }

            return false;
        }

        // Reset: Alle Felder auf Initialwerte setzen (wichtig bei Profil-Reload!)
        public void Reset()
        {
            SweepEnabled = false;
            PartyTabCycle = false;
            MobSweepEnabled = false;
            Phase = SupportPhase.Idle;
            _tabCooldown = 0;
            _healCooldown = 0;
        }

        /// <summary>
        /// Toggle sweep mode for testing purposes
        /// </summary>
        public void ToggleSweepMode()
        {
            MobSweepEnabled = !MobSweepEnabled;
        }
    }
}
