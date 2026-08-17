using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using RagnaController.Models;

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
        private List<NativeMethods.INPUT> _batch = new(64);
        private int _batchCount;

        // RSI Tracking
        private long _savedClicks;
        private long _savedKeystrokes;

        // Chat state
        private bool _isChatting;
        private volatile bool _shutdownRequested;

        public event Action<InputCmd>? OnCommandEnqueued;
        public event Action<InputCmd>? OnCommandExecuted;

        public bool IsAddingCompleted => _queue!.IsAddingCompleted;
        public int QueueCount => _queue!.Count;
        public long SessionSavedClicks => Interlocked.Read(ref _savedClicks);
        public long SessionSavedKeystrokes => Interlocked.Read(ref _savedKeystrokes);
        public bool IsChatting => _isChatting;

        // Commands collection for testing and inspection
        public List<InputCmd> Commands { get; } = new();

        public void Enqueue(InputCmd cmd)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(InputCommandQueue));
            if (!_queue!.IsAddingCompleted)
            {
                _queue!.TryAdd(cmd);
                OnCommandEnqueued?.Invoke(cmd);
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
            NativeMethods.INPUT inp = default;
            inp.type               = NativeMethods.INPUT_MOUSE;
            inp.Data.mi.mouseData  = (uint)delta;
            inp.Data.mi.dwFlags    = 0x0800; // MOUSEEVENTF_WHEEL
            uint sent1 = NativeMethods.SendInput(1, ref inp, NativeMethods.InputSize);
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
                    NativeMethods.INPUT inp = default;
                    inp.type            = NativeMethods.INPUT_KEYBOARD;
                    inp.Data.ki.wVk     = 0;           // Must be 0 for KEYEVENTF_UNICODE
                    inp.Data.ki.wScan   = (ushort)c;   // Unicode char goes in wScan
                    inp.Data.ki.dwFlags = 0x0004;      // KEYEVENTF_UNICODE
                    NativeMethods.SendInput(1, ref inp, NativeMethods.InputSize);

                    // Release-Befehl hinzufügen — verhindert permanent gedrückt gehaltene Tasten
                    NativeMethods.INPUT inpUp = default;
                    inpUp.type            = NativeMethods.INPUT_KEYBOARD;
                    inpUp.Data.ki.wVk     = 0;           // Must be 0 for KEYEVENTF_UNICODE
                    inpUp.Data.ki.wScan   = (ushort)c;   // Same Unicode char for release
                    inpUp.Data.ki.dwFlags = 0x0004 | 0x0008; // KEYEVENTF_UNICODE | KEYEVENTF_KEYUP
                    NativeMethods.SendInput(1, ref inpUp, NativeMethods.InputSize);

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
                        try { ProcessCommand(enumerator.Current, ref _batch, ref _batchCount); }
                        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[InputQueue] Command error: {ex.Message}"); }

                        // If more commands are instantly available in the queue, pull them into the same batch!
                        // (Stop pulling if we hit a Wait or Action, which automatically flushes the batch inside ProcessCommand)
                        while (_queue!.Count > 0 && enumerator.MoveNext())
                        {
                            ProcessCommand(enumerator.Current, ref _batch, ref _batchCount);
                        }

                        // Flush any remaining batched inputs before going back to sleep
                        FlushBatch(ref _batch, ref _batchCount);
                    }
                }
            }
            catch (OperationCanceledException) { }
            finally { enumerator.Dispose(); }
        }

        private void ProcessCommand(InputCmd cmd, ref List<NativeMethods.INPUT> batch, ref int batchCount)
        {
            // If the command is NOT a SendInput command, we MUST flush the batch first to maintain chronological order!
            if (cmd.Type == CmdType.Wait || cmd.Type == CmdType.Action || cmd.Type == CmdType.MouseAbs)
            {
                FlushBatch(ref batch, ref batchCount);
            }

            switch (cmd.Type)
            {
                // SendInput Commands (Batched)
                case CmdType.MouseRel:  batch.Add(CreateMouseInput(cmd.X, cmd.Y, 0, NativeMethods.MOUSEEVENTF_MOVE | 0x2000)); batchCount++; break; // 0x2000 = NOCOALESCE
                case CmdType.LeftDown:  batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_LEFTDOWN)); batchCount++; break;
                case CmdType.LeftUp:    batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_LEFTUP)); batchCount++; break;
                case CmdType.RightDown: batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_RIGHTDOWN)); batchCount++; break;
                case CmdType.RightUp:   batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_RIGHTUP)); batchCount++; break;
                case CmdType.Wheel:     batch.Add(CreateMouseInput(0, 0, (uint)cmd.X, 0x0800)); batchCount++; break; // MOUSEEVENTF_WHEEL
                case CmdType.KeyDown:   batch.Add(CreateKeyInput((ushort)cmd.Key, NativeMethods.KEYEVENTF_KEYDOWN)); batchCount++; break;
                case CmdType.KeyUp:     batch.Add(CreateKeyInput((ushort)cmd.Key, NativeMethods.KEYEVENTF_KEYUP)); batchCount++; break;

                // Complex Atomic Commands (Batched internally)
                case CmdType.AtomicLeftClick:
                    batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_LEFTDOWN));
                    FlushBatch(ref batch, ref batchCount); // Must flush before sleeping
                    Thread.Sleep(50); // FIX: JitterService ist ein Typ, nicht eine Instanz - verwenden wir konstanten Wert
                    batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_LEFTUP));
                    break;
                case CmdType.AtomicRightClick:
                    batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_RIGHTDOWN));
                    FlushBatch(ref batch, ref batchCount); // Must flush before sleeping
                    Thread.Sleep(50); // FIX: JitterService ist ein Typ, nicht eine Instanz - verwenden wir konstanten Wert
                    batch.Add(CreateMouseInput(0, 0, 0, NativeMethods.MOUSEEVENTF_RIGHTUP));
                    break;

                // Non-Batchable Commands (Already flushed above)
                case CmdType.Wait:
                    Thread.Sleep(cmd.X);
                    break;
                case CmdType.Action:
                    cmd.Callback?.Invoke();
                    break;
                case CmdType.MouseAbs:
                    NativeMethods.SetCursorPos(cmd.X, cmd.Y);
                    break;
            }

            OnCommandExecuted?.Invoke(cmd);
        }

        private void FlushBatch(ref List<NativeMethods.INPUT> batch, ref int batchCount)
        {
            if (batchCount == 0) return;

            // Send all gathered inputs to the Windows Kernel in a single API call
            NativeMethods.SendInput((uint)batchCount, batch.ToArray(), NativeMethods.InputSize);

            // Clear the batch for next accumulation
            batch.Clear();
            batchCount = 0;
        }

        // --- Helper Methods for Struct Creation ---
        private static NativeMethods.INPUT CreateMouseInput(int dx, int dy, uint data, uint flags)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_MOUSE,
                Data = new NativeMethods.INPUTUNION { mi = new NativeMethods.MOUSEINPUT { dx = dx, dy = dy, mouseData = data, dwFlags = flags } }
            };
        }

        private static NativeMethods.INPUT CreateKeyInput(ushort vk, uint flags)
        {
            return new NativeMethods.INPUT
            {
                type = NativeMethods.INPUT_KEYBOARD,
                Data = new NativeMethods.INPUTUNION { ki = new NativeMethods.KEYBDINPUT { wVk = vk, dwFlags = flags } }
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
