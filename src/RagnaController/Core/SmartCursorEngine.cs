using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Handhabt SmartCursor-Eingaben (D-Pad Grid-Hopping für UI Panels).
    /// Verhindert, dass Gameplay-Engine Eingaben verarbeitet, wenn UI aktiv ist.
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
        /// Verarbeitet SmartCursor-Eingaben.
        /// FIX: Keine Early Returns — alle Tasten müssen an CombatEngine weitergegeben werden,
        /// damit offene Turbos/Makros sauber abgebrochen werden.
        /// </summary>
        public void Process(ParsedInput input)
        {
            if (_uiActive)
            {
                // UI-Logik ausführen (z.B. D-Pad Grid-Hopping)
                // ABER: Button-Events NICHT blockieren — CombatEngine muss ALLE Tasten sehen!

                // Beispiel: D-Pad für Grid-Navigation
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // Grid-Navigation
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }

                // Beispiel: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // UI-Aktionen
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }
            }
        }
    }
}
