using System.Reflection;
using BenchmarkDotNet.Attributes;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Struct vs class: в MediatR только классы (аллокация на каждый запрос),
// в Lite.Procedures можно передавать структуры — нулевые аллокации на аргументах/результате.
// Маленькая структура 16 байт, большая до 64 байт.
// ================================================================

// 16 bytes
public struct SmallStruct
{
    public long A;
    public long B;
}

// 64 bytes
public struct LargeStruct
{
    public long A, B, C, D, E, F, G, H;
}

[Config(typeof(KillerConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class StructVsClassBenchmarks
{
    private IAsyncProcedurePipeline<SmallStruct, long> _liteSmallNo = null!;
    private IAsyncProcedurePipeline<SmallStruct, long> _liteSmallThree = null!;
    private IAsyncProcedurePipeline<LargeStruct, long> _liteLargeNo = null!;
    private IAsyncProcedurePipeline<LargeStruct, long> _liteLargeThree = null!;
    private IMediator _mediatRSmallNo = null!;
    private IMediator _mediatRSmallThree = null!;
    private IMediator _mediatRLargeNo = null!;
    private IMediator _mediatRLargeThree = null!;

    private SmallStruct _smallArg;
    private LargeStruct _largeArg;

    [GlobalSetup]
    public void Setup()
    {
        _smallArg = new SmallStruct { A = 1, B = 2 };
        _largeArg = new LargeStruct { A = 1, B = 2, C = 3, D = 4, E = 5, F = 6, G = 7, H = 8 };

        var procSmall = new NoOpProcedureSmall();
        var procLarge = new NoOpProcedureLarge();

        _liteSmallNo = new AsyncProcedurePipeline<SmallStruct, long>(procSmall, Array.Empty<IProcedureInterceptorCore>());
        _liteSmallThree = new AsyncProcedurePipeline<SmallStruct, long>(procSmall,
            new IProcedureInterceptorCore[]
            {
                new NoOpInterceptorSmall(), new NoOpInterceptorSmall(), new NoOpInterceptorSmall(),
            });

        _liteLargeNo = new AsyncProcedurePipeline<LargeStruct, long>(procLarge, Array.Empty<IProcedureInterceptorCore>());
        _liteLargeThree = new AsyncProcedurePipeline<LargeStruct, long>(procLarge,
            new IProcedureInterceptorCore[]
            {
                new NoOpInterceptorLarge(), new NoOpInterceptorLarge(), new NoOpInterceptorLarge(),
            });

        var svcSmallNo = new ServiceCollection();
        svcSmallNo.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        _mediatRSmallNo = svcSmallNo.BuildServiceProvider().GetRequiredService<IMediator>();

        var svcSmallThree = new ServiceCollection();
        svcSmallThree.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        svcSmallThree.AddTransient<IPipelineBehavior<SmallRequest, long>, MediatRNoOpBehavior1Small>();
        svcSmallThree.AddTransient<IPipelineBehavior<SmallRequest, long>, MediatRNoOpBehavior2Small>();
        svcSmallThree.AddTransient<IPipelineBehavior<SmallRequest, long>, MediatRNoOpBehavior3Small>();
        _mediatRSmallThree = svcSmallThree.BuildServiceProvider().GetRequiredService<IMediator>();

        var svcLargeNo = new ServiceCollection();
        svcLargeNo.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        _mediatRLargeNo = svcLargeNo.BuildServiceProvider().GetRequiredService<IMediator>();

        var svcLargeThree = new ServiceCollection();
        svcLargeThree.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        svcLargeThree.AddTransient<IPipelineBehavior<LargeRequest, long>, MediatRNoOpBehavior1Large>();
        svcLargeThree.AddTransient<IPipelineBehavior<LargeRequest, long>, MediatRNoOpBehavior2Large>();
        svcLargeThree.AddTransient<IPipelineBehavior<LargeRequest, long>, MediatRNoOpBehavior3Large>();
        _mediatRLargeThree = svcLargeThree.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    // --- Small (16 bytes) ---

    [Benchmark(Baseline = true)]
    public ValueTask<OneOf<long, Exception>> Lite_SmallStruct_NoInterceptors()
        => _liteSmallNo.InvokeAsync(_smallArg, CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<long, Exception>> Lite_SmallStruct_ThreeInterceptors()
        => _liteSmallThree.InvokeAsync(_smallArg, CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_SmallClass_NoBehaviors()
        => _mediatRSmallNo.Send(new SmallRequest(1, 2), CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_SmallClass_ThreeBehaviors()
        => _mediatRSmallThree.Send(new SmallRequest(1, 2), CancellationToken.None);

    // --- Large (64 bytes) ---

    [Benchmark]
    public ValueTask<OneOf<long, Exception>> Lite_LargeStruct_NoInterceptors()
        => _liteLargeNo.InvokeAsync(_largeArg, CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<long, Exception>> Lite_LargeStruct_ThreeInterceptors()
        => _liteLargeThree.InvokeAsync(_largeArg, CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_LargeClass_NoBehaviors()
        => _mediatRLargeNo.Send(new LargeRequest(1, 2, 3, 4, 5, 6, 7, 8), CancellationToken.None);

    [Benchmark]
    public Task<long> MediatR_LargeClass_ThreeBehaviors()
        => _mediatRLargeThree.Send(new LargeRequest(1, 2, 3, 4, 5, 6, 7, 8), CancellationToken.None);
}

// --- Lite: procedures and interceptors for structs ---

internal sealed class NoOpProcedureSmall : IAsyncProcedure<SmallStruct, long>
{
    public ValueTask<long> InvokeAsync(SmallStruct arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments.A + arguments.B);
}

internal sealed class NoOpProcedureLarge : IAsyncProcedure<LargeStruct, long>
{
    public ValueTask<long> InvokeAsync(LargeStruct arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments.A + arguments.H);
}

internal sealed class NoOpInterceptorSmall : IAsyncProcedureInterceptor<SmallStruct, long>
{
    public ValueTask<OneOf<Success, long, Exception>> InvokeBeforeExecutionAsync(
        SmallStruct arguments,
        CancellationToken ct)
        => ValueTask.FromResult<OneOf<Success, long, Exception>>(new Success());

    public ValueTask<OneOf<long, Exception>> InvokeAfterExecutionAsync(
        SmallStruct arguments,
        OneOf<long, Exception> result,
        CancellationToken ct)
        => ValueTask.FromResult(result);
}

internal sealed class NoOpInterceptorLarge : IAsyncProcedureInterceptor<LargeStruct, long>
{
    public ValueTask<OneOf<Success, long, Exception>> InvokeBeforeExecutionAsync(
        LargeStruct arguments,
        CancellationToken ct)
        => ValueTask.FromResult<OneOf<Success, long, Exception>>(new Success());

    public ValueTask<OneOf<long, Exception>> InvokeAfterExecutionAsync(
        LargeStruct arguments,
        OneOf<long, Exception> result,
        CancellationToken ct)
        => ValueTask.FromResult(result);
}

// --- MediatR: class requests (only classes allowed) ---

public sealed record SmallRequest(long A, long B) : IRequest<long>;

public sealed class SmallRequestHandler : IRequestHandler<SmallRequest, long>
{
    public Task<long> Handle(SmallRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.A + request.B);
}

public sealed record LargeRequest(long A, long B, long C, long D, long E, long F, long G, long H) : IRequest<long>;

public sealed class LargeRequestHandler : IRequestHandler<LargeRequest, long>
{
    public Task<long> Handle(LargeRequest request, CancellationToken cancellationToken)
        => Task.FromResult(request.A + request.H);
}

public sealed class MediatRNoOpBehavior1Small : IPipelineBehavior<SmallRequest, long>
{
    public async Task<long> Handle(SmallRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}

public sealed class MediatRNoOpBehavior2Small : IPipelineBehavior<SmallRequest, long>
{
    public async Task<long> Handle(SmallRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}

public sealed class MediatRNoOpBehavior3Small : IPipelineBehavior<SmallRequest, long>
{
    public async Task<long> Handle(SmallRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}

public sealed class MediatRNoOpBehavior1Large : IPipelineBehavior<LargeRequest, long>
{
    public async Task<long> Handle(LargeRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}

public sealed class MediatRNoOpBehavior2Large : IPipelineBehavior<LargeRequest, long>
{
    public async Task<long> Handle(LargeRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}

public sealed class MediatRNoOpBehavior3Large : IPipelineBehavior<LargeRequest, long>
{
    public async Task<long> Handle(LargeRequest request, RequestHandlerDelegate<long> next, CancellationToken ct)
        => await next();
}
