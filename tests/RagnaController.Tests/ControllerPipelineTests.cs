using System.Collections.Generic;
using Xunit;
using RagnaController.Models;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-015: Controller-Eingaben-Pipeline-Goldtests (hardware-frei).
    ///
    /// Diese Tests verifizieren die EINGABE-KETTE auf der echten <see cref="ParsedInput"/>-Währung,
    /// gespeist durch <see cref="FakeControllerProvider"/>. Der zentrale Kontrakt ist die
    /// JustPressed/JustReleased-Transition-Maschine — exakt dieselbe State-Maschine, die
    /// <c>Core/InputReader.cs</c> in Produktion pflegt (<c>_prevRawButtons = currentButtons</c>).
    /// Damit ist jede Stufe (Mapping → Transitionen) reproduzierbar verifizierbar, headless in CI.
    /// </summary>
    public class ControllerPipelineTests
    {
        // Die geteilte State-Maschine (TEST-015/016) liegt in PipelineStep.cs —
        /// <see cref="PipelineStep"/>. Damit Replay und Pipeline-Goldtests dieselbe
        /// Semantik wie Produktion (<c>Core/InputReader.Read()</c>) nutzen (DRY).

        [Fact]
        public void Press_JustPressed_FiresExactlyOnce_OnPressFrame()
        {
            var p = new FakeControllerProvider()
                .ScriptButton(GamepadButtonFlags.None)     // Frame 0: idle
                .ScriptButton(GamepadButtonFlags.BtnA)    // Frame 1: A drücken
                .ScriptButton(GamepadButtonFlags.BtnA);   // Frame 2: A gehalten

            var step = new PipelineStep();

            var f0 = step.Step(p);
            Assert.False(f0.JustPressed(GamepadButtonFlags.BtnA)); // noch nicht gedrückt

            var f1 = step.Step(p);
            Assert.True(f1.JustPressed(GamepadButtonFlags.BtnA));  // exakt hier: einmal
            Assert.True(f1.BtnA);                                  // Snapshot stimmt
            Assert.False(f1.JustReleased(GamepadButtonFlags.BtnA));

            var f2 = step.Step(p);
            Assert.False(f2.JustPressed(GamepadButtonFlags.BtnA)); // gehalten ≠ neu gedrückt
            Assert.True(f2.BtnA);
        }

        [Fact]
        public void Release_JustReleased_FiresExactlyOneFrame_AfterLastPress()
        {
            var p = new FakeControllerProvider()
                .ScriptButton(GamepadButtonFlags.None)     // Frame 0: idle
                .ScriptButton(GamepadButtonFlags.BtnA)    // Frame 1: A drücken
                .ScriptButton(GamepadButtonFlags.None);   // Frame 2: A loslassen

            var step = new PipelineStep();

            var f0 = step.Step(p);
            Assert.False(f0.JustReleased(GamepadButtonFlags.BtnA));

            var f1 = step.Step(p);
            Assert.False(f1.JustReleased(GamepadButtonFlags.BtnA)); // noch gedrückt
            Assert.True(f1.JustPressed(GamepadButtonFlags.BtnA));

            var f2 = step.Step(p);
            Assert.True(f2.JustReleased(GamepadButtonFlags.BtnA));  // exakt ein Frame nach Release
            Assert.False(f2.BtnA);
            Assert.False(f2.JustPressed(GamepadButtonFlags.BtnA));
        }

        [Fact]
        public void StickAndTriggerValues_PassThrough_BitExact()
        {
            const float lx = 0.5f, ly = -1.0f, rx = 0.25f, ry = 0.75f;
            const float lt = 0.8f, rt = 0.4f;

            var p = new FakeControllerProvider()
                .ScriptFrame(GamepadButtonFlags.None, lx, ly, rx, ry, lt, rt);

            var step = new PipelineStep();
            var f = step.Step(p);

            Assert.Equal(lx, f.LeftX);
            Assert.Equal(ly, f.LeftY);
            Assert.Equal(rx, f.RightX);
            Assert.Equal(ry, f.RightY);
            Assert.Equal(lt, f.TriggerLeft);
            Assert.Equal(rt, f.TriggerRight);

            // Trigger > 0.15 → L2/R2-Property gesetzt (XInput-Konvention)
            Assert.True(f.L2);  // lt=0.8 > 0.15
            Assert.True(f.R2);  // rt=0.4 > 0.15
        }

        [Fact]
        public void TriggerBelowThreshold_L2R2_AreFalse()
        {
            var p = new FakeControllerProvider()
                .ScriptFrame(GamepadButtonFlags.None, 0f, 0f, 0f, 0f, 0.1f, 0.05f);

            var step = new PipelineStep();
            var f = step.Step(p);

            Assert.False(f.L2); // 0.1 <= 0.15
            Assert.False(f.R2); // 0.05 <= 0.15
        }

        [Fact]
        public void Disconnect_ReturnsDisconnectedState_AndResetsPrev()
        {
            var p = new FakeControllerProvider()
                .ScriptButton(GamepadButtonFlags.BtnA)    // Frame 0: A gedrückt
                .ScriptDisconnect();                       // Frame 1: disconnect

            var step = new PipelineStep();

            var f0 = step.Step(p);
            Assert.True(f0.IsConnected);
            Assert.True(f0.JustPressed(GamepadButtonFlags.BtnA));

            var f1 = step.Step(p);
            Assert.False(f1.IsConnected);
            // Prev-State zurückgesetzt → kein Ghost auf spätem Reconnect (siehe Reconnect-Test)
        }

        [Fact]
        public void Reconnect_NoGhostButtons_OnFirstFrame()
        {
            // A drücken → disconnect → reconnect → B drücken.
            // Auf dem ersten Frame nach Reconnect darf KEIN Ghost-JustReleased für A
            // und KEIN Ghost-JustPressed für einen nicht-gedrückten Button feuern,
            // weil der Prev-State bei Disconnect zurückgesetzt wird.
            var p = new FakeControllerProvider()
                .ScriptButton(GamepadButtonFlags.BtnA)    // Frame 0: A
                .ScriptDisconnect()                        // Frame 1: disconnect
                .ScriptButton(GamepadButtonFlags.BtnB);   // Frame 2: reconnect, B

            var step = new PipelineStep();

            var f0 = step.Step(p);
            Assert.True(f0.JustPressed(GamepadButtonFlags.BtnA));

            var f1 = step.Step(p);
            Assert.False(f1.IsConnected);

            var f2 = step.Step(p);
            Assert.True(f2.IsConnected);
            Assert.True(f2.JustPressed(GamepadButtonFlags.BtnB));
            // KEIN Ghost: A war vor Disconnect gedrückt, nach Reset kein JustReleased(A)
            Assert.False(f2.JustReleased(GamepadButtonFlags.BtnA));
            Assert.False(f2.JustPressed(GamepadButtonFlags.BtnA));
        }

        [Fact]
        public void XInputDigitalMapping_MapsIdentically_ViaFake()
        {
            // Verifiziert, dass dieselbe ParsedInput-Mapping, die der XInput-Pfad baut
            // (individuelle Properties + RawButtons-Mask), über das Fake erreichbar ist.
            var mask = GamepadButtonFlags.BtnA
                     | GamepadButtonFlags.BtnB
                     | GamepadButtonFlags.BtnX
                     | GamepadButtonFlags.BtnY
                     | GamepadButtonFlags.L1
                     | GamepadButtonFlags.R1
                     | GamepadButtonFlags.L3
                     | GamepadButtonFlags.R3
                     | GamepadButtonFlags.Start
                     | GamepadButtonFlags.Back
                     | GamepadButtonFlags.DPadUp
                     | GamepadButtonFlags.DPadDown
                     | GamepadButtonFlags.DPadLeft
                     | GamepadButtonFlags.DPadRight;

            var p = new FakeControllerProvider().ScriptButton(mask);
            var step = new PipelineStep();
            var f = step.Step(p);

            Assert.True(f.BtnA); Assert.True(f.BtnB);
            Assert.True(f.BtnX); Assert.True(f.BtnY);
            Assert.True(f.L1);   Assert.True(f.R1);
            Assert.True(f.L3);   Assert.True(f.R3);
            Assert.True(f.Start);Assert.True(f.Back);
            Assert.True(f.DPadUp); Assert.True(f.DPadDown);
            Assert.True(f.DPadLeft); Assert.True(f.DPadRight);

            // RawButtons-Mask bit-exakt (XInput-Konvention: digitale Buttons in Mask)
            Assert.Equal(mask, f.RawButtons);
        }

        [Fact]
        public void LongPressHoldRelease_FiresExactlyOnePressAndOneRelease()
        {
            // Golden-Master über 20 Frames: 1× JustPressed, 1× JustReleased — nie mehr.
            const int holdFrames = 20;
            var p = new FakeControllerProvider().ScriptButton(GamepadButtonFlags.None);
            for (int i = 0; i < holdFrames; i++)
                p.ScriptButton(GamepadButtonFlags.BtnX);
            p.ScriptButton(GamepadButtonFlags.None); // Release

            var step = new PipelineStep();
            int presses = 0, releases = 0;
            for (int i = 0; i < p.FrameCount; i++)
            {
                var f = step.Step(p);
                if (f.JustPressed(GamepadButtonFlags.BtnX)) presses++;
                if (f.JustReleased(GamepadButtonFlags.BtnX)) releases++;
            }

            Assert.Equal(1, presses);
            Assert.Equal(1, releases);
        }

        [Fact]
        public void Press_Through_SnapshotBuilder_Produces_Correct_Snapshot()
        {
            // DoD(1): exaktes Button-Drücken → korrekter ControllerSnapshot.
            // Schließt die Kette end-to-end: FakeProvider → ParsedInput → SnapshotBuilder.Build → ControllerSnapshot.
            var queue = new InputCommandQueue();
            var tracker = new Core.WindowTracker();
            var feedback = new NullFeedbackProvider();
            var autoTarget = new AutoTargetEngine(queue) { AutoAttackEnabled = false }; // Default=true → für Idle-Label aus

            var builder = new SnapshotBuilder(
                autoTarget,
                new MageEngine(queue, null!),
                new ComboEngine(queue),
                tracker,
                new CursorEngine(tracker, queue),
                new SmartCursorService(queue, tracker, feedback));

            // Frame 0: idle → Frame 1: A + L1 + Stick rechts (ly=0.5) drücken
            var p = new FakeControllerProvider()
                .ScriptButton(GamepadButtonFlags.None)
                .ScriptFrame(GamepadButtonFlags.BtnA | GamepadButtonFlags.L1,
                    leftX: 0f, leftY: 0f, rightX: 0.25f, rightY: 0.5f);

            var step = new PipelineStep();
            step.Step(p); // Frame 0 konsumieren (idle)
            var f1 = step.Step(p); // Frame 1: A + L1 + Stick

            // Core-SnapshotBuilder (produktionsrelevant, EngineOrchestrator Zeile 516):
            // Build(ParsedInput, bool focusLocked, double elapsedMs)
            var snap = builder.Build(f1, focusLocked: false, elapsedMs: 8.0);

            Assert.True(snap.BtnA);   // Button-Property durchreicht bis zum Snapshot
            Assert.True(snap.L1);
            Assert.False(snap.R2);    // TriggerRight=0 → R2 false (XInput-Konvention)
            Assert.Equal(0.5f, snap.RightY);
            Assert.Equal(0.25f, snap.RightX);
            Assert.Equal("IDLE", snap.StateLabel); // keine Engine aktiv → Idle-Label
        }

        [Fact]
        public void Provider_StartsDisconnected_AndDisposesCleanly()
        {
            var p = new FakeControllerProvider();
            Assert.False(p.IsConnected);
            Assert.Equal("FAKE0001", p.ControllerGuid);
            Assert.False(p.IsDisposed);

            using (p) { }
            Assert.True(p.IsDisposed); // saubere Freigabe (MEMORY-001: Zero Resource Leak)
        }
    }
}
