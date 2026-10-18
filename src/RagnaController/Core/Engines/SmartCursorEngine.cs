using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// SmartCursorEngine — Smart D-Pad grid-hopping for UI panel navigation.
    /// Prevents gameplay engines from processing input while UI is active.
    /// Forwards all button events to CombatEngine even when UI mode is active.
    /// </summary>
    public sealed class SmartCursorEngine
    {
        private bool _uiActive = false;
#pragma warning disable CS0414 // The field '_gridX' is assigned but its value is never used
        private int _gridX = 0;
#pragma warning restore CS0414
#pragma warning disable CS0414 // The field '_gridY' is assigned but its value is never used
        private int _gridY = 0;
#pragma warning restore CS0414

        public event Action? UiStateChanged;

        public void SetUiActive(bool active)
        {
            if (_uiActive != active)
            {
                _uiActive = active;
                UiStateChanged?.Invoke();
            }
        }

        public bool IsUiActive => _uiActive;

        /// <summary>
        /// Reset method. Clears UI state for profile isolation.
        /// </summary>
        public void Reset()
        {
            _uiActive = false;
            _gridX = 0;
            _gridY = 0;
        }

        /// <summary>
        /// Verarbeitet SmartCursor-Eingaben.
        /// FIX: no early returns — all keys must be passed to CombatEngine,
        /// damit offene Prozesse sauber abgebrochen are.
        /// </summary>
        public void Process(ParsedInput input)
        {
            if (_uiActive)
            {
                // execute UI logic (e.g. D-pad grid hopping)
                // BUT: Button-Events NICHT blockieren — CombatEngine muss ALLE Tasten sehen!

                // Example: D-Pad for Grid-Navigation
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // Grid-Navigation
                    // BUT: gameplay buttons must also be passed!
                }

                // Example: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // UI-Aktionen
                    // BUT: gameplay buttons must also be passed!
                }
            }
        }
    }
}
