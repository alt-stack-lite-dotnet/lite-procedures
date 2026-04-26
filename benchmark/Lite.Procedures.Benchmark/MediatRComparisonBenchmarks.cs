using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Сравнение с реальным MediatR: время, память, нагрузка на GC.
// Цель — показать преимущество Lite.Procedures: ниже latency, нулевые
// аллокации на горячем пути.
// ================================================================

[Config(typeof(ComparisonConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class MediatRComparisonBenchmarks
{
    private static readonly EchoRequest LiteInput = new("input");

    private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _liteNo = null!;
    private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _liteThree = null!;
    private IMediator _mediatRNo = null!;
    private IMediator _mediatRThree = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new EchoAsyncProcedure();
        _liteNo = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(procedure, []);
        _liteThree = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(procedure,
            [new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor()]);

        var servicesNo = new ServiceCollection();
        servicesNo.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        _mediatRNo = servicesNo.BuildServiceProvider().GetRequiredService<IMediator>();

        var servicesThree = new ServiceCollection();
        servicesThree.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        servicesThree.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior1>();
        servicesThree.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior2>();
        servicesThree.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior3>();
        _mediatRThree = servicesThree.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<EchoResponse> Lite_NoInterceptors()
        => _liteNo.InvokeAsync(LiteInput, CancellationToken.None);

    [Benchmark]
    public ValueTask<EchoResponse> Lite_ThreeInterceptors()
        => _liteThree.InvokeAsync(LiteInput, CancellationToken.None);

    [Benchmark]
    public Task<string> MediatR_NoBehaviors()
        => _mediatRNo.Send(new MediatREchoRequest("input"), CancellationToken.None);

    [Benchmark]
    public Task<string> MediatR_ThreeBehaviors()
        => _mediatRThree.Send(new MediatREchoRequest("input"), CancellationToken.None);
}

// На 10 000 запросов: суммарные аллокации и время — насколько меньше давим на GC
[Config(typeof(ComparisonConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn]
public class Per10KRequestsBenchmarks
{
    private const int RequestsPerIteration = 10_000;
    private static readonly EchoRequest LiteInput = new("input");

    private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _lite = null!;
    private IMediator _mediatR = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new EchoAsyncProcedure();
        _lite = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(procedure,
            [new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor()]);

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior1>();
        services.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior2>();
        services.AddTransient<IPipelineBehavior<MediatREchoRequest, string>, MediatRNoOpBehavior3>();
        _mediatR = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public async Task Lite_10k_Requests()
    {
        for (var i = 0; i < RequestsPerIteration; i++)
            await _lite.InvokeAsync(LiteInput, CancellationToken.None);
    }

    [Benchmark]
    public async Task MediatR_10k_Requests()
    {
        for (var i = 0; i < RequestsPerIteration; i++)
            await _mediatR.Send(new MediatREchoRequest("input"), CancellationToken.None);
    }
}

// --- MediatR контракты для бенчмарка ---

public sealed record MediatREchoRequest(string Value) : IRequest<string>;

public sealed class MediatREchoHandler : IRequestHandler<MediatREchoRequest, string>
{
    public Task<string> Handle(MediatREchoRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

public sealed class MediatRNoOpBehavior1 : IPipelineBehavior<MediatREchoRequest, string>
{
    public async Task<string> Handle(MediatREchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

public sealed class MediatRNoOpBehavior2 : IPipelineBehavior<MediatREchoRequest, string>
{
    public async Task<string> Handle(MediatREchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

public sealed class MediatRNoOpBehavior3 : IPipelineBehavior<MediatREchoRequest, string>
{
    public async Task<string> Handle(MediatREchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

internal sealed class ComparisonConfig : ManualConfig
{
    public ComparisonConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(5)
            .WithIterationCount(15));
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
