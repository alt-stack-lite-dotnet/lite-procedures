using System.Reflection;
using BenchmarkDotNet.Attributes;
using Lite.Procedures.Generated;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Сравнение с реальным MessagePipe (NuGet): request/handler + 0 или 3 фильтра.
// Аналогично KillerComparisonBenchmarks для MediatR.
// ================================================================

public readonly struct MpEchoRequest
{
    public MpEchoRequest(string value) => Value = value;
    public string Value { get; }
}

[Config(typeof(KillerConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class MessagePipeComparisonBenchmarks
{
    private IAsyncProcedurePipeline<string, string> _liteNo = null!;
    private IAsyncProcedurePipeline<string, string> _liteThree = null!;
    private IAsyncRequestHandler<MpEchoRequest, string> _messagePipeNo = null!;
    private IAsyncRequestHandler<MpEchoRequest, string> _messagePipeThree = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new NoOpAsyncProcedure();
        _liteNo = new AsyncProcedurePipeline<string, string>(procedure, Array.Empty<IProcedureInterceptorCore>());

        // Lite_ThreeInterceptors: сгенерированный пайплайн (codegen), чтобы честно бить MessagePipe.
        var registry = GeneratedPipelineRegistryDiscovery.TryDiscover();
        var procThree = new ProcedureWithThreeInterceptors();
        var interceptor = new NoOpAsyncInterceptor();
        var interceptorTypesThree = new[] { typeof(NoOpAsyncInterceptor), typeof(NoOpAsyncInterceptor), typeof(NoOpAsyncInterceptor) };
        var generatedTypeThree = registry?.GetGeneratedPipelineType(typeof(ProcedureWithThreeInterceptors), interceptorTypesThree);
        if (generatedTypeThree != null)
            _liteThree = (IAsyncProcedurePipeline<string, string>)Activator.CreateInstance(generatedTypeThree, procThree, interceptor, interceptor, interceptor)!;
        else
            _liteThree = new AsyncProcedurePipeline<string, string>(procThree, new IProcedureInterceptorCore[] { interceptor, interceptor, interceptor });

        var servicesNo = new ServiceCollection();
        servicesNo.AddMessagePipe();
        servicesNo.AddSingleton<IAsyncRequestHandler<MpEchoRequest, string>, MpEchoHandler>();
        _messagePipeNo = servicesNo.BuildServiceProvider().GetRequiredService<IAsyncRequestHandler<MpEchoRequest, string>>();

        var servicesThree = new ServiceCollection();
        servicesThree.AddMessagePipe(options =>
        {
            options.AddGlobalAsyncRequestHandlerFilter(typeof(MpNoOpFilter1), 0);
            options.AddGlobalAsyncRequestHandlerFilter(typeof(MpNoOpFilter2), 1);
            options.AddGlobalAsyncRequestHandlerFilter(typeof(MpNoOpFilter3), 2);
        });
        servicesThree.AddSingleton<IAsyncRequestHandler<MpEchoRequest, string>, MpEchoHandler>();
        _messagePipeThree = servicesThree.BuildServiceProvider().GetRequiredService<IAsyncRequestHandler<MpEchoRequest, string>>();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<OneOf<string, Exception>> Lite_NoInterceptors()
        => _liteNo.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<string, Exception>> Lite_ThreeInterceptors()
        => _liteThree.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public ValueTask<string> MessagePipe_NoFilters()
        => _messagePipeNo.InvokeAsync(new MpEchoRequest("input"), CancellationToken.None);

    [Benchmark]
    public ValueTask<string> MessagePipe_ThreeFilters()
        => _messagePipeThree.InvokeAsync(new MpEchoRequest("input"), CancellationToken.None);
}

internal sealed class MpEchoHandler : IAsyncRequestHandler<MpEchoRequest, string>
{
    public ValueTask<string> InvokeAsync(MpEchoRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(request.Value);
}

internal sealed class MpNoOpFilter1 : AsyncRequestHandlerFilter<MpEchoRequest, string>
{
    public override async ValueTask<string> InvokeAsync(MpEchoRequest request, CancellationToken cancellationToken, Func<MpEchoRequest, CancellationToken, ValueTask<string>> next)
        => await next(request, cancellationToken);
}

internal sealed class MpNoOpFilter2 : AsyncRequestHandlerFilter<MpEchoRequest, string>
{
    public override async ValueTask<string> InvokeAsync(MpEchoRequest request, CancellationToken cancellationToken, Func<MpEchoRequest, CancellationToken, ValueTask<string>> next)
        => await next(request, cancellationToken);
}

internal sealed class MpNoOpFilter3 : AsyncRequestHandlerFilter<MpEchoRequest, string>
{
    public override async ValueTask<string> InvokeAsync(MpEchoRequest request, CancellationToken cancellationToken, Func<MpEchoRequest, CancellationToken, ValueTask<string>> next)
        => await next(request, cancellationToken);
}
