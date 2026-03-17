using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Lite.Procedures.Benchmark;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;

// ================================================================
// Entry point
// ================================================================

BenchmarkRunner.Run<AsyncProcedurePipelineBenchmarks>();
BenchmarkRunner.Run<SyncProcedurePipelineBenchmarks>();
BenchmarkRunner.Run<DiResolveBenchmarks>();
BenchmarkRunner.Run<RealWorldInterceptorsBenchmarks>();
BenchmarkRunner.Run<LiteProceduresVsMediatRBenchmarks>();
BenchmarkRunner.Run<LiteProceduresVsMessagePipeBenchmarks>();
BenchmarkRunner.Run<ConcurrentBenchmarks>();
BenchmarkRunner.Run<KillerComparisonBenchmarks>();
BenchmarkRunner.Run<Per10kRequestsBenchmarks>();
BenchmarkRunner.Run<StructVsClassBenchmarks>();
BenchmarkRunner.Run<GeneratedVsManualPipelineBenchmarks>();
BenchmarkRunner.Run<MessagePipeComparisonBenchmarks>();

namespace Lite.Procedures.Benchmark
{
    // ================================================================
// 1. Async pipeline
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class AsyncProcedurePipelineBenchmarks
    {
        private AsyncProcedurePipeline<string, string> _noInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _oneInterceptor = null!;
        private AsyncProcedurePipeline<string, string> _threeInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _fiveInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _tenInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _mixedInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _shortCircuitFirst = null!;
        private AsyncProcedurePipeline<string, string> _shortCircuitMiddle = null!;
        private AsyncProcedurePipeline<string, string> _shortCircuitLast = null!;
        private AsyncProcedurePipeline<string, string> _faultFirst = null!;
        private AsyncProcedurePipeline<string, string> _faultMiddle = null!;
        private AsyncProcedurePipeline<string, string> _procedureThrows = null!;
        private AsyncProcedurePipeline<string, string> _recoveryAfterThrow = null!;

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new NoOpAsyncProcedure();
            var throwingProcedure = new ThrowingAsyncProcedure();

            _noInterceptors = Build(procedure);
            _oneInterceptor = Build(procedure, new NoOpAsyncInterceptor());
            _threeInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _fiveInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _tenInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _mixedInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpSyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpSyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitFirst = Build(procedure, new ShortCircuitAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitMiddle = Build(procedure, new NoOpAsyncInterceptor(), new ShortCircuitAsyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitLast = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new ShortCircuitAsyncInterceptor());
            _faultFirst = Build(procedure, new FaultAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _faultMiddle = Build(procedure, new NoOpAsyncInterceptor(), new FaultAsyncInterceptor(), new NoOpAsyncInterceptor());
            _procedureThrows = Build(throwingProcedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _recoveryAfterThrow = Build(throwingProcedure, new NoOpAsyncInterceptor(), new RecoveringAsyncInterceptor(), new NoOpAsyncInterceptor());
        }

        private static AsyncProcedurePipeline<string, string> Build(
            IAsyncProcedure<string, string> procedure,
            params IProcedureInterceptorCore[] interceptors) =>
            new(procedure, interceptors);

        [Benchmark(Baseline = true)]
        public ValueTask<OneOf<string, Exception>> NoInterceptors()
            => _noInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> OneInterceptor()
            => _oneInterceptor.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> ThreeInterceptors()
            => _threeInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> FiveInterceptors()
            => _fiveInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> TenInterceptors()
            => _tenInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> FiveInterceptors_MixedSyncAsync()
            => _mixedInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> ShortCircuit_First()
            => _shortCircuitFirst.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> ShortCircuit_Middle()
            => _shortCircuitMiddle.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> ShortCircuit_Last()
            => _shortCircuitLast.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> Fault_First()
            => _faultFirst.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> Fault_Middle()
            => _faultMiddle.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> ProcedureThrows_ThreeInterceptors()
            => _procedureThrows.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> RecoveryAfterThrow()
            => _recoveryAfterThrow.InvokeAsync("input", CancellationToken.None);
    }

// ================================================================
// 2. Sync pipeline
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class SyncProcedurePipelineBenchmarks
    {
        private ProcedurePipeline<string, string> _noInterceptors = null!;
        private ProcedurePipeline<string, string> _threeInterceptors = null!;
        private ProcedurePipeline<string, string> _fiveInterceptors = null!;
        private ProcedurePipeline<string, string> _shortCircuit = null!;
        private ProcedurePipeline<string, string> _fault = null!;
        private ProcedurePipeline<string, string> _procedureThrows = null!;

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new NoOpSyncProcedure();
            var throwingProcedure = new ThrowingSyncProcedure();

            _noInterceptors = new ProcedurePipeline<string, string>(procedure, Array.Empty<IProcedureInterceptorCore>());
            _threeInterceptors = new ProcedurePipeline<string, string>(procedure, new IProcedureInterceptorCore[] { new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor() });
            _fiveInterceptors = new ProcedurePipeline<string, string>(procedure, new IProcedureInterceptorCore[] { new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor() });
            _shortCircuit = new ProcedurePipeline<string, string>(procedure, new IProcedureInterceptorCore[] { new NoOpSyncInterceptor(), new ShortCircuitSyncInterceptor(), new NoOpSyncInterceptor() });
            _fault = new ProcedurePipeline<string, string>(procedure, new IProcedureInterceptorCore[] { new NoOpSyncInterceptor(), new FaultSyncInterceptor(), new NoOpSyncInterceptor() });
            _procedureThrows = new ProcedurePipeline<string, string>(throwingProcedure, new IProcedureInterceptorCore[] { new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor() });
        }

        [Benchmark(Baseline = true)]
        public OneOf<string, Exception> NoInterceptors() => _noInterceptors.Invoke("input");

        [Benchmark]
        public OneOf<string, Exception> ThreeInterceptors() => _threeInterceptors.Invoke("input");

        [Benchmark]
        public OneOf<string, Exception> FiveInterceptors() => _fiveInterceptors.Invoke("input");

        [Benchmark]
        public OneOf<string, Exception> ShortCircuit() => _shortCircuit.Invoke("input");

        [Benchmark]
        public OneOf<string, Exception> Fault() => _fault.Invoke("input");

        [Benchmark]
        public OneOf<string, Exception> ProcedureThrows() => _procedureThrows.Invoke("input");
    }

// ================================================================
// 3. DI resolve hot path
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class DiResolveBenchmarks
    {
        private IServiceProvider _sp = null!;
        private IAsyncProcedure<string, string> _directRef = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLiteProcedures(b =>
            {
                b.AddGlobalInterceptor<NoOpAsyncInterceptor>().AddGlobalInterceptor<NoOpSyncInterceptor>().AddProcedure<NoOpAsyncProcedure>();
                b.Build();
            });

            _sp = services.BuildServiceProvider();
            _directRef = _sp.GetRequiredService<IAsyncProcedure<string, string>>();
        }

        [Benchmark(Baseline = true)]
        public async ValueTask<OneOf<string, Exception>> DirectReference()
        {
            var result = await _directRef.InvokeAsync("input", CancellationToken.None);
            return result;
        }

        [Benchmark]
        public async ValueTask<OneOf<string, Exception>> ResolveAndInvoke()
        {
            var pipeline = _sp.GetRequiredService<IAsyncProcedure<string, string>>();
            return await pipeline.InvokeAsync("input", CancellationToken.None);
        }

        [Benchmark]
        public async ValueTask<OneOf<string, Exception>> ResolveFromScope_AndInvoke()
        {
            await using var scope = _sp.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IAsyncProcedure<string, string>>();
            return await pipeline.InvokeAsync("input", CancellationToken.None);
        }
    }

// ================================================================
// 4. Real-world interceptors
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class RealWorldInterceptorsBenchmarks
    {
        private AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult> _noInterceptors = null!;
        private AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult> _withLogging = null!;
        private AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult> _withValidation = null!;
        private AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult> _withLoggingAndValidation = null!;
        private AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult> _fullStack = null!;

        private readonly CreatePlayerArgs _validArgs = new("Player1", 25);
        private readonly CreatePlayerArgs _invalidArgs = new("", -1);

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new CreatePlayerProcedure();

            _noInterceptors = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, Array.Empty<IProcedureInterceptorCore>());
            _withLogging = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, new IProcedureInterceptorCore[] { new LoggingInterceptor() });
            _withValidation = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, new IProcedureInterceptorCore[] { new ValidationInterceptor() });
            _withLoggingAndValidation = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, new IProcedureInterceptorCore[] { new LoggingInterceptor(), new ValidationInterceptor() });
            _fullStack = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, new IProcedureInterceptorCore[] { new LoggingInterceptor(), new ValidationInterceptor(), new MetricsInterceptor(), new ErrorHandlingInterceptor() });
        }

        [Benchmark(Baseline = true)]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> NoInterceptors_ValidArgs()
            => _noInterceptors.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> WithLogging_ValidArgs()
            => _withLogging.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> WithValidation_ValidArgs()
            => _withValidation.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> WithValidation_InvalidArgs_ShortCircuit()
            => _withValidation.InvokeAsync(_invalidArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> WithLoggingAndValidation_ValidArgs()
            => _withLoggingAndValidation.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> FullStack_ValidArgs()
            => _fullStack.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<CreatePlayerResult, Exception>> FullStack_InvalidArgs_ShortCircuit()
            => _fullStack.InvokeAsync(_invalidArgs, CancellationToken.None);
    }

