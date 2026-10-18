namespace RagnaController.Core
{
    /// <summary>
    /// Liefert den Prozessnamen to einer PID ab. Abstraktion about
    /// <see cref="System.Diagnostics.Process"/> damit WindowTracker in Unit-Tests
    /// deterministisch getestet are kann (DIP / Testbarkeit).
    /// </summary>
    public interface IProcessNameResolver
    {
        /// <summary>
        /// Gibt den Prozessnamen for die angegebene PID zurück, or
        /// <c>null</c>, if der Prozess nicht existiert bzw. nicht auflösbar is.
        /// </summary>
        string? GetProcessName(int pid);
    }
}
