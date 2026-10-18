// Core/ETWProvider.cs
using System;
using System.Diagnostics.Tracing;

namespace RagnaController.Core;

/// <summary>
/// ETW Provider für RagnaController Performance-Events.
/// EventSource mit Keywords: Controller, Input, Profile, Macro, Overlay, Error
/// </summary>
[EventSource(Name = "RagnaController", Guid = "00000000-0000-0000-0000-000000000001")]
public sealed class ETWProvider : EventSource
{
    public static readonly ETWProvider Log = new ETWProvider();

    // Keywords for event categorization
    public static class Keywords
    {
        public const EventKeywords Controller = (EventKeywords)1;
        public const EventKeywords Input = (EventKeywords)2;
        public const EventKeywords Profile = (EventKeywords)4;
        public const EventKeywords Macro = (EventKeywords)8;
        public const EventKeywords Overlay = (EventKeywords)16;
        public const EventKeywords Error = (EventKeywords)32;
        public const EventKeywords Performance = (EventKeywords)64;
        public const EventKeywords All = (EventKeywords)(-1);
    }

    // Controller Events
    [Event(1, Level = EventLevel.Informational, Keywords = Keywords.Controller,
        Message = "Controller connected: {0} (VID={1}, PID={2}, Instance={3})")]
    public void ControllerConnected(string controllerId, string devicePath)
    {
        if (IsEnabled()) WriteEvent(1, controllerId, devicePath);
    }

    [Event(2, Level = EventLevel.Warning, Keywords = Keywords.Controller,
        Message = "Controller disconnected: {0}")]
    public void ControllerDisconnected(string controllerId)
    {
        if (IsEnabled()) WriteEvent(2, controllerId);
    }

    // Input Emulation Events
    [Event(3, Level = EventLevel.Informational, Keywords = Keywords.Input,
        Message = "Input emulated: {0} (Type={1}, LatencyMs={2})")]
    public void InputEmulated(string actionId, string inputType, int latencyMs)
    {
        if (IsEnabled()) WriteEvent(3, actionId, inputType, latencyMs);
    }

    [Event(4, Level = EventLevel.Warning, Keywords = Keywords.Input,
        Message = "Input latency high: {0} (LatencyMs={1})")]
    public void InputLatencyHigh(string actionId, int latencyMs)
    {
        if (IsEnabled()) WriteEvent(4, actionId, latencyMs);
    }

    // Profile Events
    [Event(5, Level = EventLevel.Informational, Keywords = Keywords.Profile,
        Message = "Profile switched: {0} -> {1}")]
    public void ProfileSwitched(string fromProfile, string toProfile)
    {
        if (IsEnabled()) WriteEvent(5, fromProfile, toProfile);
    }

    // Macro Events
    [Event(6, Level = EventLevel.Informational, Keywords = Keywords.Macro,
        Message = "Macro executed: {0} (Step={1}/{2})")]
    public void MacroExecuted(string macroName, int step, int totalSteps)
    {
        if (IsEnabled()) WriteEvent(6, macroName, step, totalSteps);
    }

    // Overlay Events
    [Event(7, Level = EventLevel.Informational, Keywords = Keywords.Overlay,
        Message = "Overlay rendered: {0} (FrameTimeMs={1})")]
    public void OverlayRendered(string overlayType, float frameTimeMs)
    {
        if (IsEnabled()) WriteEvent(7, overlayType, frameTimeMs);
    }

    // Error Events
    [Event(8, Level = EventLevel.Error, Keywords = Keywords.Error,
        Message = "Error: {0} (Code={1}, Detail={2})")]
    public void ErrorOccurred(string errorCode, string errorMessage, string? exception = null)
    {
        if (IsEnabled()) WriteEvent(8, errorCode, errorMessage, exception);
    }
}
