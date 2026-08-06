using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interception;
using MediatR;
using MessagePipe;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Benchmark;

// ============================================================================
// HONEST head-to-head: Lite.Procedures vs MediatR vs MessagePipe.
//
// Each benchmark class proves ONE thesis and isolates ONE variable. Rules held
// constant across all three frameworks:
//   * same request/response type, same REUSED instance — request alloc is the
//     caller's, never the framework's, so "Allocated" = pure framework cost;
//   * handler just returns the value — no work to hide the framework difference;
//   * middleware (where present) are EMPTY pass-through (`=> next()`). Real work
//     (logging/auth) would be identical for everyone and mask the framework — so
//     to measure the framework itself, the middleware must do nothing;
//   * each dispatcher resolved ONCE from its own DI container; the body only invokes.
//
// Struct axis is Lite vs MessagePipe only — MediatR is class-only by design
// (requires IRequest<T>), so there is nothing to compare there.
// ============================================================================

public sealed record EchoRequest(string Value) : IRequest<string>; // class; also the MediatR contract
public readonly record struct EchoStruct(long A, long B);          // value type (Lite + MessagePipe only)

internal sealed class FairConfig : ManualConfig
{
    public FairConfig()
    {
        AddJob(Job.Default
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithWarmupCount(5)
            .WithIterationCount(15));
        AddDiagnoser(MemoryDiagnoser.Default);
    }
}

// THESIS: raw per-request dispatch overhead, zero middleware. Pure framework machinery.
[Config(typeof(FairConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class DispatchBenchmarks
{
    private static readonly EchoRequest Class = new("input");
    private EchoStruct _struct;

    private IAsyncProcedure<EchoRequest, string> _liteClass = null!;
    private IAsyncProcedure<EchoStruct, long> _liteStruct = null!;
    private IMediator _mediatR = null!;
    private IAsyncRequestHandler<EchoRequest, string> _mpClass = null!;
    private IAsyncRequestHandler<EchoStruct, long> _mpStruct = null!;

    [GlobalSetup]
    public void Setup()
    {
        _struct = new EchoStruct(1, 2);

        var lite = new ServiceCollection();
        lite.AddLiteProcedures(b => b
            .AddProcedure<EchoClassProcedure>()
            .AddProcedure<EchoStructProcedure>());
        var liteSp = lite.BuildServiceProvider();
        _liteClass = liteSp.GetRequiredService<IAsyncProcedure<EchoRequest, string>>();
        _liteStruct = liteSp.GetRequiredService<IAsyncProcedure<EchoStruct, long>>();

        var med = new ServiceCollection();
        med.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        _mediatR = med.BuildServiceProvider().GetRequiredService<IMediator>();

        var mp = new ServiceCollection();
        mp.AddMessagePipe();
        mp.AddSingleton<IAsyncRequestHandler<EchoRequest, string>, MpClassHandler>();
        mp.AddSingleton<IAsyncRequestHandler<EchoStruct, long>, MpStructHandler>();
        var mpSp = mp.BuildServiceProvider();
        _mpClass = mpSp.GetRequiredService<IAsyncRequestHandler<EchoRequest, string>>();
        _mpStruct = mpSp.GetRequiredService<IAsyncRequestHandler<EchoStruct, long>>();
    }

    [Benchmark(Baseline = true)] public ValueTask<string> Lite_Class() => _liteClass.ExecuteAsync(Class, CancellationToken.None);
    [Benchmark] public Task<string> MediatR_Class() => _mediatR.Send(Class, CancellationToken.None);
    [Benchmark] public ValueTask<string> MessagePipe_Class() => _mpClass.InvokeAsync(Class, CancellationToken.None);

    [Benchmark] public ValueTask<long> Lite_Struct() => _liteStruct.ExecuteAsync(_struct, CancellationToken.None);
    [Benchmark] public ValueTask<long> MessagePipe_Struct() => _mpStruct.InvokeAsync(_struct, CancellationToken.None);
}

// THESIS: cost of a 3-layer pass-through middleware chain (per-layer dispatch).
[Config(typeof(FairConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class ChainBenchmarks
{
    private static readonly EchoRequest Class = new("input");
    private EchoStruct _struct;

    private IAsyncProcedure<EchoRequest, string> _liteClass = null!;
    private IAsyncProcedure<EchoRequest, string> _liteFast = null!;
    private IAsyncProcedure<EchoStruct, long> _liteStruct = null!;
    private IMediator _mediatR = null!;
    private IAsyncRequestHandler<EchoRequest, string> _mpClass = null!;
    private IAsyncRequestHandler<EchoStruct, long> _mpStruct = null!;

    [GlobalSetup]
    public void Setup()
    {
        _struct = new EchoStruct(1, 2);

        var lite = new ServiceCollection();
        lite.AddLiteProcedures(b => b
            .AddProcedure<EchoClassProcedure>(p =>
            {
                p.UseInterceptor<ClassPassThrough1>();
                p.UseInterceptor<ClassPassThrough2>();
                p.UseInterceptor<ClassPassThrough3>();
            })
            .AddProcedure<EchoStructProcedure>(p =>
            {
                p.UseInterceptor<StructPassThrough1>();
                p.UseInterceptor<StructPassThrough2>();
                p.UseInterceptor<StructPassThrough3>();
            }));
        var liteSp = lite.BuildServiceProvider();
        _liteClass = liteSp.GetRequiredService<IAsyncProcedure<EchoRequest, string>>();
        _liteStruct = liteSp.GetRequiredService<IAsyncProcedure<EchoStruct, long>>();

        // Separate container: attribute chain -> source-generated FastPipeline.
        var liteFast = new ServiceCollection();
        liteFast.AddLiteProcedures(b => b.AddProcedure<EchoFastProcedure>());
        _liteFast = liteFast.BuildServiceProvider().GetRequiredService<IAsyncProcedure<EchoRequest, string>>();

        var med = new ServiceCollection();
        med.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior1>();
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior2>();
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior3>();
        _mediatR = med.BuildServiceProvider().GetRequiredService<IMediator>();

        var mp = new ServiceCollection();
        mp.AddMessagePipe(o =>
        {
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter1), 0);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter2), 1);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter3), 2);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpStructFilter1), 0);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpStructFilter2), 1);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpStructFilter3), 2);
        });
        mp.AddSingleton<IAsyncRequestHandler<EchoRequest, string>, MpClassHandler>();
        mp.AddSingleton<IAsyncRequestHandler<EchoStruct, long>, MpStructHandler>();
        var mpSp = mp.BuildServiceProvider();
        _mpClass = mpSp.GetRequiredService<IAsyncRequestHandler<EchoRequest, string>>();
        _mpStruct = mpSp.GetRequiredService<IAsyncRequestHandler<EchoStruct, long>>();
    }

    [Benchmark(Baseline = true)] public ValueTask<string> Lite_Class() => _liteClass.ExecuteAsync(Class, CancellationToken.None);
    [Benchmark] public ValueTask<string> Lite_Fast_Class() => _liteFast.ExecuteAsync(Class, CancellationToken.None);
    [Benchmark] public Task<string> MediatR_Class() => _mediatR.Send(Class, CancellationToken.None);
    [Benchmark] public ValueTask<string> MessagePipe_Class() => _mpClass.InvokeAsync(Class, CancellationToken.None);

    [Benchmark] public ValueTask<long> Lite_Struct() => _liteStruct.ExecuteAsync(_struct, CancellationToken.None);
    [Benchmark] public ValueTask<long> MessagePipe_Struct() => _mpStruct.InvokeAsync(_struct, CancellationToken.None);
}

