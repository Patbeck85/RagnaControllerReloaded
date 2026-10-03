using RagnaController.Models;

namespace RagnaController.Tests
{
    /// <summary>
    /// TEST-015/016: Geteilte Prev-Tracking-State-Maschine für die Controller-Eingabe-Pipeline.
    /// Repliziert exakt <c>Core/InputReader.Read()</c> (Produktion): das Ergebnis-Frame erhält
    /// <c>PrevRawButtons</c> = Vorframe, danach wird der aktuelle RawButtons-Mask als neuer Prev
    /// übernommen. Bei Disconnect wird der Prev-State zurückgesetzt → kein Ghost auf Reconnect.
    /// Ein Tick = ein konsumiertes Hardware-Sample (Advance auch bei Disconnect).
    /// </summary>
    internal sealed class PipelineStep
    {
        private GamepadButtonFlags _prev = GamepadButtonFlags.None;

        public ParsedInput Step(FakeControllerProvider provider)
        {
            var frame = provider.CurrentFrame();
            if (!frame.IsConnected)
            {
                _prev = GamepadButtonFlags.None; // Disconnect reset: kein Ghost auf Reconnect
                provider.Advance();               // ein Tick = ein konsumiertes Hardware-Sample
                return frame;
            }

            var chained = frame.With(prevRawButtons: _prev);
            _prev = frame.RawButtons;
            provider.Advance();
            return chained;
        }
    }
}
