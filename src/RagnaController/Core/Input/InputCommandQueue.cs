using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using RagnaController.Models;
using static RagnaController.Core.NativeMethods;

namespace RagnaController.Core
{
    /// <summary>
    /// Input command types for the SendInput queue.
    /// </summary>
    public enum CmdType
    {
        MouseRel, LeftDown, LeftUp, RightDown, RightUp,
        KeyDown, KeyUp, Wheel, AtomicLeftClick, AtomicRightClick,
        Wait, Action, MouseAbs
    }

    // Input Command Struct (must match all constructor patterns)
    /// <summary>
    /// Input processing and routing component.
    /// </summary>
    public sealed class InputCmd
    {
        public CmdType Type { get; init; }
        public ushort Key { get; init; }
        public int X { get; set; }
        public int Y { get; init; }
        public Action? Callback { get; init; }

        public InputCmd(CmdType type) : this(type, 0, 0, null) { }

        // FIX (TEST-10): The alte Chaining-Variante `: this(type, key, 0, null)` has
        // (type, int x, int y, Action?) gematcht → VK-Code landete in X statt Key.
        public InputCmd(CmdType type, ushort key)
        {
            Type = type;
            Key = (ushort)(key & 0xFFFF);
        }
        public InputCmd(CmdType type, int x, int y) : this(type, 0, x, y) { }
        public InputCmd(CmdType type, ushort key, int x, int y)
        {
            Type = type;
            Key = (ushort)(key & 0xFFFF); // Ensure valid ushort range
            X = x;
            Y = y;
        }

        public InputCmd(CmdType type, Action? callback) : this(type, 0, 0, callback) { }

        // FIX (TEST-10): like 2-arg variant — chaining would have set Key=0/X=key.
        public InputCmd(CmdType type, ushort key, Action? callback)
        {
            Type = type;
            Key = (ushort)(key & 0xFFFF);
            Callback = callback;
        }
        public InputCmd(CmdType type, int x, int y, Action? callback)
        {
            Type = type;
            Key = 0;
            X = x;
            Y = y;
            Callback = callback;
        }

        /// <summary>Factory method for Wait commands - explicit and unambiguous.</summary>
        public static InputCmd CreateWait(int ms)
        {
            var cmd = new InputCmd(CmdType.Wait) { X = ms };
            return cmd;
        }
    }

    /// <summary>
    /// Unified input dispatch interface - consolidates IInputService + InputSimulator + mouse strategies.
    /// Single entry point for all input operations (mouse, keyboard, chat, scrolling).
    /// </summary>
    public interface IInputDispatcher : IDisposable
    {
        // Mouse
        void MoveMouseRelative(int dx, int dy);
        void MoveMouseAbsolute(int x, int y);
        void LeftClick();
        void RightClick();
        void DoubleClick();
        void LeftDown();
        void LeftUp();
        void RightDown();
        void RightUp();

        // Keyboard
        void TapKey(VirtualKey key);
        void KeyDown(VirtualKey key);
        void KeyUp(VirtualKey key);
        void TapKeyWithModifier(VirtualKey modifier, VirtualKey key);
        void PanicHeal(VirtualKey key);

        // Wheel
        void ScrollWheel(int delta);

        // Chat
        Task SendChatString(string text);

        // RSI Tracking
        long SessionSavedClicks { get; }
        long SessionSavedKeystrokes { get; }

        // Lifecycle
        void Start();
        void Stop();
        void RequestShutdown();
        bool IsChatting { get; }
    }

    /// <summary>
    /// InputCommandQueue — Thread-safe SendInput queue for Win32 input injection.
    /// Buffers input commands with DEBUG-only command list for inspection.
    /// Ensures ordered delivery of mouse and keyboard input to the game client.
    /// </summary>
    public class InputCommandQueue : IInputDispatcher
    {
        private BlockingCollection<InputCmd>? _queue;
        private CancellationTokenSource _cts = new();
        private Thread? _consumerThread;
        private bool _isDisposed;

        // P/Invoke Batching State
        private List<INPUT> _batch = new(64);
        private int _batchCount;

        // RSI Tracking
        private long _savedClicks;
        private long _savedKeystrokes;

