using System;
using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    public class ComboEngine
    {
        public bool           Enabled       { get; set; }
        public List<VirtualKey> Sequence    { get; set; } = new();
        public List<int>      CurrentDelays { get; set; } = new();
        public bool           AutoLoop      { get; set; } = false;
        public int            ChainCooldownMs { get; set; } = 0;

        /// <summary>1-based step index (0 = idle).</summary>
        public int  CurrentStep => _stepIndex;
        public bool IsActive    => _stepIndex > 0;

        /// <summary>Fired with the 1-based step number when a skill is executed.</summary>
        public event Action<int>? ComboStepFired;

        // NEW: Smart Grid UI properties
        public string CurrentActionLabel { get; set; } = "";
        public int CurrentActionId { get; set; }
        public string MobSweepLabel { get; set; } = "";
        public string HandheldModeLabel { get; set; } = "";
        public bool HandheldModeActive { get; set; }
        public string OverlayText { get; set; } = "";
        public string MiniModeLabel { get; set; } = "";

        private int  _stepIndex;
        private int  _timer;
        private int  _bufferWindow;
        private bool _wasHeld; // für SyncHeldState False-Fire-Prevention

        private readonly InputCommandQueue _queue;

        public ComboEngine(InputCommandQueue queue)
        {
            _queue = queue;
        }

        public void Reset()
        {
            _stepIndex    = 0;
            _timer        = 0;
            _bufferWindow = 0;
        }

        /// <summary>
        /// Prevents a false step-1 trigger when an overlay closes while the button
        /// is still physically held (the engine never saw the press-down).
        /// </summary>
        public void SyncHeldState(bool currentlyHeld) => _wasHeld = currentlyHeld;

        public void Update(bool isHeld, int ms)
        {
            if (!Enabled || Sequence.Count == 0) return;

            // False-fire prevention: ignore a rising-edge if state was already held
            bool risingEdge = isHeld && !_wasHeld;
            _wasHeld = isHeld;

            if (isHeld)
            {
                _bufferWindow = 64;
                _timer -= ms;
                if (_timer <= 0)
                    // FIX: Nur blockieren, wenn es KEIN Rising Edge ist, die Taste aber vorher schon gehalten wurde
                    ExecuteNextStep(!risingEdge && _wasHeld && _stepIndex == 0);
            }
            else
            {
                if (_bufferWindow > 0)
                    _bufferWindow -= ms;
                else if (IsActive)
                    Reset();
            }
        }

        private void ExecuteNextStep(bool fromFalseFire)
        {
            if (fromFalseFire) return; // Overlay-Close guard

            if (_stepIndex >= Sequence.Count)
            {
                if (!AutoLoop) { Reset(); return; }
                _stepIndex = 0;
                if (ChainCooldownMs > 0) { _timer = ChainCooldownMs; return; }
            }

            int step = _stepIndex + 1;
            // FIX: Bounds check before accessing Sequence array to prevent IndexOutOfRangeException
            if (_stepIndex < Sequence.Count)
            {
                _queue.TapKey(Sequence[_stepIndex]);
            }
            ComboStepFired?.Invoke(step);

            int baseDelay = (_stepIndex < CurrentDelays.Count) ? CurrentDelays[_stepIndex] : 300;
            // FIX: JitterService is a static class - call Apply directly without null check
            _timer = JitterService.Apply(baseDelay, 15);
            _stepIndex++;
        }
    }
}