// ================================================================
// 5. vs MediatR
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class LiteProceduresVsMediatRBenchmarks
    {
        private AsyncProcedurePipeline<string, string> _liteNoInterceptors = null!;
        private AsyncProcedurePipeline<string, string> _liteThreeInterceptors = null!;
        private FakeMediatR _mediatR = null!;

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new NoOpAsyncProcedure();
            _liteNoInterceptors = new AsyncProcedurePipeline<string, string>(procedure, Array.Empty<IProcedureInterceptorCore>());
            _liteThreeInterceptors = new AsyncProcedurePipeline<string, string>(procedure, new IProcedureInterceptorCore[] { new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor() });
            _mediatR = new FakeMediatR();
        }

        [Benchmark(Baseline = true)]
        public ValueTask<OneOf<string, Exception>> Lite_NoInterceptors()
            => _liteNoInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<OneOf<string, Exception>> Lite_ThreeInterceptors()
            => _liteThreeInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public Task<string> MediatR_NoBehaviors()
            => _mediatR.SendNoBehaviors("input");

        [Benchmark]
        public Task<string> MediatR_ThreeBehaviors()
            => _mediatR.SendThreeBehaviors("input", CancellationToken.None);
    }

// ================================================================
// 6. vs MessagePipe
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class LiteProceduresVsMessagePipeBenchmarks
    {
        private AsyncProcedurePipeline<string, string> _liteThreeInterceptors = null!;
        private FakeMessagePipe _messagePipe = null!;

        [GlobalSetup]
        public void Setup()
        {
            _liteThreeInterceptors = new AsyncProcedurePipeline<string, string>(
                new NoOpAsyncProcedure(),
                new IProcedureInterceptorCore[] { new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor() });

            _messagePipe = new FakeMessagePipe();
        }

        [Benchmark(Baseline = true)]
        public ValueTask<OneOf<string, Exception>> LiteProcedures_ThreeInterceptors()
            => _liteThreeInterceptors.InvokeAsync("input", CancellationToken.None);

        [Benchmark]
        public ValueTask<string> MessagePipe_ThreeFilters()
            => _messagePipe.SendAsync("input", CancellationToken.None);
    }

