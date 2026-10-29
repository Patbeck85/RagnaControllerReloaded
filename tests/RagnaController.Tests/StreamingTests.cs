using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using RagnaController.Core.Services;
using RagnaController.Core.Services.Developer;
using RagnaController.Core.Services.Streaming;

namespace RagnaController.Tests
{
    /// <summary>
    /// TASK-3205: Unit-Tests für Epic 23 (Streaming / Content Creator Features).
    /// Deckt StreamDeckService, ObsWebSocketService, ViewerInteractionService, ReplaySystem ab.
    /// </summary>
    public class StreamingTests
    {
        #region StreamDeckService Tests

        [Fact]
        public void StreamDeckService_RegisterButton_ShouldAddButton()
        {
            var service = new StreamDeckService();
            service.RegisterButton(1, "Profil 1", () => { });

            Assert.True(service.Buttons.ContainsKey(1));
            Assert.Equal("Profil 1", service.Buttons[1].Label);
        }

        [Fact]
        public void StreamDeckService_PressButton_ShouldInvokeAction()
        {
            var service = new StreamDeckService();
            bool pressed = false;
            service.RegisterButton(1, "Test", () => pressed = true);
            service.Connect();

            service.PressButton(1);

            Assert.True(pressed);
            Assert.True(service.Buttons[1].IsPressed);
        }

        [Fact]
        public void StreamDeckService_SetLedColor_ShouldChangeColor()
        {
            var service = new StreamDeckService();
            service.RegisterButton(1, "Test", () => { });

            service.SetLedColor(1, StreamDeckLedColor.Green);

            Assert.Equal(StreamDeckLedColor.Green, service.Buttons[1].LedColor);
        }

        [Fact]
        public void StreamDeckService_UnregisterButton_ShouldRemoveButton()
        {
            var service = new StreamDeckService();
            service.RegisterButton(1, "Test", () => { });

            service.UnregisterButton(1);

            Assert.False(service.Buttons.ContainsKey(1));
        }

        [Fact]
        public void StreamDeckService_ButtonPressedEvent_ShouldFire()
        {
            var service = new StreamDeckService();
            service.RegisterButton(1, "Test", () => { });
            service.Connect();

            StreamDeckButtonEvent? captured = null;
            service.ButtonPressed += (s, e) => captured = e;

            service.PressButton(1);

            Assert.NotNull(captured);
            Assert.Equal(1, captured.KeyIndex);
            Assert.Equal("Test", captured.Label);
        }

        [Fact]
        public void StreamDeckService_Dispose_ShouldDisconnect()
        {
            var service = new StreamDeckService();
            service.Connect();
            Assert.True(service.IsConnected);

            service.Dispose();
            Assert.False(service.IsConnected);
        }

        #endregion

        #region ViewerInteractionService Tests

        [Fact]
        public void ViewerInteractionService_RegisterCommand_ShouldAddCommand()
        {
            var service = new ViewerInteractionService();
            service.RegisterCommand("testcmd", "Test-Befehl", args => { });

            Assert.True(service.Commands.ContainsKey("testcmd"));
        }

        [Fact]
        public void ViewerInteractionService_ProcessChatMessage_ShouldExecuteCommand()
        {
            var service = new ViewerInteractionService();
            bool executed = false;
            service.RegisterCommand("testcmd", "Test", args => executed = true);

            var result = service.ProcessChatMessage("User1", "!testcmd");

            Assert.True(result);
            Assert.True(executed);
        }

        [Fact]
        public void ViewerInteractionService_ModeratorOnly_ShouldBlockNonModerator()
        {
            var service = new ViewerInteractionService();
            service.RegisterCommand("modcmd", "Mod-Befehl", args => { }, moderatorOnly: true);
            service.AddModerator("ModUser");

            var resultNonMod = service.ProcessChatMessage("User1", "!modcmd");
            var resultMod = service.ProcessChatMessage("ModUser", "!modcmd");

            Assert.False(resultNonMod);
            Assert.True(resultMod);
        }

        [Fact]
        public void ViewerInteractionService_Cooldown_ShouldBlockRapidCalls()
        {
            var service = new ViewerInteractionService();
            service.RegisterCommand("fastcmd", "Schnell", args => { }, cooldownSeconds: 10);

            var result1 = service.ProcessChatMessage("User1", "!fastcmd");
            var result2 = service.ProcessChatMessage("User1", "!fastcmd");

            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact]
        public void ViewerInteractionService_UnknownCommand_ShouldReturnFalse()
        {
            var service = new ViewerInteractionService();

            var result = service.ProcessChatMessage("User1", "!unknowncmd");

            Assert.False(result);
        }

