using System;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Timer abstraction — decouples the engine from WPF.
    /// Two implementations:
    ///  • <see cref="WpfTickProvider"/>        — DispatcherTimer on UI thread (legacy/fallback)
    ///  • <see cref="BackgroundTickProvider"/> — PeriodicTimer on background thread (recommended)
    /// </summary>
    public interface ITickProvider : IDisposable
    {
        event EventHandler Tick;
        int  IntervalMs { get; }
        void Start();
        void Stop();
    }
}
