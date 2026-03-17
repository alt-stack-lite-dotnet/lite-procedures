using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneOf;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Сравнение с реальным MediatR: время, память, нагрузка на GC
// ================================================================

[Config(typeof(KillerConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class KillerComparisonBenchmarks
{
    private IAsyncProcedurePipeline<string, string> _liteNo = null!;
    private IAsyncProcedurePipeline<string, string> _liteThree = null!;
    private IMediator _mediatRNo = null!;
    private IMediator _mediatRThree = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new NoOpAsyncProcedure();
        _liteNo = new AsyncProcedurePipeline<string, string>(procedure, Array.Empty<IProcedureInterceptorCore>());
        _liteThree = new AsyncProcedurePipeline<string, string>(procedure,
            new IProcedureInterceptorCore[] { new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor() });

        var servicesNo = new ServiceCollection();
        servicesNo.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        _mediatRNo = servicesNo.BuildServiceProvider().GetRequiredService<IMediator>();

        var servicesThree = new ServiceCollection();
        servicesThree.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        servicesThree.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior1>();
        servicesThree.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior2>();
        servicesThree.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior3>();
        _mediatRThree = servicesThree.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public ValueTask<OneOf<string, Exception>> Lite_NoInterceptors()
        => _liteNo.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<string, Exception>> Lite_ThreeInterceptors()
        => _liteThree.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public Task<string> MediatR_NoBehaviors()
        => _mediatRNo.Send(new EchoRequest("input"), CancellationToken.None);

    [Benchmark]
    public Task<string> MediatR_ThreeBehaviors()
        => _mediatRThree.Send(new EchoRequest("input"), CancellationToken.None);
}

// На 10 000 запросов: суммарные аллокации и время — насколько меньше давим на GC
[Config(typeof(KillerConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn]
public class Per10kRequestsBenchmarks
{
    private const int RequestsPerIteration = 10_000;

    private IAsyncProcedurePipeline<string, string> _lite = null!;
    private IMediator _mediatR = null!;

    [GlobalSetup]
    public void Setup()
    {
        var procedure = new NoOpAsyncProcedure();
        _lite = new AsyncProcedurePipeline<string, string>(procedure,
            new IProcedureInterceptorCore[] { new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor() });

        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior1>();
        services.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior2>();
        services.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRNoOpBehavior3>();
        _mediatR = services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    [Benchmark(Baseline = true)]
    public async Task Lite_10k_Requests()
    {
        for (var i = 0; i < RequestsPerIteration; i++)
            await _lite.InvokeAsync("input", CancellationToken.None);
    }

    [Benchmark]
    public async Task MediatR_10k_Requests()
    {
        for (var i = 0; i < RequestsPerIteration; i++)
            await _mediatR.Send(new EchoRequest("input"), CancellationToken.None);
    }
}

// --- MediatR контракты для бенчмарка ---

public sealed record EchoRequest(string Value) : IRequest<string>;

public sealed class EchoHandler : IRequestHandler<EchoRequest, string>
{
    public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.Value);
}

public sealed class MediatRNoOpBehavior1 : IPipelineBehavior<EchoRequest, string>
{
    public async Task<string> Handle(EchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

public sealed class MediatRNoOpBehavior2 : IPipelineBehavior<EchoRequest, string>
{
    public async Task<string> Handle(EchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

public sealed class MediatRNoOpBehavior3 : IPipelineBehavior<EchoRequest, string>
{
    public async Task<string> Handle(EchoRequest request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
        => await next();
}

internal sealed class KillerConfig : ManualConfig
{
    public KillerConfig()
    {
        AddJob(Job.Default.WithWarmupCount(5).WithIterationCount(15));
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}
