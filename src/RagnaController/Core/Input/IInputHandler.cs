using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Definiert die Schnittstelle for Eingabe-Handler (Engines).
    /// </summary>
    public interface IInputHandler
    {
        /// <summary>
        /// The Priorität des Handlers. Höhere Werte are first verarbeitet.
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Verarbeitet den Input. 
        /// </summary>
        /// <returns>True, if der Input konsumiert wurde.</returns>
        bool Handle(ParsedInput input, int deltaMs);
    }
}