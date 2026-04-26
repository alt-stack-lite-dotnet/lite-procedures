using BenchmarkDotNet.Attributes;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Сравнение с реальным MessagePipe (NuGet): request/handler + 0 или 3 фильтра.
// Цель — равная производительность с MessagePipe (один из самых быстрых
// pub/sub фреймворков для .NET).
// ================================================================

public readonly struct MpEchoRequest
{
    public MpEchoRequest(string value) => Value = value;
    public string Value { get; }
}

[Config(typeof(ComparisonConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class MessagePipeComparisonBenchmarks
{
    private static readonly EchoRequest LiteInput = new("input");

    private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _liteNo = null!;
    private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _liteThree = null!;
    private IAsyncRequestHandler<MpEchoRequest, string> _messagePipeNo = null!;
    private IAsyncRequestHandler<MpEchoRequest, string> _messagePipeThree = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new EchoAsyncProcedure();

        _liteNo = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            procedure,
            []);

        _liteThree = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            procedure,
            [
                new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor()
            ]);

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
    public ValueTask<EchoResponse> Lite_NoInterceptors()
        => _liteNo.InvokeAsync(LiteInput, CancellationToken.None);

    [Benchmark]
    public ValueTask<EchoResponse> Lite_ThreeInterceptors()
        => _liteThree.InvokeAsync(LiteInput, CancellationToken.None);

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
