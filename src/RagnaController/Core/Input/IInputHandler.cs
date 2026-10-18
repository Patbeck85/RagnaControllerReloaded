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
        bool Handle(ParsedInput input, int deltaMs);
    }
}