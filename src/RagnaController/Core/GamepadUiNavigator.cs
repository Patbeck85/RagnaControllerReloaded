using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using RagnaController.Controller;

namespace RagnaController.Core
{
    /// <summary>
    /// Generic D-Pad/A-Button navigation (no SDL dependencies)
    /// Runs on UI thread (DispatcherTimer), does not collide with HybridEngine.
    /// </summary>
    public sealed class GamepadUiNavigator : IDisposable
    {
        private readonly ControllerService _ctrl;
        private readonly DispatcherTimer   _timer;
        private ulong                      _prevButtons;
        private int                        _repeatMs;
        private ulong                      _heldButtons;
        private const int REPEAT_DELAY_MS  = 400;
        private const int REPEAT_RATE_MS   = 150;

        // Button bit positions (matches standard controller layout)
        private const ulong BTN_A          = 1UL << 0;
        private const ulong BTN_B          = 1UL << 1;
        private const ulong BTN_X          = 1UL << 2;
        private const ulong BTN_Y          = 1UL << 3;
        private const ulong BTN_L1         = 1UL << 4;
        private const ulong BTN_R1         = 1UL << 5;
        private const ulong BTN_L3         = 1UL << 6;
        private const ulong BTN_R3         = 1UL << 7;
        private const ulong BTN_START      = 1UL << 8;
        private const ulong BTN_BACK       = 1UL << 9;
        private const ulong BTN_DPAD_UP    = 1UL << 10;
        private const ulong BTN_DPAD_DOWN  = 1UL << 11;
        private const ulong BTN_DPAD_LEFT  = 1UL << 12;
        private const ulong BTN_DPAD_RIGHT = 1UL << 13;

        public Window? ActiveWindow { get; set; }

        public GamepadUiNavigator(ControllerService ctrl)
        {
            _ctrl  = ctrl;
            _timer = new DispatcherTimer(DispatcherPriority.Input)
                { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += OnTick;
        }

        public void Start() => _timer.Start();
        public void Stop()  => _timer.Stop();

        private unsafe void OnTick(object? sender, EventArgs e)
        {
            if (ActiveWindow == null || !ActiveWindow.IsActive) return;

            var pad = _ctrl.GetControllerSnapshot();
            if (pad == null) { _prevButtons = 0; return; }

            // Read buttons using ControllerService API (simplified approach)
            ulong currentButtons = 0;

            // Get button states from controller service - use simplified direct access
            // Since GetButtonStates() returns a struct (not nullable), we don't need null check
            var buttons = _ctrl.GetButtonStates();

            // Map button states to bit positions
            if (buttons.APressed) currentButtons |= BTN_A;
            if (buttons.BPressed)  currentButtons |= BTN_B;
            if (buttons.XPressed)  currentButtons |= BTN_X;
            if (buttons.YPressed)  currentButtons |= BTN_Y;
            if (buttons.L1Pressed) currentButtons |= BTN_L1;
            if (buttons.R1Pressed) currentButtons |= BTN_R1;
            if (buttons.StartPressed) currentButtons |= BTN_START;
            if (buttons.BackPressed)  currentButtons |= BTN_BACK;
            
            // D-Pad buttons
            if (buttons.DPadUp) currentButtons |= BTN_DPAD_UP;
            if (buttons.DPadDown) currentButtons |= BTN_DPAD_DOWN;
            if (buttons.DPadLeft) currentButtons |= BTN_DPAD_LEFT;
            if (buttons.DPadRight) currentButtons |= BTN_DPAD_RIGHT;

            // Single press: DPad + A
            if (JustPressed(currentButtons, BTN_DPAD_UP))    Navigate(FocusNavigationDirection.Up);
            if (JustPressed(currentButtons, BTN_DPAD_DOWN))  Navigate(FocusNavigationDirection.Down);
            if (JustPressed(currentButtons, BTN_DPAD_LEFT))  Navigate(FocusNavigationDirection.Left);
            if (JustPressed(currentButtons, BTN_DPAD_RIGHT)) Navigate(FocusNavigationDirection.Right);
            if (JustPressed(currentButtons, BTN_A)) InvokeSelected();

            // Auto-repeat for held DPad buttons (like keyboard repeat)
            ulong held = currentButtons & (BTN_DPAD_UP | BTN_DPAD_DOWN | BTN_DPAD_LEFT | BTN_DPAD_RIGHT);

            if (held != 0 && held == _heldButtons)
            {
                _repeatMs -= 16;
                if (_repeatMs <= 0)
                {
                    _repeatMs = REPEAT_RATE_MS;
                    if ((held & BTN_DPAD_UP) != 0)    Navigate(FocusNavigationDirection.Up);
                    if ((held & BTN_DPAD_DOWN) != 0)  Navigate(FocusNavigationDirection.Down);
                    if ((held & BTN_DPAD_LEFT) != 0)  Navigate(FocusNavigationDirection.Left);
                    if ((held & BTN_DPAD_RIGHT) != 0) Navigate(FocusNavigationDirection.Right);
                }
            }
            else
            {
                _heldButtons  = held;
                _repeatMs = REPEAT_DELAY_MS;
            }

            _prevButtons = currentButtons;
        }

        private bool JustPressed(ulong current, ulong flag)
            => (current & flag) != 0 && (_prevButtons & flag) == 0;

        private static void Navigate(FocusNavigationDirection dir)
        {
            if (Keyboard.FocusedElement is UIElement el)
                el.MoveFocus(new TraversalRequest(dir));
        }

        private static void InvokeSelected()
        {
            if (Keyboard.FocusedElement is ButtonBase btn)
            {
                // Simple invoke - just simulate click by raising the event
                // RoutedEventArgs constructor requires RoutedEvent and source
                var routedEvent = System.Windows.Controls.Primitives.ButtonBase.ClickEvent;
                var clickArgs = new RoutedEventArgs(routedEvent, btn);
                btn.RaiseEvent(clickArgs);
            }
        }

        public void Dispose() => _timer.Stop();
    }
}
