using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Lite.Procedures.Benchmark;
using Lite.Procedures.Builders;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;
using Microsoft.Extensions.DependencyInjection;

// ================================================================
// Entry point
// ================================================================

BenchmarkRunner.Run<AsyncProcedurePipelineBenchmarks>();
BenchmarkRunner.Run<SyncProcedurePipelineBenchmarks>();
BenchmarkRunner.Run<DiResolveBenchmarks>();
BenchmarkRunner.Run<RealWorldInterceptorsBenchmarks>();
BenchmarkRunner.Run<ConcurrentBenchmarks>();
BenchmarkRunner.Run<MediatRComparisonBenchmarks>();
BenchmarkRunner.Run<Per10KRequestsBenchmarks>();
BenchmarkRunner.Run<StructVsClassBenchmarks>();
BenchmarkRunner.Run<MessagePipeComparisonBenchmarks>();

namespace Lite.Procedures.Benchmark
{
    // ================================================================
    // Domain model
    // ================================================================

    public readonly record struct EchoRequest(string Value);
    public readonly record struct EchoResponse(string Value);

    // ================================================================
    // 1. Async pipeline
    // ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class AsyncProcedurePipelineBenchmarks
    {
        private static readonly EchoRequest Input = new("input");

        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _noInterceptors = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _oneInterceptor = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _threeInterceptors = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _fiveInterceptors = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _tenInterceptors = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _shortCircuitFirst = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _shortCircuitMiddle = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _shortCircuitLast = null!;
        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _recoveryAfterThrow = null!;

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new EchoAsyncProcedure();
            var throwingProcedure = new ThrowingAsyncProcedure();

            _noInterceptors = Build(procedure);
            _oneInterceptor = Build(procedure, new NoOpAsyncInterceptor());
            _threeInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _fiveInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _tenInterceptors = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitFirst = Build(procedure, new ShortCircuitAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitMiddle = Build(procedure, new NoOpAsyncInterceptor(), new ShortCircuitAsyncInterceptor(), new NoOpAsyncInterceptor());
            _shortCircuitLast = Build(procedure, new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new ShortCircuitAsyncInterceptor());
            _recoveryAfterThrow = Build(throwingProcedure, new NoOpAsyncInterceptor(), new RecoveringAsyncInterceptor(), new NoOpAsyncInterceptor());
        }

        private static AsyncProcedurePipeline<EchoRequest, EchoResponse> Build(
            IAsyncProcedure<EchoRequest, EchoResponse> procedure,
            params IAsyncProcedureInterceptor<EchoRequest, EchoResponse>[] interceptors) =>
            new(procedure, interceptors);

        [Benchmark(Baseline = true)]
        public ValueTask<EchoResponse> NoInterceptors() => _noInterceptors.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> OneInterceptor() => _oneInterceptor.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> ThreeInterceptors() => _threeInterceptors.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> FiveInterceptors() => _fiveInterceptors.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> TenInterceptors() => _tenInterceptors.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> ShortCircuit_First() => _shortCircuitFirst.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> ShortCircuit_Middle() => _shortCircuitMiddle.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> ShortCircuit_Last() => _shortCircuitLast.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> RecoveryAfterThrow() => _recoveryAfterThrow.InvokeAsync(Input, CancellationToken.None);
    }

    // ================================================================
    // 2. Sync pipeline
    // ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class SyncProcedurePipelineBenchmarks
    {
        private static readonly EchoRequest Input = new("input");

        private ProcedurePipeline<EchoRequest, EchoResponse> _noInterceptors = null!;
        private ProcedurePipeline<EchoRequest, EchoResponse> _threeInterceptors = null!;
        private ProcedurePipeline<EchoRequest, EchoResponse> _fiveInterceptors = null!;
        private ProcedurePipeline<EchoRequest, EchoResponse> _shortCircuit = null!;
        private ProcedurePipeline<EchoRequest, EchoResponse> _recovery = null!;

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new EchoSyncProcedure();
            var throwingProcedure = new ThrowingSyncProcedure();

            _noInterceptors = new ProcedurePipeline<EchoRequest, EchoResponse>(procedure, []);
            _threeInterceptors = new ProcedurePipeline<EchoRequest, EchoResponse>(procedure, [new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor()
            ]);
            _fiveInterceptors = new ProcedurePipeline<EchoRequest, EchoResponse>(procedure, [new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor(), new NoOpSyncInterceptor()
            ]);
            _shortCircuit = new ProcedurePipeline<EchoRequest, EchoResponse>(procedure, [new NoOpSyncInterceptor(), new ShortCircuitSyncInterceptor(), new NoOpSyncInterceptor()
            ]);
            _recovery = new ProcedurePipeline<EchoRequest, EchoResponse>(throwingProcedure, [new NoOpSyncInterceptor(), new RecoveringSyncInterceptor(), new NoOpSyncInterceptor()
            ]);
        }