// THESIS: cumulative time + memory + GC over 10k requests through a 3-layer chain.
[Config(typeof(FairConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn]
public class ThroughputBenchmarks
{
    private const int N = 10_000;
    private static readonly EchoRequest Class = new("input");

    private IAsyncProcedure<EchoRequest, string> _lite = null!;
    private IAsyncProcedure<EchoRequest, string> _liteFast = null!;
    private IMediator _mediatR = null!;
    private IAsyncRequestHandler<EchoRequest, string> _mp = null!;

    [GlobalSetup]
    public void Setup()
    {
        var lite = new ServiceCollection();
        lite.AddLiteProcedures(b => b.AddProcedure<EchoClassProcedure>(p =>
        {
            p.UseInterceptor<ClassPassThrough1>();
            p.UseInterceptor<ClassPassThrough2>();
            p.UseInterceptor<ClassPassThrough3>();
        }));
        _lite = lite.BuildServiceProvider().GetRequiredService<IAsyncProcedure<EchoRequest, string>>();

        var liteFast = new ServiceCollection();
        liteFast.AddLiteProcedures(b => b.AddProcedure<EchoFastProcedure>());
        _liteFast = liteFast.BuildServiceProvider().GetRequiredService<IAsyncProcedure<EchoRequest, string>>();

        var med = new ServiceCollection();
        med.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior1>();
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior2>();
        med.AddTransient<IPipelineBehavior<EchoRequest, string>, MediatRClassBehavior3>();
        _mediatR = med.BuildServiceProvider().GetRequiredService<IMediator>();

        var mp = new ServiceCollection();
        mp.AddMessagePipe(o =>
        {
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter1), 0);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter2), 1);
            o.AddGlobalAsyncRequestHandlerFilter(typeof(MpClassFilter3), 2);
        });
        mp.AddSingleton<IAsyncRequestHandler<EchoRequest, string>, MpClassHandler>();
        _mp = mp.BuildServiceProvider().GetRequiredService<IAsyncRequestHandler<EchoRequest, string>>();
    }

    [Benchmark(Baseline = true)]
    public async Task Lite_10k()
    {
        for (var i = 0; i < N; i++) await _lite.ExecuteAsync(Class, CancellationToken.None);
    }

    [Benchmark]
    public async Task Lite_Fast_10k()
    {
        for (var i = 0; i < N; i++) await _liteFast.ExecuteAsync(Class, CancellationToken.None);
    }

    [Benchmark]
    public async Task MediatR_10k()
    {
        for (var i = 0; i < N; i++) await _mediatR.Send(Class, CancellationToken.None);
    }

    [Benchmark]
    public async Task MessagePipe_10k()
    {
        for (var i = 0; i < N; i++) await _mp.InvokeAsync(Class, CancellationToken.None);
    }
}

