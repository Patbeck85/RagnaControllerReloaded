using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Handhabt Overlay-Eingaben (Radial-Menu, Inventar, etc.).
    /// Verhindert, dass Gameplay-Engine Eingaben verarbeitet, if Overlay active is.
    /// </summary>
    public sealed class OverlayRouter
    {
        private bool _overlayActive = false;
        private string _activeOverlay = "";
        private VoiceChatService? _voice;  // FIX: Nullable, da Konstruktor voice = null erlaubt

        // Events for HybridEngine (nicht verwendet — Legacy-Signatur)
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
        /// FIX: Keine Early Returns — all Tasten müssen an CombatEngine weitergegeben are,
        /// damit offene Prozesse sauber abgebrochen are.
        /// </summary>
        public void Process(ParsedInput input)
        {
            if (_overlayActive)
            {
                // Overlay-Logik ausführen (z.B. Radial-Menu Navigation)
                // BUT: Button-Events NICHT blockieren — CombatEngine muss ALLE Tasten sehen!
                
                // Example: Radial-Menu Navigation (D-Pad)
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // D-Pad for Radial-Menu Navigation
                    // BUT: Auch Gameplay-Buttons müssen weitergegeben are!
                }

                // Example: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // Inventar-Aktionen
                    // BUT: Auch Gameplay-Buttons müssen weitergegeben are!
                }
            }
        }

        /// <summary>
        /// Versucht Overlay-Eingaben to verarbeiten.
        /// RETURN: true if Overlay den Input konsumiert has (but CombatEngine bekommt trotzdem all Buttons!).
        /// </summary>
        public bool TryHandleInput(ParsedInput input)
        {
            if (_overlayActive)
            {
                // Overlay-Logik ausführen
                // BUT: Wir RETURNEN NICHT — CombatEngine muss ALLE Tasten sehen!

                // Example: Radial-Menu Navigation (D-Pad)
                if (input.LeftX != 0 || input.LeftY != 0)
                {
                    // D-Pad for Radial-Menu Navigation
                    // BUT: Auch Gameplay-Buttons müssen weitergegeben are!
                }

                // Example: Inventar-Tasten
                if (input.BtnA || input.BtnB || input.BtnX || input.BtnY)
                {
                    // Inventar-Aktionen
                    // BUT: Auch Gameplay-Buttons müssen weitergegeben are!
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
