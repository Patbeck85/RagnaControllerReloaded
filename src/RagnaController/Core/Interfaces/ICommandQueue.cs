using System;
using System.Threading.Tasks;

namespace RagnaController.Core
{
    /// <summary>
    /// Interface für synchrone Befehlsverarbeitung in Tests.
    /// </summary>
    public interface ICommandQueue
    {
        void Enqueue(Action action);
        void Execute();
        bool IsEmpty { get; }
        int Count { get; }
        void Clear();
    }
}
