using System;

namespace RagnaController.Core
{
    /// <summary>
    /// GEX-003: Keyboard Emulation Strategy Interface
    /// Mirrors IMouseEmulationStrategy for keyboard — enables failover parity (SendInput ↔ Interception).
    /// Both implementations must produce IDENTICAL in-game key timing: down/up latency, repeat rate, modifier handling.
    /// </summary>
    public interface IKeyboardEmulationStrategy : IDisposable
    {
        bool IsAvailable { get; }
        string DisplayName { get; }

        /// <summary>Press a key down (scancode or virtual key).</summary>
        void KeyDown(ushort keyCode);

        /// <summary>Release a key (scancode or virtual key).</summary>
        void KeyUp(ushort keyCode);

        /// <summary>Tap a key: Down → wait (human-like hold) → Up.</summary>
        void TapKey(ushort keyCode, int holdMs = 0);

        /// <summary>Tap key with modifier: ModDown → wait → KeyDown → hold → KeyUp → wait → ModUp.</summary>
        void TapKeyWithModifier(ushort modifier, ushort keyCode, int holdMs = 0);
    }
}