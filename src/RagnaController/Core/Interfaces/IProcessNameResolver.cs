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
        /// Returns process name for given PID, or
        /// <c>null</c>, if process does not exist or is not resolvable.
        /// </summary>
        string? GetProcessName(int pid);
    }
}
