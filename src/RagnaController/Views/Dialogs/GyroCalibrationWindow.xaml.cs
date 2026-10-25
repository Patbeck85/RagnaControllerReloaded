using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using RagnaController.Core;
using RagnaController.Profiles;

namespace RagnaController.Views.Dialogs
{
    /// <summary>
    /// TASK-2401: GyroCalibrationWindow — Geführter Kalibrierungs-Assistent für den DualSense-Gyroskop.
    /// Schritte: 1. Willkommen → 2. Hardware-Erkennung → 3. Kalibrierung (Stillhalten) → 4. Ergebnis & Speichern
    /// </summary>
    public partial class GyroCalibrationWindow : Window
    {
        private readonly ProfileManager _profileManager;
        private readonly Profile _targetProfile;
        private readonly GyroService _gyroService;
        private int _currentStep = 1;
        private readonly int _totalSteps = 4;

        // Kalibrierungs-Zustand
        private bool _isCalibrating = false;
        private int _calibrationSamplesCollected = 0;
        private const int CalibrationTargetSamples = 125; // ~1 Sekunde bei 125 Hz
        private float _totalPitchDps = 0f;
        private float _totalYawDps = 0f;
        private float _maxPitchDeviation = 0f;
        private float _maxYawDeviation = 0f;
        private CancellationTokenSource? _calibrationCts;

        // Berechnete Ergebnisse
        private float _driftOffsetPitch = 0f;
        private float _driftOffsetYaw = 0f;
        private int _driftThreshold = 64;
        private float _sensitivity = 1.0f;

