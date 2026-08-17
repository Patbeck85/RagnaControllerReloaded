using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController
{
    public partial class InGameOverlayWindow : Window
    {
        private readonly IMessenger    _messenger;
        private readonly Core.WindowTracker _tracker;
        private IDisposable?           _subscription;

        // True after the user has manually dragged the overlay
        private bool _userPositioned;
        // True after the user has explicitly closed the overlay via BtnHide
        private bool _userHidden;

        private static readonly SolidColorBrush BrushLocked    = new SolidColorBrush(Color.FromRgb(255, 60,  60));
        private static readonly SolidColorBrush BrushSearching = new SolidColorBrush(Color.FromRgb(229, 184, 66));
        private static readonly SolidColorBrush BrushCombo     = new SolidColorBrush(Color.FromRgb(160, 64,  255));
        private static readonly SolidColorBrush BrushVacuum    = new SolidColorBrush(Color.FromRgb(64,  255, 128));
        private static readonly SolidColorBrush BrushPanic     = new SolidColorBrush(Color.FromRgb(255, 128, 40));
        private static readonly SolidColorBrush BrushDefault   = new SolidColorBrush(Color.FromRgb(63,  184, 224));
        private static readonly SolidColorBrush BrushDotGreen  = new SolidColorBrush(Color.FromRgb(80,  220, 80));
        private static readonly SolidColorBrush BrushDotRed    = new SolidColorBrush(Color.FromRgb(220, 60,  60));

        static InGameOverlayWindow()
        {
            BrushLocked.Freeze(); BrushSearching.Freeze(); BrushCombo.Freeze();
            BrushVacuum.Freeze(); BrushPanic.Freeze();     BrushDefault.Freeze();
            BrushDotGreen.Freeze(); BrushDotRed.Freeze();
        }

        public InGameOverlayWindow(IMessenger messenger, Core.WindowTracker tracker)
        {
            InitializeComponent();
            _messenger = messenger;
            _tracker   = tracker;

            SourceInitialized += OnSourceInitialized;
            Loaded            += OnLoaded;
            Closed            += OnClosed;
            SizeChanged       += (s, e) => { if (!_userPositioned && _tracker.IsTracking) RepositionOverlay(); };
        }

        private void OnSourceInitialized(object? sender, EventArgs e)
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd == IntPtr.Zero) return;

            // WS_EX_LAYERED only — allows AllowsTransparency without eating all mouse input.
            // WS_EX_TRANSPARENT is intentionally NOT set: the main content border carries
            // IsHitTestVisible="False" so RO receives clicks there; the drag strip above it
            // is interactive for moving/closing the overlay.
            long style    = NativeMethods.GetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE).ToInt64();
            long newStyle = style | (long)NativeMethods.WS_EX_LAYERED;
            NativeMethods.SetWindowLongPtr(hwnd, NativeMethods.GWL_EXSTYLE, new IntPtr(newStyle));
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            _subscription = _messenger.Subscribe<SnapshotReadyMessage>(msg =>
                Dispatcher.BeginInvoke(() => ApplySnapshot(msg.Snapshot)));
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _subscription?.Dispose();
        }

        // ── Drag strip handlers ───────────────────────────────────────────
        private void DragStrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
            _userPositioned = true;   // freeze auto-reposition after manual move
        }

        private void BtnHide_Click(object sender, RoutedEventArgs e)
        {
            _userHidden = true;
            Hide();
        }

        // ── Called from MainWindow to show/hide ───────────────────────────
        public void Toggle()
        {
            if (IsVisible) { _userHidden = true;  Hide(); }
            else           { _userHidden = false; Show(); }
        }

        private void ApplySnapshot(ControllerSnapshot snap)
        {
            if (LayerText != null)
                LayerText.Text = snap.SmartCursorMenuMode ? "MENU" : snap.LayerText;

            if (StateText != null)
            {
                StateText.Text = snap.SmartCursorMenuMode ? "GRID MODE" : snap.StateLabel;
                if (snap.SmartCursorMenuMode)
                    StateText.Foreground = new SolidColorBrush(Color.FromRgb(229, 184, 66));
                else
                    StateText.Foreground = GetStateBrush(snap);
            }

            if (TrackDot != null)
                TrackDot.Fill = snap.WindowTracked ? BrushDotGreen : BrushDotRed;

            if (snap.WindowTracked && _tracker.IsTracking && !_userPositioned)
                RepositionOverlay();

            // Auto-show/hide only when the user has not explicitly hidden the overlay
            if (!_userHidden)
            {
                if (snap.WindowTracked && !IsVisible) Show();
                else if (!snap.WindowTracked && IsVisible) Hide();
            }
        }

        private static SolidColorBrush GetStateBrush(ControllerSnapshot snap)
        {
            if (snap.PanicActive)  return BrushPanic;
            if (snap.VacuumActive) return BrushVacuum;
            if (snap.ComboActive)  return BrushCombo;

            return snap.CombatState switch
            {
                "ENGAGED" => BrushLocked,
                "SEEKING" => BrushSearching,
                _         => BrushDefault,
            };
        }

        private void RepositionOverlay()
        {
            double ow    = this.ActualWidth > 1 ? this.ActualWidth : this.Width;
            int roRight  = _tracker.CenterX + (_tracker.ClientW / 2);
            int roTop    = _tracker.CenterY - (_tracker.ClientH / 2);

            this.Left = (double)roRight - ow - 8;
            this.Top  = (double)roTop   + 8;
        }
    }
}