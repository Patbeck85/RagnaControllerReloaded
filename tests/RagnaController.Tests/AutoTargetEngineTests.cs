using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class AutoTargetEngineTests
    {
        [Fact]
        public void AutoTargetEngine_Constructor_Works()
        {
            // Arrange
            var queue = new InputCommandQueue();

            // Act
            var engine = new AutoTargetEngine(queue);

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public void AutoTargetEngine_ToggleCombatMode_ChangesState()
        {
            // Arrange
            var queue = new InputCommandQueue();
            var engine = new AutoTargetEngine(queue);

            // Act
            engine.ToggleCombatMode();

            // Assert - state should change from IDLE
            Assert.NotEqual("IDLE", engine.StateLabel);
        }

        [Fact]
        public void AutoTargetEngine_ToggleCombatMode_TogglesBack()
        {
            // Arrange
            var queue = new InputCommandQueue();
            var engine = new AutoTargetEngine(queue);

            // Act - Toggle on
            engine.ToggleCombatMode();
            // Act - Toggle off
            engine.ToggleCombatMode();

            // Assert - should be back to Idle
            Assert.Equal("IDLE", engine.StateLabel);
        }

        [Fact]
        public void AutoTargetEngine_Reset_RestoresState()
        {
            // Arrange
            var queue = new InputCommandQueue();
            var engine = new AutoTargetEngine(queue);

            // Act - Toggle then reset
            engine.ToggleCombatMode();
            engine.Reset();

            // Assert - should be Idle
            Assert.Equal("IDLE", engine.StateLabel);
        }
    }
}