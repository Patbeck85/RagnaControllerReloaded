using System;
using System.Collections.Concurrent;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Generic thread-safe object pool for high-frequency message types.
    /// Reduces Gen0 allocations in the 125Hz tick loop.
    /// </summary>
    /// <typeparam name="T">Pooled type</typeparam>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly ConcurrentBag<T> _bag = new();
        private readonly Func<T> _factory;

        public ObjectPool(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        public T Rent()
        {
            if (_bag.TryTake(out var item))
                return item;
            return _factory();
        }

        public void Return(T item)
        {
            if (item == null) return;
            _bag.Add(item);
        }
    }

    /// <summary>
    /// Pre-configured pools for hot-path message types.
    /// </summary>
    public static class MessagePools
    {
        // ActionFiredMessage - created per action fire (~50-200/sec in combat)
        public static readonly ObjectPool<ActionFiredMessage> ActionFiredPool =
            new(() => new ActionFiredMessage(string.Empty, ActionFiredKind.Skill));

        // EngineStatusMessage - rare (on connect/disconnect)
        public static readonly ObjectPool<EngineStatusMessage> EngineStatusPool =
            new(() => new EngineStatusMessage(EngineStatus.Stopped, string.Empty));

        // BatteryChangedMessage - rare (on connect/change)
        public static readonly ObjectPool<BatteryChangedMessage> BatteryChangedPool =
            new(() => new BatteryChangedMessage(string.Empty));

        // SnapshotReadyMessage - every UI tick (~30/sec)
        public static readonly ObjectPool<SnapshotReadyMessage> SnapshotReadyPool =
            new(() => new SnapshotReadyMessage(default!));

        // BuffWarningMessage - rare
        public static readonly ObjectPool<BuffWarningMessage> BuffWarningPool =
            new(() => new BuffWarningMessage(string.Empty));
    }
}