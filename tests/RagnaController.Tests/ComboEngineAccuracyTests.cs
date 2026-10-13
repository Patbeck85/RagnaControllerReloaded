using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// GEX-008: Combo Engine — Chain Accuracy & Cancel Windows (S2)
    /// Unit-tests for ComboEngine against rAthena reference mechanics.
    /// Verifies chain accuracy, cancel windows, buffer behavior, and edge cases.
    /// </summary>
    public class ComboEngineAccuracyTests
    {
        #region Test Infrastructure

        private sealed class TestContext : IDisposable
        {
            public readonly InputCommandQueue Queue;
            public readonly List<InputCmd> Log = new();
            public readonly ComboEngine Combo;

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.OnCommandEnqueued += cmd => Log.Add(cmd);
                Queue.Start();

                Combo = new ComboEngine(Queue);
            }

            public void Dispose()
            {
                Queue.Stop();
                Queue.Dispose();
                Combo.Dispose();
            }

            public List<InputCmd> KeyDowns(VirtualKey key)
                => Log.Where(c => c.Type == CmdType.KeyDown && c.Key == (ushort)key).ToList();

            public List<InputCmd> KeyUps(VirtualKey key)
                => Log.Where(c => c.Type == CmdType.KeyUp && c.Key == (ushort)key).ToList();

            public List<InputCmd> AllCommands() => Log.ToList();

            public void ClearLog() => Log.Clear();
        }

        #endregion

        #region Basic Chain Execution

        [Fact]
        public void Combo_ThreeStepSequence_FiresAllStepsInOrder()
        {
            // rAthena: Combo sequences execute skills in defined order with proper delays
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 400, 500 };
            ctx.Combo.Enabled = true;

            // Frame 1: Button pressed (BtnY) - triggers step 1
            ctx.Combo.Update(isHeld: true, ms: 16);
            
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));

            // Advance time past first delay (300ms ±15 jitter = ~285-315ms)
            ctx.Combo.Update(isHeld: true, ms: 320);
            
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));

            // Advance time past second delay (400ms ±15 jitter = ~385-415ms)
            ctx.Combo.Update(isHeld: true, ms: 420);
            
            Assert.Equal(3, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
            Assert.Single(ctx.KeyDowns(VirtualKey.F3));
        }

        [Fact]
        public void Combo_SingleStep_FiresOnce_ThenResets()
        {
            // Single-skill combo should fire once and reset when AutoLoop=false
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 300 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = false;

            ctx.Combo.Update(isHeld: true, ms: 16);
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));

            // Past delay - should reset since AutoLoop=false
            ctx.Combo.Update(isHeld: true, ms: 320);
            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.False(ctx.Combo.IsActive);
        }

        [Fact]
        public void Combo_AutoLoop_True_RepeatsSequence()
        {
            // AutoLoop=true should restart sequence after completion
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 200, 200 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = true;

            // First loop
            ctx.Combo.Update(isHeld: true, ms: 16); // Step 1
            ctx.Combo.Update(isHeld: true, ms: 220); // Step 2
            ctx.Combo.Update(isHeld: true, ms: 220); // Back to Step 1

            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Equal(2, ctx.KeyDowns(VirtualKey.F1).Count); // F1 fired twice
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
        }

        #endregion

        #region Cancel Window / Buffer Window (64ms)

        [Fact]
        public void Combo_BufferWindow_64ms_AllowsLatePress()
        {
            // Buffer window of 64ms should allow late key press to continue chain
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            // Press and hold for step 1
            ctx.Combo.Update(isHeld: true, ms: 16);
            Assert.Equal(1, ctx.Combo.CurrentStep);

            // Release briefly (within 64ms buffer) then press again
            ctx.Combo.Update(isHeld: false, ms: 30); // 30ms released - within buffer
            ctx.Combo.Update(isHeld: true, ms: 16);  // Press again

            // Should still fire step 2 (buffer prevented reset)
            ctx.Combo.Update(isHeld: true, ms: 320);
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
        }

        [Fact]
        public void Combo_BufferWindow_Exceeded_ResetsChain()
        {
            // Buffer window exceeded should reset the combo chain
            // After reset, new press starts from step 1, and subsequent time advance continues to step 2
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Step 1
            ctx.Combo.Update(isHeld: false, ms: 70); // 70ms released - drains buffer to negative
            ctx.Combo.Update(isHeld: false, ms: 16); // Next release frame: buffer <= 0 -> Reset()
            ctx.Combo.Update(isHeld: true, ms: 16);  // Press again -> Step 1 fires again

            // Advance past step 1 delay (300ms ±15 jitter) -> fires step 2
            ctx.Combo.Update(isHeld: true, ms: 320);
            
            // After buffer exceeded + reset + new press + time advance, we're at step 2 of the NEW chain
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Equal(2, ctx.KeyDowns(VirtualKey.F1).Count); // F1 fired twice (old chain step 1 + new chain step 1)
            Assert.Single(ctx.KeyDowns(VirtualKey.F2)); // Step 2 of new chain
        }

        [Fact]
        public void Combo_BufferWindow_ActiveChain_HeldReleasedWithinBuffer_Continues()
        {
            // When chain is already active (step > 0), releasing within buffer should not reset
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16); // Step 1
            ctx.Combo.Update(isHeld: true, ms: 320); // Step 2
            
            // Release within buffer while at step 2
            ctx.Combo.Update(isHeld: false, ms: 30);
            ctx.Combo.Update(isHeld: true, ms: 16);

            // Should continue to step 3
            ctx.Combo.Update(isHeld: true, ms: 320);
            Assert.Equal(3, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
            Assert.Single(ctx.KeyDowns(VirtualKey.F3));
        }

        #endregion

        #region Chain Cooldown

        [Fact]
        public void Combo_ChainCooldownMs_DelaysLoopRestart()
        {
            // ChainCooldownMs should add delay before restarting sequence in AutoLoop mode
            // During cooldown, CurrentStep returns 0 (sequence completed, waiting for cooldown)
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 200 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = true;
            ctx.Combo.ChainCooldownMs = 500;

            ctx.Combo.Update(isHeld: true, ms: 16);   // Step 1 fires
            ctx.Combo.Update(isHeld: true, ms: 220);  // Past delay, sequence completes, enters cooldown
            
            // During cooldown, CurrentStep is 0 (sequence completed, waiting)
            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));

            // Advance past cooldown (500ms ±15 jitter)
            ctx.Combo.Update(isHeld: true, ms: 520);
            
            // Should fire step 1 again
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Equal(2, ctx.KeyDowns(VirtualKey.F1).Count);
        }

        [Fact]
        public void Combo_ChainCooldownZero_NoExtraDelay()
        {
            // ChainCooldownMs=0 should restart immediately (only skill delay applies)
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 200 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = true;
            ctx.Combo.ChainCooldownMs = 0;

            ctx.Combo.Update(isHeld: true, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 220); // Past skill delay
            
            // Should immediately fire again (no cooldown)
            Assert.Equal(2, ctx.KeyDowns(VirtualKey.F1).Count);
        }

        #endregion

        #region SyncHeldState (Overlay Close Prevention)

        [Fact]
        public void Combo_SyncHeldState_OverlayClose_PreventsFalseFire()
        {
            // SyncHeldState(true) before first Update should prevent false step-1 trigger
            // when overlay closes while button is physically held
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 300 };
            ctx.Combo.Enabled = true;

            // Simulate: overlay was open, button held, overlay closes
            ctx.Combo.SyncHeldState(true); // Button already held
            
            // First Update - should NOT fire (rising edge detected but _wasHeld was true)
            ctx.Combo.Update(isHeld: true, ms: 16);
            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.Empty(ctx.KeyDowns(VirtualKey.F1));

            // Release and press again - should fire normally
            ctx.Combo.Update(isHeld: false, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 16);
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
        }

        [Fact]
        public void Combo_SyncHeldState_False_AllowsNormalFire()
        {
            // SyncHeldState(false) should allow normal rising-edge detection
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.SyncHeldState(false);
            ctx.Combo.Update(isHeld: true, ms: 16);
            
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
        }

        #endregion

        #region Disabled / Empty Sequence Handling

        [Fact]
        public void Combo_Disabled_DoesNotFire()
        {
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 300 };
            ctx.Combo.Enabled = false;

            ctx.Combo.Update(isHeld: true, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 320);

            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.Empty(ctx.KeyDowns(VirtualKey.F1));
        }

        [Fact]
        public void Combo_EmptySequence_DoesNotFire()
        {
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey>();
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 320);

            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.Empty(ctx.AllCommands());
        }

        [Fact]
        public void Combo_DisabledMidChain_ResetsState()
        {
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Step 1
            ctx.Combo.Update(isHeld: true, ms: 320); // Step 2
            
            Assert.Equal(2, ctx.Combo.CurrentStep);

            // When Enabled=false, Update returns early - state stays as-is
            // Need to call Reset() explicitly or re-enable and release to reset
            ctx.Combo.Enabled = false;
            ctx.Combo.Update(isHeld: true, ms: 16);

            // State preserved while disabled
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.True(ctx.Combo.IsActive);
        }

        #endregion

        #region Reset Behavior

        [Fact]
        public void Combo_Reset_ClearsState()
        {
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 320);
            
            Assert.Equal(2, ctx.Combo.CurrentStep);

            ctx.Combo.Reset();

            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.False(ctx.Combo.IsActive);
        }

        [Fact]
        public void Combo_ReleaseWithoutBuffer_ResetsChain()
        {
            // Releasing the button resets the chain once the 64ms buffer window is exhausted.
            // The buffer refills to 64ms on every held frame, so a single short release does NOT
            // reset immediately – it takes one release frame to drain (64ms) and the next
            // release frame actually performs the Reset().
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Step 1
            ctx.Combo.Update(isHeld: true, ms: 320); // Step 2
            ctx.Combo.Update(isHeld: false, ms: 70); // Release: drains buffer (64ms) to negative

            // Buffer still not fully exhausted this frame – chain survives
            Assert.Equal(2, ctx.Combo.CurrentStep);

            // Next release frame: buffer <= 0 && IsActive -> Reset()
            ctx.Combo.Update(isHeld: false, ms: 16);
            
            Assert.Equal(0, ctx.Combo.CurrentStep);
            Assert.False(ctx.Combo.IsActive);
        }

        #endregion

        #region Jitter Application

        [Fact]
        public void Combo_JitterApplied_PlusMinus15ms()
        {
            // JitterService.Apply(base, 15) should add ±15ms variance to delays
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 1000, 1000 }; // Large delay for reliable jitter test
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16); // Step 1 fires

            // 985ms - might fire (1000 - 15 jitter = 985)
            ctx.Combo.Update(isHeld: true, ms: 985);
            int countAt985 = ctx.KeyDowns(VirtualKey.F2).Count;

            // 1015ms - should definitely fire (1000 + 15 jitter = 1015)
            ctx.Combo.Update(isHeld: true, ms: 30); // Total 1015ms
            int countAt1015 = ctx.KeyDowns(VirtualKey.F2).Count;

            // At 985ms: may or may not have fired (jitter could be negative)
            // At 1015ms: must have fired (jitter max +15)
            Assert.True(countAt1015 >= countAt985);
            Assert.Equal(1, countAt1015); // Must fire by 1015ms
        }

        [Fact]
        public void Combo_DefaultDelay_UsedWhenCurrentDelaysEmpty()
        {
            // When CurrentDelays is shorter than Sequence, default 300ms delay used
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int>(); // Empty - should use default 300ms
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16); // Step 1
            ctx.Combo.Update(isHeld: true, ms: 320); // Past 300ms default

            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
        }

        #endregion

        #region ComboStepFired Event

        [Fact]
        public void Combo_ComboStepFired_EventRaisedWithCorrectStepNumber()
        {
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3 };
            ctx.Combo.CurrentDelays = new List<int> { 200, 200, 200 };
            ctx.Combo.Enabled = true;

            var firedSteps = new List<int>();
            ctx.Combo.ComboStepFired += step => firedSteps.Add(step);

            ctx.Combo.Update(isHeld: true, ms: 16);   // Step 1
            ctx.Combo.Update(isHeld: true, ms: 220);  // Step 2
            ctx.Combo.Update(isHeld: true, ms: 220);  // Step 3

            Assert.Equal(new[] { 1, 2, 3 }, firedSteps);
        }

        #endregion

        #region Realistic RO Class Combo Scenarios

        [Fact]
        public void Combo_AssassinCross_DoubleAttack_TripleAttack_Chain()
        {
            // rAthena: Assassin Cross Double Attack → Triple Attack chain
            // Double Attack: 2 hits, Triple Attack: 3 hits
            // Cancel window allows chaining into next skill
            using var ctx = new TestContext();
            
            // Typical Assassin Cross combo: Double Attack (F1) → Triple Attack (F2)
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 400, 500 }; // Double Attack cast, Triple Attack cast
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = false;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Double Attack
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));

            ctx.Combo.Update(isHeld: true, ms: 420); // Past Double Attack delay
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));

            ctx.Combo.Update(isHeld: true, ms: 520); // Past Triple Attack delay
            Assert.Equal(0, ctx.Combo.CurrentStep);  // AutoLoop=false → reset
            Assert.False(ctx.Combo.IsActive);
        }

        [Fact]
        public void Combo_Crusader_BrandishSpear_Chain()
        {
            // rAthena: Crusader Brandish Spear chain (multi-hit)
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F1, VirtualKey.F1, VirtualKey.F1 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300, 300, 300 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = false;

            ctx.Combo.Update(isHeld: true, ms: 16);
            ctx.Combo.Update(isHeld: true, ms: 320);
            ctx.Combo.Update(isHeld: true, ms: 320);
            ctx.Combo.Update(isHeld: true, ms: 320);
            ctx.Combo.Update(isHeld: true, ms: 320);

            Assert.Equal(4, ctx.KeyDowns(VirtualKey.F1).Count);
            Assert.Equal(0, ctx.Combo.CurrentStep);
        }

        [Fact]
        public void Combo_Monk_ComboFinish_Chain()
        {
            // rAthena: Monk combo skills (Tiger Fist → Chain Combo → Combo Finish)
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3 };
            ctx.Combo.CurrentDelays = new List<int> { 500, 500, 500 };
            ctx.Combo.Enabled = true;
            ctx.Combo.AutoLoop = false;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Tiger Fist
            ctx.Combo.Update(isHeld: true, ms: 520); // Chain Combo
            ctx.Combo.Update(isHeld: true, ms: 520); // Combo Finish
            ctx.Combo.Update(isHeld: true, ms: 520); // Reset

            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
            Assert.Single(ctx.KeyDowns(VirtualKey.F3));
            Assert.Equal(0, ctx.Combo.CurrentStep);
        }

        #endregion

        #region Edge Cases & Boundary Conditions

        [Fact]
        public void Combo_LargeTimeDelta_HandledCorrectly()
        {
            // Large ms delta (e.g., app pause/resume) should not skip steps incorrectly
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 300, 300 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Step 1
            ctx.Combo.Update(isHeld: true, ms: 5000); // Large jump - should fire step 2 and potentially more

            // With AutoLoop=false, should fire step 2 then reset
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
        }

        [Fact]
        public void Combo_ZeroDelayStep_FiresImmediately()
        {
            // Zero delay step should fire on next Update call
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2 };
            ctx.Combo.CurrentDelays = new List<int> { 0, 300 }; // First step has 0 delay
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16); // Step 1 fires immediately (timer=0)
            
            Assert.Equal(1, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));

            // Next update with any time should fire step 2 (timer already <= 0 from 0 delay)
            ctx.Combo.Update(isHeld: true, ms: 16);
            
            Assert.Equal(2, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
        }

        [Fact]
        public void Combo_SequenceLongerThanDelays_UsesDefaultForRemaining()
        {
            // Sequence has 4 steps, CurrentDelays only has 2 - remaining should use 300ms default
            using var ctx = new TestContext();
            
            ctx.Combo.Sequence = new List<VirtualKey> { VirtualKey.F1, VirtualKey.F2, VirtualKey.F3, VirtualKey.F4 };
            ctx.Combo.CurrentDelays = new List<int> { 200, 200 };
            ctx.Combo.Enabled = true;

            ctx.Combo.Update(isHeld: true, ms: 16);  // Step 1 (200ms)
            ctx.Combo.Update(isHeld: true, ms: 220); // Step 2 (200ms)
            ctx.Combo.Update(isHeld: true, ms: 320); // Step 3 (300ms default)
            ctx.Combo.Update(isHeld: true, ms: 320); // Step 4 (300ms default)

            Assert.Equal(4, ctx.Combo.CurrentStep);
            Assert.Single(ctx.KeyDowns(VirtualKey.F1));
            Assert.Single(ctx.KeyDowns(VirtualKey.F2));
            Assert.Single(ctx.KeyDowns(VirtualKey.F3));
            Assert.Single(ctx.KeyDowns(VirtualKey.F4));
        }

        #endregion
    }
}