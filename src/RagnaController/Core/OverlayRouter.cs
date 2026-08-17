using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Handhabt Overlay-Eingaben (Radial-Menu, Inventar, etc.).
    /// Verhindert, dass Gameplay-Engine Eingaben verarbeitet, wenn Overlay aktiv ist.
    /// </summary>
    public sealed class OverlayRouter
    {
        private bool _overlayActive = false;
        private string _activeOverlay = "";
        private VoiceChatService? _voice;  // FIX: Nullable, da Konstruktor voice = null erlaubt

        // Events für HybridEngine (nicht verwendet — Legacy-Signatur)
#pragma warning disable CS0067 // Event 'ProfileQuickSwitch' is never used
        public event Action<int>? ProfileQuickSwitch;
#pragma warning restore CS0067
#pragma warning disable CS0067 // Event 'RestoreMainWindowRequested' is never used
        public event Action? RestoreMainWindowRequested;
#pragma warning restore CS0067

        public OverlayRouter(VoiceChatService? voice = null)
        {
            _voice = voice;  // FIX: Nullable zuweisung
        }

        public event Action<string>? OverlayChanged;

        public void SetOverlay(string overlayName)
        {
            if (_activeOverlay != overlayName)
            {
                _activeOverlay = overlayName;
                _overlayActive = true;
                OverlayChanged?.Invoke(overlayName);
            }
        }

        public void ClearOverlay()
        {
            _overlayActive = false;
            _activeOverlay = "";
            OverlayChanged?.Invoke("");
        }

        public bool IsOverlayActive => _overlayActive;
        public string ActiveOverlayName => _activeOverlay;

        /// <summary>
        /// Verarbeitet Overlay-Eingaben.
        /// FIX: Keine Early Returns — alle Tasten müssen an CombatEngine weitergegeben werden,
        /// damit offene Turbos/Makros sauber abgebrochen werden.
        /// </summary>
        public void Process(ParsedInput input)
        {
            if (_overlayActive)
            {
                // Overlay-Logik ausführen (z.B. Radial-Menu Navigation)
                // ABER: Button-Events NICHT blockieren — CombatEngine muss ALLE Tasten sehen!
                
                // Beispiel: Radial-Menu Navigation (D-Pad)
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // D-Pad für Radial-Menu Navigation
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }

                // Beispiel: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // Inventar-Aktionen
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }
            }
        }

        /// <summary>
        /// Versucht Overlay-Eingaben zu verarbeiten.
        /// RETURN: true wenn Overlay den Input konsumiert hat (aber CombatEngine bekommt trotzdem alle Buttons!).
        /// </summary>
        public bool TryHandleInput(ParsedInput input)
        {
            if (_overlayActive)
            {
                // Overlay-Logik ausführen
                // ABER: Wir RETURNEN NICHT — CombatEngine muss ALLE Tasten sehen!

                // Beispiel: Radial-Menu Navigation (D-Pad)
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // D-Pad für Radial-Menu Navigation
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }

                // Beispiel: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // Inventar-Aktionen
                    // ABER: Auch Gameplay-Buttons müssen weitergegeben werden!
                }

                // Voice-Chat Trigger: Back + L1 (laut CONTROLS_GUIDE.md)
                if (input.Back && input.L1)
                {
                    _voice?.StartListening();  // FIX: Null-Check vor StartListening()
                }

                // DaisyWheel Trigger: Back + R1
                if (input.Back && input.R1)
                {
                    // DaisyWheel-Logik hier
                }
            }
            return false; // Immer false zurückgeben — CombatEngine bekommt ALLE Tasten!
        }
    }
}
