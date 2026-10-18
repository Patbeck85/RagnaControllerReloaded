using System.Diagnostics;

namespace RagnaController.Core
{
    /// <summary>
    /// Production-Implementierung from <see cref="IProcessNameResolver"/> about
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
                // Prozess existiert nicht mehr / no Zugriff — no Fehlerzustand,
                // sondern ein legitimes "nicht gefunden". WindowTracker behandelt
                // null als "no RO-Fenster" and setzt IsTracking=false.
                return null;
            }
        }
    }
}
