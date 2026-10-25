using System;
using System.Collections.Generic;
using Xunit;
using RagnaController.Core;
using RagnaController.Core.Developer;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-2906: Unit-Tests für Epic 20 (Developer & Power-User Tools).
    /// Deckt LuaScriptingService, InputLatencyVisualizer, AutomatedTestRecorder ab.
    /// </summary>
    public class DevToolsTests
    {
        #region LuaScriptingService Tests

        [Fact]
        public void LuaScriptingService_EnableDisable_ShouldToggleState()
        {
            // Arrange
            var lua = new LuaScriptingService();

            // Act
            lua.Enable();
            Assert.True(lua.IsEnabled);
            lua.Disable();
            Assert.False(lua.IsEnabled);
        }

        [Fact]
        public void LuaScriptingService_RegisterScript_ShouldAddScript()
        {
            // Arrange
            var lua = new LuaScriptingService();
            lua.Enable();

            // Act
            var result = lua.RegisterScript("test_script", "print('hello')");

            // Assert
            Assert.True(result);
            Assert.Contains("test_script", lua.RegisteredScripts);
        }

        [Fact]
        public void LuaScriptingService_RegisterDuplicateScript_ShouldFail()
        {
            // Arrange
            var lua = new LuaScriptingService();
            lua.Enable();
            lua.RegisterScript("test_script", "print('hello')");

            // Act
            var result = lua.RegisterScript("test_script", "print('world')");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void LuaScriptingService_UnregisterScript_ShouldRemoveScript()
        {
            // Arrange
            var lua = new LuaScriptingService();
            lua.RegisterScript("test_script", "print('hello')");

            // Act
            var result = lua.UnregisterScript("test_script");

            // Assert
            Assert.True(result);
            Assert.DoesNotContain("test_script", lua.RegisteredScripts);
        }

        [Fact]
        public void LuaScriptingService_RegisterScript_WhenDisabled_ShouldStillWork()
        {
            // Arrange
            var lua = new LuaScriptingService();

            // Act
            var result = lua.RegisterScript("test_script", "print('hello')");

            // Assert
            Assert.True(result);
        }

        #endregion

        #region InputLatencyVisualizer Tests

        [Fact]
        public void InputLatencyVisualizer_MarkInput_ShouldRecordSample()
        {
            // Arrange
            var viz = new InputLatencyVisualizer();

            // Act
            viz.MarkInputReceived();
            viz.MarkInputProcessed();

            // Assert
            Assert.Equal(1, viz.SampleCount);
            Assert.NotNull(viz.LastLatencyMs);
        }

        [Fact]
        public void InputLatencyVisualizer_ProcessWithoutInput_ShouldNotRecord()
        {
            // Arrange
            var viz = new InputLatencyVisualizer();

            // Act
            viz.MarkInputProcessed();

            // Assert
            Assert.Equal(0, viz.SampleCount);
            Assert.Null(viz.LastLatencyMs);
        }

        [Fact]
        public void InputLatencyVisualizer_Reset_ShouldClearSamples()
        {
            // Arrange
            var viz = new InputLatencyVisualizer();
            viz.MarkInputReceived();
            viz.MarkInputProcessed();

            // Act
            viz.Reset();

            // Assert
            Assert.Equal(0, viz.SampleCount);
            Assert.Null(viz.LastLatencyMs);
        }

        [Fact]
        public void InputLatencyVisualizer_MultipleSamples_ShouldCalculateStats()
        {
            // Arrange
            var viz = new InputLatencyVisualizer();

            // Act - simulate different latencies
            viz.MarkInputReceived();
            System.Threading.Thread.Sleep(1);
            viz.MarkInputProcessed();

            viz.MarkInputReceived();
            System.Threading.Thread.Sleep(2);
            viz.MarkInputProcessed();

            // Assert
            Assert.Equal(2, viz.SampleCount);
            Assert.True(viz.AverageLatencyMs >= 0);
            Assert.True(viz.MaxLatencyMs >= viz.MinLatencyMs);
        }

        [Fact]
        public void InputLatencyVisualizer_MaxSamples_ShouldLimitCount()
        {
            // Arrange
            var viz = new InputLatencyVisualizer();
            viz.MaxSamples = 3;

            // Act
            for (int i = 0; i < 5; i++)
            {
                viz.MarkInputReceived();
                viz.MarkInputProcessed();
            }

            // Assert
            Assert.Equal(3, viz.SampleCount);
        }

        #endregion

        #region AutomatedTestRecorder Tests

        [Fact]
        public void AutomatedTestRecorder_StartStop_ShouldToggleState()
        {
            // Arrange
            var recorder = new AutomatedTestRecorder();

            // Act
            recorder.StartRecording();
            Assert.True(recorder.IsRecording);
            recorder.StopRecording();
            Assert.False(recorder.IsRecording);
        }

        [Fact]
        public void AutomatedTestRecorder_RecordAction_ShouldAddAction()
        {
            // Arrange
            var recorder = new AutomatedTestRecorder();
            recorder.StartRecording();

            // Act
            recorder.RecordAction("buttonclick", "BtnA");

            // Assert
            Assert.Single(recorder.RecordedActions);
            Assert.Equal("buttonclick", recorder.RecordedActions[0].ActionType);
            Assert.Equal("BtnA", recorder.RecordedActions[0].Target);
        }

        [Fact]
        public void AutomatedTestRecorder_RecordAction_WhenNotRecording_ShouldIgnore()
        {
            // Arrange
            var recorder = new AutomatedTestRecorder();
            // Not started

            // Act
            recorder.RecordAction("buttonclick", "BtnA");

            // Assert
            Assert.Empty(recorder.RecordedActions);
        }

        [Fact]
        public void AutomatedTestRecorder_GenerateTestCode_ShouldContainTestStructure()
        {
            // Arrange
            var recorder = new AutomatedTestRecorder();
            recorder.StartRecording();
            recorder.RecordAction("buttonclick", "BtnA");
            recorder.RecordAction("keydown", "VirtualKey.A");

            // Act
            var code = recorder.GenerateTestCode("TestClass", "TestMethod");

            // Assert
            Assert.Contains("[Fact]", code);
            Assert.Contains("public void TestMethod()", code);
            Assert.Contains("queue.LeftClick()", code);
            Assert.Contains("queue.KeyDown(VirtualKey.VirtualKey.A)", code);
            Assert.Contains("Assert.True(queue != null);", code);
        }

        [Fact]
        public void AutomatedTestRecorder_Clear_ShouldRemoveAllActions()
        {
            // Arrange
            var recorder = new AutomatedTestRecorder();
            recorder.StartRecording();
            recorder.RecordAction("buttonclick", "BtnA");

            // Act
            recorder.Clear();

            // Assert
            Assert.Empty(recorder.RecordedActions);
        }

        #endregion
    }
}
