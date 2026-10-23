using System;

namespace RagnaController.Core.Utilities.Coordinators
{
    /// <summary>
    /// Base interface for all engine coordinators.
    /// Defines the lifecycle contract: Initialize, Start, Stop, Update, Shutdown.
    /// </summary>
    public interface IEngineCoordinator : IDisposable
    {
        /// <summary>Called once after construction to inject dependencies and setup engines.</summary>
        void Initialize();

        /// <summary>Called when the engine orchestrator starts the tick loop.</summary>
        void Start();

        /// <summary>Called when the engine orchestrator pauses the tick loop.</summary>
        void Pause();

        /// <summary>Called when the engine orchestrator resumes the tick loop.</summary>
        void Resume();

        /// <summary>Called on every tick. Returns true if any engine did work.</summary>
        bool Tick();

        /// <summary>Called when the orchestrator shuts down. Cleanup resources.</summary>
        void Shutdown();

        /// <summary>Optional: Apply profile settings to this coordinator's engines.</summary>
        void ApplyProfile(RagnaController.Profiles.Profile profile);
    }

    /// <summary>
    /// Base class for engine coordinators with common functionality.
    /// </summary>
    public abstract class EngineCoordinatorBase : IEngineCoordinator
    {
        protected bool _isRunning;
        protected bool _isPaused;
        protected bool _disposed;

        public virtual void Initialize() { }

        public virtual void Start()
        {
            _isRunning = true;
            _isPaused = false;
        }

        public virtual void Pause()
        {
            _isPaused = true;
        }

        public virtual void Resume()
        {
            _isPaused = false;
        }

        public abstract bool Tick();

        public virtual void Shutdown()
        {
            _isRunning = false;
        }

        public virtual void ApplyProfile(RagnaController.Profiles.Profile profile) { }

        public virtual void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Shutdown();
        }
    }
}