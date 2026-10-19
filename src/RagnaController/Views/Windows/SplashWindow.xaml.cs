using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RagnaController.Views.Windows
{
    public partial class SplashWindow : Window
    {
        private static readonly string[] StatusMessages = { "Initializing…", "Loading profiles…", "Starting engine…", "Connecting controller…", "Almost ready…", "Ready." };
        private readonly DispatcherTimer _statusTimer = new();
        private int _statusPhase = 0;

        // TASK-2003: Progress percentage
        private readonly DispatcherTimer _progressTimer = new();
        private int _progressValue = 0;

        // Skip splash per click
        public readonly System.Threading.CancellationTokenSource SkipCts = new();

        public SplashWindow()
        {
            InitializeComponent();
            SplashVersionLabel.Text = $"v{Core.AppVersion.Current}";
            PrepareVoice();
            ContentRendered += async (s, e) => await StartAnimationsAsync().ConfigureAwait(false);
            KeyDown += SplashWindow_KeyDown;
        }

        private void SplashWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SkipCts.Cancel();
            }
        }

        private void SplashWindow_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SkipCts.Cancel(); // Signal to App.xaml.cs: skip splash
        }

        private void BtnSkipSplash_Click(object sender, RoutedEventArgs e)
        {
            SkipCts.Cancel();
        }

        private void PrepareVoice()
                {
                    try 
                    {
                        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                        string mp3Path = Path.Combine(baseDir, "startup_voice.mp3");
                        string wavPath = Path.Combine(baseDir, "startup_voice.wav");
                
                        if (File.Exists(mp3Path)) VoicePlayer.Source = new Uri(mp3Path, UriKind.Absolute);
                        else if (File.Exists(wavPath)) VoicePlayer.Source = new Uri(wavPath, UriKind.Absolute);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SplashWindow] PrepareVoice failed: {ex.Message}");
                    }
                }

        public void PlayVoice()
                {
                    try
                    {
                        if (VoicePlayer.Source != null) VoicePlayer.Play();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SplashWindow] PlayVoice failed: {ex.Message}");
                    }
                }

        private async Task StartAnimationsAsync()
        {
            try
            {
                Play("FadeIn");
                await Task.Delay(700);
                Play("GoldPulse");
                Play("LogoIn");
                Play("ProgressAnim");

                // TASK-2003: Progress percentage counter (0% → 100% over ~4 seconds)
                _progressTimer.Interval = TimeSpan.FromMilliseconds(100);
                _progressTimer.Tick += (s, e) =>
                {
                    _progressValue += 2;
                    if (_progressValue >= 100)
                    {
                        _progressValue = 100;
                        _progressTimer.Stop();
                    }
                    SplashProgressPercent.Text = $"{_progressValue}%";
                };
                _progressTimer.Start();

                DoubleAnimation fadeInBottom = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(800));
                BottomRow.BeginAnimation(OpacityProperty, fadeInBottom);

                _statusTimer.Interval = TimeSpan.FromMilliseconds(700);
                _statusTimer.Tick += (s, e) =>
                {
                    _statusPhase++;
                    if (_statusPhase < StatusMessages?.Length && StatusMessages != null)
                        StatusText.Text = StatusMessages[_statusPhase];
                    else _statusTimer.Stop();
                };
                _statusTimer.Start();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashWindow] Animation error: {ex.Message}");
            }
        }

        // TASK-2003: Show error details on splash screen
        public void ShowError(string title, string details)
        {
            _progressTimer.Stop();
            ErrorPanel.Visibility = Visibility.Visible;
            ErrorTitle.Text = title;
            ErrorDetails.Text = details;
        }

        public void FadeAndClose(int durationMs = 600)
        {
            _statusTimer.Stop();
            var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(durationMs))) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            fade.Completed += (s, e) => Close();
            Root.BeginAnimation(OpacityProperty, fade);
        }

        private void Play(string key) { if (Resources.Contains(key) && Resources[key] is Storyboard sb) sb.Begin(this); }
    }
}