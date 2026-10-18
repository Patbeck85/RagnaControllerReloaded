using System;
using System.Collections.Concurrent;

namespace RagnaController.Core
{
    public interface IMessenger
    {
        void Publish<T>(T message) where T : class;
        IDisposable Subscribe<T>(Action<T> handler) where T : class;
    }

    /// <summary>
    /// Copy-on-Write Messenger — Publish alloziert keine neue Liste mehr.
    /// Subscribe/Unsubscribe ersetzen das Array atomar (selten).
    /// </summary>
    public class Messenger : IMessenger
    {
        // Starke Referenzen — Handler werden bis Dispose gehalten
        private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Action<object>[]> _handlers = new();

        public void Publish<T>(T message) where T : class
        {
            if (!_handlers.TryGetValue(typeof(T), out var arr)) return;
            // Snapshot-Referenz — keine Allokation (Array ist immutable nach CoW-Swap)
            foreach (var h in arr)
                h(message);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : class
        {
            Action<object> wrapper = obj => handler((T)obj);
            _handlers.AddOrUpdate(
                typeof(T),
                _ => new[] { wrapper },
                (_, old) => { var n = new Action<object>[old.Length + 1]; old.CopyTo(n, 0); n[old.Length] = wrapper; return n; });
            return new DisposableAction(() =>
            {
                _handlers.AddOrUpdate(
                    typeof(T),
                    _ => System.Array.Empty<Action<object>>(),
                    (_, old) =>
                    {
                        int idx = System.Array.IndexOf(old, wrapper);
                        if (idx < 0) return old;
                        var n = new Action<object>[old.Length - 1];
                        System.Array.Copy(old, 0, n, 0, idx);
                        System.Array.Copy(old, idx + 1, n, idx, old.Length - idx - 1);
                        return n;
                    });
            });
        }

        /// <summary>
        /// Helper to return pooled messages after publishing.
        /// Call this after Publish to return the message to its pool.
        /// </summary>
        public void ReturnToPool<T>(T message) where T : class
        {
            // No-op by default; overridden for specific pooled types via extension
        }
    }

    internal sealed class DisposableAction : IDisposable
    {
        private Action? _action;
        public DisposableAction(Action action) => _action = action;
        public void Dispose() { _action?.Invoke(); _action = null; }
    }
}