// ================================================================
// 7. Concurrent
// ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class ConcurrentBenchmarks
    {
        private AsyncProcedurePipeline<string, string> _pipeline = null!;

        [Params(1, 4, 8, 16)]
        public int ThreadCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _pipeline = new AsyncProcedurePipeline<string, string>(
                new NoOpAsyncProcedure(),
                new IProcedureInterceptorCore[] { new NoOpAsyncInterceptor(), new NoOpSyncInterceptor(), new NoOpAsyncInterceptor() });
        }

        [Benchmark]
        public async Task Concurrent_Invoke()
        {
            var tasks = new Task[ThreadCount];
            for (var i = 0; i < ThreadCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (var j = 0; j < 1000; j++)
                        await _pipeline.InvokeAsync("input", CancellationToken.None);
                });
            }
            await Task.WhenAll(tasks);
        }

        [Benchmark]
        public async Task Concurrent_Invoke_WithCancellation()
        {
            using var cts = new CancellationTokenSource();
            var tasks = new Task[ThreadCount];
            for (var i = 0; i < ThreadCount; i++)
            {
                tasks[i] = Task.Run(async () =>
                {
                    for (var j = 0; j < 1000; j++)
                        await _pipeline.InvokeAsync("input", cts.Token);
                });
            }
            await Task.WhenAll(tasks);
        }
    }

