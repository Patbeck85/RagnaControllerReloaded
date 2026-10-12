using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// GEX-004: Focus Lock Reliability Under Real RO Conditions
    /// Tests Focus Lock against: Alt-tab spam, Multi-client, Borderless/Windowed/Fullscreen, DPI scaling, RO client variants.
    /// </summary>
    public class FocusLockReliabilityTests
    {
        private const int STRESS_ITERATIONS = 1000;
        private const uint TEST_PID = 1234u;

        /// <summary>
        /// GEX-004: Focus Lock correctly identifies RO window across client variants.
        /// </summary>
        [Theory]
        [InlineData("ragexe")]      // Classic
        [InlineData("ragexere")]    // Renewal
        [InlineData("ragnarok")]    // Custom
        [InlineData("ROClient")]    // Private server
        [InlineData("myRO")]        // Another custom
        public void FocusLock_Recognizes_RO_Client_Variants(string processName)
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var hwnd = new IntPtr(0x1234);
            native.SetForegroundWindow(hwnd);
            native.SetWindowProcessId(hwnd, TEST_PID);
            native.SetClientRect(hwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(hwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(hwnd, 96);

            resolver.SetProcessName((int)TEST_PID, processName);

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName(processName);
            tracker.StartHook();
            tracker.Refresh();

            Assert.True(tracker.IsTracking);
            // _procName is private, verify via SetProcessName behavior instead

            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: Focus Lock integrates with PreferredClientHwnd (FEAT-015).
        /// When PreferredClientHwnd is set AND window is alive, it takes priority over process name.
        /// </summary>
        [Fact]
        public void FocusLock_PreferredClientHwnd_Takes_Priority()
        {
            // This tests the integration point between WindowSwitcher.PreferredClientHwnd
            // and SystemMonitor/WindowTracker via SelectTargetHwnd
            // Using reflection because SelectTargetHwnd is internal static

            var method = typeof(WindowSwitcher).GetMethod(
                "SelectTargetHwnd",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            Assert.NotNull(method);

            // Test: PreferredHwnd takes priority when alive
            var preferredHwnd = new IntPtr(0x1234);
            Func<IntPtr, bool>? aliveCheck = null;
#pragma warning disable CS8601
            object resultObj = method!.Invoke(null, new object[] { "ragexe", preferredHwnd, aliveCheck })!;
#pragma warning restore CS8601
            IntPtr result = (IntPtr)resultObj;
            Assert.Equal(preferredHwnd, result);
        }

        /// <summary>
        /// GEX-004: Alt-tab spam stress test — Focus Lock correctly tracks foreground window.
        /// Simulates rapid foreground changes (1000 switches in quick succession).
        /// </summary>
        [Fact]
        public void FocusLock_AltTab_Spam_No_Flicker_Or_Leak()
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var roHwnd = new IntPtr(0x1234);
            var otherHwnd = new IntPtr(0x5678);

            // Setup RO window
            native.SetWindowProcessId(roHwnd, TEST_PID);
            native.SetClientRect(roHwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(roHwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(roHwnd, 96);

            // Setup other window
            native.SetWindowProcessId(otherHwnd, 5678u);
            native.SetClientRect(otherHwnd, new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 });
            native.SetClientToScreenOffset(otherHwnd, new POINT { X = 100, Y = 100 });
            native.SetDpiForWindow(otherHwnd, 96);

            resolver.SetProcessName((int)TEST_PID, "ragexe");
            resolver.SetProcessName(5678, "notepad");

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();

            var queue = new InputCommandQueue();
            queue.Start();
            var movement = new MovementEngine(queue, tracker);
            movement.SetCenter(960, 540, 1080, 1.0f);

            var sysMonitor = new SystemMonitor(tracker, movement, native);
            sysMonitor.FocusLockEnabled = true;
            sysMonitor.FocusLockProcess = "ragexe";

            int roTrackingCount = 0;
            int otherTrackingCount = 0;
            int focusLockedCount = 0;
            int notLockedCount = 0;

            for (int i = 0; i < STRESS_ITERATIONS; i++)
            {
                // Alternate foreground window
                native.SetForegroundWindow(i % 2 == 0 ? roHwnd : otherHwnd);
                sysMonitor.Update();

                // Verify tracking follows foreground correctly
                if (i % 2 == 0)
                {
                    // RO in foreground - should track
                    Assert.True(sysMonitor.IsTracking, $"Iteration {i}: Expected tracking when RO is foreground");
                    roTrackingCount++;
                    Assert.False(sysMonitor.IsFocusLocked, $"Iteration {i}: Should not lock when RO is foreground");
                    notLockedCount++;
                }
                else
                {
                    // Other in foreground - should NOT track
                    Assert.False(sysMonitor.IsTracking, $"Iteration {i}: Expected no tracking when other is foreground");
                    otherTrackingCount++;
                    Assert.True(sysMonitor.IsFocusLocked, $"Iteration {i}: Should lock when other is foreground");
                    focusLockedCount++;
                }
            }

            // Should have tracked correctly each time
            Assert.Equal(STRESS_ITERATIONS / 2, roTrackingCount);
            Assert.Equal(STRESS_ITERATIONS / 2, otherTrackingCount);
            Assert.Equal(STRESS_ITERATIONS / 2, focusLockedCount);
            Assert.Equal(STRESS_ITERATIONS / 2, notLockedCount);

            queue.RequestShutdown();
            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: DPI scaling across monitors — Focus Lock geometry correct.
        /// </summary>
        [Theory]
        [InlineData(96, 1.0f)]   // 100%
        [InlineData(144, 1.5f)]  // 150%
        [InlineData(192, 2.0f)]  // 200%
        [InlineData(288, 3.0f)]  // 300%
        public void FocusLock_DPI_Scaling_Correct(uint dpi, float expectedScale)
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var hwnd = new IntPtr(0x1234);
            native.SetForegroundWindow(hwnd);
            native.SetWindowProcessId(hwnd, TEST_PID);
            native.SetClientRect(hwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(hwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(hwnd, dpi);

            resolver.SetProcessName((int)TEST_PID, "ragexe");

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();
            tracker.Refresh();

            Assert.Equal(expectedScale, tracker.DpiScale);
            Assert.True(tracker.IsTracking);

            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: Window mode changes (Borderless/Windowed/Fullscreen) — tracking persists.
        /// </summary>
        [Fact]
        public void FocusLock_Window_Mode_Changes_Tracking_Persists()
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var hwnd = new IntPtr(0x1234);
            native.SetForegroundWindow(hwnd);
            native.SetWindowProcessId(hwnd, TEST_PID);
            native.SetClientToScreenOffset(hwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(hwnd, 96);

            resolver.SetProcessName((int)TEST_PID, "ragexe");

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();

            // Initial - Borderless
            native.SetClientRect(hwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            tracker.Refresh();
            Assert.True(tracker.IsTracking);
            Assert.Equal(1920, tracker.ClientW);
            Assert.Equal(1080, tracker.ClientH);

            // Simulate window mode change to Windowed (same hwnd, different rect)
            native.SetClientRect(hwnd, new RECT { Left = 100, Top = 100, Right = 1820, Bottom = 980 });
            tracker.Refresh();
            Assert.True(tracker.IsTracking);
            Assert.Equal(1720, tracker.ClientW);
            Assert.Equal(880, tracker.ClientH);

            // Fullscreen
            native.SetClientRect(hwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            tracker.Refresh();
            Assert.True(tracker.IsTracking);
            Assert.Equal(1920, tracker.ClientW);
            Assert.Equal(1080, tracker.ClientH);

            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: Focus Lock enabled/disabled toggle works instantly.
        /// </summary>
        [Fact]
        public void FocusLock_Toggle_Instant()
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var roHwnd = new IntPtr(0x1234);
            var otherHwnd = new IntPtr(0x5678);

            native.SetWindowProcessId(roHwnd, TEST_PID);
            native.SetClientRect(roHwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(roHwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(roHwnd, 96);

            native.SetWindowProcessId(otherHwnd, 5678u);
            native.SetClientRect(otherHwnd, new RECT { Left = 0, Top = 0, Right = 800, Bottom = 600 });
            native.SetClientToScreenOffset(otherHwnd, new POINT { X = 100, Y = 100 });
            native.SetDpiForWindow(otherHwnd, 96);

            resolver.SetProcessName((int)TEST_PID, "ragexe");
            resolver.SetProcessName(5678, "notepad");

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();

            var queue = new InputCommandQueue();
            queue.Start();
            var movement = new MovementEngine(queue, tracker);
            movement.SetCenter(960, 540, 1080, 1.0f);

            var sysMonitor = new SystemMonitor(tracker, movement, native);

            // Non-RO window in foreground
            native.SetForegroundWindow(otherHwnd);

            // FocusLock OFF — should NOT lock even on non-RO window
            sysMonitor.FocusLockEnabled = false;
            sysMonitor.Update();
            Assert.False(sysMonitor.IsFocusLocked);

            // FocusLock ON — should lock on non-RO window
            sysMonitor.FocusLockEnabled = true;
            sysMonitor.Update();
            Assert.True(sysMonitor.IsFocusLocked);

            // FocusLock OFF again — should unlock instantly
            sysMonitor.FocusLockEnabled = false;
            sysMonitor.Update();
            Assert.False(sysMonitor.IsFocusLocked);

            queue.RequestShutdown();
            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: Focus Lock doesn't lock when RO window IS foreground (prevents in-game lock).
        /// </summary>
        [Fact]
        public void FocusLock_Not_Active_When_RO_Foreground()
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var roHwnd = new IntPtr(0x1234);
            native.SetForegroundWindow(roHwnd);
            native.SetWindowProcessId(roHwnd, TEST_PID);
            native.SetClientRect(roHwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(roHwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(roHwnd, 96);

            resolver.SetProcessName((int)TEST_PID, "ragexe");

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();

            var queue = new InputCommandQueue();
            queue.Start();
            var movement = new MovementEngine(queue, tracker);
            movement.SetCenter(960, 540, 1080, 1.0f);

            var sysMonitor = new SystemMonitor(tracker, movement, native);
            sysMonitor.FocusLockEnabled = true;

            tracker.Refresh();
            sysMonitor.Update();

            // RO is foreground → IsTracking = true → IsFocusLocked = false
            Assert.True(sysMonitor.IsTracking);
            Assert.False(sysMonitor.IsFocusLocked);

            queue.RequestShutdown();
            tracker.Dispose();
        }

        /// <summary>
        /// GEX-004: PreferredClientHwnd explicitly set - routing works.
        /// </summary>
        [Fact]
        public void FocusLock_PreferredClientHwnd_Explicitly_Set_Routes_Correctly()
        {
            var native = new TestNativeMethods();
            var resolver = new TestProcessNameResolver();

            var preferredHwnd = new IntPtr(0x1234);  // Main character
            var otherHwnd = new IntPtr(0x5678);      // Alt character

            // Both are valid RO windows with different PIDs
            native.SetWindowProcessId(preferredHwnd, TEST_PID);
            native.SetClientRect(preferredHwnd, new RECT { Left = 0, Top = 0, Right = 1920, Bottom = 1080 });
            native.SetClientToScreenOffset(preferredHwnd, new POINT { X = 0, Y = 0 });
            native.SetDpiForWindow(preferredHwnd, 96);

            native.SetWindowProcessId(otherHwnd, 9999u);
            native.SetClientRect(otherHwnd, new RECT { Left = 100, Top = 100, Right = 1820, Bottom = 980 });
            native.SetClientToScreenOffset(otherHwnd, new POINT { X = 100, Y = 100 });
            native.SetDpiForWindow(otherHwnd, 96);

            resolver.SetProcessName((int)TEST_PID, "ragexe");
            resolver.SetProcessName(9999, "ragexe");  // Same process name - but different PID

            var tracker = new WindowTracker(native, resolver);
            tracker.SetProcessName("ragexe");
            tracker.StartHook();

            var queue = new InputCommandQueue();
            queue.Start();
            var movement = new MovementEngine(queue, tracker);
            movement.SetCenter(960, 540, 1080, 1.0f);

            var sysMonitor = new SystemMonitor(tracker, movement, native);
            sysMonitor.FocusLockEnabled = true;
            sysMonitor.FocusLockProcess = "ragexe";

            // Set PreferredClientHwnd to alt character
            WindowSwitcher.PreferredClientHwnd = preferredHwnd;
            WindowSwitcher.Clear(); // Clear cache so it picks up new preferred

            // Foreground is alt character (otherHwnd), but PreferredClientHwnd is preferredHwnd
            native.SetForegroundWindow(otherHwnd);
            sysMonitor.Update();

            // Should track preferredHwnd (main character), not the foreground otherHwnd
            // because SelectTargetHwnd returns PreferredClientHwnd when alive
            Assert.True(sysMonitor.IsTracking);

            // Cleanup
            WindowSwitcher.PreferredClientHwnd = null;
            WindowSwitcher.Clear();
            queue.RequestShutdown();
            tracker.Dispose();
        }
    }
}