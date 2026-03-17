using System;
using BenchmarkDotNet.Attributes;
using Lite.Procedures.Generated;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Interceptors.Attributes;
using Lite.Procedures.Pipeline;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Benchmark;

// ================================================================
// Сгенерированный пайплайн (source generator) vs ручная сборка (new AsyncProcedurePipeline).
// Один и тот же сценарий — одна процедура, 0 или 3 интерцептора.
// Generated: тип из реестра (GeneratedPipeline_Async_*), создаём через рефлексию.
// Manual: new AsyncProcedurePipeline(procedure, interceptors).
// ================================================================

/// <summary>
/// Процедура с [InterceptWith]: генератор эмитит класс пайплайна с тремя интерцепторами.
/// </summary>
[InterceptWith(typeof(NoOpAsyncInterceptor))]
[InterceptWith(typeof(NoOpAsyncInterceptor))]
[InterceptWith(typeof(NoOpAsyncInterceptor))]
internal sealed class ProcedureWithThreeInterceptors : IAsyncProcedure<string, string>
{
    public ValueTask<string> InvokeAsync(string arguments, CancellationToken ct)
        => ValueTask.FromResult(arguments);
}

[Config(typeof(KillerConfig))]
[MemoryDiagnoser]
[MinColumn, MaxColumn, BaselineColumn]
public class GeneratedVsManualPipelineBenchmarks
{
    private IAsyncProcedurePipeline<string, string> _generatedNo = null!;
    private IAsyncProcedurePipeline<string, string> _generatedThree = null!;
    private IAsyncProcedurePipeline<string, string> _manualNo = null!;
    private IAsyncProcedurePipeline<string, string> _manualThree = null!;

    [GlobalSetup]
    public void Setup()
    {
        var registry = GeneratedPipelineRegistryDiscovery.TryDiscover()
            ?? throw new InvalidOperationException("Generated pipeline registry not found. Build the project so the source generator runs.");

        var noOp = new NoOpAsyncProcedure();
        var procThree = new ProcedureWithThreeInterceptors();
        var interceptor = new NoOpAsyncInterceptor();

        // Сгенерированный пайплайн: 0 интерцепторов (NoOpAsyncProcedure, пустая цепочка).
        var generatedTypeNo = registry.GetGeneratedPipelineType(typeof(NoOpAsyncProcedure), Type.EmptyTypes);
        if (generatedTypeNo != null)
        {
            _generatedNo = (IAsyncProcedurePipeline<string, string>)Activator.CreateInstance(generatedTypeNo, noOp)!;
        }
        else
        {
            _generatedNo = new AsyncProcedurePipeline<string, string>(noOp, Array.Empty<IProcedureInterceptorCore>());
        }

        // Сгенерированный пайплайн: 3 интерцептора (ProcedureWithThreeInterceptors).
        var interceptorTypesThree = new[] { typeof(NoOpAsyncInterceptor), typeof(NoOpAsyncInterceptor), typeof(NoOpAsyncInterceptor) };
        var generatedTypeThree = registry.GetGeneratedPipelineType(typeof(ProcedureWithThreeInterceptors), interceptorTypesThree);
        if (generatedTypeThree != null)
        {
            _generatedThree = (IAsyncProcedurePipeline<string, string>)Activator.CreateInstance(generatedTypeThree, procThree, interceptor, interceptor, interceptor)!;
        }
        else
        {
            _generatedThree = new AsyncProcedurePipeline<string, string>(procThree,
                new IProcedureInterceptorCore[] { interceptor, interceptor, interceptor });
        }

        // Ручная сборка: те же процедуры и цепочки.
        _manualNo = new AsyncProcedurePipeline<string, string>(noOp, Array.Empty<IProcedureInterceptorCore>());
        _manualThree = new AsyncProcedurePipeline<string, string>(procThree,
            new IProcedureInterceptorCore[] { interceptor, interceptor, interceptor });
    }

    // --- Без интерцепторов ---

    [Benchmark(Baseline = true)]
    public ValueTask<OneOf<string, Exception>> Generated_NoInterceptors()
        => _generatedNo.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<string, Exception>> Manual_NoInterceptors()
        => _manualNo.InvokeAsync("input", CancellationToken.None);

    // --- С тремя интерцепторами ---

    [Benchmark]
    public ValueTask<OneOf<string, Exception>> Generated_ThreeInterceptors()
        => _generatedThree.InvokeAsync("input", CancellationToken.None);

    [Benchmark]
    public ValueTask<OneOf<string, Exception>> Manual_ThreeInterceptors()
        => _manualThree.InvokeAsync("input", CancellationToken.None);
}