// ================================================================
// Fixtures — procedures
// ================================================================

    internal sealed class NoOpAsyncProcedure : IAsyncProcedure<string, string>
    {
        public ValueTask<string> InvokeAsync(string arguments, CancellationToken ct)
            => ValueTask.FromResult(arguments);
    }

    internal sealed class NoOpSyncProcedure : IProcedure<string, string>
    {
        public string Invoke(string arguments) => arguments;
    }

    internal sealed class ThrowingAsyncProcedure : IAsyncProcedure<string, string>
    {
        private static readonly Exception _ex = new InvalidOperationException("procedure error");
        public ValueTask<string> InvokeAsync(string arguments, CancellationToken ct) => throw _ex;
    }

    internal sealed class ThrowingSyncProcedure : IProcedure<string, string>
    {
        private static readonly Exception _ex = new InvalidOperationException("procedure error");
        public string Invoke(string arguments) => throw _ex;
    }

// ================================================================
// Fixtures — no-op interceptors
// ================================================================

    internal sealed class NoOpAsyncInterceptor : IAsyncProcedureInterceptor<string, string>
    {
        public ValueTask<OneOf<Success, string, Exception>> InvokeBeforeExecutionAsync(string arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, string, Exception>>(new Success());

        public ValueTask<OneOf<string, Exception>> InvokeAfterExecutionAsync(string arguments, OneOf<string, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result);
    }

    internal sealed class NoOpSyncInterceptor : IProcedureInterceptor<string, string>
    {
        public OneOf<Success, string, Exception> InvokeBefore(string arguments) => new Success();
        public OneOf<string, Exception> InvokeAfter(string arguments, OneOf<string, Exception> result) => result;
    }

    internal sealed class ShortCircuitAsyncInterceptor : IAsyncProcedureInterceptor<string, string>
    {
        public ValueTask<OneOf<Success, string, Exception>> InvokeBeforeExecutionAsync(string arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, string, Exception>>("short_circuit");

        public ValueTask<OneOf<string, Exception>> InvokeAfterExecutionAsync(string arguments, OneOf<string, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result);
    }

    internal sealed class ShortCircuitSyncInterceptor : IProcedureInterceptor<string, string>
    {
        public OneOf<Success, string, Exception> InvokeBefore(string arguments) => "short_circuit";
        public OneOf<string, Exception> InvokeAfter(string arguments, OneOf<string, Exception> result) => result;
    }

    internal sealed class FaultAsyncInterceptor : IAsyncProcedureInterceptor<string, string>
    {
        private static readonly Exception _ex = new InvalidOperationException("fault");

        public ValueTask<OneOf<Success, string, Exception>> InvokeBeforeExecutionAsync(string arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, string, Exception>>(_ex);

        public ValueTask<OneOf<string, Exception>> InvokeAfterExecutionAsync(string arguments, OneOf<string, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result);
    }

    internal sealed class FaultSyncInterceptor : IProcedureInterceptor<string, string>
    {
        private static readonly Exception _ex = new InvalidOperationException("fault");
        public OneOf<Success, string, Exception> InvokeBefore(string arguments) => _ex;
        public OneOf<string, Exception> InvokeAfter(string arguments, OneOf<string, Exception> result) => result;
    }

    internal sealed class RecoveringAsyncInterceptor : IAsyncProcedureInterceptor<string, string>
    {
        public ValueTask<OneOf<Success, string, Exception>> InvokeBeforeExecutionAsync(string arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, string, Exception>>(new Success());

        public ValueTask<OneOf<string, Exception>> InvokeAfterExecutionAsync(string arguments, OneOf<string, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result.IsT1 ? (OneOf<string, Exception>)"recovered" : result);
    }

// ================================================================
// Fixtures — real-world domain
// ================================================================

    internal sealed record CreatePlayerArgs(string Name, int Age);

    public sealed record CreatePlayerResult(Guid Id, string Name);

    internal sealed class CreatePlayerProcedure : IAsyncProcedure<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<CreatePlayerResult> InvokeAsync(CreatePlayerArgs arguments, CancellationToken ct)
            => ValueTask.FromResult(new CreatePlayerResult(Guid.NewGuid(), arguments.Name));
    }

    internal sealed class LoggingInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        private int _callCount;

        public ValueTask<OneOf<Success, CreatePlayerResult, Exception>> InvokeBeforeExecutionAsync(CreatePlayerArgs arguments, CancellationToken ct)
        {
            Interlocked.Increment(ref _callCount);
            return ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new Success());
        }

        public ValueTask<OneOf<CreatePlayerResult, Exception>> InvokeAfterExecutionAsync(CreatePlayerArgs arguments, OneOf<CreatePlayerResult, Exception> result, CancellationToken ct)
        {
            Interlocked.Increment(ref _callCount);
            return ValueTask.FromResult(result);
        }
    }

    internal sealed class ValidationInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<OneOf<Success, CreatePlayerResult, Exception>> InvokeBeforeExecutionAsync(CreatePlayerArgs arguments, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(arguments.Name))
                return ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new ArgumentException("Name is required"));

            if (arguments.Age < 0)
                return ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new ArgumentException("Age must be non-negative"));

            return ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new Success());
        }

        public ValueTask<OneOf<CreatePlayerResult, Exception>> InvokeAfterExecutionAsync(CreatePlayerArgs arguments, OneOf<CreatePlayerResult, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result);
    }

    internal sealed class MetricsInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        private long _successCount;
        private long _errorCount;

        public ValueTask<OneOf<Success, CreatePlayerResult, Exception>> InvokeBeforeExecutionAsync(CreatePlayerArgs arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new Success());

        public ValueTask<OneOf<CreatePlayerResult, Exception>> InvokeAfterExecutionAsync(CreatePlayerArgs arguments, OneOf<CreatePlayerResult, Exception> result, CancellationToken ct)
        {
            if (result.IsT0) Interlocked.Increment(ref _successCount);
            else Interlocked.Increment(ref _errorCount);
            return ValueTask.FromResult(result);
        }
    }

    internal sealed class ErrorHandlingInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<OneOf<Success, CreatePlayerResult, Exception>> InvokeBeforeExecutionAsync(CreatePlayerArgs arguments, CancellationToken ct)
            => ValueTask.FromResult<OneOf<Success, CreatePlayerResult, Exception>>(new Success());

        public ValueTask<OneOf<CreatePlayerResult, Exception>> InvokeAfterExecutionAsync(CreatePlayerArgs arguments, OneOf<CreatePlayerResult, Exception> result, CancellationToken ct)
            => ValueTask.FromResult(result);
    }

