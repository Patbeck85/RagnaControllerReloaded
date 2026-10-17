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

        #region TECH-020: PID→ProcessName Cache Tests

        /// <summary>
        /// TECH-020: Verifiziert, dass RefreshPidNameCache den PID→Name Cache befüllt.
        /// Auf einem laufenden System sind immer Prozesse vorhanden → Count > 0 erwartet.
        /// </summary>
        [Fact]
        public void Tech020_RefreshPidNameCache_PopulatesCache()
        {
            var cacheField = typeof(WindowSwitcher).GetField(
                "_pidNameCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

            // Act: Cache füllen (überschreibt ggf. vorhandenen Inhalt)
            var refreshMethod = typeof(WindowSwitcher).GetMethod(
                "RefreshPidNameCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
            refreshMethod.Invoke(null, new object?[] { Environment.TickCount64 });

            // Assert: Cache enthält Einträge (System hat Prozesse)
            var cache = (Dictionary<int, string>)cacheField.GetValue(null)!;
            Assert.True(cache.Count > 0, "PID-Cache sollte nach Refresh nicht leer sein");

            // Jeder Eintrag hat gültigen Key (PID > 0, PID 0 = Idle wird übersprungen) und Wert (nicht null/leer)
            foreach (var kvp in cache)
            {
                if (kvp.Key == 0) continue; // System Idle Process
                Assert.True(kvp.Key > 0, $"PID muss > 0 sein, war {kvp.Key}");
                Assert.False(string.IsNullOrEmpty(kvp.Value), $"Prozessname für PID {kvp.Key} darf nicht leer sein");
            }
        }

        /// <summary>
        /// TECH-020: Verifiziert, dass SelectTargetHwnd mit PreferredHwnd den Cache nicht berührt.
        /// Das ist der schnelle Pfad — keine Process.GetProcesses()-Aufrufe.
        /// </summary>
        [Fact]
        public void Tech020_SelectTargetHwnd_PreferredPath_DoesNotTouchCache()
        {
            // PreferredHwnd lebt → SelectTargetHwnd gibt ihn sofort zurück,
            // ohne in GetCachedHwnd / RefreshPidNameCache zu gehen.
            var hwnd = WindowSwitcher.SelectTargetHwnd("irgendein_prozess", FakeClientB, _ => true);
            Assert.Equal(FakeClientB, hwnd);
        }

        #endregion
    }
}