        public GyroCalibrationWindow(ProfileManager profileManager, Profile? targetProfile = null)
        {
            InitializeComponent();
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _targetProfile = targetProfile ?? new Profile { Name = "Neues Profil" };
            _gyroService = new GyroService();
            _gyroService.Start();

            UpdateNavigation();
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

        // ── Navigation ───────────────────────────────────────────────
        private void UpdateNavigation()
        {
            StepTitle.Text = $"Schritt {_currentStep} von {_totalSteps}";
            StepProgress.Value = _currentStep;

            StepWelcome.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            StepHardware.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
            StepCalibration.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
            StepResult.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;

            Title = $"Gyro Kalibrierung — Schritt {_currentStep} von {_totalSteps}";
        }

        // ── Schritt 1: Willkommen ────────────────────────────────────
        private void BtnWelcomeNext_Click(object sender, RoutedEventArgs e)
        {
            _currentStep = 2;
            UpdateNavigation();
            DetectHardware();
        }

        // ── Schritt 2: Hardware-Erkennung ────────────────────────────
        private async void DetectHardware()
        {
            BtnHardwareNext.IsEnabled = false;
            BtnHardwareBack.IsEnabled = false;
            GyroStatusText.Text = "Suche nach DualSense-Controller…";
            GyroStatusIndicator.Fill = Brushes.Orange;
            GyroStatusText.Foreground = Brushes.Orange;

            // Warte kurz, damit UI aktualisiert wird
            await Task.Delay(100);

            // Prüfe ob GyroService ein DualSense gefunden hat
            bool dualSenseAvailable = _gyroService.IsAvailable;

            if (dualSenseAvailable)
            {
                GyroStatusIndicator.Fill = Brushes.LimeGreen;
                GyroStatusText.Text = "DualSense-Controller erkannt — Gyroskop bereit.";
                GyroStatusText.Foreground = Brushes.LimeGreen;
                BtnHardwareNext.IsEnabled = true;
            }
            else
            {
                GyroStatusIndicator.Fill = Brushes.Orange;
                GyroStatusText.Text = "Kein DualSense erkannt — Testmodus verfügbar.";
                GyroStatusText.Foreground = Brushes.Orange;
                BtnHardwareNext.IsEnabled = true; // Kann im Testmodus fortfahren
            }

            BtnHardwareBack.IsEnabled = true;
        }

        private void BtnRefreshHardware_Click(object sender, RoutedEventArgs e)
        {
            DetectHardware();
        }

        private void BtnHardwareSkip_Click(object sender, RoutedEventArgs e)
        {
            // Testmodus – gehe direkt zur Kalibrierung mit simulierten Werten
            _currentStep = 3;
            UpdateNavigation();
            StartCalibrationSimulation();
        }

        private void BtnHardwareBack_Click(object sender, RoutedEventArgs e)
        {
            _currentStep = 1;
            UpdateNavigation();
        }

        private void BtnHardwareNext_Click(object sender, RoutedEventArgs e)
        {
            _currentStep = 3;
            UpdateNavigation();
            StartCalibration();
        }

        // ── Schritt 3: Kalibrierung ──────────────────────────────────
        private void StartCalibration()
        {
            _isCalibrating = true;
            _calibrationSamplesCollected = 0;
            _totalPitchDps = 0f;
            _totalYawDps = 0f;
            _maxPitchDeviation = 0f;
            _maxYawDeviation = 0f;

            CalibrationProgress.Value = 0;
            CalibrationStatusText.Text = "Kalibrierung läuft… Halte den Controller still.";
            CalibrationInstruction.Text = "Controller stillhalten – Kalibrierung läuft.";
            BtnStartCalibration.IsEnabled = false;

            _calibrationCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_calibrationSamplesCollected < CalibrationTargetSamples && !_calibrationCts.Token.IsCancellationRequested)
                    {
                        float pitch = _gyroService.PitchDps;
                        float yaw = _gyroService.YawDps;

                        _totalPitchDps += pitch;
                        _totalYawDps += yaw;

                        float pitchDev = Math.Abs(pitch);
                        float yawDev = Math.Abs(yaw);
                        if (pitchDev > _maxPitchDeviation) _maxPitchDeviation = pitchDev;
                        if (yawDev > _maxYawDeviation) _maxYawDeviation = yawDev;

                        _calibrationSamplesCollected++;

                        // UI-Update im UI-Thread
                        Dispatcher.Invoke(() =>
                        {
                            CalibrationProgress.Value = (double)_calibrationSamplesCollected / CalibrationTargetSamples * 100.0;
                            TxtPitchValue.Text = $"{pitch:F2}°";
                            TxtYawValue.Text = $"{yaw:F2}°";
                            CalibrationStatusText.Text = $"Probe {_calibrationSamplesCollected} / {CalibrationTargetSamples}";
                        });

                        await Task.Delay(8); // ~125 Hz
                    }

                    // Berechne Ergebnisse
                    _driftOffsetPitch = _calibrationSamplesCollected > 0 ? _totalPitchDps / _calibrationSamplesCollected : 0f;
                    _driftOffsetYaw = _calibrationSamplesCollected > 0 ? _totalYawDps / _calibrationSamplesCollected : 0f;

                    // Drift-Schwelle: 2x die maximale Abweichung vom Mittelwert, mindestens 32
                    float maxDev = Math.Max(_maxPitchDeviation, _maxYawDeviation);
                    _driftThreshold = Math.Max(32, (int)(maxDev * 2.0f + 10));

                    // Sensitivität: Standard 1.0, kann später feinjustiert werden
                    _sensitivity = 1.0f;

                    Dispatcher.Invoke(() =>
                    {
                        _isCalibrating = false;
                        ShowResults();
                    });
                }
                catch (OperationCanceledException)
                {
                    // Abgebrochen – nicht weiter tun
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _isCalibrating = false;
                        CalibrationStatusText.Text = $"Fehler: {ex.Message}";
                        CalibrationStatusText.Foreground = Brushes.OrangeRed;
                        BtnStartCalibration.IsEnabled = true;
                    });
                }
            }, _calibrationCts.Token);
        }

        private void StartCalibrationSimulation()
        {
            // Simuliere Kalibrierung ohne echten Hardware-Controller
            _isCalibrating = true;
            _calibrationSamplesCollected = 0;
            _totalPitchDps = 0f;
            _totalYawDps = 0f;
            _maxPitchDeviation = 0f;
            _maxYawDeviation = 0f;

            CalibrationProgress.Value = 0;
            CalibrationStatusText.Text = "Testmodus – Simuliere Kalibrierung…";
            CalibrationInstruction.Text = "Simuliere Stillhalten (Testmodus ohne echten Controller).";
            BtnStartCalibration.IsEnabled = false;

            var random = new Random();
            _calibrationCts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    while (_calibrationSamplesCollected < CalibrationTargetSamples && !_calibrationCts.Token.IsCancellationRequested)
                    {
                        // Simuliere kleine Drift-Werte um 0
                        float pitch = (float)(random.NextDouble() * 20 - 10); // -10 bis +10
                        float yaw = (float)(random.NextDouble() * 20 - 10);

                        _totalPitchDps += pitch;
                        _totalYawDps += yaw;

                        float pitchDev = Math.Abs(pitch);
                        float yawDev = Math.Abs(yaw);
                        if (pitchDev > _maxPitchDeviation) _maxPitchDeviation = pitchDev;
                        if (yawDev > _maxYawDeviation) _maxYawDeviation = yawDev;

                        _calibrationSamplesCollected++;

                        Dispatcher.Invoke(() =>
                        {
                            CalibrationProgress.Value = (double)_calibrationSamplesCollected / CalibrationTargetSamples * 100.0;
                            TxtPitchValue.Text = $"{pitch:F2}°";
                            TxtYawValue.Text = $"{yaw:F2}°";
                            CalibrationStatusText.Text = $"Simulierte Probe {_calibrationSamplesCollected} / {CalibrationTargetSamples}";
                        });

                        await Task.Delay(8);
                    }

                    _driftOffsetPitch = _calibrationSamplesCollected > 0 ? _totalPitchDps / _calibrationSamplesCollected : 0f;
                    _driftOffsetYaw = _calibrationSamplesCollected > 0 ? _totalYawDps / _calibrationSamplesCollected : 0f;

                    float maxDev = Math.Max(_maxPitchDeviation, _maxYawDeviation);
                    _driftThreshold = Math.Max(32, (int)(maxDev * 2.0f + 10));
                    _sensitivity = 1.0f;

                    Dispatcher.Invoke(() =>
                    {
                        _isCalibrating = false;
                        ShowResults();
                    });
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _isCalibrating = false;
                        CalibrationStatusText.Text = $"Fehler: {ex.Message}";
                        CalibrationStatusText.Foreground = Brushes.OrangeRed;
                        BtnStartCalibration.IsEnabled = true;
                    });
                }
            }, _calibrationCts.Token);
        }

        private void BtnStartCalibration_Click(object sender, RoutedEventArgs e)
        {
            StartCalibration();
        }

        private void BtnCalibrationBack_Click(object sender, RoutedEventArgs e)
        {
            _calibrationCts?.Cancel();
            _currentStep = 2;
            UpdateNavigation();
        }

        private void ShowResults()
        {
            _currentStep = 4;
            UpdateNavigation();

            // Wende Werte auf das temporäre Profil-Objekt an (wird erst beim Fertigstellen gespeichert)
            _targetProfile.LightbarColor = "#005AD2"; // Behält Standard

            TxtResultPitchOffset.Text = $"{_driftOffsetPitch:F2}°";
            TxtResultYawOffset.Text = $"{_driftOffsetYaw:F2}°";
            TxtResultThreshold.Text = _driftThreshold.ToString();
            TxtResultSensitivity.Text = $"{_sensitivity:F2}";

            BtnResultFinish.IsEnabled = true;
            CalibrationStatusText.Text = "Kalibrierung erfolgreich abgeschlossen.";
            CalibrationStatusText.Foreground = Brushes.LimeGreen;
        }

        // ── Schritt 4: Ergebnis ──────────────────────────────────────
        private void BtnResultBack_Click(object sender, RoutedEventArgs e)
        {
            _currentStep = 3;
            UpdateNavigation();
        }

        private async void BtnResultFinish_Click(object sender, RoutedEventArgs e)
        {
            // Wende Kalibrierungswerte auf das Zielprofil an
            var profile = _targetProfile;

            // Speichere Kalibrierungswerte als Tags oder in einem separaten Mechanismus
            // Da Profile keine dedizierten Gyro-Kalibrierungsfelder hat, speichern wir sie als Tags
            profile.Tags.Clear();
            profile.Tags.Add($"gyro_offset_pitch={_driftOffsetPitch:F4}");
            profile.Tags.Add($"gyro_offset_yaw={_driftOffsetYaw:F4}");
            profile.Tags.Add($"gyro_threshold={_driftThreshold}");
            profile.Tags.Add($"gyro_sensitivity={_sensitivity:F4}");
            profile.Tags.Add($"gyro_calibrated={DateTime.Now:yyyy-MM-dd HH:mm}");

            // Aktiviere Gyro im Profil
            profile.GyroEnabled = true;

            // Speichere Profil
                        try
                        {
                            _profileManager.AddAndSave(profile);
                        }
                        catch
                        {
                            _profileManager.AddAndSave(profile);
                        }

            DialogResult = true;
            Close();
        }

        // ── Cleanup ──────────────────────────────────────────────────
        protected override void OnClosed(EventArgs e)
        {
            _calibrationCts?.Cancel();
            _calibrationCts?.Dispose();
            _gyroService?.Stop();
            _gyroService?.Dispose();
            base.OnClosed(e);
        }
    }
}
