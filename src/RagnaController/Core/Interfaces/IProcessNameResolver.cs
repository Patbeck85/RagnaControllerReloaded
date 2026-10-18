namespace RagnaController.Core
{
    /// <summary>
    /// Liefert den Prozessnamen zu einer PID ab. Abstraktion über
    /// <see cref="System.Diagnostics.Process"/> damit WindowTracker in Unit-Tests
    /// deterministisch getestet werden kann (DIP / Testbarkeit).
    /// </summary>
    public interface IProcessNameResolver
    {
        /// <summary>
        /// Gibt den Prozessnamen für die angegebene PID zurück, oder
        /// <c>null</c>, wenn der Prozess nicht existiert bzw. nicht auflösbar ist.
        /// </summary>
        string? GetProcessName(int pid);
    }
}
