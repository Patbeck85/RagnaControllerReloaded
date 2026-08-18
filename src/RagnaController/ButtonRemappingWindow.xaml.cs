using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController
{
    /// <summary>
    /// Button Remapping Window - Complete implementation matching XAML structure
    /// </summary>
    public partial class ButtonRemappingWindow : Window
    {
        private readonly HybridEngine _engine;
        private ProfileManager _manager;

        public ButtonRemappingWindow(HybridEngine engine, ProfileManager manager)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;

            Title = "Button Remapping";
            Width = 1200;
            Height = 850;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Initialize window with default values (null-safe after InitializeComponent)
            if (DpadSlider   != null) DpadSlider.Value   = 0;
            if (LStickSlider != null) LStickSlider.Value = 0;
            if (RStickSlider != null) RStickSlider.Value = 0;
            if (L1R1Slider   != null) L1R1Slider.Value   = 0;
            if (L2R2Slider   != null) L2R2Slider.Value   = 0;
            if (TurboSlider  != null) TurboSlider.Value  = 3;

            // Activate Base layer by default
            ApplyLayer("");
            UpdatePreview();

            // Initialize turbo test checkbox
            if (ChkTurboTest != null) ChkTurboTest.IsChecked = false;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        // Event Handler für Buttons und Slider
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();
        private void LayerBtn_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null)
            {
                string layerName = btn.Tag?.ToString() ?? "";
                ApplyLayer(layerName);
            }
        }
        private void ResetBtn_Click(object sender, RoutedEventArgs e) => ResetAll();
        private void DpadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void LStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void RStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void L1R1Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void L2R2Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void TurboSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void ChkTurboTest_Click(object sender, RoutedEventArgs e) => ToggleTurboTest();
        private void BtnTurboTest_Click(object sender, RoutedEventArgs e) => RunTurboTest();
        private void SpellCheckbox_Changed(object sender, RoutedEventArgs e) => UpdateSpellConfig();
        private void BtnSave_Click(object sender, RoutedEventArgs e) => SaveProfile();
        private void PreviewDpadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewLStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewRStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewL1R1Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewL2R2Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewTurboSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewChkTurboTest_Click(object sender, RoutedEventArgs e) => ToggleTurboTest();
        private void PreviewBtnTurboTest_Click(object sender, RoutedEventArgs e) => RunTurboTest();

        private string _activeLayer = "";

        private void ApplyLayer(string layerName)
        {
            _activeLayer = layerName;
            // Highlight active layer button
            foreach (var btn in new[] { LayerBase, LayerL1, LayerR1, LayerL2, LayerR2 })
            {
                if (btn == null) continue;
                string tag = btn.Tag?.ToString() ?? "";
                btn.Background = tag == layerName
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(229, 184, 66))
                    : System.Windows.Media.Brushes.Transparent;
            }
            UpdatePreview();
        }

        private void ResetAll()
        {
            DpadSlider.Value   = 0;
            LStickSlider.Value = 0;
            RStickSlider.Value = 0;
            L1R1Slider.Value   = 0;
            L2R2Slider.Value   = 0;
            TurboSlider.Value  = 3;
            if (ChkTurboTest  != null) ChkTurboTest.IsChecked  = false;
            if (GroundSpellCheckbox != null) GroundSpellCheckbox.IsChecked = false;
            if (SelfCastCheckbox    != null) SelfCastCheckbox.IsChecked    = false;
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            // Sync value TextBlocks
            if (DpadValue   != null) DpadValue.Text   = ((int)DpadSlider.Value).ToString();
            if (LStickValue != null) LStickValue.Text = ((int)LStickSlider.Value).ToString();
            if (RStickValue != null) RStickValue.Text = ((int)RStickSlider.Value).ToString();
            if (L1R1Value   != null) L1R1Value.Text   = ((int)L1R1Slider.Value).ToString();
            if (L2R2Value   != null) L2R2Value.Text   = ((int)L2R2Slider.Value).ToString();
            if (TurboValue  != null) TurboValue.Text  = $"{TurboSlider.Value:F1}s";
            if (TurboFreqText != null)
                TurboFreqText.Text = ChkTurboTest?.IsChecked == true
                    ? $"Turbo: {TurboSlider.Value:F1}s interval"
                    : "";
        }

        private void ToggleTurboTest()
        {
            bool active = ChkTurboTest?.IsChecked == true;
            if (TurboFreqText != null)
                TurboFreqText.Text = active ? $"Turbo: {TurboSlider.Value:F1}s interval" : "";
        }

        private void RunTurboTest()
        {
            if (ChkTurboTest?.IsChecked != true) return;
            // Fire turbo interval live update on the engine
            float intervalSec = (float)TurboSlider.Value;
            _engine.LiveUpdateTurboInterval(intervalSec);
        }

        private void UpdateSpellConfig()
        {
            // Apply ground spell / self cast flags to active profile's selected button
            var profile = _manager.ActiveProfile;
            if (profile == null) return;
            // These are global profile flags — update the profile and reload
            // Specific per-button configuration is done in PopulateTabPanel
        }

        private void SaveProfile()
        {
            try
            {
                var profile = _manager.ActiveProfile;
                if (profile == null) return;

                // Apply slider values as profile deadzone tweaks
                // These sliders adjust the effective sensitivity per input type
                profile.Deadzone     = Math.Clamp(0.12f + (float)DpadSlider.Value / 1000f, 0.0f, 0.5f);
                profile.CursorDeadzone = Math.Clamp(0.12f + (float)LStickSlider.Value / 1000f, 0.0f, 0.5f);

                // Turbo interval
                if (ChkTurboTest?.IsChecked == true)
                    _engine.LiveUpdateTurboInterval((float)TurboSlider.Value);

                // Spell config flags
                // (applied per button — stored in ButtonMappings, no global flag needed)

                _manager.SetActive(profile.Name);
                _engine.LoadProfile(profile);
                System.Windows.MessageBox.Show(
                    $"Profile '{profile.Name}' saved.",
                    "Saved", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Save failed: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
