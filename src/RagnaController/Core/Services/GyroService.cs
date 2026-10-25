using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static RagnaController.Core.NativeMethods;
using RagnaController.Core.Hardware;

namespace RagnaController.Core
{
    /// <summary>
    /// Reads the DualSense gyroscope via HID input reports and exposes
    /// filtered angular velocity for use in groand-spell aiming.
    /// </summary>
    public sealed class GyroService : IDisposable
    {
        // ── HID report offsets ────────────────────────────────────────────
        private const int  REPORT_SIZE   = 64;
        private const int  OFFSET_PITCH  = 13;   // tilt up/down    → cursor Y
        private const int  OFFSET_YAW    = 15;   // rotate L/R      → cursor X
        // Roll (offset 17) is not used — device tilt aroand the Z-axis

        /// <summary>
        /// Raw-to-deg/s divisor for the DualSense gyro.
        /// </summary>
        public float GyroScale { get; set; } = 8.696f;

        /// <summary>
        /// Final sensitivity multiplier applied after scaling.
        /// </summary>
        public float Sensitivity
        {
            get => _sensitivity;
            set
            {
                if (_disposed) return;
                _sensitivity = Math.Clamp(value, 0.1f, 5.0f);
            }
        }

        private float _sensitivity = 1.0f;

        /// <summary>
        /// Low-pass filter coefficient (0.0 = instant, 1.0 = frozen).
        /// 0.25 provides ~4-tick lag which eliminates sensor buzz without
        /// introducing perceptible aiming delay.
        /// </summary>
        public float Smoothing { get; set; } = 0.25f;

        /// <summary>
        /// Raw-unit threshold below which the gyro output is zeroed.
        /// Eliminates the 10–50 unit DC drift present on most DualSense units.
        /// </summary>
        public int DriftThreshold { get; set; } = 64;

        // ── Exposed state (written by reader thread, read by engine tick) ─
        // Interlocked.Exchange on float requires the bit-cast trick.
        private float _pitchDps;
        private float _yawDps;

        /// <summary>Filtered pitch angular velocity in deg/s (positive = tilt up).</summary>
        public float PitchDps => _pitchDps;
        /// <summary>Filtered yaw angular velocity in deg/s (positive = rotate right).</summary>
        public float YawDps   => _yawDps;

        /// <summary>True when a DualSense HID device was foand and the reader is running.</summary>
        public bool IsAvailable { get; private set; }

        /// <summary>Gyro-Eingabe enabled/disabled (Software-Flag, unabhängig from Hardware-Verfügbarkeit).</summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// Gyro-Daten manual einspeisen (for Tests / Simulierte Input ohne echtes HID-Device).
        /// Nimmt Angular Velocity in Grad/s entgegen and wendet Low-Pass-Filter an.
        /// </summary>
        public void FeedGyro(float pitchRateDegPerS, float yawRateDegPerS, float rollRateDegPerS, float deltaMs)
        {
            if (deltaMs <= 0f) return;

            // Drift threshold (wie im Reader-Loop)
            var rawPitch = pitchRateDegPerS * GyroScale;
            var rawYaw   = yawRateDegPerS   * GyroScale;
            if (Math.Abs(rawPitch) < DriftThreshold) rawPitch = 0;
            if (Math.Abs(rawYaw)   < DriftThreshold) rawYaw   = 0;

            // Scale to deg/s
            float scaledPitch = rawPitch / GyroScale;
            float scaledYaw   = rawYaw   / GyroScale;

            // Low-pass filter (identisch to ReadLoop)
            float alpha = 1f - Smoothing;
            _pitchDps = _pitchDps * Smoothing + scaledPitch * alpha;
            _yawDps   = _yawDps   * Smoothing + scaledYaw   * alpha;
        }

        // ── Internal ──────────────────────────────────────────────────────
        private IntPtr            _handle = IntPtr.Zero;
        private CancellationTokenSource? _cts;
        private Task?             _reader;
        private readonly byte[]   _buf = new byte[REPORT_SIZE];
        private bool              _disposed;

        // DualSense PID constants (shared with DualSenseHardwareService)
        private const string DualSenseVid  = "VID_054C";
        private const string DualSensePid1 = "PID_0CE6";
        private const string DualSensePid2 = "PID_0DF2";

