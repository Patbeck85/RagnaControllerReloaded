using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// TASK-1602: Multi-Client WindowTracker.
    ///
    /// Verfolgt MEHRERE RO-Client-Fenster (gleicher Prozessname, verschiedene HWNDs)
    /// gleichzeitig. Im Gegensatz zum einzelnen WindowTracker (der only das erste
    /// gefundene Fenster merkt) sammelt this all passenden HWNDs pro Prozessname.
    ///
    /// Architekturzusammenfassung:
    /// - Refresh(processName): EnumWindows about all Top-Level-Fenster,
    ///   GetWindowThreadProcessId → PID, PID→Name-Cache (TECH-020 Muster),
    ///   all passenden HWNDs in Dictionary<string, List<ClientWindowEntry>> sammeln.
    /// - IsAlive(hwnd): IsWindow-Prüfung for tot/lebend.
    /// - TTL-basiertes Caching (10s), EnumWindows only außerhalb des Hot Paths.
    /// - INativeMethods-Injektion for Unit-Testbarkeit (Mock möglich).
    /// </summary>
    /// <summary>
    /// Window management and tracking component.
    /// </summary>
    public sealed class MultiClientWindowTracker : IDisposable
    {
        private readonly INativeMethods _native;
        private readonly IProcessNameResolver _processNames;
        private readonly object _lock = new();

        // processName → Liste aller ClientWindowEntry (active + tot)
        private readonly Dictionary<string, List<ClientWindowEntry>> _clients = new(StringComparer.OrdinalIgnoreCase);

        // TECH-020 Muster: PID → ProcessName Cache (einmal pro TTL, nicht pro Callback)
        private readonly Dictionary<int, string> _pidNameCache = new();
        private long _pidNameCacheTick = 0;
        private const long PID_NAME_CACHE_TTL_MS = 10_000;

        private const long CLIENT_CACHE_TTL_MS = 10_000;

        /// <summary>
        /// Eintrag for ein verfolgtes Client-Fenster.
        /// </summary>
        public record ClientWindowEntry(IntPtr Hwnd, uint Pid, string ProcessName, bool IsAlive, long LastSeenTick);

        /// <summary>
        /// Erstellt einen neuen MultiClientWindowTracker.
        /// </summary>
        public MultiClientWindowTracker(INativeMethods native, IProcessNameResolver processNames)
        {
            _native = native ?? throw new ArgumentNullException(nameof(native));
            _processNames = processNames ?? throw new ArgumentNullException(nameof(processNames));
        }

        /// <summary>
        /// Gibt all bekannten Clients for einen Prozessnamen zurück (Thread-sicher).
        /// </summary>
        public IReadOnlyList<ClientWindowEntry> GetClients(string processName)
        {
            lock (_lock)
            {
                if (_clients.TryGetValue(processName, out var list))
                    return list.AsReadOnly();
                return Array.Empty<ClientWindowEntry>().AsReadOnly();
            }
        }

        /// <summary>
        /// Gibt zurück, ob ein HWND noch gültig is (IsWindow-Prüfung).
        /// </summary>
        public bool IsAlive(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return false;
            lock (_lock)
            {
                return _native.IsWindow(hwnd);
            }
        }

        /// <summary>
        /// Aktualisiert die Client-Liste for einen Prozessnamen via EnumWindows.
        /// Is außerhalb des Hot Paths aufgerufen (TTL-gesteuert).
        /// </summary>
        public void Refresh(string processName)
        {
            var now = Environment.TickCount64;
            List<ClientWindowEntry> found;

            lock (_lock)
            {
                // TTL-Prüfung: Cache gültig?
                if (_clients.TryGetValue(processName, out var existing) &&
                    existing.Count > 0 &&
                    existing[0].LastSeenTick > 0 &&
                    (now - existing[0].LastSeenTick) < CLIENT_CACHE_TTL_MS)
                {
                    return; // Cache noch gültig
                }

                found = new List<ClientWindowEntry>();
            }

            // TECH-020: PID→Name Cache auffrischen (außerhalb des Locks, teuer)
            RefreshPidNameCache(now);

            // EnumWindows about all Top-Level-Fenster
            _native.EnumWindows((hwnd, lParam) =>
            {
                if (hwnd == IntPtr.Zero)
                    return true;

                _native.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0)
                    return true;

                // PID → Prozessname aus Cache (keine Allokation im Hot Path)
                string? procName = null;
                lock (_lock)
                {
                    if (_pidNameCache.TryGetValue((int)pid, out var cached))
                        procName = cached;
                }

                // Fallback: direkt about IProcessNameResolver auflösen (testbar, for PIDs die nicht im Cache are)
                if (string.IsNullOrEmpty(procName))
                {
                    procName = _processNames.GetProcessName((int)pid);
                    if (!string.IsNullOrEmpty(procName))
                    {
                        lock (_lock)
                        {
                            _pidNameCache[(int)pid] = procName;
                        }
                    }
                }

                if (string.IsNullOrEmpty(procName))
                    return true;

                if (!string.Equals(procName, processName, StringComparison.OrdinalIgnoreCase))
                    return true;

                bool alive = _native.IsWindow(hwnd);
                lock (_lock)
                {
                    found.Add(new ClientWindowEntry(hwnd, pid, procName, alive, now));
                }

                return true; // Enum fortsetzen — alle passenden Fenster sammeln
            }, IntPtr.Zero);

            // Ergebnis speichern
            lock (_lock)
            {
                _clients[processName] = found;
            }
        }

        /// <summary>
        /// TECH-020 Muster: Baut PID→ProcessName Mapping einmal pro TTL on.
        /// Vermeidet Process.GetProcessById() Allokationen im EnumWindows-Callback.
        /// </summary>
        private void RefreshPidNameCache(long now)
        {
            if ((now - _pidNameCacheTick) < PID_NAME_CACHE_TTL_MS && _pidNameCache.Count > 0)
                return;

            lock (_lock)
            {
                if ((now - _pidNameCacheTick) < PID_NAME_CACHE_TTL_MS && _pidNameCache.Count > 0)
                    return; // Double-checked nach Lock-Erwerb

                _pidNameCache.Clear();
            }

            try
            {
                foreach (var p in Process.GetProcesses())
                {
                    try
                    {
                        string? name = _processNames.GetProcessName(p.Id);
                        if (!string.IsNullOrEmpty(name))
                        {
                            lock (_lock)
                            {
                                _pidNameCache[p.Id] = name;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ERROR-001: No leerer catch
                        System.Diagnostics.Debug.WriteLine(
                            $"[MultiClientWindowTracker] Prozessname für PID {p.Id} nicht lesbar: {ex.Message}");
                    }
                    finally
                    {
                        p.Dispose();
                    }
                }

                lock (_lock)
                {
                    _pidNameCacheTick = now;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[MultiClientWindowTracker] RefreshPidNameCache äußerer Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Entfernt all toten Einträge aus dem Cache.
        /// </summary>
        public void CleanupDead()
        {
            lock (_lock)
            {
                var toRemove = new List<string>();
                foreach (var kvp in _clients)
                {
                    var aliveList = new List<ClientWindowEntry>();
                    foreach (var entry in kvp.Value)
                    {
                        if (_native.IsWindow(entry.Hwnd))
                            aliveList.Add(entry);
                    }
                    if (aliveList.Count == 0)
                        toRemove.Add(kvp.Key);
                    else
                        _clients[kvp.Key] = aliveList;
                }
                foreach (var key in toRemove)
                    _clients.Remove(key);
            }
        }

        /// <summary>
        /// Leert all Caches. Is beim Shutdown aufgerufen for saubere Test-Runs.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _clients.Clear();
                _pidNameCache.Clear();
                _pidNameCacheTick = 0;
            }
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
