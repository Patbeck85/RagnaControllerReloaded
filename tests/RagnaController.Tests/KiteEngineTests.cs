using System;
using Xunit;
using RagnaController.Core;
using RagnaController.Models;

namespace RagnaController.Tests
{
    public class KiteEngineTests
    {
        private static InputCommandQueue CreateQueue() => new InputCommandQueue();

        [Fact]
        public void KiteEngine_Constructor_Works()
        {
            // Act
            var engine = new KiteEngine(CreateQueue());

            // Assert
            Assert.NotNull(engine);
        }

        [Fact]
        public void KiteEngine_ToggleKiteMode_Activates()
        {
            // Arrange
            var engine = new KiteEngine(CreateQueue());

            // Act
            engine.ToggleKiteMode();

            // Assert - should be active
            Assert.True(engine.IsActive);
        }

        [Fact]
        public void KiteEngine_ToggleKiteMode_Deactivates()
        {
            // Arrange
            var engine = new KiteEngine(CreateQueue());

            // Act - Toggle on
            engine.ToggleKiteMode();
            // Act - Toggle off
            engine.ToggleKiteMode();

            // Assert - should be inactive
            Assert.False(engine.IsActive);
        }

        [Fact]
        public void KiteEngine_Reset_RestoresState()
        {
            // Arrange
            var engine = new KiteEngine(CreateQueue());

            // Act - Toggle then reset
            engine.ToggleKiteMode();
            engine.Reset();

            // Assert - should be disabled and Idle
            Assert.False(engine.KiteEnabled);
            Assert.Equal("IDLE", engine.PhaseLabel);
        }
    }
}