        // Input Latency Tracking (PERF-005 - legacy)
        private long _totalInputLatencyUs;
        private long _inputCount;
        private long _maxInputLatencyUs;
        private readonly object _latencyLock = new();

        // Chat state
        private bool _isChatting;
        private volatile bool _shutdownRequested;

        // PERF-005: Input Latency Tracker
        private readonly InputLatencyTracker? _latencyTracker;
        private readonly string _controllerId;

        public event Action<InputCmd>? OnCommandEnqueued;
        public event Action<InputCmd>? OnCommandExecuted;

        /// <summary>
        /// ROB-002: Feuert nach jedem SendInput-Flush with der gemessenen API-Latenz in ms.
        /// Is vom Failover-Mechanismus (InputRouter) genutzt, um langsamere
        /// Emulations-Pfade to erkennen and on die Kernel-Strategie to wechseln.
        /// </summary>
        public event Action<double>? SendInputLatencyRecorded;

        public bool IsAddingCompleted => _queue!.IsAddingCompleted;
        public int QueueCount => _queue!.Count;
        public long SessionSavedClicks => Interlocked.Read(ref _savedClicks);
        public long SessionSavedKeystrokes => Interlocked.Read(ref _savedKeystrokes);
        public bool IsChatting => _isChatting;

        // Input Latency Properties (PERF-005 - legacy)
        public double AverageInputLatencyMs => _inputCount > 0 ? (Interlocked.Read(ref _totalInputLatencyUs) / (double)_inputCount) / 1000.0 : 0;
        public long TotalInputsProcessed => Interlocked.Read(ref _inputCount);
        public double MaxInputLatencyMs => Interlocked.Read(ref _maxInputLatencyUs) / 1000.0;

        // Commands collection for testing and inspection (bounded; both configurations).
        public List<InputCmd> Commands { get; } = new();

        // FIX (TEST-011): RecordCommand is from mehreren Enqueue-Threads aufgerufen —
        // unguarded List.Add throws ArgumentException on resize path.
        private readonly object _commandsLock = new();

        // Bounded history in DEBUG and RELEASE: Replay/Inspection braucht they in beiden,
        // CI tests with --configuration Release (2048→4096 entries ≈ 128 KB worst case, deterministic).
        private const int MaxRecordedCommands = 4096;

        private void RecordCommand(InputCmd cmd)
        {
            lock (_commandsLock)
            {
                Commands.Add(cmd);
                if (Commands.Count > MaxRecordedCommands)
                {
                    Commands.RemoveRange(0, Commands.Count - MaxRecordedCommands);
                }
            }
        }

        /// <summary>
        /// Creates a new InputCommandQueue with optional latency tracking.
        /// </summary>
        /// <param name="controllerId">Identifier for the controller (used for per-controller latency tracking).</param>
        public InputCommandQueue(InputLatencyTracker? latencyTracker = null, string controllerId = "Default")
        {
            _latencyTracker = latencyTracker;
            _controllerId = controllerId;
            _queue = new BlockingCollection<InputCmd>();
        }

