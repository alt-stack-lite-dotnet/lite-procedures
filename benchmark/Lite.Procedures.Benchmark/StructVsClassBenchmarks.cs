using System.Reflection;
using BenchmarkDotNet.Attributes;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Struct vs class: в MediatR только классы (аллокация на каждый запрос),
// в Lite.Procedures можно передавать структуры — нулевые аллокации на
// аргументах/результате. Sync- и async-варианты для структур 16/32/64 байта.
// ================================================================

// 16 bytes
public struct SmallStruct
{
    public long A;
    public long B;
}

// 32 bytes
public struct MediumStruct
{
    public long A, B, C, D;
}

// 64 bytes
public struct LargeStruct
{
    public long A, B, C, D, E, F, G, H;
}

[Config(typeof(ComparisonConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class StructVsClassBenchmarks
{
    private IAsyncProcedurePipeline<SmallStruct, long> _liteSmallAsync = null!;
    private IAsyncProcedurePipeline<MediumStruct, long> _liteMediumAsync = null!;
    private IAsyncProcedurePipeline<LargeStruct, long> _liteLargeAsync = null!;

    private IProcedurePipeline<SmallStruct, long> _liteSmallSync = null!;
    private IProcedurePipeline<MediumStruct, long> _liteMediumSync = null!;
    private IProcedurePipeline<LargeStruct, long> _liteLargeSync = null!;

    private IMediator _mediatRSmall = null!;
    private IMediator _mediatRMedium = null!;
    private IMediator _mediatRLarge = null!;

    private SmallStruct _smallArg;
    private MediumStruct _mediumArg;
    private LargeStruct _largeArg;

    [GlobalSetup]
    public void Setup()
    {
        _smallArg = new SmallStruct { A = 1, B = 2 };
        _mediumArg = new MediumStruct { A = 1, B = 2, C = 3, D = 4 };
        _largeArg = new LargeStruct { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8 };

        var procSmallAsync = new SumProcedureSmallAsync();
        var procMediumAsync = new SumProcedureMediumAsync();
        var procLargeAsync = new SumProcedureLargeAsync();

        var procSmallSync = new SumProcedureSmallSync();
        var procMediumSync = new SumProcedureMediumSync();
        var procLargeSync = new SumProcedureLargeSync();

        _liteSmallAsync = new AsyncProcedurePipeline<SmallStruct, long>(procSmallAsync,
            [new NoOpInterceptorSmallAsync(), new NoOpInterceptorSmallAsync(), new NoOpInterceptorSmallAsync()]);
        _liteMediumAsync = new AsyncProcedurePipeline<MediumStruct, long>(procMediumAsync,
            [new NoOpInterceptorMediumAsync(), new NoOpInterceptorMediumAsync(), new NoOpInterceptorMediumAsync()]);
        _liteLargeAsync = new AsyncProcedurePipeline<LargeStruct, long>(procLargeAsync,
            [new NoOpInterceptorLargeAsync(), new NoOpInterceptorLargeAsync(), new NoOpInterceptorLargeAsync()]);

        _liteSmallSync = new ProcedurePipeline<SmallStruct, long>(procSmallSync,
            [new NoOpInterceptorSmallSync(), new NoOpInterceptorSmallSync(), new NoOpInterceptorSmallSync()]);
        _liteMediumSync = new ProcedurePipeline<MediumStruct, long>(procMediumSync,
            [new NoOpInterceptorMediumSync(), new NoOpInterceptorMediumSync(), new NoOpInterceptorMediumSync()]);
        _liteLargeSync = new ProcedurePipeline<LargeStruct, long>(procLargeSync,
            [new NoOpInterceptorLargeSync(), new NoOpInterceptorLargeSync(), new NoOpInterceptorLargeSync()]);

        _mediatRSmall = BuildMediatR<SmallRequest, long>();
        _mediatRMedium = BuildMediatR<MediumRequest, long>();
        _mediatRLarge = BuildMediatR<LargeRequest, long>();
    }

    private static IMediator BuildMediatR<TRequest, TResponse>() where TRequest : IRequest<TResponse>
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, MediatRNoOpBehavior<TRequest, TResponse>>();
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, MediatRNoOpBehavior<TRequest, TResponse>>();
        services.AddTransient<IPipelineBehavior<TRequest, TResponse>, MediatRNoOpBehavior<TRequest, TResponse>>();
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    // --- Async ---

    [Benchmark(Baseline = true)]
    public ValueTask<long> Lite_Small_Async() => _liteSmallAsync.InvokeAsync(_smallArg, CancellationToken.None);

    [Benchmark]
    public ValueTask<long> Lite_Medium_Async() => _liteMediumAsync.InvokeAsync(_mediumArg, CancellationToken.None);

    [Benchmark]
    public ValueTask<long> Lite_Large_Async() => _liteLargeAsync.InvokeAsync(_largeArg, CancellationToken.None);

    // --- Sync ---

    [Benchmark]
    public long Lite_Small_Sync() => _liteSmallSync.Invoke(_smallArg);

    [Benchmark]
    public long Lite_Medium_Sync() => _liteMediumSync.Invoke(_mediumArg);

    [Benchmark]
    public long Lite_Large_Sync() => _liteLargeSync.Invoke(_largeArg);

    // --- MediatR (always async, classes only) ---

    [Benchmark]
    public Task<long> MediatR_Small() => _mediatRSmall.Send(new SmallRequest(1, 2), CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_Medium() => _mediatRMedium.Send(new MediumRequest(1, 2, 3, 4), CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_Large() => _mediatRLarge.Send(new LargeRequest(1, 2, 3, 4, 5, 6, 7, 8), CancellationToken.None);
}

// --- Lite: async procedures and interceptors ---

internal sealed class SumProcedureSmallAsync : IAsyncProcedure<SmallStruct, long>
{
    public ValueTask<long> InvokeAsync(SmallStruct arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments.A + arguments.B);
}

internal sealed class SumProcedureMediumAsync : IAsyncProcedure<MediumStruct, long>
{
    public ValueTask<long> InvokeAsync(MediumStruct arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments.A + arguments.D);
}

internal sealed class SumProcedureLargeAsync : IAsyncProcedure<LargeStruct, long>
{
    public ValueTask<long> InvokeAsync(LargeStruct arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments.A + arguments.H);
}

internal sealed class NoOpInterceptorSmallAsync : IAsyncProcedureInterceptor<SmallStruct, long>
{
    public ValueTask<long> InvokeAsync(SmallStruct arguments, Func<SmallStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct)
        => next(arguments, ct);
}

internal sealed class NoOpInterceptorMediumAsync : IAsyncProcedureInterceptor<MediumStruct, long>
{
    public ValueTask<long> InvokeAsync(MediumStruct arguments, Func<MediumStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct)
        => next(arguments, ct);
}

internal sealed class NoOpInterceptorLargeAsync : IAsyncProcedureInterceptor<LargeStruct, long>
{
    public ValueTask<long> InvokeAsync(LargeStruct arguments, Func<LargeStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct)
        => next(arguments, ct);
}

// --- Lite: sync procedures and interceptors ---

internal sealed class SumProcedureSmallSync : IProcedure<SmallStruct, long>
{
    public long Invoke(SmallStruct arguments) => arguments.A + arguments.B;
}

internal sealed class SumProcedureMediumSync : IProcedure<MediumStruct, long>
{
    public long Invoke(MediumStruct arguments) => arguments.A + arguments.D;
}

internal sealed class SumProcedureLargeSync : IProcedure<LargeStruct, long>
{
    public long Invoke(LargeStruct arguments) => arguments.A + arguments.H;
}

internal sealed class NoOpInterceptorSmallSync : IProcedureInterceptor<SmallStruct, long>
{
    public long Invoke(SmallStruct arguments, Func<SmallStruct, long> next) => next(arguments);
}

internal sealed class NoOpInterceptorMediumSync : IProcedureInterceptor<MediumStruct, long>
{
    public long Invoke(MediumStruct arguments, Func<MediumStruct, long> next) => next(arguments);
}

internal sealed class NoOpInterceptorLargeSync : IProcedureInterceptor<LargeStruct, long>
{
    public long Invoke(LargeStruct arguments, Func<LargeStruct, long> next) => next(arguments);
}

// --- MediatR: class requests (only classes allowed) ---

public sealed record SmallRequest(long A, long B) : IRequest<long>;

public sealed class SmallRequestHandler : IRequestHandler<SmallRequest, long>
{
    public Task<long> Handle(SmallRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.A + request.B);
}

public sealed record MediumRequest(long A, long B, long C, long D) : IRequest<long>;

public sealed class MediumRequestHandler : IRequestHandler<MediumRequest, long>
{
    public Task<long> Handle(MediumRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.A + request.D);
}

public sealed record LargeRequest(long A, long B, long C, long D, long E, long F, long G, long H) : IRequest<long>;

public sealed class LargeRequestHandler : IRequestHandler<LargeRequest, long>
{
    public Task<long> Handle(LargeRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.A + request.H);
}

public sealed class MediatRNoOpBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        => await next();
}