        [Benchmark(Baseline = true)]
        public EchoResponse NoInterceptors() => _noInterceptors.Invoke(Input);

        [Benchmark]
        public EchoResponse ThreeInterceptors() => _threeInterceptors.Invoke(Input);

        [Benchmark]
        public EchoResponse FiveInterceptors() => _fiveInterceptors.Invoke(Input);

        [Benchmark]
        public EchoResponse ShortCircuit() => _shortCircuit.Invoke(Input);

        [Benchmark]
        public EchoResponse RecoveryAfterThrow() => _recovery.Invoke(Input);
    }

    // ================================================================
    // 3. DI resolve hot path
    // ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class DiResolveBenchmarks
    {
        private static readonly EchoRequest Input = new("input");

        private IServiceProvider _sp = null!;
        private IAsyncProcedurePipeline<EchoRequest, EchoResponse> _directRef = null!;

        [GlobalSetup]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLiteProcedures(b =>
                b.AddDefaultInterceptor<NoOpAsyncInterceptor>().AddProcedure<EchoAsyncProcedure>());

            _sp = services.BuildServiceProvider();
            _directRef = _sp.GetRequiredService<IAsyncProcedurePipeline<EchoRequest, EchoResponse>>();
        }

        [Benchmark(Baseline = true)]
        public ValueTask<EchoResponse> DirectReference() => _directRef.InvokeAsync(Input, CancellationToken.None);

        [Benchmark]
        public ValueTask<EchoResponse> ResolveAndInvoke()
        {
            var pipeline = _sp.GetRequiredService<IAsyncProcedurePipeline<EchoRequest, EchoResponse>>();
            return pipeline.InvokeAsync(Input, CancellationToken.None);
        }

        [Benchmark]
        public async ValueTask<EchoResponse> ResolveFromScope_AndInvoke()
        {
            await using var scope = _sp.CreateAsyncScope();
            var pipeline = scope.ServiceProvider.GetRequiredService<IAsyncProcedurePipeline<EchoRequest, EchoResponse>>();
            return await pipeline.InvokeAsync(Input, CancellationToken.None);
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

        [GlobalSetup]
        public void Setup()
        {
            var procedure = new CreatePlayerProcedure();

            _noInterceptors = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, []);
            _withLogging = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, [new LoggingInterceptor()
            ]);
            _withValidation = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, [new ValidationInterceptor()
            ]);
            _withLoggingAndValidation = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, [new LoggingInterceptor(), new ValidationInterceptor()
            ]);
            _fullStack = new AsyncProcedurePipeline<CreatePlayerArgs, CreatePlayerResult>(procedure, [new LoggingInterceptor(), new ValidationInterceptor(), new MetricsInterceptor(), new ErrorHandlingInterceptor()
            ]);
        }

        [Benchmark(Baseline = true)]
        public ValueTask<CreatePlayerResult> NoInterceptors_ValidArgs()
            => _noInterceptors.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<CreatePlayerResult> WithLogging_ValidArgs()
            => _withLogging.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<CreatePlayerResult> WithValidation_ValidArgs()
            => _withValidation.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<CreatePlayerResult> WithLoggingAndValidation_ValidArgs()
            => _withLoggingAndValidation.InvokeAsync(_validArgs, CancellationToken.None);

        [Benchmark]
        public ValueTask<CreatePlayerResult> FullStack_ValidArgs()
            => _fullStack.InvokeAsync(_validArgs, CancellationToken.None);
    }

    // ================================================================
    // 5. Concurrent
    // ================================================================

    [Config(typeof(BenchmarkConfig))]
    public class ConcurrentBenchmarks
    {
        private static readonly EchoRequest Input = new("input");

        private AsyncProcedurePipeline<EchoRequest, EchoResponse> _pipeline = null!;

        [Params(1, 4, 8, 16)]
        public int ThreadCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
                new EchoAsyncProcedure(),
                [new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor(), new NoOpAsyncInterceptor()]);
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
                        await _pipeline.InvokeAsync(Input, CancellationToken.None);
                });
            }
            await Task.WhenAll(tasks);
        }
    }

    // ================================================================
    // Fixtures — procedures
    // ================================================================

    internal sealed class EchoAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> InvokeAsync(EchoRequest arguments, CancellationToken ct)
            => ValueTask.FromResult(new EchoResponse(arguments.Value));
    }

    internal sealed class EchoSyncProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        public EchoResponse Invoke(EchoRequest arguments) => new(arguments.Value);
    }

    internal sealed class ThrowingAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        private static readonly Exception _ex = new InvalidOperationException("procedure error");
        public ValueTask<EchoResponse> InvokeAsync(EchoRequest arguments, CancellationToken ct) => throw _ex;
    }

    internal sealed class ThrowingSyncProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        private static readonly Exception _ex = new InvalidOperationException("procedure error");
        public EchoResponse Invoke(EchoRequest arguments) => throw _ex;
    }

    // ================================================================
    // Fixtures — interceptors
    // ================================================================

    internal sealed class NoOpAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
            => next(arguments, cancellationToken);
    }

    internal sealed class NoOpSyncInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
            => next(arguments);
    }

    internal sealed class ShortCircuitAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private static readonly EchoResponse _short = new("short_circuit");
        public ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(_short);
    }

    internal sealed class ShortCircuitSyncInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private static readonly EchoResponse _short = new("short_circuit");
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next) => _short;
    }

    internal sealed class RecoveringAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private static readonly EchoResponse _recovered = new("recovered");
        public async ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
        {
            try { return await next(arguments, cancellationToken).ConfigureAwait(false); }
            catch { return _recovered; }
        }
    }

    internal sealed class RecoveringSyncInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private static readonly EchoResponse _recovered = new("recovered");
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
        {
            try { return next(arguments); }
            catch { return _recovered; }
        }
    }

    // ================================================================
    // Fixtures — real-world domain
    // ================================================================

    public sealed record CreatePlayerArgs(string Name, int Age);

    public sealed record CreatePlayerResult(Guid Id, string Name);

    internal sealed class CreatePlayerProcedure : IAsyncProcedure<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<CreatePlayerResult> InvokeAsync(CreatePlayerArgs arguments, CancellationToken ct)
            => ValueTask.FromResult(new CreatePlayerResult(Guid.NewGuid(), arguments.Name));
    }

    internal sealed class LoggingInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        private int _callCount;

        public async ValueTask<CreatePlayerResult> InvokeAsync(
            CreatePlayerArgs arguments,
            Func<CreatePlayerArgs, CancellationToken, ValueTask<CreatePlayerResult>> next,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            var result = await next(arguments, cancellationToken).ConfigureAwait(false);
            Interlocked.Increment(ref _callCount);
            return result;
        }
    }

    internal sealed class ValidationInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<CreatePlayerResult> InvokeAsync(
            CreatePlayerArgs arguments,
            Func<CreatePlayerArgs, CancellationToken, ValueTask<CreatePlayerResult>> next,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(arguments.Name))
                throw new ArgumentException("Name is required");

            if (arguments.Age < 0)
                throw new ArgumentException("Age must be non-negative");

            return next(arguments, cancellationToken);
        }
    }

    internal sealed class MetricsInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        private long _successCount;
        private long _errorCount;

        public async ValueTask<CreatePlayerResult> InvokeAsync(
            CreatePlayerArgs arguments,
            Func<CreatePlayerArgs, CancellationToken, ValueTask<CreatePlayerResult>> next,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await next(arguments, cancellationToken).ConfigureAwait(false);
                Interlocked.Increment(ref _successCount);
                return result;
            }
            catch
            {
                Interlocked.Increment(ref _errorCount);
                throw;
            }
        }
    }

    internal sealed class ErrorHandlingInterceptor : IAsyncProcedureInterceptor<CreatePlayerArgs, CreatePlayerResult>
    {
        public ValueTask<CreatePlayerResult> InvokeAsync(
            CreatePlayerArgs arguments,
            Func<CreatePlayerArgs, CancellationToken, ValueTask<CreatePlayerResult>> next,
            CancellationToken cancellationToken)
            => next(arguments, cancellationToken);
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
