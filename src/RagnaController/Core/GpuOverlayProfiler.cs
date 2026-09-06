using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using RagnaController.Core;

namespace RagnaController.Core
{
    /// <summary>
    /// GPU/Overlay Render Profiling Service
    /// Captures WPF render tier, frame timing, composition engine metrics, and GPU memory pressure.
    /// Integrates with ETW for telemetry and FrameBudgetMonitor for budget tracking.
    /// </summary>
    public sealed class GpuOverlayProfiler : IDisposable
    {
        private readonly string _overlayType;
        private readonly RagnaControllerETW _etw = RagnaControllerETW.Log;
        private readonly AdvancedLogger? _logger;
        private readonly FrameBudgetMonitor? _frameMonitor;

        // Frame timing components
        private readonly Stopwatch _frameStopwatch = new();
        private readonly Stopwatch _layoutStopwatch = new();
        private readonly Stopwatch _renderStopwatch = new();
        private readonly Stopwatch _compositeStopwatch = new();

        // Sampling
        private Timer? _samplingTimer;
        private readonly CancellationTokenSource _cts = new();
        private int _frameCount = 0;
        private int _droppedFrames = 0;
        private double _lastFrameTimeMs = 0;
        private long _lastPresentTime = 0;

        // Render tier info (cached)
        private int _wpfRenderTier = -1;
        private string _deviceName = "Unknown";
        private int _pixelShaderVersion = 0;
        private int _vertexShaderVersion = 0;

        // GPU memory tracking (via DXGI if available)
        private long _gpuDedicatedMb = 0;
        private long _gpuSharedMb = 0;
        private long _gpuBudgetMb = 0;

        public GpuOverlayProfiler(string overlayType, AdvancedLogger? logger = null, FrameBudgetMonitor? frameMonitor = null)
        {
            _overlayType = overlayType;
            _logger = logger;
            _frameMonitor = frameMonitor;

            InitializeRenderTier();
            StartSampling();

            _logger?.Info($"[GpuOverlayProfiler] Started for {_overlayType} — Render Tier: {_wpfRenderTier}, Device: {_deviceName}");
            _etw.WpfRenderTier(_wpfRenderTier, _deviceName, _pixelShaderVersion, _vertexShaderVersion);
        }

        private void InitializeRenderTier()
        {
            try
            {
                // WPF RenderCapability.Tier returns 0 (no HW accel), 1 (partial), 2 (full)
                _wpfRenderTier = (RenderCapability.Tier >> 16);

                // Try to get more detailed GPU info via WMI or DXGI
                // For now, we'll use basic WPF info
                _deviceName = GetGpuDeviceName();
                _pixelShaderVersion = GetShaderVersion(true);
                _vertexShaderVersion = GetShaderVersion(false);
            }
            catch (Exception ex)
            {
                _logger?.Warn($"[GpuOverlayProfiler] Failed to initialize render tier: {ex.Message}");
                _wpfRenderTier = 0;
            }
        }

