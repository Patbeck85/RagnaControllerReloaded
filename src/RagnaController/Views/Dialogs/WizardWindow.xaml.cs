using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using RagnaController.Core;
using RagnaController.Core.Services;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-1631: WizardWindow — Geführter First-Run Wizard.
    /// Führt den neuen Nutzer durch 4 Schritte:
    /// 1. Willkommen (Übersicht)
    /// 2. RO-Client-Pfad auswählen (ragexe.exe / sakexe.exe)
    /// 3. Controller-Erkennung (XInput / DualSense)
    /// 4. Erstes Profil erstellen
    /// </summary>
    public partial class WizardWindow : Window
    {
        private int _currentStep = 1;
        private readonly FirstRunService _firstRunService;
        private readonly ProfileManager _profileManager;
        private readonly ProfileTemplateManager _templateManager;
        private bool _controllerDetected;
        private ProfileTemplate? _selectedTemplate;
        private readonly Dictionary<string, VirtualKey> _customButtonMappings = new();

        public WizardWindow(FirstRunService firstRunService, ProfileManager profileManager)
        {
            InitializeComponent();
            _firstRunService = firstRunService ?? throw new ArgumentNullException(nameof(firstRunService));
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _templateManager = new ProfileTemplateManager();
            UpdateNavigation();
            DetectControllers();
        }

        // ── Schritt-Navigation ──────────────────────────────────────────

        private void UpdateNavigation()
        {
            StepWelcome.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            StepRoPath.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            StepController.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            StepProfile.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            BtnBack.IsEnabled = _currentStep > 1;
            BtnNext.Visibility = _currentStep < 4 ? Visibility.Visible : Visibility.Collapsed;
            BtnFinish.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
            BtnFinish.IsEnabled = false;

            // Populate class template combo when entering step 4
            if (_currentStep == 4)
                PopulateTemplateCombo();

            Title = $"RagnaController — First-Run Wizard (Schritt {_currentStep} von 4)";
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateNavigation();
            }
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < 4)
            {
                _currentStep++;
                UpdateNavigation();

                // Controller-Erkennung neu ausführen beim Betreten von Schritt 3
                if (_currentStep == 3)
                    DetectControllers();
            }
        }

        private void BtnFinish_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // ── Schritt 2: RO-Client-Pfad ───────────────────────────────────

        private void RoExePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var path = RoExePathTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(path))
            {
                RoPathStatus.Text = "";
                BtnNext.IsEnabled = false;
                return;
            }

            if (FirstRunService.IsValidRoClient(path))
            {
                RoPathStatus.Text = "✓ Gültiger RO-Client erkannt";
                RoPathStatus.Foreground = System.Windows.Media.Brushes.Green;
                BtnNext.IsEnabled = true;
            }
            else
            {
                RoPathStatus.Text = "✗ Dies ist keine gültige RO-Client-Executable (ragexe.exe / sakexe.exe)";
                RoPathStatus.Foreground = System.Windows.Media.Brushes.Red;
                BtnNext.IsEnabled = false;
            }
        }

        private void BtnBrowseRoExe_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "RO-Client-Executable auswählen",
                Filter = "RO Client (*.exe)|ragexe.exe;sakexe.exe|Alle Dateien (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                RoExePathTextBox.Text = dlg.FileName;
            }
        }

        // ── Schritt 3: Controller-Erkennung ─────────────────────────────

        private void DetectControllers()
        {
            try
            {
                // Prüfe über SDL2 / XInput ob Controller verbunden sind
                using var controllerService = new ControllerService();
                bool detected = false;
                controllerService.ControllerDetected += (_, __) => detected = true;
                controllerService.ControllerDisconnected += (_, __) => { };

                // Kurze Wartezeit für Event-Feuerung (Controller-Scan)
                System.Threading.Thread.Sleep(200);

                _controllerDetected = detected;

                ControllerStatusIndicator.Fill = detected
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Orange;
                ControllerStatusText.Text = detected
                    ? "Controller erkannt — bereit für Gamepad-Steuerung"
                    : "Kein Controller erkannt — du kannst trotzdem fortfahren (Tastatur/Maus verfügbar)";
                ControllerStatusText.Foreground = detected
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Orange;

                BtnNext.IsEnabled = true;
            }
            catch
            {
                // Fehler bei der Erkennung ist nicht kritisch — einfach fortfahren
                _controllerDetected = false;
                ControllerStatusIndicator.Fill = System.Windows.Media.Brushes.Orange;
                ControllerStatusText.Text = "Controller-Erkennung nicht verfügbar — fortfahren möglich";
                ControllerStatusText.Foreground = System.Windows.Media.Brushes.Orange;
                BtnNext.IsEnabled = true;
            }
        }

        private void BtnRefreshController_Click(object sender, RoutedEventArgs e)
        {
            DetectControllers();
        }

        private void BtnSkipController_Click(object sender, RoutedEventArgs e)
        {
            BtnNext.IsEnabled = true;
            BtnNext_Click(sender, e);
        }

        // ── Schritt 4: Template-Auswahl ─────────────────────────────────
        private void PopulateTemplateCombo()
        {
            ClassTemplateCombo.Items.Clear();
            foreach (var kvp in _templateManager.Templates)
            {
                var t = kvp.Value;
                ClassTemplateCombo.Items.Add(new ComboBoxItem
                {
                    Content = $"{t.DisplayName} — {t.Class}",
                    Tag = t.Id
                });
            }
            ClassTemplateCombo.SelectedIndex = 0;
        }

        private void ClassTemplateCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = ClassTemplateCombo.SelectedItem as ComboBoxItem;
            if (item == null) return;

            var templateId = item.Tag?.ToString() ?? "novice";
            _selectedTemplate = _templateManager.GetTemplate(templateId);
            if (_selectedTemplate == null) return;

            TemplateDescription.Text = _selectedTemplate.Description;
            TemplateSkills.Text = "Recommended skills: " + string.Join(", ", _selectedTemplate.SkillRecommendations);

            // Load button mappings from template into custom mappings
            _customButtonMappings.Clear();
            foreach (var kvp in _selectedTemplate.ButtonMappings)
                _customButtonMappings[kvp.Key] = kvp.Value;

            // Populate button mapping combo boxes
            PopulateButtonMapCombo(BtnAMap, VirtualKey.A);
            PopulateButtonMapCombo(BtnBMap, VirtualKey.B);
            PopulateButtonMapCombo(BtnXMap, VirtualKey.X);
            PopulateButtonMapCombo(BtnYMap, VirtualKey.Y);

            UpdateCreateButtonState();
        }

        private void PopulateButtonMapCombo(ComboBox combo, VirtualKey controllerButton)
        {
            combo.Items.Clear();
            var availableKeys = new[]
            {
                VirtualKey.Z, VirtualKey.X, VirtualKey.C, VirtualKey.V,
                VirtualKey.A, VirtualKey.S, VirtualKey.D, VirtualKey.F,
                VirtualKey.Q, VirtualKey.W, VirtualKey.E, VirtualKey.R,
                VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4
            };

            foreach (var key in availableKeys)
            {
                combo.Items.Add(new ComboBoxItem { Content = key.ToString(), Tag = key });
            }

            // Select current mapping
            if (_customButtonMappings.TryGetValue(controllerButton.ToString(), out var mappedKey))
            {
                foreach (ComboBoxItem item in combo.Items)
                {
                    if ((VirtualKey)item.Tag == mappedKey)
                    {
                        combo.SelectedItem = item;
                        break;
                    }
                }
            }
            if (combo.SelectedItem == null)
                combo.SelectedIndex = 0;
        }

        private void ButtonMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_selectedTemplate == null) return;

            var combo = sender as ComboBox;
            if (combo?.SelectedItem is not ComboBoxItem item) return;

            var mappedKey = (VirtualKey)item.Tag;

            // Determine which controller button this combo represents
            string controllerButton;
            if (combo == BtnAMap) controllerButton = VirtualKey.A.ToString();
            else if (combo == BtnBMap) controllerButton = VirtualKey.B.ToString();
            else if (combo == BtnXMap) controllerButton = VirtualKey.X.ToString();
            else if (combo == BtnYMap) controllerButton = VirtualKey.Y.ToString();
            else return;

            _customButtonMappings[controllerButton] = mappedKey;
        }

        private void ProfileNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCreateButtonState();
        }

        private void UpdateCreateButtonState()
        {
            bool hasName = !string.IsNullOrWhiteSpace(ProfileNameTextBox.Text?.Trim());
            bool hasTemplate = ClassTemplateCombo.SelectedIndex >= 0;
            BtnCreateProfile.IsEnabled = hasName && hasTemplate;
        }

        // ── Schritt 4: Erstes Profil ────────────────────────────────────

        private void BtnCreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileName = ProfileNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(profileName))
            {
                ProfileStatus.Text = "Please enter a profile name";
                return;
            }

            try
            {
                var item = ClassTemplateCombo.SelectedItem as ComboBoxItem;
                var templateId = item?.Tag?.ToString() ?? "novice";

                var profile = _templateManager.CreateProfileFromTemplate(templateId, profileName);

                // Apply customized button mappings
                profile.ButtonMappings.Clear();
                foreach (var kvp in _customButtonMappings)
                {
                    var buttonKey = ButtonKey.Parse(kvp.Key);
                    if (buttonKey.Key != VirtualKey.None)
                    {
                        profile.ButtonMappings[buttonKey] = new ButtonAction
                        {
                            Type = ActionType.Key,
                            Key = kvp.Value,
                            Label = $"Quick Key {kvp.Key}"
                        };
                    }
                }

                _profileManager.Profiles.Add(profile);
                _profileManager.SetActive(profile.Name);

                ProfileStatus.Text = $"✓ Profile '{profileName}' created ({profile.Class})";
                ProfileStatus.Foreground = System.Windows.Media.Brushes.Green;
                BtnFinish.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ProfileStatus.Text = $"✗ Error creating profile: {ex.Message}";
                ProfileStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        // ── Ergebnis ────────────────────────────────────────────────────

        /// <summary>
        /// Gibt den ausgewählten RO-Client-Pfad zurück (nach Fertigstellen).
        /// </summary>
        public string? SelectedRoExePath => RoExePathTextBox.Text?.Trim();

        /// <summary>
        /// Gibt zurück, ob ein Controller erkannt wurde.
        /// </summary>
        public bool ControllerDetected => _controllerDetected;
    }
}
