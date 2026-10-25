using System;
using System.Collections.Generic;
using RagnaController.Models; // IMPORTANT for ActionFiredKind
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// Background service for game functionality.
    /// </summary>
    public sealed class ActionLogService : IDisposable
    {
        private readonly object _lock = new();
        private readonly LinkedList<ActionEntry> _entries = new();
        public int MaxEntries { get; set; } = 20;

        // ActionFiredMessage is jetzt durch das projektweite Namespace-System gefunden
        /// <summary>
        /// Record method.
        /// <param name="msg">msg parameter.</param>
        /// </summary>
        public void Record(ActionFiredMessage msg) => Record(msg.Label, msg.Kind);

        public void Record(string label, ActionFiredKind kind)
        {
            // Environment.TickCount64 for hot path — DateTime.Now only for display
            var entry = new ActionEntry(label, kind, DateTime.Now);
            lock (_lock)
            {
                _entries.AddFirst(entry);
                while (_entries.Count > MaxEntries)
                    _entries.RemoveLast();
            }
            EntryAdded?.Invoke(entry);
        }

        public IReadOnlyList<ActionEntry> Entries    => GetSnapshot();
        public IReadOnlyList<ActionEntry> GetSnapshot()
        {
            lock (_lock) return new List<ActionEntry>(_entries);
        }

        public event Action<ActionEntry>? EntryAdded;
        /// <summary>
        /// Clear method.
        /// </summary>
        public void Clear() { lock (_lock) _entries.Clear(); }

        public void Dispose()
        {
            EntryAdded = null;
            lock (_lock) _entries.Clear();
        }
    }

    public sealed record ActionEntry(string Label, ActionFiredKind Kind, DateTime Time)
    {
        public string Icon => Kind switch
        {
            ActionFiredKind.Combo   => "⚡",
            ActionFiredKind.Click   => "🖱",
            ActionFiredKind.Special => "✨",
            _                      => "▶"
        };
        public string FormattedTime => Time.ToString("HH:mm:ss.ff");
    }
}