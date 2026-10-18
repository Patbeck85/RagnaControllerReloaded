using System.Collections.Generic;
using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// Runs the ordered <see cref="IInputHandler"/> chain each tick.
    /// Replaces the nested if/else blocks in CombatRouter and OverlayRouter.
    /// Zero-allocation implementation.
    /// </summary>
    public sealed class InputChain
    {
        private IInputHandler[] _sorted = System.Array.Empty<IInputHandler>();

        public void Build(IEnumerable<IInputHandler> handlers)
        {
            // Manual sort to avoid LINQ allocation
            var list = new List<IInputHandler>();
            foreach (var h in handlers) list.Add(h);
            
            // Simple insertion sort by priority (descending) - small N, so efficient
            for (int i = 1; i < list.Count; i++)
            {
                var key = list[i];
                int j = i - 1;
                while (j >= 0 && list[j].Priority < key.Priority)
                {
                    list[j + 1] = list[j];
                    j--;
                }
                list[j + 1] = key;
            }
            _sorted = list.ToArray();
        }

        /// <summary>Runs the chain. Returns the consuming handler or null.</summary>
        public IInputHandler? Run(ParsedInput input, int deltaMs)
        {
            foreach (var h in _sorted)
                if (h.Handle(input, deltaMs)) return h;
            return null;
        }
    }
}