        public void Enqueue(InputCmd cmd)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(InputCommandQueue));
            if (!_queue!.IsAddingCompleted)
            {
                _queue!.TryAdd(cmd);
                OnCommandEnqueued?.Invoke(cmd);
                
                // PERF-005: Record enqueue latency (time from hardware event to queue enqueue)
                _latencyTracker?.RecordEnqueueLatency(0, _controllerId); // Hardware timestamp not available here, measured at source
                
                RecordCommand(cmd);
            }
        }

        // Mouse
        /// <summary>
        /// LeftDown method.
        /// </summary>
        public void LeftDown() => Enqueue(new InputCmd(CmdType.LeftDown));
        /// <summary>
        /// LeftUp method.
        /// </summary>
        public void LeftUp() => Enqueue(new InputCmd(CmdType.LeftUp));
        /// <summary>
        /// RightDown method.
        /// </summary>
        public void RightDown() => Enqueue(new InputCmd(CmdType.RightDown));
        /// <summary>
        /// RightUp method.
        /// </summary>
        public void RightUp() => Enqueue(new InputCmd(CmdType.RightUp));
        /// <summary>
        /// LeftClick method.
        /// </summary>
        public void LeftClick()   { Enqueue(new InputCmd(CmdType.LeftDown)); Interlocked.Increment(ref _savedClicks); }
        /// <summary>
        /// RightClick method.
        /// </summary>
        public void RightClick()  { Enqueue(new InputCmd(CmdType.RightDown)); Interlocked.Increment(ref _savedClicks); }
        /// <summary>
        /// DoubleClick method.
        /// </summary>
        public void DoubleClick() { Enqueue(new InputCmd(CmdType.AtomicLeftClick)); Interlocked.Add(ref _savedClicks, 2); }
        /// <summary>
        /// MoveMouseRelative method.
        /// <param name="dx">dx parameter.</param>
        /// <param name="dy">dy parameter.</param>
        /// </summary>
        public void MoveMouseRelative(int dx, int dy) => Enqueue(new InputCmd(CmdType.MouseRel, dx, dy));
        /// <summary>
        /// MoveMouseAbsolute method.
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// </summary>
        public void MoveMouseAbsolute(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        /// <summary>
        /// MouseMove method.
        /// <param name="dx">dx parameter.</param>
        /// <param name="dy">dy parameter.</param>
        /// </summary>
        public void MouseMove(int dx, int dy) => Enqueue(new InputCmd(CmdType.MouseRel, dx, dy));
        /// <summary>
        /// MouseAbs method.
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// </summary>
        public void MouseAbs(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        /// <summary>
        /// MouseMoveAbsolute method.
        /// <param name="x">x parameter.</param>
        /// <param name="y">y parameter.</param>
        /// </summary>
        public void MouseMoveAbsolute(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        /// <summary>
        /// AtomicLeftClick method.
        /// </summary>
        public void AtomicLeftClick() => Enqueue(new InputCmd(CmdType.AtomicLeftClick));
        /// <summary>
        /// AtomicRightClick method.
        /// </summary>
        public void AtomicRightClick() => Enqueue(new InputCmd(CmdType.AtomicRightClick));

        // Keyboard
        /// <summary>
        /// KeyDown method.
        /// <param name="k">k parameter.</param>
        /// </summary>
        public void KeyDown(VirtualKey k) => Enqueue(new InputCmd(CmdType.KeyDown, (ushort)k));
        /// <summary>
        /// KeyUp method.
        /// <param name="k">k parameter.</param>
        /// </summary>
        public void KeyUp(VirtualKey k)   => Enqueue(new InputCmd(CmdType.KeyUp, (ushort)k));
        public void TapKey(VirtualKey k)
        {
            // Enqueue KeyDown -> Wait -> KeyUp sequence (same pattern as Win32InputService)
            KeyDown(k);
            Wait(JitterService.ClickHold() / 3);
            KeyUp(k);
            Interlocked.Increment(ref _savedKeystrokes);
        }
        public void TapKeyWithModifier(VirtualKey mod, VirtualKey key)
        {
            KeyDown(mod);
            Wait(10);
            KeyDown(key);
            Wait(JitterService.ClickHold() / 3);
            KeyUp(key);
            Wait(10);
            KeyUp(mod);
            Interlocked.Increment(ref _savedKeystrokes);
        }
        /// <summary>
        /// PanicHeal method.
        /// <param name="k">k parameter.</param>
        /// </summary>
        public void PanicHeal(VirtualKey k) { for (int i = 0; i < 10; i++) TapKey(k); }

        // Wheel
                /// <summary>
                /// Wheel method.
                /// <param name="delta">delta parameter.</param>
                /// </summary>
                public void Wheel(int delta) => Enqueue(new InputCmd(CmdType.Wheel) { X = delta });
                /// <summary>
                /// ScrollWheel method.
                /// <param name="delta">delta parameter.</param>
                /// </summary>
                public void ScrollWheel(int delta) => Enqueue(new InputCmd(CmdType.Wheel) { X = delta });

        // Chat
        public async Task SendChatString(string text)
        {
            if (_isChatting || _shutdownRequested) return;

            _isChatting = true;
            try
            {
                TapKey(VirtualKey.Enter);
                await Task.Delay(80);

                foreach (char c in text)
                {
                    if (_shutdownRequested) break; // Ghost-Typing verhindern

                    // FIX: With KEYEVENTF_UNICODE, wVk must be 0 and character goes in wScan
                    INPUT inp = default;
                    inp.type            = INPUT_KEYBOARD;
                    inp.Data.ki.wVk     = 0;           // Must be 0 for KEYEVENTF_UNICODE
                    inp.Data.ki.wScan   = (ushort)c;   // Unicode char goes in wScan
                    inp.Data.ki.dwFlags = 0x0004;      // KEYEVENTF_UNICODE
                    SendInput(1, ref inp, InputSize);

                    // add release command — prevents permanently held keys
                    INPUT inpUp = default;
                    inpUp.type            = INPUT_KEYBOARD;
                    inpUp.Data.ki.wVk     = 0;           // Must be 0 for KEYEVENTF_UNICODE
                    inpUp.Data.ki.wScan   = (ushort)c;   // Same Unicode char for release
                    inpUp.Data.ki.dwFlags = 0x0004 | 0x0008; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                    SendInput(1, ref inpUp, InputSize);

                    await Task.Delay(18);
                }

                if (!_shutdownRequested)
                {
                    await Task.Delay(50);
                    TapKey(VirtualKey.Enter);
                }
            }
            finally
            {
                _isChatting = false;
            }
        }

        // Wait/Action helpers
        /// <summary>
        /// Wait method.
        /// <param name="ms">ms parameter.</param>
        /// </summary>
        public void Wait(int ms) => Enqueue(InputCmd.CreateWait(ms));
        /// <summary>
        /// Action method.
        /// <param name="callback">callback parameter.</param>
        /// </summary>
        public void Action(Action callback) => Enqueue(new InputCmd(CmdType.Action, callback));

        public void Start()
        {
            if (_consumerThread != null && _consumerThread.IsAlive) return;

            // Nach Stop() is der alte CTS disposed → for einen Restart neu erzeugen.
            if (_cts == null || _cts.IsCancellationRequested)
                _cts = new CancellationTokenSource();

            _consumerThread = new Thread(Process) { Name = "InputCommandQueue_Consumer", IsBackground = true };
            _consumerThread.Start();
        }

        public void Stop()
        {
            // FIX (TEST-011 class / race): CTS erst NACH dem Join des Consumer-Threads
            // dispose. Consumer reads _cts.Token in loop; an early null!
            // leads to NullReferenceException in Process().
            var cts = _cts;
            if (cts != null)
            {
                cts.Cancel();
            }

            if (_consumerThread != null)
            {
                _consumerThread.Join(1000);
                _consumerThread = null;
            }

            // Thread is jetzt garantiert beendet → CTS sicher freigeben.
            cts?.Dispose();
            _cts = null!;
        }

        public void RequestShutdown()
        {
            _shutdownRequested = true;
            Dispose();
        }

        private void Process()
        {
            // local token snapshot: remains valid even if _cts later
            // from Stop() on null gesetzt is (Join-Garantie).
            var token = _cts.Token;
            var enumerator = _queue!.GetConsumingEnumerable(token).GetEnumerator();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // Block until at least ONE command is available
                    if (enumerator.MoveNext())
                    {
                        // PERF-005: Record dispatch latency (time from dequeue to processing start)
                        var dispatchSw = System.Diagnostics.Stopwatch.StartNew();
                        
                        try { ProcessCommand(enumerator.Current, ref _batch, ref _batchCount, dispatchSw); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[InputQueue] Command error: {ex.Message}"); }

                        // If more commands are instantly available in the queue, pull them into the same batch!
                        // (Stop pulling if we hit a Wait or Action, which automatically flushes the batch inside ProcessCommand)
                        while (_queue!.Count > 0 && enumerator.MoveNext())
                        {
                            try { ProcessCommand(enumerator.Current, ref _batch, ref _batchCount, dispatchSw); }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[InputQueue] Command error: {ex.Message}"); }
                        }

                        // Flush any remaining batched inputs before going back to sleep
                        FlushBatch(ref _batch, ref _batchCount);
                    }
                }
            }
            // Intentional: sauberer Shutdown des Consumer-Threads — no Fehler, only Logging-Kommentar (ERROR-001).
            catch (OperationCanceledException) { System.Diagnostics.Debug.WriteLine("[InputQueue] Consumer loop stopped (cancellation)."); }
            finally { enumerator.Dispose(); }
        }

        private void ProcessCommand(InputCmd cmd, ref List<INPUT> batch, ref int batchCount, Stopwatch dispatchSw)
                {
                    // PERF-005: Start latency measurement for this command
                    var latencyStopwatch = System.Diagnostics.Stopwatch.StartNew();

                    // GEX-002: Measure Queue dispatch latency (opt-in, zero-allocation when disabled per PERF-010)
                    var queueSw = InputLatencyBudgets.EnableBudgetTracking ? System.Diagnostics.Stopwatch.StartNew() : null;

                    // Record dispatch latency (time from dequeue to actual processing start)
                    dispatchSw.Stop();
                    _latencyTracker?.RecordDispatchLatency(dispatchSw.Elapsed.TotalMilliseconds, _controllerId);

                    // If the command is NOT a SendInput command, we MUST flush the batch first to maintain chronological order!
                    if (cmd.Type == CmdType.Wait || cmd.Type == CmdType.Action || cmd.Type == CmdType.MouseAbs)
                    {
                        FlushBatch(ref batch, ref batchCount);
                    }

                    switch (cmd.Type)
                    {
                        // SendInput Commands (Batched)
                        case CmdType.MouseRel:  batch.Add(CreateMouseInput(cmd.X, cmd.Y, 0, MOUSEEVENTF_MOVE | 0x2000)); batchCount++; break; // 0x2000 = NOCOALESCE
                        case CmdType.LeftDown:  batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_LEFTDOWN)); batchCount++; break;
                        case CmdType.LeftUp:    batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_LEFTUP)); batchCount++; break;
                        case CmdType.RightDown: batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_RIGHTDOWN)); batchCount++; break;
                        case CmdType.RightUp:   batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_RIGHTUP)); batchCount++; break;
                        case CmdType.Wheel:     batch.Add(CreateMouseInput(0, 0, (uint)cmd.X, 0x0800)); batchCount++; break; // MOUSEEVENTF_WHEEL
                        case CmdType.KeyDown:   batch.Add(CreateKeyInput((ushort)cmd.Key, KEYEVENTF_KEYDOWN)); batchCount++; break;
                        case CmdType.KeyUp:     batch.Add(CreateKeyInput((ushort)cmd.Key, KEYEVENTF_KEYUP)); batchCount++; break;

                        // Complex Atomic Commands (Batched internally)
                        case CmdType.AtomicLeftClick:
                            batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_LEFTDOWN));
                            FlushBatch(ref batch, ref batchCount); // Must flush before sleeping
                            Thread.Sleep(JitterService.ClickHold()); // Human-like click hold time
                            batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_LEFTUP));
                            break;
                        case CmdType.AtomicRightClick:
                            batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_RIGHTDOWN));
                            FlushBatch(ref batch, ref batchCount); // Must flush before sleeping
                            Thread.Sleep(JitterService.ClickHold()); // Human-like click hold time
                            batch.Add(CreateMouseInput(0, 0, 0, MOUSEEVENTF_RIGHTUP));
                            break;

                        // Non-Batchable Commands (Already flushed above)
                        case CmdType.Wait:
                            Thread.Sleep(cmd.X);
                            break;
                        case CmdType.Action:
                            cmd.Callback?.Invoke();
                            break;
                        case CmdType.MouseAbs:
                                            // Use SendInput with absolute coordinates for consistency with batching and failover
                                            {
                                                int screenWidth = GetSystemMetrics(SM_CXSCREEN);
                                                int screenHeight = GetSystemMetrics(SM_CYSCREEN);
                                                int absX = (int)((65535.0 * cmd.X) / screenWidth);
                                                int absY = (int)((65535.0 * cmd.Y) / screenHeight);
                                                batch.Add(CreateMouseInput(absX, absY, 0, MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE_NOCOALESCE));
                                                batchCount++;
                                            }
                                            break;
                    }

                    // GEX-002: Record Queue latency
                    if (queueSw != null) RecordQueueBudget(queueSw);

                    // PERF-005: Record latency after command execution
                    latencyStopwatch.Stop();
                    RecordInputLatency(latencyStopwatch.Elapsed.TotalMilliseconds * 1000.0); // Convert to microseconds
            
                    // PERF-005: Record total end-to-end latency
                    _latencyTracker?.RecordTotalLatency(latencyStopwatch.Elapsed.TotalMilliseconds, _controllerId);

                    OnCommandExecuted?.Invoke(cmd);
                }

                /// <summary>
                /// GEX-002: Record Queue dispatch latency budget.
                /// </summary>
                private void RecordQueueBudget(Stopwatch sw)
                {
                    sw.Stop();
                    InputLatencyBudgets.RecordQueue(sw.Elapsed.TotalMilliseconds);
                }

        private void FlushBatch(ref List<INPUT> batch, ref int batchCount)
                {
                    if (batchCount == 0) return;

                    // GEX-002: Measure Emulation (SendInput) latency (opt-in, zero-allocation when disabled per PERF-010)
                    var emulationSw = InputLatencyBudgets.EnableBudgetTracking ? System.Diagnostics.Stopwatch.StartNew() : null;

                    // PERF-005: Measure SendInput latency
                    var sendInputSw = System.Diagnostics.Stopwatch.StartNew();

                    // Send all gathered inputs to the Windows Kernel in a single API call
                    SendInput((uint)batchCount, batch.ToArray(), InputSize);

                    sendInputSw.Stop();
                    double elapsedMs = sendInputSw.Elapsed.TotalMilliseconds;
                    _latencyTracker?.RecordSendInputLatency(elapsedMs, batchCount, _controllerId);

                    // ROB-002: Failover-Mechanismus bekommt die gemessene Flush-Latenz.
                    SendInputLatencyRecorded?.Invoke(elapsedMs);

                    // GEX-002: Record Emulation latency
                    if (emulationSw != null) RecordEmulationBudget(emulationSw);

                    // Clear the batch for next accumulation
                    batch.Clear();
                    batchCount = 0;
                }

                /// <summary>
                /// GEX-002: Record Emulation (SendInput) latency budget.
                /// </summary>
                private void RecordEmulationBudget(Stopwatch sw)
                {
                    sw.Stop();
                    InputLatencyBudgets.RecordEmulation(sw.Elapsed.TotalMilliseconds);
                }

        // PERF-005: Record input latency statistics (legacy)
        private void RecordInputLatency(double latencyMicroseconds)
        {
            Interlocked.Add(ref _totalInputLatencyUs, (long)latencyMicroseconds);
            long count = Interlocked.Increment(ref _inputCount);

            // Update max latency (thread-safe using CompareExchange loop)
            long currentMax = Interlocked.Read(ref _maxInputLatencyUs);
            while (latencyMicroseconds > currentMax)
            {
                if (Interlocked.CompareExchange(ref _maxInputLatencyUs, (long)latencyMicroseconds, currentMax) == currentMax)
                    break;
                currentMax = Interlocked.Read(ref _maxInputLatencyUs);
            }

            // Emit ETW event for high latency (throttled)
            if (latencyMicroseconds > 5000.0) // > 5ms threshold
            {
                RagnaControllerETW.Log.InputLatencyP99((long)latencyMicroseconds, "InputCommandQueue");
            }
        }

        // --- Helper Methods for Struct Creation ---
        private static INPUT CreateMouseInput(int dx, int dy, uint data, uint flags)
        {
            return new INPUT
            {
                type = INPUT_MOUSE,
                Data = new INPUTUNION { mi = new MOUSEINPUT { dx = dx, dy = dy, mouseData = data, dwFlags = flags } }
            };
        }

        private static INPUT CreateKeyInput(ushort vk, uint flags)
        {
            return new INPUT
            {
                type = INPUT_KEYBOARD,
                Data = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = flags } }
            };
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _cts?.Dispose();
                _isDisposed = true;
            }
        }
    }
}