using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using RagnaController.Core;
using RagnaController.Core.Services.FirstRun;
using RagnaController.Core.Services.Update;
using RagnaController.Profiles;
using RagnaController.Core.Services.ProfileManagement;
using RagnaController.Presentation.Services;
using RagnaController.Models;
using RagnaController.Views.Windows;
using RagnaController.Views.Overlays;
using RagnaController.Views.Dialogs;

namespace RagnaController
{
    public partial class App : Application
    {
        private static readonly string CrashLog = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "crash.log");

        public static ToastService? ToastService { get; private set; }

        public static void InitializeToastService(ToastService toastService)
        {
            ToastService = toastService;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                LogFatal("UnhandledException", ex.ExceptionObject?.ToString() ?? "unknown");
                var settings = Models.Settings.Load();
                if (settings.AutoRestartOnCrash)
                {
                    settings.Save();
                    MessageBox.Show("Fatal error - App will auto-restart.\n\nDetails: " + CrashLog,
                        "RagnaController - Auto-Restart", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RestartApp();
                    return;
                }
                MessageBox.Show(ex.ExceptionObject?.GetType().Name + "\n\nDetails: " + CrashLog,
                    "RagnaController - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                LogFatal("DispatcherUnhandledException", ex.Exception?.ToString() ?? "unknown");
                var settings = Models.Settings.Load();
                if (settings.AutoRestartOnCrash)
                {
                    settings.Save();
                    MessageBox.Show("UI error - App will auto-restart.\n\nDetails: " + CrashLog,
                        "RagnaController - Auto-Restart", MessageBoxButton.OK, MessageBoxImage.Warning);
                    RestartApp();
                    return;
                }
                MessageBox.Show(ex.Exception?.Message + "\n\nDetails: " + CrashLog,
                    "RagnaController - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                ex.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, ev) =>
            {
                LogFatal("UnobservedTaskException", ev.Exception?.ToString() ?? "unknown");
                ev.SetObserved();
            };

            Application.Current.Exit += (s, ev) =>
            {
                CommunityBrowserWindow.DisposeRegistryClient();
                Environment.Exit(0);
            };

            base.OnStartup(e);
            _ = StartWorkflowAsync();
        }

        private async Task StartWorkflowAsync()
        {
            try
            {
                var settings = Models.Settings.Load();

                var firstRunService = new FirstRunService(settings);
                ProfileManager? preCreatedProfileManager = null;

                if (firstRunService.IsFirstRun)
                {
                    preCreatedProfileManager = new ProfileManager();

                    var wizard = new WizardWindow(firstRunService, preCreatedProfileManager);
                    var wizardResult = wizard.ShowDialog();

                    if (wizardResult == true && !string.IsNullOrWhiteSpace(wizard.SelectedRoExePath))
                    {
                        firstRunService.CompleteFirstRun(wizard.SelectedRoExePath);
                    }
                    else if (wizardResult != true)
                    {
                        Shutdown();
                        return;
                    }
                }

                bool shouldShowSplash = !settings.SkipSplash;

                LocalizationManager.Instance.CurrentLanguage = settings.AppLanguage;

                SplashWindow? splash = null;
                if (shouldShowSplash)
                {
                    splash = new SplashWindow();
                    MainWindow = splash;
                    splash.Show();

                    var warmupTask = Task.Run(() =>
                    {
                        _ = Models.Settings.Load();
                    });

                    var skipToken = splash.SkipCts.Token;
                    await Task.Delay(1000).ConfigureAwait(true);
                    splash.PlayVoice();
                    try { await Task.WhenAll(warmupTask, Task.Delay(2500, skipToken)); }
                    catch (TaskCanceledException)
                    {
                        System.Diagnostics.Debug.WriteLine("[App] Splash skipped via click");
                    }
                }
                else
                {
                    var warmupTask = Task.Run(() => { _ = Models.Settings.Load(); });
                    await warmupTask.ConfigureAwait(true);
                }

                bool isHandheld = await Task.Run(() => Core.HandheldDetector.IsHandheldDevice())
                                      || settings.ForceHandheldMode;

                MultiClientEngineOrchestrator? multiEngine = null;
                ProfileManager? manager = null;
                try
                {
                    await Task.Run(() =>
                    {
#pragma warning disable CS8625
                        var controllerService = new ControllerService();
                        var profileManager = preCreatedProfileManager ?? new ProfileManager();
                        var messenger = new Messenger();
                        var logger = new AdvancedLogger(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RagnaController", "logs", "engine.log"));
                        var settingsObj = Models.Settings.Load();

                        var nativeMethods = new NativeMethodsImpl();
                        var processNames = new ProcessNameResolver();
                        var windowTracker = new MultiClientWindowTracker(nativeMethods, processNames);
                        var multiClientRouter = new MultiClientInputRouter(nativeMethods, processNames, windowTracker);

                        multiEngine = new MultiClientEngineOrchestrator(
                            controllerService,
                            profileManager,
                            settingsObj,
                            messenger,
                            logger,
                            multiClientRouter,
                            windowTracker);
                        manager = profileManager;
#pragma warning restore CS8625
                    });
                }
                catch (Exception ex)
                {
                    LogFatal("Engine Creation", ex.ToString());
                    MessageBox.Show("Error creating engine:\n" + ex.Message + "\n\nDetails: " + CrashLog,
                        "RagnaController - Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                if (multiEngine == null || manager == null)
                {
                    LogFatal("Engine Creation", "Engine or Manager is null after initialization");
                    MessageBox.Show("Error creating engine: Unknown error",
                        "RagnaController - Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown();
                    return;
                }

                var viewModel = new MainViewModel();

                Window main;
                if (isHandheld)
                {
                    System.Diagnostics.Debug.WriteLine(
                        "[App] Handheld erkannt: " + Core.HandheldDetector.DeviceName + " - HandheldWindow");
                    HybridEngine? handheldEngine = null;
                    await Task.Run(() =>
                    {
#pragma warning disable CS8625
                        handheldEngine = new HybridEngine(new BackgroundTickProvider(8), new Messenger(), null, null);
#pragma warning restore CS8625
                    });
                    main = new HandheldWindow(handheldEngine!, manager);
                }
                else
                {
                    HybridEngine? mainEngine = null;
                    await Task.Run(() =>
                    {
#pragma warning disable CS8625
                        mainEngine = new HybridEngine(new BackgroundTickProvider(8), new Messenger(), null, null);
#pragma warning restore CS8625
                    });
                    main = new MainWindow(mainEngine!, manager, viewModel);
                }
                MainWindow = main;

                if (settings.AutoUpdateEnabled)
                {
                    var lastCheck = settings.LastUpdateCheckAt;
                    var intervalHours = settings.UpdateCheckIntervalHours;
                    var shouldCheck = lastCheck == null ||
                                      (DateTime.Now - lastCheck.Value).TotalHours >= intervalHours;

                    if (shouldCheck)
                                        {
                                            _ = Task.Run(async () =>
                                            {
                                                try
                                                {
                                                    using var updateService = new UpdateService(
                                                        System.Reflection.Assembly.GetExecutingAssembly()
                                                            .GetName()?.Version?.ToString() ?? "0.0.0");
                                                    var update = await updateService.CheckForUpdatesAsync();

                                                    if (update != null)
                                                    {
                                                        await Dispatcher.InvokeAsync(() =>
                                                        {
                                                            var updateWindow = new UpdateWindow();
                                                            updateWindow.Owner = main;
                                                            updateWindow.ShowDialog();
                                                        });
                                                    }
                                                    else
                                                    {
                                                        System.Diagnostics.Debug.WriteLine("[App] Kein Update verfugbar.");
                                                    }

                                                    settings.LastUpdateCheckAt = DateTime.Now;
                                                    settings.Save();
                                                }
                                                catch (Exception ex)
                                                {
                                                    System.Diagnostics.Debug.WriteLine("[App] Auto-Update-Check fehlgeschlagen: " + ex.Message);
                                                }
                                            });
                                        }
                                    }

                if (settings.StartMinimized || settings.StartWithWindows)
                {
                    main.Show();
                }
                else
                {
                    main.Opacity = 0;
                    main.Show();

                    await Task.Delay(200);
                    if (splash != null)
                    {
                        splash.FadeAndClose(600);
                    }

                    var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(600));
                    main.BeginAnimation(Window.OpacityProperty, anim);
                }

                if (!settings.HasAskedForTelemetry)
                {
                    var result = MessageBox.Show(
                        "To help improve RagnaController, the app can send anonymous crash reports and basic usage statistics to the developer. No personal data is collected.\n\nDo you want to enable anonymous telemetry?",
                        "Help Improve RagnaController",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    settings.EnableTelemetry = (result == MessageBoxResult.Yes);
                    settings.HasAskedForTelemetry = true;
                    settings.Save();
                }

                TelemetryService.Initialize(settings);
                TelemetryService.SendAppStartPing();

                if (settings.AutoUpdateEnabled)
                {
                    _ = UpdateChecker.CheckAndNotifyAsync(main).ContinueWith(t =>
                    {
                        if (t.IsFaulted)
                        {
                            System.Diagnostics.Debug.WriteLine("[App] Update check failed: " + t.Exception);
                        }
                    }, TaskContinuationOptions.OnlyOnFaulted);
                }
            }
            catch (Exception ex)
            {
                LogFatal("StartWorkflow", ex.ToString());
                MessageBox.Show("Error starting:\n" + ex.Message + "\n\nDetails: " + CrashLog,
                    "RagnaController - Start Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        private static void LogFatal(string source, string msg)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CrashLog)!);
                File.AppendAllText(CrashLog,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] [" + source + "]\n" + msg + "\n\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[App] LogFatal failed: " + ex.Message);
            }
        }

        private static void RestartApp()
        {
            try
            {
                var exePath = Assembly.GetExecutingAssembly().Location;
                Process.Start(new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = exePath
                });
            }
            catch (Exception ex)
            {
                LogFatal("RestartApp", ex.ToString());
            }
            finally
            {
                Application.Current.Shutdown();
            }
        }
    }
}
