using Lite.Procedures;
using Lite.Procedures.Configuration;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Pipeline.Interception;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Test;

public class FastPipelineTests
{
    [Fact]
    public void AttributeChain_ResolvesToGeneratedFastPipeline()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new List<string>());
        services.AddLiteProcedures(b => b.AddProcedure<FastProc>());

        using var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<IAsyncProcedure<FastReq, string>>();

        Assert.StartsWith("FastPipeline_", pipeline.GetType().Name);
    }

    [Fact]
    public async Task GeneratedFastPipeline_RunsInterceptorsInOrder()
    {
        var probe = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b.AddProcedure<FastProc>());

        using var sp = services.BuildServiceProvider();
        var pipeline = sp.GetRequiredService<IAsyncProcedure<FastReq, string>>();
        var result = await pipeline.ExecuteAsync(new FastReq("x"), CancellationToken.None);

        Assert.Equal("x", result);
        Assert.Equal(new[] { "A", "B" }, probe);
    }

    public sealed record FastReq(string Value);

    [InterceptWith(typeof(TrackA))]
    [InterceptWith(typeof(TrackB))]
    public sealed class FastProc : IAsyncProcedure<FastReq, string>
    {
        public ValueTask<string> ExecuteAsync(FastReq arguments, CancellationToken cancellationToken = default)
            => new(arguments.Value);
    }

    public sealed class TrackA : AsyncInterceptor<FastReq, string>
    {
        private readonly List<string> _probe;
        public TrackA(List<string> probe) => _probe = probe;
        public override ValueTask<string> InvokeAsync(FastReq a, Func<FastReq, CancellationToken, ValueTask<string>> next, CancellationToken ct)
        { _probe.Add("A"); return next(a, ct); }
    }

    public sealed class TrackB : AsyncInterceptor<FastReq, string>
    {
        private readonly List<string> _probe;
        public TrackB(List<string> probe) => _probe = probe;
        public override ValueTask<string> InvokeAsync(FastReq a, Func<FastReq, CancellationToken, ValueTask<string>> next, CancellationToken ct)
        { _probe.Add("B"); return next(a, ct); }
    }
}
