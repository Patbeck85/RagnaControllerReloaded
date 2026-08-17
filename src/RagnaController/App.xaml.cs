using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using RagnaController.Core;
using RagnaController.Profiles;
using RagnaController.Models;

namespace RagnaController
{
    /// <summary>
    /// App.xaml.cs v2.0.0 - Main application entry point
    /// </summary>
    public partial class App : Application
    {
        private static readonly string CrashLog = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "crash.log");

        protected override void OnStartup(StartupEventArgs e)
        {
            // ── Alle Exception-Handler so früh wie möglich registrieren ──────
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                LogFatal("UnhandledException", ex.ExceptionObject?.ToString() ?? "unknown");
                MessageBox.Show($"Fataler Fehler:\n{ex.ExceptionObject?.GetType().Name}\n\nDetails: {CrashLog}",
                    "RagnaController – Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                LogFatal("DispatcherUnhandledException", ex.Exception?.ToString() ?? "unknown");
                MessageBox.Show($"UI-Fehler:\n{ex.Exception?.Message}\n\nDetails: {CrashLog}",
                    "RagnaController – Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                LogFatal("UnobservedTaskException", ev.Exception?.ToString() ?? "unknown");
                ev.SetObserved();
            };

            // Nuclear exit handler — if WPF gets stuck with no windows, kill the process
            Application.Current.Exit += (s, ev) => Environment.Exit(0);

            base.OnStartup(e);
            StartWorkflow();
        }

        private async void StartWorkflow()
        {
            try
            {
                // Splash anzeigen
                var splash = new SplashWindow();
                MainWindow = splash;
                splash.Show();

                // DI-Warmup + Profil-Laden PARALLEL zur Splash-Animation
                var warmupTask = Task.Run(() =>
                {
                    _ = Models.Settings.Load();
                });

                // Splash dauert min. 1s, dann Voice, dann max. weitere 2.5s
                // — per Klick sofort überspringbar
                var skipToken = splash.SkipCts.Token;
                await Task.Delay(1000).ConfigureAwait(true);
                splash.PlayVoice();
                try { await Task.WhenAll(warmupTask, Task.Delay(2500, skipToken)); }
                catch (TaskCanceledException) { } // Klick = Splash überspringen

                // Settings laden
                var settings = Models.Settings.Load();

                // i18n: Sprache initialisieren
                LocalizationManager.Instance.CurrentLanguage = settings.AppLanguage;

                // Handheld-Erkennung im Hintergrund
                bool isHandheld = await Task.Run(() => Core.HandheldDetector.IsHandheldDevice())
                                  || settings.ForceHandheldMode;

                // ── Engine + Manager im Hintergrund erstellen ────────────────
                // WICHTIG: HybridEngine erstellt ControllerService (SDL Init) —
                // das MUSS im Background-Thread passieren, sonst hängt der UI-Thread!
                HybridEngine engine = null!;
                ProfileManager manager = null!;
                await Task.Run(() =>
                {
#pragma warning disable CS8625
                    engine  = new HybridEngine(new BackgroundTickProvider(8), new Messenger(), null, null);
#pragma warning restore CS8625
                    manager = new ProfileManager();
                });

                var viewModel = new MainViewModel();

                Window main;
                if (isHandheld)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[App] Handheld erkannt: {Core.HandheldDetector.DeviceName} → HandheldWindow");
                    main = new HandheldWindow(engine, manager);
                }
                else
                {
                    main = new MainWindow(engine, manager, viewModel);
                }
                MainWindow = main;

                // ── Silent Mode ──────────────────────────────────────────────
                if (settings.StartMinimized || settings.StartWithWindows)
                {
                    main.Show();
                }
                else
                {
                    main.Opacity = 0;
                    main.Show();

                    await Task.Delay(200);
                    splash.FadeAndClose(600);

                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600));
                    main.BeginAnimation(Window.OpacityProperty, anim);
                }

                // ── Telemetry Opt-In (einmalig beim ersten Start) ────────────
                if (!settings.HasAskedForTelemetry)
                {
                    var result = MessageBox.Show(
                        "To help improve RagnaController, the app can send anonymous crash reports and basic usage statistics to the developer. No personal data is collected.\n\nDo you want to enable anonymous telemetry?",
                        "Help Improve RagnaController",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    settings.EnableTelemetry    = (result == MessageBoxResult.Yes);
                    settings.HasAskedForTelemetry = true;
                    settings.Save();
                }

                TelemetryService.Initialize(settings);
                TelemetryService.SendAppStartPing();
            }
            catch (Exception ex)
            {
                LogFatal("StartWorkflow", ex.ToString());
                MessageBox.Show($"Fehler beim Start:\n{ex.Message}\n\nDetails: {CrashLog}",
                    "RagnaController – Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static void LogFatal(string source, string msg)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CrashLog)!);
                File.AppendAllText(CrashLog,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}]\n{msg}\n\n");
            }
            catch { }
        }
    }
}
