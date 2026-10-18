using RagnaController.Models;

namespace RagnaController.Core
{
    /// <summary>
    /// ParsedInput is now a readonly record struct with zero heap allocation.
    /// The pool is no longer needed since structs are stack-allocated.
    /// This file is kept for backward compatibility during transition.
    /// </summary>
    [Obsolete("ParsedInput is now a readonly record struct. Direct instantiation is zero-allocation. Pool is no longer needed.")]
    public static class ParsedInputPool
    {
        [Obsolete("Use 'new ParsedInput { ... }' or 'ParsedInput.Disconnected' directly.")]
        public static ParsedInput Get() => ParsedInput.Disconnected;

        [Obsolete("No-op for readonly record struct.")]
        public static void Return(ParsedInput item) { }
    }
}