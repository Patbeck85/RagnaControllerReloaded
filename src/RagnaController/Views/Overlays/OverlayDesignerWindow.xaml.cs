using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Core.Services;
using RagnaController.Models;

namespace RagnaController.Views.Overlays
{
    /// <summary>
    /// Overlay-Designer für Drag &amp; Drop Positionierung von Overlay-Elementen.
    /// FEAT-035: Overlay-Designer mit visueller Positionierung und Preset-Speicherung.
    /// </summary>
    public partial class OverlayDesignerWindow : Window
    {
        private readonly Settings _settings;
        private Border? _draggedElement;
        private Point _dragStart;
        private double _originalLeft;
        private double _originalTop;

        // Element-Identifikatoren für Preset-Speicherung
        private enum OverlayElementId
        {
            SkillCooldowns,
            BuffTimers,
            HpSp,
            TargetInfo,
            Profile
        }

        // Mapping von UI-Element zu Element-ID
        private readonly Dictionary<Border, OverlayElementId> _elementMap = new();

        // Preset-Datei-Pfad
        private static readonly string PresetsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RagnaController", "OverlayPresets");

        public OverlayDesignerWindow(Settings settings)
        {
            InitializeComponent();
            _settings = settings;

            // Element-Mapping aufbauen
            _elementMap[ElemSkillCooldowns] = OverlayElementId.SkillCooldowns;
            _elementMap[ElemBuffTimers] = OverlayElementId.BuffTimers;
            _elementMap[ElemHpSp] = OverlayElementId.HpSp;
            _elementMap[ElemTargetInfo] = OverlayElementId.TargetInfo;
            _elementMap[ElemProfile] = OverlayElementId.Profile;

            // Gespeicherte Positionen laden
            LoadSavedPositions();
        }

        /// <summary>Lädt gespeicherte Element-Positionen aus den Settings.</summary>
        private void LoadSavedPositions()
        {
            // Positionen werden pro Element in Settings gespeichert (vereinfacht: hier als Kommentar, da Settings diese Properties noch nicht hat)
            // In einer vollständigen Implementierung würden hier die Werte aus Settings geladen und auf die Canvas.Left/Top gesetzt.
            // Für jetzt starten wir mit den Default-Positionen aus dem XAML.
        }

        /// <summary>Speichert die aktuelle Position eines Elements.</summary>
        private void SaveElementPosition(Border element, OverlayElementId id)
        {
            var left = Canvas.GetLeft(element);
            var top = Canvas.GetTop(element);
            System.Diagnostics.Debug.WriteLine($"[OverlayDesigner] Position {id}: Left={left}, Top={top}");
            // Hinweis: Vollständige Settings-Integration erfordert zusätzliche Properties in Settings.cs
        }

        // ── Drag &amp; Drop Handlers ──────────────────────────────────

        private void Element_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border)
            {
                _draggedElement = border;
                _dragStart = e.GetPosition(DesignerCanvas);
                _originalLeft = Canvas.GetLeft(border);
                _originalTop = Canvas.GetTop(border);
                border.CaptureMouse();
            }
        }

        private void Element_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedElement == null) return;

            var currentPos = e.GetPosition(DesignerCanvas);
            var deltaX = currentPos.X - _dragStart.X;
            var deltaY = currentPos.Y - _dragStart.Y;

            var newLeft = Math.Clamp(_originalLeft + deltaX, 0, DesignerCanvas.ActualWidth - _draggedElement.ActualWidth);
            var newTop = Math.Clamp(_originalTop + deltaY, 0, DesignerCanvas.ActualHeight - _draggedElement.ActualHeight);

            Canvas.SetLeft(_draggedElement, newLeft);
            Canvas.SetTop(_draggedElement, newTop);
        }

        private void Element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedElement != null && _elementMap.TryGetValue(_draggedElement, out var id))
            {
                SaveElementPosition(_draggedElement, id);
            }
            _draggedElement?.ReleaseMouseCapture();
            _draggedElement = null;
        }

        // ── Preset Controls ───────────────────────────────────────────

        private void BtnSavePreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(PresetsPath)) Directory.CreateDirectory(PresetsPath);

                var preset = new OverlayLayoutPreset
                {
                    Name = "Custom_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                    CreatedAt = DateTime.Now,
                    Elements = new List<OverlayElementPosition>()
                };

                foreach (var kvp in _elementMap)
                {
                    preset.Elements.Add(new OverlayElementPosition
                    {
                        ElementId = kvp.Value.ToString(),
                        Left = Canvas.GetLeft(kvp.Key),
                        Top = Canvas.GetTop(kvp.Key)
                    });
                }

                var presetFile = Path.Combine(PresetsPath, preset.Name + ".json");
                var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(presetFile, json);

                MessageBox.Show($"Preset gespeichert: {preset.Name}", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("[OverlayDesigner] Preset-Save-Fehler: " + ex.Message);
            }
        }

        private void BtnLoadPreset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(PresetsPath))
                {
                    MessageBox.Show("Keine Presets gefunden.", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var files = Directory.GetFiles(PresetsPath, "*.json");
                if (files.Length == 0)
                {
                    MessageBox.Show("Keine Presets gefunden.", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Einfacher Dialog: erstes Preset laden (in einer vollständigen Implementierung wäre hier eine Auswahl-Liste)
                var presetFile = files[0];
                var json = File.ReadAllText(presetFile);
                var preset = JsonSerializer.Deserialize<OverlayLayoutPreset>(json);

                if (preset == null) return;

                foreach (var elemPos in preset.Elements)
                {
                    if (Enum.TryParse<OverlayElementId>(elemPos.ElementId, out var id))
                    {
                        foreach (var kvp in _elementMap)
                        {
                            if (kvp.Value == id)
                            {
                                Canvas.SetLeft(kvp.Key, elemPos.Left);
                                Canvas.SetTop(kvp.Key, elemPos.Top);
                                break;
                            }
                        }
                    }
                }

                MessageBox.Show($"Preset geladen: {preset.Name}", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden: {ex.Message}", "Overlay Designer", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine("[OverlayDesigner] Preset-Load-Fehler: " + ex.Message);
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            // Default-Positionen wiederherstellen
            Canvas.SetLeft(ElemSkillCooldowns, 20); Canvas.SetTop(ElemSkillCooldowns, 20);
            Canvas.SetLeft(ElemBuffTimers, 160); Canvas.SetTop(ElemBuffTimers, 20);
            Canvas.SetLeft(ElemHpSp, 20); Canvas.SetTop(ElemHpSp, 80);
            Canvas.SetLeft(ElemTargetInfo, 140); Canvas.SetTop(ElemTargetInfo, 80);
            Canvas.SetLeft(ElemProfile, 20); Canvas.SetTop(ElemProfile, 130);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }

    /// <summary>Layout-Preset für Overlay-Elemente.</summary>
    public class OverlayLayoutPreset
    {
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<OverlayElementPosition> Elements { get; set; } = new();
    }

    /// <summary>Position eines einzelnen Overlay-Elements.</summary>
    public class OverlayElementPosition
    {
        public string ElementId { get; set; } = "";
        public double Left { get; set; }
        public double Top { get; set; }
    }
}