        public GyroService() => TryOpen();

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Starts the backgroand reader thread.
        /// Safe to call multiple times; no-op if already running.
        /// </summary>
        public void Start()
        {
            if (!IsAvailable || _reader != null) return;
            _cts    = new CancellationTokenSource();
            _reader = Task.Factory.StartNew(ReadLoop, _cts.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts    = null;
            _reader = null;
        }

        /// <summary>
        /// Convert the current angular velocity to a cursor pixel delta for one engine tick.
        /// Call every tick while groand-spell aiming is active.
        /// </summary>
        /// <param name="dx">Horizontal pixel delta (positive = right).</param>
        /// <param name="dy">Vertical pixel delta (positive = down).</param>
        public void GetCursorDelta(int tickMs, out int dx, out int dy)
        {
            if (!IsEnabled)
            {
                dx = dy = 0;
                return;
            }

            float dt     = tickMs * 0.001f;
            float rawX   = _yawDps   * Sensitivity * dt;
            float rawY   = -_pitchDps * Sensitivity * dt; // pitch up → cursor up = negative Y

            // Accumulate sub-pixel remainder (stored in fields to survive across ticks)
            _subX += rawX;
            _subY += rawY;

            dx = (int)_subX;
            dy = (int)_subY;

            _subX -= dx;
            _subY -= dy;
        }

        /// <summary>Akumuliertes Maus-Delta konsumieren and on 0 zurücksetzen (Standard-Tick 16ms).</summary>
        public (int dx, int dy) ConsumeDelta()
        {
            GetCursorDelta(16, out var dx, out var dy);
            return (dx, dy);
        }

        /// <summary>Internen Zustand zurücksetzen (z.B. nach Sleep/Resume).</summary>
                public void Reset()
                {
                    _subX = 0f;
                    _subY = 0f;
                    _pitchDps = 0f;
                    _yawDps = 0f;
                    System.Diagnostics.Debug.WriteLine("[GyroService] State reset.");
                }

        private float _subX, _subY; // sub-pixel accumulators

        // ── Backgroand reader ─────────────────────────────────────────────
        private void ReadLoop()
        {
            float filtPitch = 0f, filtYaw = 0f;

                    while (_cts?.IsCancellationRequested == false)
                    {
                        if (!ReadFile(_handle, _buf, (uint)REPORT_SIZE,
                                out uint read, IntPtr.Zero) || read < 19)
                {
                    // ReadFile can return false on disconnect — brief sleep before retry
                    Thread.Sleep(16);
                    continue;
                }

                // Extract raw int16 values (little-endian)
                short rawPitch = BitConverter.ToInt16(_buf, OFFSET_PITCH);
                short rawYaw   = BitConverter.ToInt16(_buf, OFFSET_YAW);

                // Drift threshold (zero-point correction)
                if (Math.Abs(rawPitch) < DriftThreshold) rawPitch = 0;
                if (Math.Abs(rawYaw)   < DriftThreshold) rawYaw   = 0;

                // Scale to deg/s
                float scaledPitch = rawPitch / GyroScale;
                float scaledYaw   = rawYaw   / GyroScale;

                // Low-pass filter: new = (1-α)×old + α×new
                float alpha = 1f - Smoothing;
                filtPitch = filtPitch * Smoothing + scaledPitch * alpha;
                filtYaw   = filtYaw   * Smoothing + scaledYaw   * alpha;

                // Write with volatile semantics (float fields, not Interlocked — .NET guarantees
                // aligned float writes are atomic on x86/x64, and this is read-only from engine)
                _pitchDps = filtPitch;
                _yawDps   = filtYaw;
            }
        }

        // ── Device enumeration ────────────────────────────────────────────
                private void TryOpen()
                {
                    try
                    {
                        var devices = HidDeviceUtils.EnumerateDualSenseDevices();
                        foreach (var path in devices)
                        {
                            // Open with READ+WRITE+SHARE so lightbar service can coexist
                            IntPtr h = CreateFile(
                                path,
                                GENERIC_READ | GENERIC_WRITE,
                                FILE_SHARE_READ | FILE_SHARE_WRITE,
                                IntPtr.Zero,
                                OPEN_EXISTING,
                                FILE_ATTRIBUTE_NORMAL,
                                IntPtr.Zero);

                            if (h != IntPtr.Zero && h != new IntPtr(-1))
                            {
                                _handle = h;
                                IsAvailable = true;
                                return;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GyroService] Init failed: {ex.Message}");
                    }
                }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            
            // FIX: Race Condition vermeiden!
            // 1. CancellationTokenSource canceln
            // 2. On _reader?.Wait(500) warten, bis der Lese-Thread beendet is
            // 3. DANN das Handle schließen
            // Sonst wirft Windows eine Access Violation, if ReadFile on dem Handle wartet,
            // während es unter ihm weggerissen is!
            Stop();
            
            // Warten, bis der Reader-Thread beendet is (max. 500ms)
            if (_reader != null)
            {
                try
                {
                    _reader.Wait(500);
                }
                catch (TaskCanceledException)
                {
                    // Thread wurde gecancelt, but nicht rechtzeitig beendet — Handle trotzdem schließen
                }
                catch (AggregateException inner)
                {
                    if (inner.InnerException is TaskCanceledException)
                    {
                        // Ignorieren, wir warten only on den Thread-Exit
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GyroService] Reader wait failed: {inner.InnerException?.Message}");
                    }
                }
                
                _reader = null;
            }
            
            // Jetzt sicher das Handle schließen — Reader is definitiv beendet!
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
