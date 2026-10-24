using System;
using RagnaController.Models;
using RagnaController.Profiles;

namespace RagnaController.Core
{
    /// <summary>
    /// Common interface for all engine components.
    /// Provides standardized lifecycle: Tick, ApplyProfile, Dispose.
    /// </summary>
    public interface IEngine : IDisposable
    {
        /// <summary>
        /// Called every engine tick. Performs one iteration of engine logic.
        /// </summary>
        /// <param name="input">Normalized input from InputReader.</param>
        /// <param name="deltaMs">Time in milliseconds since last tick.</param>
        void Tick(ParsedInput input, int deltaMs);

        /// <summary>
        /// Called when the active profile changes.
        /// </summary>
        /// <param name="profile">The active profile.</param>
        void ApplyProfile(Profile profile);

        /// <summary>
        /// Gets the engine name for profiling/debugging.
        /// </summary>
        string Name { get; }
    }
}
