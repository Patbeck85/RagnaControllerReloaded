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
    // Input Command Types (must match all switch cases)
    public enum CmdType
    {
        MouseRel, LeftDown, LeftUp, RightDown, RightUp,
        KeyDown, KeyUp, Wheel, AtomicLeftClick, AtomicRightClick,
        Wait, Action, MouseAbs
    }

    // Input Command Struct (must match all constructor patterns)
    public sealed class InputCmd
    {
        public CmdType Type { get; init; }
        public ushort Key { get; init; }
        public int X { get; set; }
        public int Y { get; init; }
        public Action? Callback { get; init; }

        public InputCmd(CmdType type) : this(type, 0, 0, null) { }
        public InputCmd(CmdType type, ushort key) : this(type, key, 0, null) { }
        public InputCmd(CmdType type, int x, int y) : this(type, 0, x, y) { }
        public InputCmd(CmdType type, ushort key, int x, int y)
        {
            Type = type;
            Key = (ushort)(key & 0xFFFF); // Ensure valid ushort range
            X = x;
            Y = y;
        }

        public InputCmd(CmdType type, Action? callback) : this(type, 0, 0, callback) { }
        public InputCmd(CmdType type, ushort key, Action? callback) : this(type, key, 0, callback) { }
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

        public bool IsAddingCompleted => _queue!.IsAddingCompleted;
        public int QueueCount => _queue!.Count;
        public long SessionSavedClicks => Interlocked.Read(ref _savedClicks);
        public long SessionSavedKeystrokes => Interlocked.Read(ref _savedKeystrokes);
        public bool IsChatting => _isChatting;

        // Input Latency Properties (PERF-005 - legacy)
        public double AverageInputLatencyMs => _inputCount > 0 ? (Interlocked.Read(ref _totalInputLatencyUs) / (double)_inputCount) / 1000.0 : 0;
        public long TotalInputsProcessed => Interlocked.Read(ref _inputCount);
        public double MaxInputLatencyMs => Interlocked.Read(ref _maxInputLatencyUs) / 1000.0;

        // Commands collection for testing and inspection (only populated in DEBUG builds)
        public List<InputCmd> Commands { get; } = new();
#if DEBUG
        private void RecordCommand(InputCmd cmd) => Commands.Add(cmd);
#else
        private void RecordCommand(InputCmd cmd) { }
#endif

        /// <summary>
        /// Creates a new InputCommandQueue with optional latency tracking.
        /// </summary>
        /// <param name="latencyTracker">Optional latency tracker for PERF-005 end-to-end latency measurement.</param>
        /// <param name="controllerId">Identifier for the controller (used for per-controller latency tracking).</param>
        public InputCommandQueue(InputLatencyTracker? latencyTracker = null, string controllerId = "Default")
        {
            _latencyTracker = latencyTracker;
            _controllerId = controllerId;
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
        public void LeftDown() => Enqueue(new InputCmd(CmdType.LeftDown));
        public void LeftUp() => Enqueue(new InputCmd(CmdType.LeftUp));
        public void RightDown() => Enqueue(new InputCmd(CmdType.RightDown));
        public void RightUp() => Enqueue(new InputCmd(CmdType.RightUp));
        public void LeftClick()   { Enqueue(new InputCmd(CmdType.LeftDown)); Interlocked.Increment(ref _savedClicks); }
        public void RightClick()  { Enqueue(new InputCmd(CmdType.RightDown)); Interlocked.Increment(ref _savedClicks); }
        public void DoubleClick() { Enqueue(new InputCmd(CmdType.AtomicLeftClick)); Interlocked.Add(ref _savedClicks, 2); }
        public void MoveMouseRelative(int dx, int dy) => Enqueue(new InputCmd(CmdType.MouseRel, dx, dy));
        public void MoveMouseAbsolute(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        public void MouseMove(int dx, int dy) => Enqueue(new InputCmd(CmdType.MouseRel, dx, dy));
        public void MouseAbs(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        public void MouseMoveAbsolute(int x, int y) => Enqueue(new InputCmd(CmdType.MouseAbs, x, y));
        public void AtomicLeftClick() => Enqueue(new InputCmd(CmdType.AtomicLeftClick));
        public void AtomicRightClick() => Enqueue(new InputCmd(CmdType.AtomicRightClick));

        // Keyboard
        public void KeyDown(VirtualKey k) => Enqueue(new InputCmd(CmdType.KeyDown, (ushort)k));
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
        public void PanicHeal(VirtualKey k) { for (int i = 0; i < 10; i++) TapKey(k); }

        // Wheel
        public void Wheel(int delta) => Enqueue(new InputCmd(CmdType.Wheel, (ushort)delta));
        public void ScrollWheel(int delta)
        {
            INPUT inp = default;
            inp.type               = INPUT_MOUSE;
            inp.Data.mi.mouseData  = (uint)delta;
            inp.Data.mi.dwFlags    = 0x0800; // MOUSEEVENTF_WHEEL
            uint sent1 = SendInput(1, ref inp, InputSize);
            if (sent1 == 0) System.Diagnostics.Debug.WriteLine("[Win32Input] ScrollWheel blocked — run as admin?");
        }

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

                    // Release-Befehl hinzufügen — verhindert permanent gedrückt gehaltene Tasten
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
        public void Wait(int ms) => Enqueue(InputCmd.CreateWait(ms));
        public void Action(Action callback) => Enqueue(new InputCmd(CmdType.Action, callback));

        public void Start()
        {
            if (_consumerThread != null && _consumerThread.IsAlive) return;

            _queue = new BlockingCollection<InputCmd>();
            _consumerThread = new Thread(Process) { Name = "InputCommandQueue_Consumer", IsBackground = true };
            _consumerThread.Start();
        }

        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null!;
            }

            if (_consumerThread != null)
            {
                _consumerThread.Join(1000);
                _consumerThread = null;
            }
        }

        public void RequestShutdown()
        {
            _shutdownRequested = true;
            Dispose();
        }

        private void Process()
        {
            var enumerator = _queue!.GetConsumingEnumerable(_cts.Token).GetEnumerator();

            try
            {
                while (!_cts.Token.IsCancellationRequested)
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
            catch (OperationCanceledException) { }
            finally { enumerator.Dispose(); }
        }

        private void ProcessCommand(InputCmd cmd, ref List<INPUT> batch, ref int batchCount, Stopwatch dispatchSw)
        {
            // PERF-005: Start latency measurement for this command
            var latencyStopwatch = System.Diagnostics.Stopwatch.StartNew();

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
                    SetCursorPos(cmd.X, cmd.Y);
                    break;
            }

            // PERF-005: Record latency after command execution
            latencyStopwatch.Stop();
            RecordInputLatency(latencyStopwatch.Elapsed.TotalMilliseconds * 1000.0); // Convert to microseconds
            
            // PERF-005: Record total end-to-end latency
            _latencyTracker?.RecordTotalLatency(latencyStopwatch.Elapsed.TotalMilliseconds, _controllerId);

            OnCommandExecuted?.Invoke(cmd);
        }

        private void FlushBatch(ref List<INPUT> batch, ref int batchCount)
        {
            if (batchCount == 0) return;

            // PERF-005: Measure SendInput latency
            var sendInputSw = System.Diagnostics.Stopwatch.StartNew();
            
            // Send all gathered inputs to the Windows Kernel in a single API call
            SendInput((uint)batchCount, batch.ToArray(), InputSize);
            
            sendInputSw.Stop();
            _latencyTracker?.RecordSendInputLatency(sendInputSw.Elapsed.TotalMilliseconds, batchCount, _controllerId);

            // Clear the batch for next accumulation
            batch.Clear();
            batchCount = 0;
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