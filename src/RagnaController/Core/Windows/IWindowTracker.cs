using System;

namespace RagnaController.Core
{
    /// <summary>
    /// Interface für Window-Tracking (Focus Lock, Foreground State).
    /// </summary>
    public interface IWindowTracker
    {
        bool IsROForeground { get; }
        bool IsInGameWindow { get; }
        event EventHandler? OnWindowStateChange;
        void UpdateWindowState();
    }
}
