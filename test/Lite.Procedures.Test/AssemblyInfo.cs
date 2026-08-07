using Xunit;

// PipelineFactoryRegistry is a process-wide static registry; PipelineFactoryRegistryTests.Clear()
// would race with any other test's AddLiteProcedures() call running on a different thread at the
// same time. Serializing the whole assembly is the standard, low-cost fix for shared static state
// (this suite runs in well under a second either way).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
