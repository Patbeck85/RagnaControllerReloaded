using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Definiert die Schnittstelle für Eingabe-Handler (Engines).
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// Die Priorität des Handlers. Höhere Werte werden zuerst verarbeitet.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Verarbeitet den Input. 
        /// </summary>
        /// <returns>True, wenn der Input konsumiert wurde.</returns>
        bool Handle(ParsedInput input, int deltaMs);
    }
}