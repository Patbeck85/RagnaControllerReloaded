using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-2304: Profile Diff/Compare UI.
    /// Vergleicht zwei Profile nebeneinander und hebt Unterschiede hervor.
    /// </summary>
    public partial class ProfileDiffWindow : Window, INotifyPropertyChanged
    {
        private readonly ProfileManager _manager;
        private readonly ObservableCollection<DiffItem> _diffItems = new();
        private string _summaryText = "Wählen Sie zwei Profile aus, um Unterschiede anzuzeigen.";

        public string SummaryText
        {
            get => _summaryText;
            set { _summaryText = value; OnPropertyChanged(); }
        }

        public ProfileDiffWindow(ProfileManager manager)
        {
            InitializeComponent();
            _manager = manager;
            DiffResultsList.ItemsSource = _diffItems;
            DataContext = this;
            Loaded += (s, e) => PopulateCombos();
        }

        // ── Window Drag ──────────────────────────────────────────────
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ── Combo Population ─────────────────────────────────────────
        private void PopulateCombos()
        {
            var profiles = _manager.Profiles.OrderBy(p => p.Name).ToList();
            ProfileComboA.ItemsSource = profiles;
            ProfileComboB.ItemsSource = profiles;

            if (profiles.Count > 0)
            {
                ProfileComboA.SelectedIndex = 0;
                if (profiles.Count > 1)
                    ProfileComboB.SelectedIndex = 1;
            }
        }

        // ── Selection Changed ────────────────────────────────────────
        private void ProfileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CompareProfiles();
        }

        // ── Compare Logic ────────────────────────────────────────────
        private void CompareProfiles()
        {
            if (ProfileComboA.SelectedItem is not Profile profileA ||
                ProfileComboB.SelectedItem is not Profile profileB)
            {
                _diffItems.Clear();
                SummaryText = "Wählen Sie zwei Profile aus, um Unterschiede anzuzeigen.";
                return;
            }

            if (profileA == profileB)
            {
                _diffItems.Clear();
                SummaryText = "Bitte wählen Sie zwei verschiedene Profile aus.";
                return;
            }

            _diffItems.Clear();
            var diffs = new List<DiffItem>();

            // Compare all public properties via reflection
            var type = typeof(Profile);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .OrderBy(p => p.Name);

            int diffCount = 0;
            foreach (var prop in properties)
            {
                var valueA = prop.GetValue(profileA);
                var valueB = prop.GetValue(profileB);

                // Skip identical values
                if (Equals(valueA, valueB))
                {
                    diffs.Add(new DiffItem
                    {
                        PropertyName = prop.Name,
                        ValueA = FormatValue(valueA),
                        ValueB = FormatValue(valueB),
                        ValueAColor = "#39FF14",
                        ValueBColor = "#39FF14",
                        Remark = "identisch"
                    });
                    continue;
                }

                // Special handling for known important properties
                var (remark, isImportant) = GetPropertyRemark(prop.Name, valueA, valueB);

                                diffs.Add(new DiffItem
                                {
                                    PropertyName = prop.Name,
                                    ValueA = FormatValue(valueA),
                                    ValueB = FormatValue(valueB),
                                    ValueAColor = "#FF3A52",
                                    ValueBColor = "#FF3A52",
                                    Remark = remark,
                                    IsImportant = isImportant,
                                    RowBackground = diffCount % 2 == 0 ? "#1a1a20" : "#0f0f12"
                                });
                                diffCount++;
            }

            // Add to collection
            foreach (var d in diffs)
                _diffItems.Add(d);

            // Summary
            int identicalCount = diffs.Count(d => d.Remark == "identisch");
            int differentCount = diffs.Count - identicalCount;
            SummaryText = $"Vergleich: {profileA.Name} vs {profileB.Name} | {identicalCount} identisch | {differentCount} unterschiedlich";
        }

        private (string remark, bool important) GetPropertyRemark(string propName, object? valueA, object? valueB)
        {
            // Wichtige Eigenschaften kennzeichnen
            var importantProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Name", "Class", "LightbarColor", "OverlayTheme",
                "ButtonMappings", "MouseSensitivity", "Deadzone",
                "MovementCurve", "CursorMaxSpeed", "ActionSpeed",
                "AutoAttackEnabled", "AutoRetargetEnabled",
                "MageEnabled", "SupportEnabled", "KiteEnabled",
                "ComboEnabled", "ComboAutoLoop",
                "GyroEnabled", "GyroSensitivity", "GyroBlend",
                "StickDeadzone", "TriggerDeadzone", "StickNormalization",
                "SchemaVersion", "SchemaVersion",
                "EnableSmartStandby", "StandbyTimeoutMinutes",
                "EnableDynamicTickReduction", "LightIdleIntervalMs", "DeepIdleIntervalMs",
                "LastControllerGuid", "AutoActivateOnControllerConnect",
                "LeftTriggerMode", "RightTriggerMode"
            };

            if (importantProperties.Contains(propName))
            {
                return (remark: "WICHTIG", isImportant: true);
            }

            return (remark: "", isImportant: false);
        }

        private string FormatValue(object? value)
        {
            if (value == null) return "null";

            return value switch
            {
                string s => string.IsNullOrEmpty(s) ? "(leer)" : s,
                DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
                IEnumerable<object> enumVal => $"[{enumVal.Count()}] Items",
                Dictionary<string, object> dict => $"Dict[{dict.Count}]",
                _ => value.ToString() ?? "null"
            };
        }

        // ── INotifyPropertyChanged ───────────────────────────────────
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// Einzelnes Diff-Item für die Liste.
    /// </summary>
    public class DiffItem : INotifyPropertyChanged
    {
        public string PropertyName { get; set; } = "";
        public string ValueA { get; set; } = "";
        public string ValueB { get; set; } = "";
        public string ValueAColor { get; set; } = "#FF3A52";
        public string ValueBColor { get; set; } = "#FF3A52";
        public string Remark { get; set; } = "";
        public bool IsImportant { get; set; } = false;
        public string RowBackground { get; set; } = "#1a1a20";

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}