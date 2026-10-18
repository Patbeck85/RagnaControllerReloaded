using System;
using RagnaController.Controller;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Unified controller abstraction interface that supports both SDL2 and XInput backends.
    /// ControllerManager automatically tries SDL2 first, with fallback to XInput.
    /// </summary>
    public interface IControllerProvider : IDisposable
    {
        /// <summary>
        /// Gets whether a controller is currently connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Gets the current button states from the controller.
        /// </summary>
        ButtonState ButtonStates { get; }

        /// <summary>
        /// Gets the controller GUID as a hex string for profile mapping.
        /// </summary>
        string ControllerGuid { get; }

        /// <summary>
        /// Gets the controller name (e.g., "Xbox", "PS5", "Switch").
        /// </summary>
        string ControllerName { get; }

        /// <summary>
        /// Gets the controller type (Xbox, PS4, PS5, Switch, xinput).
        /// </summary>
        string ControllerType { get; }

        /// <summary>
        /// Gets the current battery level.
        /// </summary>
        string BatteryLevel { get; }

        /// <summary>
        /// Requests an immediate controller re-scan.
        /// Safe to call from any thread.
        /// </summary>
        void DetectController();

        /// <summary>
        /// Sets rumble/vibration on the controller.
        /// </summary>
        /// <param name="left">Left motor speed (0.0 to 1.0).</param>
        /// <param name="right">Right motor speed (0.0 to 1.0).</param>
        void SetRumble(float left, float right);

        /// <summary>
        /// Sets LED color on the controller (if supported).
        /// </summary>
        /// <param name="r">Red component (0-255).</param>
        /// <param name="g">Green component (0-255).</param>
        /// <param name="b">Blue component (0-255).</param>
        void SetLED(byte r, byte g, byte b);
    }
}