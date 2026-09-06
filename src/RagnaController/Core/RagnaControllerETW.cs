using System;
using System.Diagnostics.Tracing;

namespace RagnaController.Core
{
    /// <summary>
    /// ETW EventSource for RagnaController telemetry and performance tracing.
    /// Events can be captured with: dotnet trace collect -p <pid> --providers RagnaController-ETW
    /// Viewed in PerfView, WPA, or dotnet-counters.
    /// </summary>
    [EventSource(Name = "RagnaController-ETW", Guid = "7A3F8C2E-4D1B-4A9E-B8F2-1C6D9E3A7B5F")]
    public sealed class RagnaControllerETW : EventSource
    {
        public static readonly RagnaControllerETW Log = new RagnaControllerETW();

        // Keywords for event categorization
        public static class Keywords
        {
            public const EventKeywords Controller = (EventKeywords)1;      // Controller lifecycle events
            public const EventKeywords Input = (EventKeywords)2;           // Input emulation events
            public const EventKeywords Profile = (EventKeywords)4;         // Profile switching events
            public const EventKeywords Macro = (EventKeywords)8;           // Macro execution events
            public const EventKeywords Overlay = (EventKeywords)16;        // Overlay rendering events
            public const EventKeywords Error = (EventKeywords)32;          // Error/diagnostic events
            public const EventKeywords Performance = (EventKeywords)64;    // Performance metrics
            public const EventKeywords All = (EventKeywords)(-1);
        }

        // Event levels: Critical=1, Error=2, Warning=3, Informational=4, Verbose=5

        #region Controller Events

        [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Controller,
            Message = "Controller connected: {0} (VID={1}, PID={2}, Instance={3})")]
        public void ControllerConnected(string deviceName, ushort vid, ushort pid, string instanceId)
        {
            if (IsEnabled()) WriteEvent(1, deviceName, vid, pid, instanceId);
        }

        [Event(2, Level = EventLevel.Informational, Keywords = Keywords.Controller,
            Message = "Controller disconnected: {0} (Instance={1})")]
        public void ControllerDisconnected(string deviceName, string instanceId)
        {
            if (IsEnabled()) WriteEvent(2, deviceName, instanceId);
        }

        [Event(3, Level = EventLevel.Informational, Keywords = Keywords.Controller,
            Message = "Controller reconnected: {0} (Instance={1})")]
        public void ControllerReconnected(string deviceName, string instanceId)
        {
            if (IsEnabled()) WriteEvent(3, deviceName, instanceId);
        }

        #endregion

        #region Input Emulation Events

