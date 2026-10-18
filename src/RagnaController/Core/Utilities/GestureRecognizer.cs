using System;
using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// Erkennt Gesten about den Right-Stick for Handheld-Navigation.
    ///
    /// Architektur:
    ///   - Every Tick is der aktuelle Right-Stick-Wert (RightX/RightY) eingespeist
    ///   - Bei schneller Bewegung in eine Richtung is ein Swipe erkannt
    ///   - Richtung bestimmt welche Aktion ausgelöst is:
    ///     * Rechts → Radial Menu öffnen/schließen
    ///     * Oben/Unten → Im geöffneten Menü navigieren (SelectNext/Previous)
    ///   - R3-Drücken bestätigt die aktuelle Auswahl (ActivateSelection)
    ///
    /// Vollständig headless-testbar ohne echtes HID-Device.
    /// </summary>
    /// <summary>
    /// Utility class for shared infrastructure.
    /// </summary>
    public sealed class GestureRecognizer : IDisposable
    {
        private bool _disposed;
        private bool _menuOpen;
        private float _lastRightX;
        private float _lastRightY;
        private GestureDirection _lastDirection = GestureDirection.None;

        /// <summary>Schwelle for Richtungs-Erkennung (Stick-Wert -1..1). Default 0.6.</summary>
        public float DirectionThreshold { get; set; } = 0.6f;

        /// <summary>Radial Menu aktuell geöffnet?</summary>
        public bool IsMenuOpen => _menuOpen;

        /// <summary>Event: Radial Menu geöffnet/geschlossen.</summary>
        public event EventHandler<bool>? MenuStateChanged;

        /// <summary>Event: Menü-Navigation (nächstes Item).</summary>
        public event EventHandler? ItemNext;

        /// <summary>Event: Menü-Navigation (vorheriges Item).</summary>
        public event EventHandler? ItemPrevious;

        /// <summary>Event: Aktuelle Auswahl bestätigen.</summary>
        public event EventHandler? ItemActivate;

        /// <summary>
        /// Speist Right-Stick Werte and R3-Zustand ein.
        /// Is jeden Tick vom InputRouter aufgerufen.
        /// </summary>
        /// <param name="rightX">Right-Stick X (-1..1)</param>
        /// <param name="rightY">Right-Stick Y (-1..1)</param>
        /// <param name="r3Pressed">R3 (Right Stick Click) gedrückt?</param>
        public void Tick(float rightX, float rightY, bool r3Pressed)
        {
            if (_disposed) return;

            // R3-Klick bestätigt Auswahl
            if (r3Pressed)
            {
                ItemActivate?.Invoke(this, EventArgs.Empty);
            }

            // Aktuelle Richtung bestimmen (dominante Achse)
            var direction = DetectDirection(rightX, rightY);

            // Edge-Detektion: Only feuern if sich die Richtung ändert
            // (from keiner Richtung to einer spezifischen Richtung)
            if (direction != GestureDirection.None && direction != _lastDirection)
            {
                OnDirectionDetected(direction);
            }

            _lastDirection = direction;
            _lastRightX = rightX;
            _lastRightY = rightY;
        }

        private GestureDirection DetectDirection(float rightX, float rightY)
        {
            // Dominante Achse bestimmen
            if (Math.Abs(rightX) > Math.Abs(rightY))
            {
                // Horizontale Bewegung dominiert
                if (rightX > DirectionThreshold)
                    return GestureDirection.Right;
                if (rightX < -DirectionThreshold)
                    return GestureDirection.Left;
            }
            else
            {
                // Vertikale Bewegung dominiert
                if (rightY < -DirectionThreshold) // Oben (Y invertiert im Screen-Koordinatensystem)
                    return GestureDirection.Up;
                if (rightY > DirectionThreshold) // Unten
                    return GestureDirection.Down;
            }

            return GestureDirection.None;
        }

        private void OnDirectionDetected(GestureDirection direction)
        {
            switch (direction)
            {
                case GestureDirection.Right:
                    // Rechts → Radial Menu toggle
                    ToggleMenu();
                    break;

                case GestureDirection.Left:
                    // Links → Im Menü: vorheriges Item
                    if (_menuOpen)
                        ItemPrevious?.Invoke(this, EventArgs.Empty);
                    break;

                case GestureDirection.Up:
                    // Oben → Im Menü: nächstes Item
                    if (_menuOpen)
                        ItemNext?.Invoke(this, EventArgs.Empty);
                    break;

                case GestureDirection.Down:
                    // Unten → Im Menü: vorheriges Item
                    if (_menuOpen)
                        ItemPrevious?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }

        private void ToggleMenu()
        {
            _menuOpen = !_menuOpen;
            MenuStateChanged?.Invoke(this, _menuOpen);
            Debug.WriteLine($"[GestureRecognizer] Radial Menu {(_menuOpen ? "geöffnet" : "geschlossen")}");
        }

        /// <summary>Menü programmatisch öffnen.</summary>
        public void OpenMenu()
        {
            if (_disposed || _menuOpen) return;
            _menuOpen = true;
            MenuStateChanged?.Invoke(this, true);
        }

        /// <summary>Menü programmatisch schließen.</summary>
        public void CloseMenu()
        {
            if (_disposed || !_menuOpen) return;
            _menuOpen = false;
            MenuStateChanged?.Invoke(this, false);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            MenuStateChanged = null;
            ItemNext = null;
            ItemPrevious = null;
            ItemActivate = null;
        }
    }

    /// <summary>
    /// Erkannte Stick-Richtung for Gesten-Steuerung.
    /// </summary>
    public enum GestureDirection
    {
        None,
        Right,
        Left,
        Up,
        Down
    }
}
