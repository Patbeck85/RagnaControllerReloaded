using System;
using System.IO;
using System.Linq;
using RagnaController.Core;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// TECH-024: AdvancedLogger — Log-Loss unter Last.
    /// Warn/Error laufen über einen UNBOUNDED Channel (werden nie verworfen);
    /// Debug/Info über einen bounded DropOldest-Channel (Memory-Backpressure).
    /// </summary>
    public class AdvancedLoggerTests : IDisposable
    {
        private readonly string _dir =
            Path.Combine(Path.GetTempPath(), "ragna_logger_tests_" + Guid.NewGuid().ToString("N"));

        public AdvancedLoggerTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, true); } catch { /* best effort */ }
        }

        private string NewFile() => Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".log");

        private static int CountTag(string[] lines, string tag) =>
            lines.Count(l => l.Contains(tag));

        // Logger schreiben, dann EXPLIZIT Dispose (schließt FileStream), dann Datei lesen.
        // (using var geht nicht: doppeltes _cts.Dispose() würde werfen; und der Handle
        //  wäre beim Lesen sonst noch gesperrt → IOException.)
        private static string[] WriteThenRead(string file, Action<AdvancedLogger> fill, int capacity = 64)
        {
            var logger = new AdvancedLogger(file, normalCapacity: capacity);
            fill(logger);
            logger.Dispose(); // Consumer wartet → Datei vollständig & Handle frei
            return File.ReadAllLines(file);
        }

        [Fact]
        public void ErrorLogs_UnterLast_WerdenNieVerloren()
        {
            // Kapazität 2 (extrem klein) + 200 Errors → bei DropOldest gingen alle über
            // die Kapazität verloren. Mit unbounded Error-Channel müssen ALLE ankommen.
            var file = NewFile();
            var lines = WriteThenRead(file, logger =>
            {
                logger.LogLevel = 3; // nur Errors
                for (int i = 0; i < 200; i++) logger.Error($"Error {i}");
            }, capacity: 2);

            Assert.Equal(200, CountTag(lines, "[ERR]"));
        }

        [Fact]
        public void WarnLogs_UnterLast_WerdenNieVerloren()
        {
            var file = NewFile();
            var lines = WriteThenRead(file, logger =>
            {
                logger.LogLevel = 2; // Info + Warn (kein Error)
                for (int i = 0; i < 100; i++) logger.Warn($"Warn {i}");
            }, capacity: 2);

            Assert.Equal(100, CountTag(lines, "[WRN]"));
        }

        [Fact]
        public void InfoLogs_UnterLast_WerdenGeworfen_BackpressureBleibt()
        {
            // Normale Logs bleiben bounded (DropOldest): unter Last dürfen sie verloren
            // gehen — das ist gewollt (Memory-Schutz). Der Logger darf nicht blockieren/hängen.
            var file = NewFile();
            var lines = WriteThenRead(file, logger =>
            {
                logger.LogLevel = 0;
                for (int i = 0; i < 500; i++) logger.Info($"Info {i}");
            }, capacity: 2);

            int written = CountTag(lines, "[INF]");
            Assert.True(written >= 1, "Consumer muss mindestens einen Eintrag schreiben");
            Assert.True(written < 500, $"Unter Last müssen Info-Logs verworfen werden (DropOldest), waren {written}/500");
        }

        [Fact]
        public void AlleLevel_WerdenMitTagInDateiGesetzt()
        {
            var file = NewFile();
            var lines = WriteThenRead(file, logger =>
            {
                logger.LogLevel = 0; // alles erlauben
                logger.Debug("dbg-msg");
                logger.Info("inf-msg");
                logger.Warn("wrn-msg");
                logger.Error("err-msg");
            });

            Assert.Equal(4, lines.Length);
            var all = string.Join("\n", lines);
            Assert.Contains("[DBG] dbg-msg", all);
            Assert.Contains("[INF] inf-msg", all);
            Assert.Contains("[WRN] wrn-msg", all);
            Assert.Contains("[ERR] err-msg", all);
        }

        [Fact]
        public void LogLevel_FiltertTiefereLevel()
        {
            var file = NewFile();
            var lines = WriteThenRead(file, logger =>
            {
                logger.LogLevel = 2; // nur Warn + Error
                logger.Debug("kein-dbg");
                logger.Info("kein-inf");
                logger.Warn("ja-wrn");
                logger.Error("ja-err");
            });

            Assert.Equal(2, lines.Length);
            var all = string.Join("\n", lines);
            Assert.DoesNotContain("[DBG]", all);
            Assert.DoesNotContain("[INF]", all);
        }
    }
}
