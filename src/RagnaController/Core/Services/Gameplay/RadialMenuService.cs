using System;
using System.Collections.Generic;
using System.Diagnostics;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// Steam Deck Quick-Access Radial Menu — Kreisradial-Menü for schnellen Zugriff
    /// on häufig verwendete Befehle (Calibrate, Remap, Profile, Telemetry, etc.).
    /// </summary>
    public sealed class RadialMenuService : IDisposable
    {
        private readonly List<RadialMenuItem> _items = new();
        private readonly IRumbleProvider? _rumble;
        private bool _disposed;
        private bool _isOpen;
        private int _selectedIndex = -1;

        public int ItemCount => _items.Count;
        public bool IsOpen => _isOpen;
        public int SelectedIndex => _selectedIndex;

        public event EventHandler<RadialMenuItem>? ItemSelected;
        public event EventHandler<bool>? MenuStateChanged;

        public RadialMenuService(IRumbleProvider? rumble = null)
        {
            _rumble = rumble;
        }

        public void AddItem(string label, Action action, double angleDegrees = 0)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(RadialMenuService));
            _items.Add(new RadialMenuItem(label, action, angleDegrees));
        }

        public void ClearItems()
        {
            if (_disposed) return;
            _items.Clear();
            _selectedIndex = -1;
        }

        public void Open()
        {
            if (_disposed) return;
            _isOpen = true;
            _selectedIndex = 0;
            MenuStateChanged?.Invoke(this, true);
            Debug.WriteLine("[RadialMenu] Geöffnet");
        }

        public void Close()
        {
            if (_disposed) return;
            _isOpen = false;
            _selectedIndex = -1;
            MenuStateChanged?.Invoke(this, false);
            Debug.WriteLine("[RadialMenu] Geschlossen");
        }

        public void SelectNext()
        {
            if (!_isOpen || _items.Count == 0) return;
            _selectedIndex = (_selectedIndex + 1) % _items.Count;
            _rumble?.SetRumble(0.3f, 0.1f); // Kurzes Haptic Feedback bei Navigation
        }

        public void SelectPrevious()
        {
            if (!_isOpen || _items.Count == 0) return;
            _selectedIndex = (_selectedIndex - 1 + _items.Count) % _items.Count;
            _rumble?.SetRumble(0.3f, 0.1f); // Kurzes Haptic Feedback bei Navigation
        }

        public void ActivateSelection()
        {
            if (!_isOpen || _selectedIndex < 0 || _selectedIndex >= _items.Count) return;
            var item = _items[_selectedIndex];
            ItemSelected?.Invoke(this, item);
            item.Action?.Invoke();
            _rumble?.SetRumble(0.8f, 0.8f); // Stärkeres Feedback bei Auswahl
            Close();
        }

        public RadialMenuItem? GetItem(int index)
        {
            if (index < 0 || index >= _items.Count) return null;
            return _items[index];
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Close();
            _items.Clear();
        }
    }

    /// <summary>
    /// Ein einzelnes Element im Radial Menu.
    /// </summary>
    public sealed class RadialMenuItem
    {
        public string Label { get; }
        public Action? Action { get; }
        public double AngleDegrees { get; }

        public RadialMenuItem(string label, Action? action = null, double angleDegrees = 0)
        {
            Label = label ?? throw new ArgumentNullException(nameof(label));
            Action = action;
            AngleDegrees = angleDegrees;
        }
    }
}
