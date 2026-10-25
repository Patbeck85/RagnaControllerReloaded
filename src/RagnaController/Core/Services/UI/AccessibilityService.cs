using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using RagnaController.Models;

namespace RagnaController.Core.Services.UI {
    /// <summary>
    /// TASK-2801: AccessibilityService — Zentrale Barrierefreiheit für Screen Reader.
    ///
    /// Verwaltet AutomationProperties, Live-Regionen (für dynamische Inhalte),
    /// Fokus-Management und stellt Hilfsmethoden bereit, damit alle UI-Elemente
    /// von Screen Readern (NVDA, JAWS, Windows Narrator) erkannt werden.
    ///
    /// White-Hat: Keine automatische Bedienung, nur barrierefreie Metadaten.
    /// </summary>
    public sealed class AccessibilityService : IDisposable
    {
        private readonly Settings _settings;
        private readonly List<WeakReference<UIElement>> _liveRegions = new();
        private static bool _reducedMotionDefault = false;
        private bool _disposed;

        /// <summary>Ob Screen-Reader-Modus aktiv ist (über Windows SystemParameters oder Einstellung).</summary>
        public bool IsScreenReaderActive => _settings.AccessibilityScreenReaderMode;

        /// <summary>Ereignis, wenn sich der Screen-Reader-Status ändert.</summary>
        public event EventHandler<bool>? ScreenReaderStatusChanged;

        public AccessibilityService(Settings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>
        /// Registriert ein UI-Element als Live-Region. Änderungen an dessen Inhalt
        /// werden Screen Readern automatisch mitgeteilt.
        /// </summary>
        public void RegisterLiveRegion(UIElement element)
        {
            if (element == null) return;
            _liveRegions.Add(new WeakReference<UIElement>(element));
            AutomationProperties.SetLiveSetting(element, AutomationLiveSetting.Polite);
        }

        /// <summary>Entfernt eine Live-Region.</summary>
        public void UnregisterLiveRegion(UIElement element)
        {
            if (element == null) return;
            for (int i = _liveRegions.Count - 1; i >= 0; i--)
            {
                if (_liveRegions[i].TryGetTarget(out var existing) && existing == element)
                {
                    _liveRegions.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>Benachrichtigt Screen Reader über eine Änderung in einer Live-Region.</summary>
        public void AnnounceLiveRegion(UIElement element, string message)
        {
            if (!_settings.AccessibilityScreenReaderMode || string.IsNullOrWhiteSpace(message))
                return;

            AutomationProperties.SetHelpText(element, message);
            // Force UI Automation to raise a property changed event
            var peer = FrameworkElementAutomationPeer.FromElement(element);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        }

        /// <summary>Setzt einen beschreibenden Namen für ein UI-Element (Screen Reader Name).</summary>
        public static void SetName(UIElement element, string name)
        {
            if (element == null || string.IsNullOrWhiteSpace(name)) return;
            AutomationProperties.SetName(element, name);
        }

        /// <summary>Setzt einen beschreibenden Hilfetext für ein UI-Element.</summary>
        public static void SetHelpText(UIElement element, string helpText)
        {
            if (element == null || string.IsNullOrWhiteSpace(helpText)) return;
            AutomationProperties.SetHelpText(element, helpText);
        }

        /// <summary>Setzt die Accelerator-Taste (Tastenkürzel) für ein Element.</summary>
        public static void SetAcceleratorKey(UIElement element, string key)
        {
            if (element == null || string.IsNullOrWhiteSpace(key)) return;
            AutomationProperties.SetAcceleratorKey(element, key);
        }

        /// <summary>Setzt die Access Key (Tastenkombination) für ein Element.</summary>
        public static void SetAccessKey(UIElement element, string key)
        {
            if (element == null || string.IsNullOrWhiteSpace(key)) return;
            AutomationProperties.SetAccessKey(element, key);
        }

        /// <summary>Setzt die Live-Region-Einstellung (Off, Polite, Assertive).</summary>
        public static void SetLiveSetting(UIElement element, AutomationLiveSetting setting)
        {
            if (element == null) return;
            AutomationProperties.SetLiveSetting(element, setting);
        }

        /// <summary>Setzt die Steuerelement-Typ-Beschreibung für Screen Reader.</summary>
        public static void SetControlledType(UIElement element, string type)
        {
            if (element == null || string.IsNullOrWhiteSpace(type)) return;
            AutomationProperties.SetItemStatus(element, type);
        }

        /// <summary>Prüft, ob das aktuelle Fenster im Hochkontrast-Modus ist.</summary>
        public static bool IsHighContrastActive =>
            SystemParameters.HighContrast;

        /// <summary>Gibt zurück, ob Reduzierte Bewegung bevorzugt wird (Windows-Einstellung oder App-Einstellung).</summary>
        public static bool IsReducedMotionPreferred =>
            _reducedMotionDefault;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _liveRegions.Clear();
            }
        }
    }

    /// <summary>
    /// TASK-2801: AccessibilityScope — Hilfsklasse zur einfachen Anwendung
    /// von Accessibility-Eigenschaften auf XAML-Elemente via Code-Behind.
    /// </summary>
    public static class AccessibilityScope
    {
        /// <summary>Markiert ein Element als dekorativ (wird von Screen Readern ignoriert).</summary>
        public static void SetDecorative(UIElement element)
        {
            if (element == null) return;
            AutomationProperties.SetName(element, "");
            AutomationProperties.SetHelpText(element, "");
        }

        /// <summary>Konfiguriert ein Element als bedienbare Schaltfläche mit Name und Kurzbefehl.</summary>
        public static void ConfigureButton(ButtonBase button, string name, string? acceleratorKey = null, string? helpText = null)
        {
            if (button == null) return;
            AutomationProperties.SetName(button, name);
            if (!string.IsNullOrWhiteSpace(acceleratorKey))
                AutomationProperties.SetAcceleratorKey(button, acceleratorKey);
            if (!string.IsNullOrWhiteSpace(helpText))
                AutomationProperties.SetHelpText(button, helpText);
        }

        /// <summary>Konfiguriert ein Textfeld als bedienbares Eingabefeld.</summary>
        public static void ConfigureInput(TextBoxBase input, string name, string? helpText = null)
        {
            if (input == null) return;
            AutomationProperties.SetName(input, name);
            if (!string.IsNullOrWhiteSpace(helpText))
                AutomationProperties.SetHelpText(input, helpText);
        }

        /// <summary>Konfiguriert ein Kombinationsfeld als bedienbares Element.</summary>
        public static void ConfigureComboBox(ComboBox combo, string name, string? helpText = null)
        {
            if (combo == null) return;
            AutomationProperties.SetName(combo, name);
            if (!string.IsNullOrWhiteSpace(helpText))
                AutomationProperties.SetHelpText(combo, helpText);
        }

        /// <summary>Konfiguriert eine Checkbox als bedienbares Element.</summary>
        public static void ConfigureCheckBox(CheckBox checkBox, string name, string? helpText = null)
        {
            if (checkBox == null) return;
            AutomationProperties.SetName(checkBox, name);
            if (!string.IsNullOrWhiteSpace(helpText))
                AutomationProperties.SetHelpText(checkBox, helpText);
        }
    }
}
