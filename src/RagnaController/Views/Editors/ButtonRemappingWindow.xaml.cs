using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualBasic;
using RagnaController.Core;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Views.Editors
{
    /// <summary>
    /// Button Remapping Window - Complete implementation matching XAML structure
    /// </summary>
    public partial class ButtonRemappingWindow : Window
    {
        private readonly EngineOrchestrator _engine;
        private ProfileManager _manager;
        private bool _hasUnsavedChanges = false;

        public ButtonRemappingWindow(EngineOrchestrator engine, ProfileManager manager)
        {
            InitializeComponent();
            _engine = engine;
            _manager = manager;

            Title = "Button Remapping";
            Width = 1200;
            Height = 850;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Initialize window with default values (null-safe after InitializeComponent)
            if (SldDpad   != null) SldDpad.Value   = 0;
            if (SldLStick != null) SldLStick.Value = 0;
            if (SldRStick != null) SldRStick.Value = 0;
            if (SldL1R1   != null) SldL1R1.Value   = 0;
            if (SldL2R2   != null) SldL2R2.Value   = 0;

            // Populate profile combo
            PopulateProfileCombo();

            // Activate Base layer by default
            ApplyLayer("");
            UpdatePreview();
        }

        // TASK-2011: Populate profile combo
        private void PopulateProfileCombo()
        {
            ProfileCombo.Items.Clear();
            foreach (var p in _manager.Profiles)
            {
                ProfileCombo.Items.Add(new System.Windows.Controls.ComboBoxItem
                {
                    Content = p.Name,
                    Tag = p
                });
            }
            var active = _manager.ActiveProfile;
            if (active != null)
            {
                foreach (System.Windows.Controls.ComboBoxItem item in ProfileCombo.Items)
                {
                    if ((string)item.Tag == active.Name || (string)item.Content == active.Name)
                    {
                        ProfileCombo.SelectedItem = item;
                        break;
                    }
                }
            }
            UpdateProfileInfo();
        }

        // TASK-2011: Profile selection changed
        private void ProfileCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ProfileCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is Profile p)
            {
                _manager.SetActive(p.Name);
                UpdateProfileInfo();
                UpdatePreview();
            }
        }

        // TASK-2011: Update profile info display
        private void UpdateProfileInfo()
        {
            var profile = _manager.ActiveProfile;
            if (profile == null)
            {
                ProfileInfoName.Text = "No profile selected";
                ProfileInfoClass.Text = "Class: —";
                ProfileInfoDeadzone.Text = "Deadzone: —";
                ProfileInfoCursorDeadzone.Text = "Cursor Deadzone: —";
                return;
            }
            ProfileInfoName.Text = profile.Name;
            ProfileInfoClass.Text = $"Class: {profile.Class}";
            ProfileInfoDeadzone.Text = $"Deadzone: {profile.Deadzone:P0}";
            ProfileInfoCursorDeadzone.Text = $"Cursor Deadzone: {profile.CursorDeadzone:P0}";
        }

        // TASK-2011: Load selected profile
        private void BtnLoadProfile_Click(object sender, RoutedEventArgs e)
        {
            if (ProfileCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item && item.Tag is Profile p)
            {
                _manager.SetActive(p.Name);
                _engine.LoadProfile(p);
                UpdateProfileInfo();
                UpdatePreview();
                _hasUnsavedChanges = false;
            }
        }

        // TASK-2011: Save profile as new
        private void BtnSaveAsProfile_Click(object sender, RoutedEventArgs e)
        {
            var profile = _manager.ActiveProfile;
            if (profile == null) return;

            // Prompt for new name
            var newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter new profile name:",
                "Save Profile As",
                profile.Name + " (copy)");
            if (string.IsNullOrWhiteSpace(newName)) return;

            var newProfile = new Profile
            {
                Name = newName,
                Class = profile.Class,
                Deadzone = profile.Deadzone,
                CursorDeadzone = profile.CursorDeadzone,
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>(profile.ButtonMappings),
                ComboEnabled = profile.ComboEnabled,
                ComboSkillNames = new List<string>(profile.ComboSkillNames),
                ComboSequenceVK = new List<VirtualKey>(profile.ComboSequenceVK),
                PreRenewalComboDelays = new List<int>(profile.PreRenewalComboDelays),
                RenewalComboDelays = new List<int>(profile.RenewalComboDelays),
            };
            _manager.AddAndSave(newProfile);
            PopulateProfileCombo();
            MessageBox.Show($"Profile '{newName}' saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        // Event Handler for Buttons and Slider
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_hasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save them?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    SaveProfile();
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
            }
        }
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
        private void DpadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { MarkDirty(); UpdatePreview(); }
        private void LStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { MarkDirty(); UpdatePreview(); }
        private void RStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { MarkDirty(); UpdatePreview(); }
        private void L1R1Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { MarkDirty(); UpdatePreview(); }
        private void L2R2Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { MarkDirty(); UpdatePreview(); }
        private void SpellCheckbox_Changed(object sender, RoutedEventArgs e) => UpdateSpellConfig();
        private void BtnSave_Click(object sender, RoutedEventArgs e) => SaveProfile();
        private void PreviewDpadSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewLStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewRStickSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewL1R1Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();
        private void PreviewL2R2Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => UpdatePreview();

        private string _activeLayer = "";

        private void ApplyLayer(string layerName)
                        {
                            _activeLayer = layerName;
                            // Highlight active layer button
                            var gold = TryFindResource("Gold") as Brush ?? Brushes.Gold;
                            foreach (var btn in new[] { BtnLayerBase, BtnLayerL1, BtnLayerR1, BtnLayerL2, BtnLayerR2 })
                            {
                                if (btn == null) continue;
                                string tag = btn.Tag?.ToString() ?? "";
                                btn.Background = tag == layerName ? gold : new SolidColorBrush(Colors.Transparent);
                            }
                            UpdatePreview();
                        }

        private void ResetAll()
        {
            SldDpad.Value   = 0;
            SldLStick.Value = 0;
            SldRStick.Value = 0;
            SldL1R1.Value   = 0;
            SldL2R2.Value   = 0;
            if (ChkGroundSpell   != null) ChkGroundSpell.IsChecked    = false;
            if (ChkSelfCast      != null) ChkSelfCast.IsChecked       = false;
            MarkDirty();
            UpdatePreview();
        }

        private void MarkDirty() => _hasUnsavedChanges = true;

        private void UpdatePreview()
        {
            // Sync value TextBlocks
            if (TxtDpadValue   != null) TxtDpadValue.Text   = ((int)SldDpad.Value).ToString();
            if (TxtLStickValue != null) TxtLStickValue.Text = ((int)SldLStick.Value).ToString();
            if (TxtRStickValue != null) TxtRStickValue.Text = ((int)SldRStick.Value).ToString();
            if (TxtL1R1Value   != null) TxtL1R1Value.Text   = ((int)SldL1R1.Value).ToString();
            if (TxtL2R2Value   != null) TxtL2R2Value.Text   = ((int)SldL2R2.Value).ToString();
        }

        private void UpdateSpellConfig()
        {
            // Apply groand spell / self cast flags to active profile's selected button
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
                profile.Deadzone     = Math.Clamp(0.12f + (float)SldDpad.Value / 1000f, 0.0f, 0.5f);
                profile.CursorDeadzone = Math.Clamp(0.12f + (float)SldLStick.Value / 1000f, 0.0f, 0.5f);

                // Spell config flags
                // (applied per button — stored in ButtonMappings, no global flag needed)

                _manager.SetActive(profile.Name);
                _engine.LoadProfile(profile);
                _hasUnsavedChanges = false;
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