        [Event(10, Level = EventLevel.Verbose, Keywords = Keywords.Input | Keywords.Performance,
            Message = "Input emulated: {0} -> {1} (Controller={2}, LatencyUs={3})")]
        public void InputEmulated(string actionName, string inputType, string controllerId, long latencyMicroseconds)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Input | Keywords.Performance))
                WriteEvent(10, actionName, inputType, controllerId, latencyMicroseconds);
        }

        [Event(11, Level = EventLevel.Warning, Keywords = Keywords.Input | Keywords.Error,
            Message = "Input emulation failed: {0} (Controller={1}, Error={2})")]
        public void InputEmulationFailed(string actionName, string controllerId, string error)
        {
            if (IsEnabled()) WriteEvent(11, actionName, controllerId, error);
        }

        [Event(12, Level = EventLevel.Informational, Keywords = Keywords.Input,
            Message = "Input batch sent: {0} commands (Controller={1}, BatchSize={2})")]
        public void InputBatchSent(int commandCount, string controllerId, int batchSize)
        {
            if (IsEnabled()) WriteEvent(12, commandCount, controllerId, batchSize);
        }

        #endregion

        #region Profile Events

        [Event(20, Level = EventLevel.Informational, Keywords = Keywords.Profile,
            Message = "Profile switched: {0} -> {1} (Controller={2})")]
        public void ProfileSwitched(string fromProfile, string toProfile, string controllerId)
        {
            if (IsEnabled()) WriteEvent(20, fromProfile, toProfile, controllerId);
        }

        [Event(21, Level = EventLevel.Informational, Keywords = Keywords.Profile,
            Message = "Profile loaded: {0} (Controller={1}, ActionCount={2})")]
        public void ProfileLoaded(string profileName, string controllerId, int actionCount)
        {
            if (IsEnabled()) WriteEvent(21, profileName, controllerId, actionCount);
        }

        [Event(22, Level = EventLevel.Warning, Keywords = Keywords.Profile | Keywords.Error,
            Message = "Profile load failed: {0} (Controller={1}, Error={2})")]
        public void ProfileLoadFailed(string profileName, string controllerId, string error)
        {
            if (IsEnabled()) WriteEvent(22, profileName, controllerId, error);
        }

        #endregion

        #region Macro Events

        [Event(30, Level = EventLevel.Informational, Keywords = Keywords.Macro,
            Message = "Macro started: {0} (Controller={1}, SequenceId={2})")]
        public void MacroStarted(string macroName, string controllerId, Guid sequenceId)
        {
            if (IsEnabled()) WriteEvent(30, macroName, controllerId, sequenceId);
        }

        [Event(31, Level = EventLevel.Informational, Keywords = Keywords.Macro,
            Message = "Macro completed: {0} (Controller={1}, SequenceId={2}, DurationMs={3}, Steps={4})")]
        public void MacroCompleted(string macroName, string controllerId, Guid sequenceId, long durationMs, int stepCount)
        {
            if (IsEnabled()) WriteEvent(31, macroName, controllerId, sequenceId, durationMs, stepCount);
        }

        [Event(32, Level = EventLevel.Warning, Keywords = Keywords.Macro | Keywords.Error,
            Message = "Macro cancelled: {0} (Controller={1}, SequenceId={2}, Reason={3})")]
        public void MacroCancelled(string macroName, string controllerId, Guid sequenceId, string reason)
        {
            if (IsEnabled()) WriteEvent(32, macroName, controllerId, sequenceId, reason);
        }

        [Event(33, Level = EventLevel.Error, Keywords = Keywords.Macro | Keywords.Error,
            Message = "Macro failed: {0} (Controller={1}, SequenceId={2}, Error={3})")]
        public void MacroFailed(string macroName, string controllerId, Guid sequenceId, string error)
        {
            if (IsEnabled()) WriteEvent(33, macroName, controllerId, sequenceId, error);
        }

        #endregion

        #region Overlay Events

        [Event(40, Level = EventLevel.Informational, Keywords = Keywords.Overlay,
            Message = "Overlay shown: {0} (Mode={1})")]
        public void OverlayShown(string overlayType, string mode)
        {
            if (IsEnabled()) WriteEvent(40, overlayType, mode);
        }

        [Event(41, Level = EventLevel.Informational, Keywords = Keywords.Overlay,
            Message = "Overlay hidden: {0}")]
        public void OverlayHidden(string overlayType)
        {
            if (IsEnabled()) WriteEvent(41, overlayType);
        }

        [Event(42, Level = EventLevel.Verbose, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "Overlay frame rendered: {0} (FrameTimeMs={1}, ElementCount={2})")]
        public void OverlayFrameRendered(string overlayType, double frameTimeMs, int elementCount)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Overlay | Keywords.Performance))
                WriteEvent(42, overlayType, frameTimeMs, elementCount);
        }

        [Event(43, Level = EventLevel.Warning, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "Overlay frame budget exceeded: {0} (FrameTimeMs={1}, BudgetMs={2})")]
        public void OverlayFrameBudgetExceeded(string overlayType, double frameTimeMs, double budgetMs)
        {
            if (IsEnabled()) WriteEvent(43, overlayType, frameTimeMs, budgetMs);
        }

        #endregion

        #region Error / Diagnostic Events

        [Event(50, Level = EventLevel.Error, Keywords = Keywords.Error,
            Message = "Error: {0} (Source={1}, HResult={2})")]
        public void ErrorOccurred(string message, string source, int hresult)
        {
            if (IsEnabled()) WriteEvent(50, message, source, hresult);
        }

        [Event(51, Level = EventLevel.Critical, Keywords = Keywords.Error,
            Message = "Critical failure: {0} (Source={1}, StackTrace={2})")]
        public void CriticalFailure(string message, string source, string stackTrace)
        {
            if (IsEnabled()) WriteEvent(51, message, source, stackTrace);
        }

        [Event(52, Level = EventLevel.Warning, Keywords = Keywords.Error,
            Message = "Recovered from error: {0} (Source={1}, RecoveryAction={2})")]
        public void ErrorRecovered(string message, string source, string recoveryAction)
        {
            if (IsEnabled()) WriteEvent(52, message, source, recoveryAction);
        }

        #endregion

        #region Performance / Counter Events

        [Event(60, Level = EventLevel.Informational, Keywords = Keywords.Performance,
            Message = "Polling rate: {0} Hz (Controller={1})")]
        public void PollingRateUpdated(double rateHz, string controllerId)
        {
            if (IsEnabled()) WriteEvent(60, rateHz, controllerId);
        }

        [Event(61, Level = EventLevel.Verbose, Keywords = Keywords.Performance,
            Message = "Input latency: {0} us (Controller={1}, Percentile=P99)")]
        public void InputLatencyP99(long latencyMicroseconds, string controllerId)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Performance))
                WriteEvent(61, latencyMicroseconds, controllerId);
        }

        [Event(62, Level = EventLevel.Informational, Keywords = Keywords.Performance,
            Message = "Memory stats: WorkingSet={0} MB, Gen0={1}, Gen1={2}, Gen2={3}")]
        public void MemoryStats(long workingSetMb, int gen0, int gen1, int gen2)
        {
            if (IsEnabled()) WriteEvent(62, workingSetMb, gen0, gen1, gen2);
        }

        [Event(63, Level = EventLevel.Informational, Keywords = Keywords.Performance,
            Message = "Startup time: {0} ms (Cold={1})")]
        public void StartupTime(long elapsedMs, bool coldStart)
        {
            if (IsEnabled()) WriteEvent(63, elapsedMs, coldStart);
        }

        [Event(64, Level = EventLevel.Informational, Keywords = Keywords.Performance,
            Message = "CPU time: {0} ms (Thread={1})")]
        public void CpuTime(long cpuTimeMs, string threadName)
        {
            if (IsEnabled()) WriteEvent(64, cpuTimeMs, threadName);
        }

        #endregion

        #region GPU / Overlay Render Profiling Events

        [Event(70, Level = EventLevel.Informational, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "WPF Render Tier: {0} (Device={1}, PixelShader={2}, VertexShader={3})")]
        public void WpfRenderTier(int tier, string deviceName, int pixelShaderVersion, int vertexShaderVersion)
        {
            if (IsEnabled()) WriteEvent(70, tier, deviceName, pixelShaderVersion, vertexShaderVersion);
        }

        [Event(71, Level = EventLevel.Verbose, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "Overlay frame timing: {0} (LayoutMs={1}, RenderMs={2}, CompositeMs={3}, TotalMs={4})")]
        public void OverlayFrameTiming(string overlayType, double layoutMs, double renderMs, double compositeMs, double totalMs)
        {
            if (IsEnabled(EventLevel.Verbose, Keywords.Overlay | Keywords.Performance))
                WriteEvent(71, overlayType, layoutMs, renderMs, compositeMs, totalMs);
        }

        [Event(72, Level = EventLevel.Informational, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "Composition engine stats: {0} (FPS={1}, FrameTimeMs={2}, PresentMs={3}, DroppedFrames={4})")]
        public void CompositionEngineStats(string overlayType, double fps, double frameTimeMs, double presentMs, int droppedFrames)
        {
            if (IsEnabled()) WriteEvent(72, overlayType, fps, frameTimeMs, presentMs, droppedFrames);
        }

        [Event(73, Level = EventLevel.Warning, Keywords = Keywords.Overlay | Keywords.Performance,
            Message = "GPU memory pressure: {0} (DedicatedMB={1}, SharedMB={2}, BudgetMB={3})")]
        public void GpuMemoryPressure(string overlayType, long dedicatedMb, long sharedMb, long budgetMb)
        {
            if (IsEnabled()) WriteEvent(73, overlayType, dedicatedMb, sharedMb, budgetMb);
        }

        #endregion
    }
}