// ================================================================
// Fixtures — fake MediatR
// ================================================================

    internal sealed class FakeMediatR
    {
        public Task<string> SendNoBehaviors(string request) => Task.FromResult(request);

        public async Task<string> SendThreeBehaviors(string request, CancellationToken ct)
            => await Behavior3(request, ct, () => Behavior2(request, ct, () => Behavior1(request, ct, () => Task.FromResult(request))));

        private static Task<string> Behavior1(string req, CancellationToken ct, Func<Task<string>> next) => next();
        private static Task<string> Behavior2(string req, CancellationToken ct, Func<Task<string>> next) => next();
        private static Task<string> Behavior3(string req, CancellationToken ct, Func<Task<string>> next) => next();
    }

// ================================================================
// Fixtures — fake MessagePipe
// ================================================================

    internal sealed class FakeMessagePipe
    {
        public async ValueTask<string> SendAsync(string request, CancellationToken ct)
            => await Filter3(request, ct);

        private async ValueTask<string> Filter3(string request, CancellationToken ct)
            => await Filter2(request, ct);

        private async ValueTask<string> Filter2(string request, CancellationToken ct)
            => await Filter1(request, ct);

        private ValueTask<string> Filter1(string request, CancellationToken ct)
            => ValueTask.FromResult(request);
    }

// ================================================================
// Config
// ================================================================

    internal sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            AddJob(Job.Default
                .WithWarmupCount(5)
                .WithIterationCount(20));

            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}