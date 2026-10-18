using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// Production-Implementierung von <see cref="IProcessNameResolver"/> über
    /// <see cref="Process.GetProcessById(int)"/>.
    /// </summary>
    public sealed class ProcessNameResolver : IProcessNameResolver
    {
        public string? GetProcessName(int pid)
        {
            if (pid <= 0) return null;

            try
            {
                using Process proc = Process.GetProcessById(pid);
                return proc.ProcessName;
            }
            catch
            {
                // Prozess existiert nicht mehr / kein Zugriff — kein Fehlerzustand,
                // sondern ein legitimes "nicht gefunden". WindowTracker behandelt
                // null als "kein RO-Fenster" und setzt IsTracking=false.
                return null;
            }
        }
    }
}