        [Fact]
        public void ViewerInteractionService_CommandExecutedEvent_ShouldFire()
        {
            var service = new ViewerInteractionService();
            service.RegisterCommand("testcmd", "Test", args => { });

            ViewerCommandEvent? captured = null;
            service.CommandExecuted += (s, e) => captured = e;

            service.ProcessChatMessage("User1", "!testcmd");

            Assert.NotNull(captured);
            Assert.Equal("User1", captured.Username);
            Assert.Equal("testcmd", captured.Command);
        }

        #endregion

        #region ReplaySystem Tests

        [Fact]
        public void ReplaySystem_StartStopRecording_ShouldCreateSession()
        {
            var system = new ReplaySystem(Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests"));
            Directory.CreateDirectory(system.GetType().GetField("_recordingsDirectory", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!.ToString()!);

            var session = system.StartRecording("TestChar", "prontera", "Test-Notiz");

            Assert.True(system.IsRecording);
            Assert.Equal("TestChar", session.CharacterName);
            Assert.Equal("prontera", session.MapName);
            Assert.Equal("Test-Notiz", session.Notes);
        }

        [Fact]
        public void ReplaySystem_StopRecording_ShouldReturnSessionWithDuration()
        {
            var system = new ReplaySystem(Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests2"));

            system.StartRecording("TestChar", "payon");
            System.Threading.Thread.Sleep(50);
            var session = system.StopRecording();

            Assert.False(system.IsRecording);
            Assert.NotNull(session);
            Assert.True(session.DurationSeconds > 0);
        }

        [Fact]
        public void ReplaySystem_RecordInput_ShouldAddEvent()
        {
            var system = new ReplaySystem(Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests3"));
            system.StartRecording("TestChar", "geffen");

            system.RecordInput("buttonclick", "BtnA", 100.5);

            Assert.Single(system.CurrentSession!.InputEvents);
            Assert.Equal("buttonclick", system.CurrentSession.InputEvents[0].InputType);
            Assert.Equal("BtnA", system.CurrentSession.InputEvents[0].InputData);
        }

        [Fact]
        public void ReplaySystem_RecordState_ShouldAddEvent()
        {
            var system = new ReplaySystem(Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests4"));
            system.StartRecording("TestChar", "payon");

            system.RecordState("hp_changed", "50/100", 200.0);

            Assert.Single(system.CurrentSession!.StateEvents);
            Assert.Equal("hp_changed", system.CurrentSession.StateEvents[0].StateType);
        }

        [Fact]
        public void ReplaySystem_SaveAndLoadSession_ShouldRoundTrip()
        {
            var dir = Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests5");
            Directory.CreateDirectory(dir);
            var system = new ReplaySystem(dir);

            system.StartRecording("TestChar", "comodo", "Speicher-Test");
            system.RecordInput("keydown", "VirtualKey.A", 50.0);
            var session = system.StopRecording()!;

            var filePath = system.SaveSession(session);
            Assert.True(File.Exists(filePath));

            var loaded = system.LoadSession(filePath);
            Assert.NotNull(loaded);
            Assert.Equal(session.SessionId, loaded.SessionId);
            Assert.Equal("TestChar", loaded.CharacterName);
            Assert.Single(loaded.InputEvents);
        }

        [Fact]
        public void ReplaySystem_GetSavedSessions_ShouldReturnFiles()
        {
            var dir = Path.Combine(Path.GetTempPath(), $"RagnaController_ReplayTests6_{Guid.NewGuid():N}");
            Directory.CreateDirectory(dir);
            var system = new ReplaySystem(dir);

            system.StartRecording("Char1", "map1");
            var s1 = system.StopRecording()!;
            system.SaveSession(s1);
            system.StartRecording("Char2", "map2");
            var s2 = system.StopRecording()!;
            system.SaveSession(s2);

            var sessions = system.GetSavedSessions();
            Assert.Equal(2, sessions.Count);

            Directory.Delete(dir, true);
        }

        [Fact]
        public void ReplaySystem_DeleteSession_ShouldRemoveFile()
        {
            var dir = Path.Combine(Path.GetTempPath(), "RagnaController_ReplayTests7");
            Directory.CreateDirectory(dir);
            var system = new ReplaySystem(dir);

            system.StartRecording("Char1", "map1");
            var session = system.StopRecording()!;
            var filePath = system.SaveSession(session);

            Assert.True(File.Exists(filePath));
            Assert.True(system.DeleteSession(filePath));
            Assert.False(File.Exists(filePath));
        }

        #endregion
    }
}
