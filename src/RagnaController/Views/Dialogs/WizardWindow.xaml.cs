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
        private bool _controllerDetected;

        public WizardWindow(FirstRunService firstRunService, ProfileManager profileManager)
        {
            InitializeComponent();
            _firstRunService = firstRunService ?? throw new ArgumentNullException(nameof(firstRunService));
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
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

        // ── Schritt 4: Erstes Profil ────────────────────────────────────

        private void BtnCreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var profileName = ProfileNameTextBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(profileName))
            {
                ProfileStatus.Text = "Bitte einen Profilnamen eingeben";
                return;
            }

            try
            {
                var profile = new Profile
                {
                    Name = profileName,
                    Class = "Novice",
                    IsEnabled = true
                };

                _profileManager.Profiles.Add(profile);
                _profileManager.SetActive(profile.Name);

                ProfileStatus.Text = $"✓ Profil '{profileName}' erstellt";
                ProfileStatus.Foreground = System.Windows.Media.Brushes.Green;
                BtnFinish.IsEnabled = true;
            }
            catch (Exception ex)
            {
                ProfileStatus.Text = $"✗ Fehler beim Erstellen: {ex.Message}";
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
