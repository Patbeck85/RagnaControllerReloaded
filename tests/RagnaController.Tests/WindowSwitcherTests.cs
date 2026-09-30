using System;
using Xunit;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// FEAT-015: Multi-Client-Routing — WindowSwitcher.SelectTargetHwnd (reine Auswahl-Funktion).
    /// Deckt das Routing-Verhalten ab: Preferred-HWND gewinnt, solange es lebt; sonst Legacy
    /// Prozessname-Auflösung. aliveCheck injiziert (keine Win32-Seitenwirkung → CI-sicher headless).
    /// </summary>
    public class WindowSwitcherTests
    {
        private static readonly IntPtr FakeClientA = new IntPtr(0x1001);
        private static readonly IntPtr FakeClientB = new IntPtr(0x2002);

        [Fact]
        public void SelectTargetHwnd_PrefersPreferred_WhenAlive()
        {
            // Preferred lebt → wird verwendet (Multi-Client: exakt dieses Fenster)
            var hwnd = WindowSwitcher.SelectTargetHwnd("ragnarok", FakeClientB, _ => true);

            Assert.Equal(FakeClientB, hwnd);
        }

        [Fact]
        public void SelectTargetHwnd_FallsBackToLegacy_WhenPreferredDead()
        {
            // Preferred tot (z.B. Client neu gestartet → neues HWND) → Legacy-Pfad.
            // "nonexistent_process_xyz" hat kein Fenster → IntPtr.Zero (kein Crash, sauberes Fallback).
            var hwnd = WindowSwitcher.SelectTargetHwnd("nonexistent_process_xyz", FakeClientB, _ => false);

            Assert.NotEqual(FakeClientB, hwnd);
            Assert.Equal(IntPtr.Zero, hwnd);
        }

        [Fact]
        public void SelectTargetHwnd_NoAliveCheck_UsesPreferredUnconditionally()
        {
            // aliveCheck == null (z.B. reine Unit-Tests) → Preferred wird vertraut.
            var hwnd = WindowSwitcher.SelectTargetHwnd("ragnarok", FakeClientB, null);

            Assert.Equal(FakeClientB, hwnd);
        }

        [Fact]
        public void SelectTargetHwnd_ZeroPreferred_AlwaysUsesLegacy()
        {
            // IntPtr.Zero bedeutet "kein Preferred" → nie als Ziel akzeptiert (auch nicht durch aliveCheck).
            var hwnd = WindowSwitcher.SelectTargetHwnd("nonexistent_process_xyz", IntPtr.Zero, _ => true);

            Assert.Equal(IntPtr.Zero, hwnd);
        }

        [Fact]
        public void SelectTargetHwnd_AliveCheck_IsConsultedNotIgnored()
        {
            // Regression: aliveCheck darf nicht umgangen werden — tot = Fallback, lebt = Preferred.
            var deadResult = WindowSwitcher.SelectTargetHwnd("nonexistent_process_xyz", FakeClientA, h => h != FakeClientA);
            var aliveResult = WindowSwitcher.SelectTargetHwnd("ragnarok", FakeClientA, h => h == FakeClientA);

            Assert.Equal(IntPtr.Zero, deadResult);
            Assert.Equal(FakeClientA, aliveResult);
        }
    }
}
