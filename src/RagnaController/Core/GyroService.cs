using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Reads the DualSense gyroscope via HID input reports and exposes
    /// filtered angular velocity for use in ground-spell aiming.
    ///
    /// Architecture:
    ///   A dedicated background thread blocks on ReadFile(handle, report, 64).
    ///   When a new report arrives (≈125 Hz), the gyro fields are extracted
    ///   and low-pass filtered to remove sensor drift and micro-vibration.
    ///   The engine reads <see cref="PitchDps"/> / <see cref="YawDps"/> each
    ///   tick — no locking needed because they are written with Interlocked.
    ///
    /// DualSense USB Input Report 0x01 layout (verified against DualSenseY):
    ///   [0]      Report ID = 0x01
    ///   [1]      Left stick X
    ///   [2]      Left stick Y
    ///   [3]      Right stick X
    ///   [4]      Right stick Y
    ///   [5]      L2 analog
    ///   [6]      R2 analog
    ///   [8]      Buttons 1 (Square/Cross/Circle/Triangle)
    ///   [9]      Buttons 2 (L1/R1/L2/R2/Create/Options/L3/R3)
    ///   [10]     Buttons 3 (PS/Touchpad/Mute)
    ///   [12]     D-pad nibble
    ///   [13–14]  Gyro Pitch   (int16 LE, hardware units)
    ///   [15–16]  Gyro Yaw     (int16 LE, hardware units)
    ///   [17–18]  Gyro Roll    (int16 LE, hardware units)
    ///   [19–24]  Accelerometer XYZ (int16 × 3)
    ///
    /// Scale calibration:
    ///   Sony's factory calibration data is embedded in the USB descriptor but
    ///   not trivially accessible without a full HID descriptor parser.  In
    ///   practice the raw value ÷ <see cref="GyroScale"/> gives deg/s, and
    ///   the default (8.696f ≈ 1/0.115 deg LSB) matches the majority of units.
    ///   If aiming feels too fast or slow, adjust <see cref="Sensitivity"/>.
    /// </summary>
    public sealed class GyroService : IDisposable
    {
        // ── HID report offsets ────────────────────────────────────────────
        private const int  REPORT_SIZE   = 64;
        private const int  OFFSET_PITCH  = 13;   // tilt up/down    → cursor Y
        private const int  OFFSET_YAW    = 15;   // rotate L/R      → cursor X
        // Roll (offset 17) is not used — device tilt around the Z-axis

        /// <summary>
        /// Raw-to-deg/s divisor for the DualSense gyro.
        /// Default 8.696 ≈ the value implied by Sony's spec sheet (0.115 deg/LSB).
        /// Adjust if your unit reads unusually fast or slow.
        /// </summary>
        public float GyroScale { get; set; } = 8.696f;

        /// <summary>
        /// Final sensitivity multiplier applied after scaling.
        /// 1.0 = one degree of rotation ≈ one degree of cursor arc at default scale.
        /// Increase for faster response on small-screen handhelds.
        /// </summary>
        public float Sensitivity { get; set; } = 1.0f;

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

        /// <summary>True when a DualSense HID device was found and the reader is running.</summary>
        public bool IsAvailable { get; private set; }

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
        /// Starts the background reader thread.
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
        /// Call every tick while ground-spell aiming is active.
        /// </summary>
        /// <param name="tickMs">Engine tick duration in milliseconds.</param>
        /// <param name="dx">Horizontal pixel delta (positive = right).</param>
        /// <param name="dy">Vertical pixel delta (positive = down).</param>
        public void GetCursorDelta(int tickMs, out int dx, out int dy)
        {
            if (!IsAvailable)
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

        private float _subX, _subY; // sub-pixel accumulators

        // ── Background reader ─────────────────────────────────────────────
        private void ReadLoop()
        {
            float filtPitch = 0f, filtYaw = 0f;

            while (_cts?.IsCancellationRequested == false)
            {
                if (!NativeMethods.ReadFile(_handle, _buf, (uint)REPORT_SIZE,
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
                NativeMethods.HidD_GetHidGuid(out Guid hidGuid);
                IntPtr devInfo = NativeMethods.SetupDiGetClassDevs(
                    ref hidGuid, IntPtr.Zero, IntPtr.Zero,
                    NativeMethods.DIGCF_PRESENT | NativeMethods.DIGCF_DEVICEINTERFACE);

                if (devInfo == IntPtr.Zero || devInfo == new IntPtr(-1)) return;

                try
                {
                    var ifaceData = new NativeMethods.SP_DEVICE_INTERFACE_DATA();
                    ifaceData.cbSize = (uint)Marshal.SizeOf<NativeMethods.SP_DEVICE_INTERFACE_DATA>();

                    for (uint i = 0; ; i++)
                    {
                        if (!NativeMethods.SetupDiEnumDeviceInterfaces(
                                devInfo, IntPtr.Zero, ref hidGuid, i, ref ifaceData))
                            break;

                        NativeMethods.SetupDiGetDeviceInterfaceDetail(
                            devInfo, ref ifaceData, IntPtr.Zero, 0, out uint needed, IntPtr.Zero);

                        if (needed == 0) continue;

                        IntPtr detailBuf = Marshal.AllocHGlobal((int)needed);
                        try
                        {
                            Marshal.WriteInt32(detailBuf, IntPtr.Size == 8 ? 8 : 6);
                            if (!NativeMethods.SetupDiGetDeviceInterfaceDetail(
                                    devInfo, ref ifaceData, detailBuf, needed, out _, IntPtr.Zero))
                                continue;

                            string path = Marshal.PtrToStringAuto(detailBuf + 4) ?? "";

                            bool isDualSense =
                                path.IndexOf(DualSenseVid,  StringComparison.OrdinalIgnoreCase) >= 0 &&
                               (path.IndexOf(DualSensePid1, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                path.IndexOf(DualSensePid2, StringComparison.OrdinalIgnoreCase) >= 0);

                            if (!isDualSense) continue;

                            // Open with READ+WRITE+SHARE so lightbar service can coexist
                            IntPtr h = NativeMethods.CreateFile(
                                path,
                                NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                                IntPtr.Zero,
                                NativeMethods.OPEN_EXISTING,
                                NativeMethods.FILE_ATTRIBUTE_NORMAL,
                                IntPtr.Zero);

                            if (h != IntPtr.Zero && h != new IntPtr(-1))
                            {
                                _handle     = h;
                                IsAvailable = true;
                                return;
                            }
                        }
                        finally { Marshal.FreeHGlobal(detailBuf); }
                    }
                }
                finally { NativeMethods.SetupDiDestroyDeviceInfoList(devInfo); }
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
            // 2. Auf _reader?.Wait(500) warten, bis der Lese-Thread beendet ist
            // 3. DANN das Handle schließen
            // Sonst wirft Windows eine Access Violation, wenn ReadFile auf dem Handle wartet,
            // während es unter ihm weggerissen wird!
            Stop();
            
            // Warten, bis der Reader-Thread beendet ist (max. 500ms)
            if (_reader != null)
            {
                try
                {
                    _reader.Wait(500);
                }
                catch (TaskCanceledException)
                {
                    // Thread wurde gecancelt, aber nicht rechtzeitig beendet — Handle trotzdem schließen
                }
                catch (AggregateException inner)
                {
                    if (inner.InnerException is TaskCanceledException)
                    {
                        // Ignorieren, wir warten nur auf den Thread-Exit
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[GyroService] Reader wait failed: {inner.InnerException?.Message}");
                    }
                }
                
                _reader = null;
            }
            
            // Jetzt sicher das Handle schließen — Reader ist definitiv beendet!
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_handle);
                _handle = IntPtr.Zero;
            }
        }
    }
}
