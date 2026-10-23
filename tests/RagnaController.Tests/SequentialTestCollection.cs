using System;
using Xunit;

namespace RagnaController.Tests
{
    /// <summary>
    /// Collection definition for tests that must run sequentially (not in parallel)
    /// because they measure process-wide metrics like GC allocations.
    /// </summary>
    [CollectionDefinition("SequentialTests", DisableParallelization = true)]
    public class SequentialTestCollection : ICollectionFixture<SequentialTestFixture>
    {
    }

    /// <summary>
    /// Fixture for sequential tests - no setup/teardown needed, just marks the collection.
    /// </summary>
    public class SequentialTestFixture : IDisposable
    {
        public void Dispose() { }
    }
}
