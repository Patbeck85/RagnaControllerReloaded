using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using Tesseract;

namespace RagnaController.Core.Services.Monitoring {
    /// <summary>
    /// OCR-Service zur Extraktion von HP/SP-Werten aus RO-Client-Screenshots.
    /// Nutzt Tesseract OCR für Texterkennung in definierten Bildregionen.
    /// </summary>
    public class OcrService : IDisposable
    {
        private readonly TesseractEngine? _engine;
        private readonly object _lock = new();
        private bool _disposed;

        /// <summary>Sprache für OCR (eng = Englisch, deu = Deutsch).</summary>
        public string Language { get; } = "eng";

        /// <summary>Ob OCR verfügbar ist.</summary>
        public bool IsAvailable => _engine != null && !_disposed;

        /// <summary>Letzte OCR-Dauer in Millisekunden.</summary>
        public long LastOcrDurationMs { get; private set; }

        public OcrService(string? tessdataPath = null, string language = "eng")
        {
            Language = language;

            try
            {
                // TesseractEngine benötigt tessdata-Verzeichnis mit traineddata-Dateien
                _engine = new TesseractEngine(tessdataPath, language, EngineMode.Default);
                _engine.SetVariable("tessedit_char_whitelist", "0123456789HP/SP%:");
                _engine.SetVariable("tessedit_pageseg_mode", "6"); // Single block of text
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[OcrService] Tesseract initialisierung fehlgeschlagen: {ex.Message}");
                _engine = null;
            }
        }

        /// <summary>
        /// Extrahiert einen ganzzahligen Wert aus einer Bildregion (z.B. HP/SP-Zahl).
        /// Gibt null zurück, wenn kein Zahl erkannt werden kann.
        /// </summary>
        public int? ExtractNumber(Bitmap image, Rectangle region)
        {
            if (_engine == null || _disposed) return null;

            lock (_lock)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    using var cropped = CropRegion(image, region);
                    if (cropped == null) return null;

                    using var pix = Pix.LoadFromMemory(BitmapToPngBytes(cropped));
                    using var page = _engine.Process(pix);
                    var text = page.GetText();

                    LastOcrDurationMs = sw.ElapsedMilliseconds;

                    return ParseNumber(text);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[OcrService] OCR-Fehler: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Extrahiert Text aus einer Bildregion (z.B. Ziel-Name).
        /// Gibt den erkannten Text zurück oder null.
        /// </summary>
        public string? ExtractText(Bitmap image, Rectangle region)
        {
            if (_engine == null || _disposed) return null;

            lock (_lock)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    using var cropped = CropRegion(image, region);
                    if (cropped == null) return null;

                    using var pix = Pix.LoadFromMemory(BitmapToPngBytes(cropped));
                    using var page = _engine.Process(pix);
                    var text = page.GetText();

                    LastOcrDurationMs = sw.ElapsedMilliseconds;

                    return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[OcrService] OCR-Text-Fehler: " + ex.Message);
                    return null;
                }
            }
        }

        /// <summary>
        /// Extrahiert einen ganzzahligen Wert aus einem Bitmap (komplettes Bild).
        /// </summary>
        public int? ExtractNumber(Bitmap image)
        {
            if (_engine == null || _disposed) return null;

            lock (_lock)
            {
                try
                {
                    var sw = Stopwatch.StartNew();

                    using var pix = Pix.LoadFromMemory(BitmapToPngBytes(image));
                    using var page = _engine.Process(pix);
                    var text = page.GetText();

                    LastOcrDurationMs = sw.ElapsedMilliseconds;

                    return ParseNumber(text);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[OcrService] OCR-Fehler: {ex.Message}");
                    return null;
                }
            }
        }

        private static byte[] BitmapToPngBytes(Bitmap bitmap)
        {
            using var ms = new global::System.IO.MemoryStream();
            bitmap.Save(ms, global::System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static int? ParseNumber(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Nur Ziffern extrahieren
            var digits = new string(global::System.Array.FindAll(text.ToCharArray(), c => char.IsDigit(c)));
            if (digits.Length == 0) return null;

            if (int.TryParse(digits, out var value))
                return value;

            return null;
        }

        private static Bitmap? CropRegion(Bitmap source, Rectangle region)
        {
            try
            {
                // Stellen sicher, dass Region im Bild liegt
                var x = Math.Max(0, Math.Min(region.X, source.Width - 1));
                var y = Math.Max(0, Math.Min(region.Y, source.Height - 1));
                var w = Math.Max(1, Math.Min(region.Width, source.Width - x));
                var h = Math.Max(1, Math.Min(region.Height, source.Height - y));

                var rect = new Rectangle(x, y, w, h);
                return source.Clone(rect, source.PixelFormat);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OcrService] CropRegion-Fehler: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Nimmt einen Screenshot des angegebenen Client-Fensters auf.
        /// </summary>
        public static Bitmap? CaptureWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return null;

            try
            {
                var rect = new Core.RECT();
                if (!NativeMethods.GetWindowRect(hwnd, out rect)) return null;

                var width = rect.Right - rect.Left;
                var height = rect.Bottom - rect.Top;
                if (width <= 0 || height <= 0) return null;

                var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                using var gfx = Graphics.FromImage(bmp);

                // Kopiere Fenster-Inhalt via PrintWindow (besser als BitBlt für überlappende Fenster)
                var hdc = gfx.GetHdc();
                try
                {
                    NativeMethods.PrintWindow(hwnd, hdc, 0);
                }
                finally
                {
                    gfx.ReleaseHdc(hdc);
                }

                return bmp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[OcrService] CaptureWindow-Fehler: " + ex.Message);
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _engine?.Dispose();
        }
    }
}
