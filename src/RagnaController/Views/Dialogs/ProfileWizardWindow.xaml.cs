using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Models;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
/// ProfileWizardWindow — Guided profile creation wizard (FEAT-010).
/// Walks the user through class selection, skill mapping, and auto-detection.
/// </summary>
public partial class ProfileWizardWindow : Window
    {
        private int _step = 1;
        private readonly ProfileManager _profileManager;
        private readonly ProfileTemplateManager _templateManager;
        public Profile? CreatedProfile { get; private set; }
        private ProfileTemplate? _loadedTemplate;

        public ProfileWizardWindow(ProfileManager profileManager)
        {
            InitializeComponent();
            _profileManager = profileManager;
            _templateManager = new ProfileTemplateManager();
            PopulateCombos();
            UpdateUI();
        }

        private void PopulateCombos()
        {
            var keys = new[] { VirtualKey.Z, VirtualKey.X, VirtualKey.C, VirtualKey.V, VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.F, VirtualKey.Q, VirtualKey.W, VirtualKey.E, VirtualKey.R, VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4 };
            foreach (var combo in new[] { AButtonCombo, BButtonCombo, XButtonCombo, YButtonCombo })
            {
                if (combo == null) continue;
                foreach (var k in keys) combo.Items.Add(new ComboBoxItem { Content = k.ToString(), Tag = k });
                combo.SelectedIndex = 0;
            }
        }

        private void UpdateUI()
        {
            if (Step1Panel == null) return;

            Step1Panel.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
            Step2Panel.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
            Step3Panel.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
            Step4Panel.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;

            BtnBack.IsEnabled = _step > 1;
            BtnNext.Content = _step == 4 ? "CREATE" : "NEXT →";

            // Validate current step before enabling Next
            bool canProceed = ValidateCurrentStep();
            BtnNext.IsEnabled = canProceed;

            Brush gold = (Brush)FindResource("Gold");
            Brush dim = (Brush)FindResource("BgBorder");

            if (Step1Dot != null) Step1Dot.Fill = _step >= 1 ? gold : dim;
            if (Step2Dot != null) Step2Dot.Fill = _step >= 2 ? gold : dim;
            if (Step3Dot != null) Step3Dot.Fill = _step >= 3 ? gold : dim;
            if (Step4Dot != null) Step4Dot.Fill = _step >= 4 ? gold : dim;

            if (_step == 4)
            {
                ReviewName.Text = ProfileNameText.Text.ToUpper();
                var selectedItem = ClassCombo.SelectedItem as ComboBoxItem;
                string className = selectedItem?.Content?.ToString() ?? "Unknown";
                ReviewClass.Text = "CLASS: " + className.ToUpper();

                // Engines
                var engines = new List<string>();
                if (EnableMeleeCheck.IsChecked == true) engines.Add("Auto-Target");
                if (EnableKiteCheck.IsChecked == true) engines.Add("Kite Engine");
                if (EnableMageCheck.IsChecked == true) engines.Add("Mage System");
                if (EnableSupportCheck.IsChecked == true) engines.Add("Support Mode");
                ReviewEngines.Text = engines.Count > 0 ? "Engines: " + string.Join(", ", engines) : "Engines: None";

                // Buttons
                var btns = new List<string>();
                if (AButtonCombo.SelectedItem is ComboBoxItem a) btns.Add($"A → {a.Tag}");
                if (BButtonCombo.SelectedItem is ComboBoxItem b) btns.Add($"B → {b.Tag}");
                if (XButtonCombo.SelectedItem is ComboBoxItem x) btns.Add($"X → {x.Tag}");
                if (YButtonCombo.SelectedItem is ComboBoxItem y) btns.Add($"Y → {y.Tag}");
                ReviewButtons.Text = "Quick Keys: " + string.Join(" | ", btns);

                // Template info
                ReviewTemplateInfo.Text = _loadedTemplate != null ? $"Template: {_loadedTemplate.DisplayName} ({_loadedTemplate.Class})" : "No template loaded";
            }
        }

        private bool ValidateCurrentStep()
        {
            switch (_step)
            {
                case 1:
                    // Step 1: Name and Class required
                    return !string.IsNullOrWhiteSpace(ProfileNameText.Text) && ClassCombo.SelectedIndex >= 0;
                case 2:
                    // Step 2: Always valid (optional checkboxes)
                    return true;
                case 3:
                    // Step 3: All four key combos must have selection
                    return AButtonCombo.SelectedIndex >= 0 && 
                           BButtonCombo.SelectedIndex >= 0 && 
                           XButtonCombo.SelectedIndex >= 0 && 
                           YButtonCombo.SelectedIndex >= 0;
                case 4:
                    // Step 4: Always valid (review step)
                    return true;
                default:
                    return false;
            }
        }

        private void ValidateField(TextBox textBox, bool isValid)
        {
            textBox.BorderBrush = isValid ? (Brush)FindResource("BgBorder") : Brushes.Red;
            textBox.ToolTip = isValid ? null : "This field is required";
        }

        private void ValidateCombo(ComboBox combo, bool isValid)
        {
            combo.BorderBrush = isValid ? (Brush)FindResource("BgBorder") : Brushes.Red;
            combo.ToolTip = isValid ? null : "Please select a value";
        }

        private void ProfileNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            bool valid = !string.IsNullOrWhiteSpace(tb.Text);
            ValidateField(tb, valid);
            UpdateUI();
        }

        private void ClassCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnableMeleeCheck == null) return;
            string tag = (ClassCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "";
            EnableMeleeCheck.IsChecked = tag == "Melee";
            EnableKiteCheck.IsChecked = tag == "Ranged";
            EnableMageCheck.IsChecked = tag == "Mage";
            EnableSupportCheck.IsChecked = tag == "Support";
            
            var combo = (ComboBox)sender;
            bool valid = combo.SelectedIndex >= 0;
            ValidateCombo(combo, valid);
            UpdateUI();
        }

        private void ButtonCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var combo = (ComboBox)sender;
            bool valid = combo.SelectedIndex >= 0;
            ValidateCombo(combo, valid);
            UpdateUI();
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateCurrentStep()) return;
            
            if (_step < 4) { _step++; UpdateUI(); }
            else Finish();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_step > 1) { _step--; UpdateUI(); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Finish()
        {
            string name = ProfileNameText.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Please enter a profile name.", "Name required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Strip characters illegal in Windows file names (SafeName logic)
            string safeName = string.Concat(name.Split(Path.GetInvalidFileNameChars())).Trim();
            if (string.IsNullOrWhiteSpace(safeName))
            {
                MessageBox.Show("Profile name contains only invalid characters.\\nUse letters, numbers or spaces.", "Invalid Name", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            CreatedProfile = new Profile
            {
                Name = safeName,
                Class = (ClassCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Melee",
                AutoAttackEnabled = EnableMeleeCheck.IsChecked == true,
                KiteEnabled = EnableKiteCheck.IsChecked == true,
                MageEnabled = EnableMageCheck.IsChecked == true,
                SupportEnabled = EnableSupportCheck.IsChecked == true,
                ButtonMappings = new Dictionary<ButtonKey, ButtonAction>
                {
                    { VirtualKey.A, new ButtonAction { Type = ActionType.Key, Key = AButtonCombo.SelectedItem is ComboBoxItem a ? (VirtualKey)a.Tag : VirtualKey.None, Label = "Quick Key A" } },
                    { VirtualKey.B, new ButtonAction { Type = ActionType.Key, Key = BButtonCombo.SelectedItem is ComboBoxItem b ? (VirtualKey)b.Tag : VirtualKey.None, Label = "Quick Key B" } },
                    { VirtualKey.X, new ButtonAction { Type = ActionType.Key, Key = XButtonCombo.SelectedItem is ComboBoxItem x ? (VirtualKey)x.Tag : VirtualKey.None, Label = "Quick Key X" } },
                    { VirtualKey.Y, new ButtonAction { Type = ActionType.Key, Key = YButtonCombo.SelectedItem is ComboBoxItem y ? (VirtualKey)y.Tag : VirtualKey.None, Label = "Quick Key Y" } }
                }
            };

            // Save profile via ProfileManager
            _profileManager.AddAndSave(CreatedProfile);

            // Also ensure it's in the profiles list and set as active
            _profileManager.SetActive(safeName);

            DialogResult = true;
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        // TASK-2005: Load template
        private void BtnLoadTemplate_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new TemplateSelectWindow(_templateManager);
            if (dlg.ShowDialog() == true && dlg.SelectedTemplate != null)
            {
                _loadedTemplate = dlg.SelectedTemplate;

                // Apply template engines to checkboxes
                EnableMeleeCheck.IsChecked = _loadedTemplate.Engines.PreRenewal.AutoAttackEnabled;
                EnableKiteCheck.IsChecked = _loadedTemplate.Engines.PreRenewal.KiteEnabled;
                EnableMageCheck.IsChecked = _loadedTemplate.Engines.PreRenewal.MageEnabled;
                EnableSupportCheck.IsChecked = _loadedTemplate.Engines.PreRenewal.SupportEnabled;

                // Apply template button mappings
                if (_loadedTemplate.ButtonMappings != null)
                {
                    foreach (var kvp in _loadedTemplate.ButtonMappings)
                    {
                        var targetCombo = kvp.Key switch
                        {
                            "A" => AButtonCombo,
                            "B" => BButtonCombo,
                            "X" => XButtonCombo,
                            "Y" => YButtonCombo,
                            _ => null
                        };
                        if (targetCombo != null)
                        {
                            foreach (ComboBoxItem item in targetCombo.Items)
                            {
                                if ((VirtualKey)item.Tag == kvp.Value)
                                {
                                    targetCombo.SelectedItem = item;
                                    break;
                                }
                            }
                        }
                    }
                }

                MessageBox.Show($"Template '{_loadedTemplate.DisplayName}' loaded successfully!", "Template Loaded", MessageBoxButton.OK, MessageBoxImage.Information);
                UpdateUI();
            }
        }
    }

    /// <summary>
    /// Simple template selection dialog for the ProfileWizard "Load Template" feature.
    /// </summary>
    public class TemplateSelectWindow : Window
    {
        public ProfileTemplate? SelectedTemplate { get; private set; }

        public TemplateSelectWindow(ProfileTemplateManager templateManager)
        {
            Title = "Load Profile Template";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            WindowStyle = WindowStyle.SingleBorderWindow;
            Background = (Brush)new BrushConverter().ConvertFrom("#1E1E1E")!;

            var grid = new Grid { Margin = new Thickness(16) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var lbl = new TextBlock { Text = "Select a template to load:", FontSize = 14, Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 8) };
            grid.Children.Add(lbl);

            var list = new ListBox { FontSize = 13, Background = Brushes.Transparent, Foreground = Brushes.White, BorderBrush = Brushes.Gray, BorderThickness = new Thickness(1) };
            foreach (var kvp in templateManager.Templates)
            {
                var t = kvp.Value;
                list.Items.Add(new ComboBoxItem { Content = $"{t.DisplayName} — {t.Class}", Tag = t });
            }
            if (list.Items.Count > 0) list.SelectedIndex = 0;
            grid.Children.Add(list);
            Grid.SetRow(list, 1);

            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var btnCancel = new Button { Content = "Cancel", Width = 80, Height = 32, Margin = new Thickness(0, 0, 8, 0) };
            btnCancel.Click += (s, _) => { DialogResult = false; Close(); };
            var btnOk = new Button { Content = "Load", Width = 80, Height = 32, Background = (Brush)new BrushConverter().ConvertFrom("#4a90e2")!, BorderBrush = (Brush)new BrushConverter().ConvertFrom("#4a90e2")!, Foreground = Brushes.White };
            btnOk.Click += (s, _) =>
            {
                if (list.SelectedItem is ComboBoxItem item && item.Tag is ProfileTemplate t)
                {
                    SelectedTemplate = t;
                    DialogResult = true;
                    Close();
                }
            };
            btnPanel.Children.Add(btnCancel);
            btnPanel.Children.Add(btnOk);
            grid.Children.Add(btnPanel);
            Grid.SetRow(btnPanel, 2);

            Content = grid;
        }
    }
}