namespace RagnaController.Core
{
    /// <summary>
    /// Abstraction over mouse movement emulation strategies.
    ///
    /// Why this exists (Proposal #3):
    ///   Some private RO servers (or anti-cheat middleware) block Win32 <c>SendInput</c>
    ///   calls that carry the <c>LLMHF_INJECTED</c> flag in the raw input stream.
    ///   A kernel-mode driver like <see href="https://github.com/oblitum/Interception">Interception</see>
    ///   bypasses this by injecting at the HID driver level, where no injection flag is set.
    ///
    ///   This interface decouples the rest of the engine from the specific emulation backend
    ///   so the strategy can be swapped at runtime without touching movement logic.
    ///
    /// Current implementations:
    ///   <list type="bullet">
    ///     <item><see cref="SendInputMouseStrategy"/> — default, works on all standard servers</item>
    ///     <item><see cref="InterceptionMouseStrategy"/> — opt-in, requires Interception driver</item>
    ///   </list>
    ///
    /// Usage (swap in DI or HybridEngine constructor):
    /// <code>
    ///   IMouseEmulationStrategy mouse = InterceptionMouseStrategy.IsDriverInstalled
    ///       ? new InterceptionMouseStrategy()
    ///       : new SendInputMouseStrategy();
    /// </code>
    /// </summary>
    public interface IMouseEmulationStrategy
    {
        /// <summary>Move the cursor by a relative offset in physical screen pixels.</summary>
        void MoveRelative(int dx, int dy);

        /// <summary>Warp the cursor to an absolute physical screen coordinate.</summary>
        void MoveAbsolute(int x, int y);

        void LeftDown();
        void LeftUp();
        void RightDown();
        void RightUp();

        /// <summary>True if this strategy is available on the current machine.</summary>
        bool IsAvailable { get; }

        /// <summary>Human-readable name shown in settings UI.</summary>
        string DisplayName { get; }
    }
}
