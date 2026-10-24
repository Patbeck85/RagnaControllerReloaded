using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using RagnaController.Models;

namespace RagnaController.Core.Services
{
    /// <summary>
    /// TASK-2804: GamepadNavigationService — Ermöglicht die Bedienung aller Fenster
    /// ausschließlich per Gamepad (D-Pad, Buttons, Sticks) mit Fokus-Management.
    ///
    /// White-Hat: Keine automatische Spielsteuerung, nur UI-Navigation.
    /// </summary>
    public sealed class GamepadNavigationService
    {
        private readonly List<WeakReference<UIElement>> _focusableElements = new();
        private int _currentFocusIndex = -1;
        private bool _navigationEnabled = true;
        private readonly Dictionary<UIElement, UIElement?> _elementToNext = new();

        /// <summary>Ob die Gamepad-Navigation aktiv ist.</summary>
        public bool IsNavigationEnabled => _navigationEnabled;

        /// <summary>Aktuell fokussiertes Element (falls nachverfolgbar).</summary>
        public UIElement? CurrentFocusedElement { get; private set; }

        /// <summary>Event bei Fokus-Änderung.</summary>
        public event EventHandler<UIElement?>? FocusChanged;

        /// <summary>Registriert ein fokussierbares UI-Element für Gamepad-Navigation.</summary>
        public void RegisterFocusable(UIElement element)
        {
            if (element == null) return;
            _focusableElements.Add(new WeakReference<UIElement>(element));
            element.GotFocus += (s, e) =>
            {
                CurrentFocusedElement = s as UIElement;
                UpdateFocusIndex(CurrentFocusedElement);
                FocusChanged?.Invoke(this, CurrentFocusedElement);
            };
        }

        /// <summary>Entfernt ein Element aus der Navigation.</summary>
        public void UnregisterFocusable(UIElement element)
        {
            if (element == null) return;
            for (int i = _focusableElements.Count - 1; i >= 0; i--)
            {
                if (_focusableElements[i].TryGetTarget(out var existing) && existing == element)
                {
                    _focusableElements.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>Navigiert zum nächsten Element (D-Pad rechts/unten oder Stick).</summary>
        public bool NavigateNext()
        {
            if (!_navigationEnabled || _focusableElements.Count == 0)
                return false;

            var valid = GetValidElements();
            if (valid.Count == 0) return false;

            _currentFocusIndex = (_currentFocusIndex + 1) % valid.Count;
            FocusElement(valid[_currentFocusIndex]);
            return true;
        }

        /// <summary>Navigiert zum vorherigen Element (D-Pad links/oben oder Stick).</summary>
        public bool NavigatePrevious()
        {
            if (!_navigationEnabled || _focusableElements.Count == 0)
                return false;

            var valid = GetValidElements();
            if (valid.Count == 0) return false;

            _currentFocusIndex--;
            if (_currentFocusIndex < 0)
                _currentFocusIndex = valid.Count - 1;
            FocusElement(valid[_currentFocusIndex]);
            return true;
        }

        /// <summary>Löst das aktuell fokussierte Element aus (A-Button / Enter).</summary>
        public bool ActivateCurrent()
        {
            if (!_navigationEnabled || CurrentFocusedElement == null)
                return false;

            if (CurrentFocusedElement is ButtonBase btn)
            {
                btn.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                return true;
            }

            if (CurrentFocusedElement is CheckBox cb)
            {
                cb.IsChecked = !cb.IsChecked;
                return true;
            }

            return false;
        }

        /// <summary>Aktiviert oder deaktiviert die Navigation.</summary>
        public void SetNavigationEnabled(bool enabled)
        {
            _navigationEnabled = enabled;
            if (!enabled)
                CurrentFocusedElement = null;
        }

        /// <summary>Setzt den Fokus auf ein spezifisches Element.</summary>
        public void FocusElement(UIElement? element)
        {
            if (element == null) return;

            element.Focus();
            CurrentFocusedElement = element;

            for (int i = 0; i < _focusableElements.Count; i++)
            {
                if (_focusableElements[i].TryGetTarget(out var target) && target == element)
                {
                    _currentFocusIndex = i;
                    break;
                }
            }

            FocusChanged?.Invoke(this, CurrentFocusedElement);
        }

        /// <summary>Deaktiviert die Navigation und gibt den Fokus frei.</summary>
        public void Reset()
        {
            _currentFocusIndex = -1;
            CurrentFocusedElement = null;
        }

        private List<UIElement> GetValidElements()
        {
            var valid = new List<UIElement>();
            for (int i = _focusableElements.Count - 1; i >= 0; i--)
            {
                if (!_focusableElements[i].TryGetTarget(out var target) || target == null)
                {
                    _focusableElements.RemoveAt(i);
                }
                else if (target.IsEnabled && target.Visibility == Visibility.Visible)
                {
                    valid.Add(target);
                }
            }
            return valid;
        }

        private void UpdateFocusIndex(UIElement? element)
        {
            if (element == null) return;
            for (int i = 0; i < _focusableElements.Count; i++)
            {
                if (_focusableElements[i].TryGetTarget(out var target) && target == element)
                {
                    _currentFocusIndex = i;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Re-Export von ButtonBase für die Navigation (vermeidet doppelten Using in consumer-Code).
    /// </summary>
    public static class GamepadNavigationKeys
    {
        /// <summary>D-Pad Hoch — Fokus vorheriges Element.</summary>
        public const Key DPadUp = Key.Up;
        /// <summary>D-Pad Runter — Fokus nächstes Element.</summary>
        public const Key DPadDown = Key.Down;
        /// <summary>D-Pad Links — Fokus vorheriges Element.</summary>
        public const Key DPadLeft = Key.Left;
        /// <summary>D-Pad Rechts — Fokus nächstes Element.</summary>
        public const Key DPadRight = Key.Right;
        /// <summary>A-Button — Aktuelles Element aktivieren.</summary>
        public const Key ButtonA = Key.Space;
        /// <summary>B-Button — Zurück / Menü schließen.</summary>
        public const Key ButtonB = Key.Escape;
    }
}
