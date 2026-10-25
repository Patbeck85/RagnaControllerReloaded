using System;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Interface for Window-Tracking (Focus Lock, Foregroand State).
    /// </summary>
    public interface IWindowTracker
    {
        bool IsROForeground { get; }
        bool IsInGameWindow { get; }
        event EventHandler? OnWindowStateChange;
        void UpdateWindowState();
    }
}
