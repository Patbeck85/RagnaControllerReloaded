using System.Collections.Generic;
using RagnaController.Core;

namespace RagnaController.Tests
{
    /// <summary>
    /// Deterministischer Test-Double für IProcessNameResolver.
    /// Ermöglicht kontrollierte PID → ProcessName Zuordnung in Unit-Tests.
    /// </summary>
    public sealed class TestProcessNameResolver : IProcessNameResolver
    {
        private readonly Dictionary<int, string> _pidNameMap = new();

        public void SetProcessName(int pid, string processName)
        {
            _pidNameMap[pid] = processName;
        }

        public void Clear() => _pidNameMap.Clear();

        public string? GetProcessName(int pid)
        {
            return _pidNameMap.TryGetValue(pid, out var name) ? name : null;
        }
    }
}