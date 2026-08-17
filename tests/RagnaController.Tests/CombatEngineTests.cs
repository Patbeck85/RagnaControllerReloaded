using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class CombatEngineTests
    {
        [Fact]
        public void CombatEngine_Constructor_Works()
        {
            // Arrange
            var tracker = new Core.WindowTracker();
            var queue = new InputCommandQueue();

            // Act
            var engine = new CombatEngine(tracker, queue);

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public void CombatEngine_UpdateLayers_Processes()
        {
            // Arrange
            var tracker = new Core.WindowTracker();
            var queue = new InputCommandQueue();
            var engine = new CombatEngine(tracker, queue);

            // Act
            engine.UpdateLayers(true, false, false, false);

            // Assert - should not throw
            Assert.NotNull(engine);
        }

        [Fact]
        public void CombatEngine_ProcessButton_Processes()
        {
            // Arrange
            var tracker = new Core.WindowTracker();
            var queue = new InputCommandQueue();
            var engine = new CombatEngine(tracker, queue);

            // Act
            engine.ProcessButton("A", true, 100);

            // Assert - should not throw
            Assert.NotNull(engine);
        }
    }
}