        private string GetGpuDeviceName()
        {
            try
            {
                // Use System.Management to query Win32_VideoController
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var name = obj["Name"]?.ToString();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch { }
            return "Unknown GPU";
        }

        private int GetShaderVersion(bool pixel)
        {
            // WPF doesn't expose this directly, return reasonable defaults based on tier
            return _wpfRenderTier >= 2 ? (pixel ? 50 : 40) : (_wpfRenderTier >= 1 ? (pixel ? 30 : 20) : 0);
        }

        /// <summary>
        /// Call at the start of a frame (before layout).
        /// </summary>
        public void BeginFrame()
        {
            _frameStopwatch.Restart();
            _layoutStopwatch.Restart();
        }

        /// <summary>
        /// Call after layout pass, before render.
        /// </summary>
        public void EndLayout()
        {
            _layoutStopwatch.Stop();
            _renderStopwatch.Restart();
        }

        /// <summary>
        /// Call after render pass, before composition.
        /// </summary>
        public void EndRender()
        {
            _renderStopwatch.Stop();
            _compositeStopwatch.Restart();
        }

        /// <summary>
        /// Call after composition/present. Completes the frame.
        /// </summary>
        public void EndFrame()
        {
            _compositeStopwatch.Stop();
            _frameStopwatch.Stop();

            var layoutMs = _layoutStopwatch.Elapsed.TotalMilliseconds;
            var renderMs = _renderStopwatch.Elapsed.TotalMilliseconds;
            var compositeMs = _compositeStopwatch.Elapsed.TotalMilliseconds;
            var totalMs = _frameStopwatch.Elapsed.TotalMilliseconds;

            _frameCount++;
            _lastFrameTimeMs = totalMs;

            // Track dropped frames (if frame time > 16.67ms for 60fps)
            if (totalMs > 16.67)
                _droppedFrames++;

            // Record in frame budget monitor
            _frameMonitor?.RecordTick(totalMs);

            // Emit ETW frame timing event (verbose, sampled)
            if (_frameCount % 10 == 0) // Every 10th frame
            {
                _etw.OverlayFrameTiming(_overlayType, layoutMs, renderMs, compositeMs, totalMs);
            }

            // Check for budget exceedance
            if (totalMs > 2.0) // Sub-2ms budget
            {
                _etw.OverlayFrameBudgetExceeded(_overlayType, totalMs, 2.0);
            }
        }

        private void StartSampling()
        {
            _samplingTimer = new Timer(SampleMetrics, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
        }

        private void SampleMetrics(object? state)
        {
            if (_cts.Token.IsCancellationRequested) return;

            try
            {
                // Calculate FPS
                var fps = _frameCount / 5.0; // 5-second window
                _frameCount = 0;

                // Get composition engine stats
                var compositionStats = GetCompositionStats();

                // Get GPU memory pressure
                GetGpuMemoryInfo();

                // Emit ETW events
                _etw.CompositionEngineStats(_overlayType, fps, compositionStats.FrameTimeMs, compositionStats.PresentMs, _droppedFrames);
                _droppedFrames = 0;

                _etw.GpuMemoryPressure(_overlayType, _gpuDedicatedMb, _gpuSharedMb, _gpuBudgetMb);

                _logger?.Debug($"[GpuOverlayProfiler] {_overlayType} — FPS: {fps:F1}, Frame: {compositionStats.FrameTimeMs:F2}ms, Present: {compositionStats.PresentMs:F2}ms, GPU: {_gpuDedicatedMb}MB dedicated");
            }
            catch (Exception ex)
            {
                _logger?.Warn($"[GpuOverlayProfiler] Sampling error: {ex.Message}");
            }
        }

        private (double FrameTimeMs, double PresentMs) GetCompositionStats()
        {
            // WPF doesn't expose composition engine stats directly
            // We estimate from our frame timing
            return (_lastFrameTimeMs, _lastFrameTimeMs * 0.3); // Estimate present as ~30% of frame
        }

        private void GetGpuMemoryInfo()
        {
            try
            {
                // Use DXGI via WMI or Performance Counters
                // For now, estimate from process working set and GPU process
                var process = Process.GetCurrentProcess();
                var workingSetMb = process.WorkingSet64 / (1024 * 1024);

                // Query GPU memory via WMI
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT AdapterRAM, AdapterCompatibility FROM Win32_VideoController");
                foreach (var obj in searcher.Get())
                {
                    var ram = obj["AdapterRAM"];
                    if (ram != null && long.TryParse(ram.ToString(), out var adapterRam))
                    {
                        _gpuDedicatedMb = adapterRam / (1024 * 1024);
                        break;
                    }
                }

                // Estimate shared/ budget
                _gpuSharedMb = workingSetMb;
                _gpuBudgetMb = _gpuDedicatedMb > 0 ? _gpuDedicatedMb * 3 / 4 : 2048; // 75% of dedicated or 2GB default
            }
            catch
            {
                _gpuDedicatedMb = 0;
                _gpuSharedMb = 0;
                _gpuBudgetMb = 2048;
            }
        }

        /// <summary>
        /// Gets a snapshot of current profiling metrics.
        /// </summary>
        public GpuOverlayMetrics GetMetrics()
        {
            return new GpuOverlayMetrics
            {
                OverlayType = _overlayType,
                WpfRenderTier = _wpfRenderTier,
                DeviceName = _deviceName,
                PixelShaderVersion = _pixelShaderVersion,
                VertexShaderVersion = _vertexShaderVersion,
                LastFrameTimeMs = _lastFrameTimeMs,
                DroppedFrames = _droppedFrames,
                GpuDedicatedMb = _gpuDedicatedMb,
                GpuSharedMb = _gpuSharedMb,
                GpuBudgetMb = _gpuBudgetMb,
                FrameBudgetPercentiles = _frameMonitor?.GetPercentiles() ?? (0, 0, 0)
            };
        }

        public void Dispose()
        {
            _cts.Cancel();
            _samplingTimer?.Dispose();

            var metrics = GetMetrics();
            _logger?.Info($"[GpuOverlayProfiler] {_overlayType} stopped — Avg Frame: {metrics.LastFrameTimeMs:F2}ms, Tier: {metrics.WpfRenderTier}, GPU: {metrics.GpuDedicatedMb}MB");

            _etw.WpfRenderTier(_wpfRenderTier, _deviceName, _pixelShaderVersion, _vertexShaderVersion);
        }
    }

    /// <summary>
    /// Snapshot of GPU/Overlay profiling metrics.
    /// </summary>
    public readonly struct GpuOverlayMetrics
    {
        public string OverlayType { get; init; }
        public int WpfRenderTier { get; init; }
        public string DeviceName { get; init; }
        public int PixelShaderVersion { get; init; }
        public int VertexShaderVersion { get; init; }
        public double LastFrameTimeMs { get; init; }
        public int DroppedFrames { get; init; }
        public long GpuDedicatedMb { get; init; }
        public long GpuSharedMb { get; init; }
        public long GpuBudgetMb { get; init; }
        public (double P50, double P95, double P99) FrameBudgetPercentiles { get; init; }

        public override string ToString()
        {
            return $"[{OverlayType}] Tier={WpfRenderTier} ({DeviceName}), PS={PixelShaderVersion}, VS={VertexShaderVersion}, " +
                   $"Frame={LastFrameTimeMs:F2}ms, Dropped={DroppedFrames}, GPU={GpuDedicatedMb}MB/{GpuBudgetMb}MB, " +
                   $"P50={FrameBudgetPercentiles.P50:F2}ms, P95={FrameBudgetPercentiles.P95:F2}ms, P99={FrameBudgetPercentiles.P99:F2}ms";
        }
    }

    /// <summary>
    /// Central registry for GPU overlay profilers.
    /// </summary>
    public static class GpuOverlayProfilerRegistry
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, GpuOverlayProfiler> _profilers = new();
        private static AdvancedLogger? _logger;

        public static void Initialize(AdvancedLogger? logger = null)
        {
            _logger = logger;
        }

        public static GpuOverlayProfiler GetOrCreate(string overlayType, AdvancedLogger? logger = null, FrameBudgetMonitor? frameMonitor = null)
        {
            return _profilers.GetOrAdd(overlayType, name => new GpuOverlayProfiler(name, logger ?? _logger, frameMonitor));
        }

        public static GpuOverlayProfiler? TryGet(string overlayType)
        {
            _profilers.TryGetValue(overlayType, out var profiler);
            return profiler;
        }

        public static System.Collections.Generic.IReadOnlyDictionary<string, GpuOverlayProfiler> GetAll() => _profilers;

        public static string GenerateReport()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                "=== GPU/Overlay Profiling Report ===",
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}",
                ""
            };

            foreach (var kvp in _profilers)
            {
                lines.Add(kvp.Value.GetMetrics().ToString());
            }

            return string.Join(Environment.NewLine, lines);
        }

        public static void DisposeAll()
        {
            foreach (var profiler in _profilers.Values)
            {
                profiler.Dispose();
            }
            _profilers.Clear();
        }
    }
}