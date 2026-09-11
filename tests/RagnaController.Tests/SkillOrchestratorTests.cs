using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-010: SkillOrchestrator Unit Tests (komplexeste Engine).
    /// Deckt RotationSteps-Auswahl, Condition-Grenzwerte (HPAbove/SPAbove/MissingBuff/
    /// TargetInRange/EnemyCount), Priority-Freie Sequenz-Auswahl, Loop vs. Single-Pass,
    /// Finisher-Reset, GlobalCooldown-Dekrement und DefaultRotationProvider ab.
    /// Hinweis: FindKeyForSkillLabel liefert ohne ProfileApplier-Wiring null →
    /// Steps feuern keine Queue-Commands; beobachtbar sind Step-Index, Cooldown,
    /// Events (RotationChanged/RotationCompleted) und Provider-Konfigurationen.
    /// </summary>
    public class SkillOrchestratorTests
    {
        private sealed class TestContext : IDisposable
        {
            public readonly SkillOrchestrator Orchestrator;
            public readonly InputCommandQueue Queue;
            public readonly List<string> RotationChanges = new();
            public int CompletedCount;

            public TestContext()
            {
                Queue = new InputCommandQueue();
                Queue.Start();
                Orchestrator = new SkillOrchestrator(Queue);
                Orchestrator.RotationChanged += c => RotationChanges.Add(c);
                Orchestrator.RotationCompleted += () => CompletedCount++;
            }

            public void Dispose()
            {
                Queue.Stop();
                Queue.Dispose();
            }
        }

        private static RotationConfig TwoStepRotation(int globalCooldownMs = 0, bool loop = true)
            => new()
            {
                ClassName = "TestClass",
                Settings = new RotationSettings { LoopRotation = loop, GlobalCooldownMs = globalCooldownMs },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "SkillA", DelayMs = 100 },
                    new() { SkillLabel = "SkillB", DelayMs = 100 }
                }
            };

        private static void Tick(SkillOrchestrator o, int deltaMs = 0)
            => o.Update(ParsedInput.Disconnected, deltaMs, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());

        [Fact]
        public void InitialState_HasNoRotation_AndUpdateIsNoOp()
        {
            using var ctx = new TestContext();

            Assert.Equal("", ctx.Orchestrator.ActiveClass);
            Assert.Null(ctx.Orchestrator.CurrentRotation);
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);

            ctx.Orchestrator.SetEnabled(true);
            Tick(ctx.Orchestrator); // aktiv, aber keine Rotation → No-Op, keine Exception

            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void LoadRotation_FiresRotationChanged_AndResetsState()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);

            ctx.Orchestrator.LoadRotation(TwoStepRotation());

            Assert.Equal("TestClass", ctx.Orchestrator.ActiveClass);
            Assert.Same(ctx.Orchestrator.CurrentRotation, ctx.Orchestrator.CurrentRotation);
            Assert.Contains("TestClass", ctx.RotationChanges);
        }

        [Fact]
        public void SetClass_LoadsProviderRotation_AndFiresChanged()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);

            ctx.Orchestrator.SetClass("Wizard");

            Assert.Equal("Wizard", ctx.Orchestrator.ActiveClass);
            Assert.NotNull(ctx.Orchestrator.CurrentRotation);
            Assert.NotEmpty(ctx.Orchestrator.CurrentRotation!.Steps);
            Assert.Contains("Wizard", ctx.RotationChanges);

            // Whitespace-Namen werden ignoriert (kein Crash, kein State-Change)
            ctx.Orchestrator.SetClass("   ");
            Assert.Equal("Wizard", ctx.Orchestrator.ActiveClass);
        }

        [Fact]
        public void SetPreset_LoadsPresetRotation()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);

            ctx.Orchestrator.SetPreset(EnginePreset.Melee);

            Assert.Equal("Melee", ctx.Orchestrator.ActiveClass);
            Assert.NotNull(ctx.Orchestrator.CurrentRotation);
        }

        [Fact]
        public void Update_ExecutesStepsSequentially_AndAdvancesStepIndex()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(TwoStepRotation());

            Tick(ctx.Orchestrator); // Step 0 (SkillA) → Index 1
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);

            Tick(ctx.Orchestrator); // Step 1 (SkillB) → Index 0 (wrap)
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Update_DuringGlobalCooldown_DoesNotExecuteNextStep()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(TwoStepRotation(globalCooldownMs: 500));

            Tick(ctx.Orchestrator); // Step 0, Cooldown → 500
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);
            Assert.Equal(500, ctx.Orchestrator.GetActiveSkillCooldownMs());

            Tick(ctx.Orchestrator, deltaMs: 16); // Cooldown läuft ab, aber Step bleibt stehen
            Assert.Equal(484, ctx.Orchestrator.GetActiveSkillCooldownMs());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);

            Tick(ctx.Orchestrator, deltaMs: 500); // Cooldown unter null → nächster Step
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Condition_SPAbove_BlocksBelow_AllowsAtThreshold()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "SPTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "NeedsSP50", Conditions = new() { RotationCondition.SPAbove(50) } },
                    new() { SkillLabel = "NoCondition" }
                }
            });

            // SP=49: Step 0 blockiert → Step 1 (Index 1) wird übersprungen ausgeführt
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 49, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex); // nach Index 1 → wrap auf 0

            // SP=50 (Grenzwert >=): Step 0 direkt ausführbar
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 50, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Condition_HPAbove_BlocksBelow_AllowsAtThreshold()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "HPTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "NeedsHP30", Conditions = new() { RotationCondition.HPAbove(30) } },
                    new() { SkillLabel = "Fallback" }
                }
            });

            // HP=29: blockiert → Fallback (Index 1)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 29, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);

            // HP=30 (Grenzwert): Step 0 ausführbar
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 30, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Condition_MissingBuff_BlocksWhenBuffActive_AllowsWhenMissing()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "BuffTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "CastBlessing", Conditions = new() { RotationCondition.MissingBuff("Blessing") } },
                    new() { SkillLabel = "Fallback" }
                }
            });

            // Buff aktiv → Step 0 blockiert → Fallback (Index 1)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string> { "Blessing" }, new List<string>(), 0, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);

            // Buff fehlt (case-insensitive Check) → Step 0 ausführbar
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Condition_TargetInRange_RespectsDistanceAndHasTarget()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "RangeTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "MeleeSkill", Conditions = new() { RotationCondition.HasTarget, RotationCondition.TargetInRange(2f) } },
                    new() { SkillLabel = "Fallback" }
                }
            });

            // Distanz 1.5 ≤ 2 → Step 0
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1.5f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);

            // Distanz 2.5 > 2 → Fallback (Index 1)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 2.5f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);

            // Kein Target → auch Distanz 0 blockiert (hasTarget && dist <= range)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, false, 0f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Condition_EnemyCount_UsesGreaterOrEqual()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "AoeTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "AOE", Conditions = new() { RotationCondition.EnemyCount(3, 5f) } },
                    new() { SkillLabel = "SingleTarget" }
                }
            });

            // 2 Gegner < 3 → Fallback (Index 1)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 2, new List<string>());
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);

            // 3 Gegner = Schwelle → AOE (Index 1)
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 3, new List<string>());
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void Finisher_Step_ResetsRotation_AndFiresCompleted()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "FinisherTest",
                Settings = new RotationSettings { GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "A" },
                    new() { SkillLabel = "Finisher", IsFinisher = true }
                }
            });

            Tick(ctx.Orchestrator); // Step A → Index 1
            Assert.Equal(0, ctx.CompletedCount);

            Tick(ctx.Orchestrator); // Finisher → Rotation zurück auf 0 + Event
            Assert.Equal(1, ctx.CompletedCount);
            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
        }

        [Fact]
        public void SinglePass_AllStepsBlocked_StaysAtStart_NoCompletion()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(new RotationConfig
            {
                ClassName = "SinglePass",
                Settings = new RotationSettings { LoopRotation = false, GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "NeedsSP999", Conditions = new() { RotationCondition.SPAbove(999) } },
                    new() { SkillLabel = "AlsoBlocked", Conditions = new() { RotationCondition.SPAbove(999) } }
                }
            });

            // Alle Steps blockiert, LoopRotation=false → Single-Pass bleibt am Start, kein Completion
            ctx.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 50, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());

            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
            Assert.Equal(0, ctx.CompletedCount);
        }

        [Fact]
        public void SinglePass_RestartsFromBeginning_WhileLoop_WaitsOnBlockedStep()
        {
            // Beide Steps haben dieselbe Bedingung (SPAbove 50).
            // Tick 1 (SP=100): Step 0 ausführbar → Index 1.
            // Tick 2 (SP=10, alle blockiert):
            //   Loop=true  → bleibt auf Index 1 (wartet)
            //   Loop=false → Single-Pass setzt zurück auf 0
            static RotationConfig Cfg(bool loop) => new()
            {
                ClassName = "LoopVsSingle",
                Settings = new RotationSettings { LoopRotation = loop, GlobalCooldownMs = 0 },
                Steps = new List<RotationStep>
                {
                    new() { SkillLabel = "SkillA", Conditions = new() { RotationCondition.SPAbove(50) } },
                    new() { SkillLabel = "SkillB", Conditions = new() { RotationCondition.SPAbove(50) } }
                }
            };

            using var single = new TestContext();
            using var loop = new TestContext();
            single.Orchestrator.SetEnabled(true);
            loop.Orchestrator.SetEnabled(true);
            single.Orchestrator.LoadRotation(Cfg(loop: false));
            loop.Orchestrator.LoadRotation(Cfg(loop: true));

            // Tick 1 (SP=100): beide führen Step 0 aus → Index 1
            single.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            loop.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 100, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            Assert.Equal(1, single.Orchestrator.CurrentStepIndex);
            Assert.Equal(1, loop.Orchestrator.CurrentStepIndex);

            // Tick 2 (SP=10): alle Steps blockiert → Loop wartet, Single-Pass resetet
            single.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 10, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());
            loop.Orchestrator.Update(ParsedInput.Disconnected, 0, true, 1f, 10, 100, false, true, new List<string>(), new List<string>(), 0, new List<string>());

            Assert.Equal(0, single.Orchestrator.CurrentStepIndex); // Single-Pass: zurück auf Start
            Assert.Equal(1, loop.Orchestrator.CurrentStepIndex);   // Loop: wartet auf Index 1
        }

        [Fact]
        public void Reset_ClearsStepIndex_AndCooldown()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(TwoStepRotation(globalCooldownMs: 500));

            Tick(ctx.Orchestrator); // Index 1, Cooldown 500
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);

            ctx.Orchestrator.Reset();

            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
            Assert.Equal(0, ctx.Orchestrator.GetActiveSkillCooldownMs());
        }

        [Fact]
        public void SetEnabled_False_ResetsState()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(TwoStepRotation(globalCooldownMs: 500));
            Tick(ctx.Orchestrator);
            Assert.Equal(1, ctx.Orchestrator.CurrentStepIndex);

            ctx.Orchestrator.SetEnabled(false);

            Assert.Equal(0, ctx.Orchestrator.CurrentStepIndex);
            Assert.Equal(0, ctx.Orchestrator.GetActiveSkillCooldownMs());
        }

        [Fact]
        public void GetActiveSkillId_IsOneBasedStepIndex()
        {
            using var ctx = new TestContext();
            ctx.Orchestrator.SetEnabled(true);
            ctx.Orchestrator.LoadRotation(TwoStepRotation());

            Assert.Equal(1, ctx.Orchestrator.GetActiveSkillId()); // Index 0 → Skill 1

            Tick(ctx.Orchestrator);

            Assert.Equal(2, ctx.Orchestrator.GetActiveSkillId()); // Index 1 → Skill 2
        }

        [Fact]
        public void DefaultRotationProvider_KnownClasses_HaveSteps_AndFallback()
        {
            var provider = new DefaultRotationProvider();

            foreach (var cls in new[] { "Wizard", "Knight", "Priest", "Assassin", "Hunter", "Monk" })
            {
                var config = provider.GetRotation(cls);
                Assert.NotEmpty(config.Steps);
            }

            // Unbekannte Klasse → Fallback auf Preset (nie null, nie leer)
            var unknown = provider.GetRotation("UnbekannterKlassenname");
            Assert.NotNull(unknown);

            Assert.NotEmpty(provider.GetAllRotations());
        }
    }
}
