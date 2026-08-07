using Lite.Procedures;

namespace Lite.Procedures.Test;

public class PipelineFactoryRegistryTests
{
    [Fact]
    public void Register_ThenTryGet_ReturnsTheSameFactory()
    {
        var factory = new FakeFactory();
        PipelineFactoryRegistry.Register(factory);

        var found = PipelineFactoryRegistry.TryGet(typeof(FakeProcedure), out var got);

        Assert.True(found);
        Assert.Same(factory, got);
    }

    [Fact]
    public void Register_ThenContains_ReturnsTrue()
    {
        PipelineFactoryRegistry.Register(new FakeFactory());

        Assert.True(PipelineFactoryRegistry.Contains(typeof(FakeProcedure)));
    }

    [Fact]
    public void TryGet_UnknownType_ReturnsFalse()
    {
        var found = PipelineFactoryRegistry.TryGet(typeof(PipelineFactoryRegistryTests), out _);

        Assert.False(found);
    }

    [Fact]
    public void ProcedureTypes_IncludesRegisteredType()
    {
        PipelineFactoryRegistry.Register(new FakeFactory());

        Assert.Contains(typeof(FakeProcedure), PipelineFactoryRegistry.ProcedureTypes);
    }

    [Fact]
    public void Clear_RemovesRegisteredFactory()
    {
        // Clear() wipes every factory in the process, including the ones every other test's
        // AddLiteProcedures() call depends on ([ModuleInitializer] registers them exactly once
        // per assembly load — there's no way to re-trigger it). Snapshot and restore so this
        // test doesn't leave the rest of the suite without its generated fast-path factories.
        var snapshot = PipelineFactoryRegistry.ProcedureTypes
            .Select(t =>
            {
                PipelineFactoryRegistry.TryGet(t, out var f);
                return f;
            })
            .ToArray();

        PipelineFactoryRegistry.Register(new FakeFactory());
        Assert.True(PipelineFactoryRegistry.Contains(typeof(FakeProcedure)));

        try
        {
            PipelineFactoryRegistry.Clear();

            Assert.False(PipelineFactoryRegistry.Contains(typeof(FakeProcedure)));
        }
        finally
        {
            foreach (var factory in snapshot) PipelineFactoryRegistry.Register(factory);
        }
    }

    private sealed class FakeProcedure;

    private sealed class FakeFactory : IProcedurePipelineFactory
    {
        public Type ProcedureType => typeof(FakeProcedure);
        public Type ProcedureInterfaceType => typeof(FakeProcedure);

        public object Assemble(IServiceProvider services, IReadOnlyList<(Type InterceptorType, int Priority)> interceptors)
            => throw new NotSupportedException("Not exercised by these tests.");
    }
}