// ============================== Lite fixtures ==============================

internal sealed class EchoClassProcedure : IAsyncProcedure<EchoRequest, string>
{
    public ValueTask<string> ExecuteAsync(EchoRequest arguments, CancellationToken cancellationToken = default)
        => new(arguments.Value);
}

// Static chain via attributes -> source generator emits an unrolled FastPipeline.
[InterceptWith(typeof(ClassPassThrough1))]
[InterceptWith(typeof(ClassPassThrough2))]
[InterceptWith(typeof(ClassPassThrough3))]
internal sealed class EchoFastProcedure : IAsyncProcedure<EchoRequest, string>
{
    public ValueTask<string> ExecuteAsync(EchoRequest arguments, CancellationToken cancellationToken = default)
        => new(arguments.Value);
}
internal sealed class EchoStructProcedure : IAsyncProcedure<EchoStruct, long>
{
    public ValueTask<long> ExecuteAsync(EchoStruct arguments, CancellationToken cancellationToken = default)
        => new(arguments.A + arguments.B);
}
internal sealed class ClassPassThrough1 : AsyncInterceptor<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest a, Func<EchoRequest, CancellationToken, ValueTask<string>> next, CancellationToken ct) => next(a, ct); }
internal sealed class ClassPassThrough2 : AsyncInterceptor<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest a, Func<EchoRequest, CancellationToken, ValueTask<string>> next, CancellationToken ct) => next(a, ct); }
internal sealed class ClassPassThrough3 : AsyncInterceptor<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest a, Func<EchoRequest, CancellationToken, ValueTask<string>> next, CancellationToken ct) => next(a, ct); }
internal sealed class StructPassThrough1 : AsyncInterceptor<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct a, Func<EchoStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct) => next(a, ct); }
internal sealed class StructPassThrough2 : AsyncInterceptor<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct a, Func<EchoStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct) => next(a, ct); }
internal sealed class StructPassThrough3 : AsyncInterceptor<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct a, Func<EchoStruct, CancellationToken, ValueTask<long>> next, CancellationToken ct) => next(a, ct); }

// ============================== MediatR fixtures (class only) ==============================

public sealed class MediatRClassHandler : MediatR.IRequestHandler<EchoRequest, string>
{ public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken) => Task.FromResult(request.Value); }
public sealed class MediatRClassBehavior1 : IPipelineBehavior<EchoRequest, string>
{ public Task<string> Handle(EchoRequest r, RequestHandlerDelegate<string> next, CancellationToken ct) => next(); }
public sealed class MediatRClassBehavior2 : IPipelineBehavior<EchoRequest, string>
{ public Task<string> Handle(EchoRequest r, RequestHandlerDelegate<string> next, CancellationToken ct) => next(); }
public sealed class MediatRClassBehavior3 : IPipelineBehavior<EchoRequest, string>
{ public Task<string> Handle(EchoRequest r, RequestHandlerDelegate<string> next, CancellationToken ct) => next(); }

// ============================== MessagePipe fixtures ==============================

internal sealed class MpClassHandler : IAsyncRequestHandler<EchoRequest, string>
{ public ValueTask<string> InvokeAsync(EchoRequest request, CancellationToken cancellationToken = default) => new(request.Value); }
internal sealed class MpStructHandler : IAsyncRequestHandler<EchoStruct, long>
{ public ValueTask<long> InvokeAsync(EchoStruct request, CancellationToken cancellationToken = default) => new(request.A + request.B); }
internal sealed class MpClassFilter1 : AsyncRequestHandlerFilter<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest r, CancellationToken ct, Func<EchoRequest, CancellationToken, ValueTask<string>> next) => next(r, ct); }
internal sealed class MpClassFilter2 : AsyncRequestHandlerFilter<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest r, CancellationToken ct, Func<EchoRequest, CancellationToken, ValueTask<string>> next) => next(r, ct); }
internal sealed class MpClassFilter3 : AsyncRequestHandlerFilter<EchoRequest, string>
{ public override ValueTask<string> InvokeAsync(EchoRequest r, CancellationToken ct, Func<EchoRequest, CancellationToken, ValueTask<string>> next) => next(r, ct); }
internal sealed class MpStructFilter1 : AsyncRequestHandlerFilter<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct r, CancellationToken ct, Func<EchoStruct, CancellationToken, ValueTask<long>> next) => next(r, ct); }
internal sealed class MpStructFilter2 : AsyncRequestHandlerFilter<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct r, CancellationToken ct, Func<EchoStruct, CancellationToken, ValueTask<long>> next) => next(r, ct); }
internal sealed class MpStructFilter3 : AsyncRequestHandlerFilter<EchoStruct, long>
{ public override ValueTask<long> InvokeAsync(EchoStruct r, CancellationToken ct, Func<EchoStruct, CancellationToken, ValueTask<long>> next) => next(r, ct); }
