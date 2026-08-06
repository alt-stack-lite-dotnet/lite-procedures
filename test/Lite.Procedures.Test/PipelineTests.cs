using Lite.Procedures;
using Lite.Procedures;
using Lite.Procedures.Interception;

namespace Lite.Procedures.Test;

public sealed record EchoRequest(string Value);
public sealed record EchoResponse(string Value);

public class SyncPipelineTests
{
    [Fact]
    public void Execute_NoInterceptors_CallsProcedure()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoProcedure(), []);

        var result = pipeline.Execute(new EchoRequest("hello"));

        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Execute_InterceptorsRunInOrder()
    {
        var log = new List<string>();
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoProcedure(),
            new Interceptor<EchoRequest, EchoResponse>[]
            {
                new TrackingInterceptor(log, "outer"),
                new TrackingInterceptor(log, "inner"),
            });

        pipeline.Execute(new EchoRequest("x"));

        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, log);
    }

    [Fact]
    public void Execute_ShortCircuit_DoesNotCallProcedure()
    {
        var procedureCalled = false;
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new TrackingProcedure(() => procedureCalled = true),
            new Interceptor<EchoRequest, EchoResponse>[]
            {
                new ShortCircuitInterceptor(new EchoResponse("short"))
            });

        var result = pipeline.Execute(new EchoRequest("x"));

        Assert.False(procedureCalled);
        Assert.Equal("short", result.Value);
    }

    [Fact]
    public void Execute_ProcedureThrows_PropagatesException()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingProcedure(),
            Array.Empty<Interceptor<EchoRequest, EchoResponse>>());

        Assert.Throws<InvalidOperationException>(() => pipeline.Execute(new EchoRequest("x")));
    }

    [Fact]
    public void Execute_RecoveringInterceptor_CatchesProcedureException()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingProcedure(),
            new Interceptor<EchoRequest, EchoResponse>[]
            {
                new RecoveringInterceptor(new EchoResponse("recovered"))
            });

        var result = pipeline.Execute(new EchoRequest("x"));

        Assert.Equal("recovered", result.Value);
    }

    private sealed class EchoProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        public EchoResponse Execute(EchoRequest arguments) => new(arguments.Value);
    }

    private sealed class TrackingProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        private readonly Action _onCall;
        public TrackingProcedure(Action onCall) => _onCall = onCall;
        public EchoResponse Execute(EchoRequest arguments) { _onCall(); return new EchoResponse(arguments.Value); }
    }

    private sealed class ThrowingProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        public EchoResponse Execute(EchoRequest arguments) => throw new InvalidOperationException("boom");
    }

    private sealed class TrackingInterceptor : Interceptor<EchoRequest, EchoResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;
        public TrackingInterceptor(List<string> log, string name) { _log = log; _name = name; }
        public override EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
        {
            _log.Add($"{_name}:before");
            var result = next(arguments);
            _log.Add($"{_name}:after");
            return result;
        }
    }

    private sealed class ShortCircuitInterceptor : Interceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _response;
        public ShortCircuitInterceptor(EchoResponse response) => _response = response;
        public override EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next) => _response;
    }

    private sealed class RecoveringInterceptor : Interceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _fallback;
        public RecoveringInterceptor(EchoResponse fallback) => _fallback = fallback;
        public override EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
        {
            try { return next(arguments); }
            catch { return _fallback; }
        }
    }
}

public class AsyncPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_NoInterceptors_CallsProcedure()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoAsyncProcedure(),
            Array.Empty<AsyncInterceptor<EchoRequest, EchoResponse>>());

        var result = await pipeline.ExecuteAsync(new EchoRequest("hello"), CancellationToken.None);

        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_InterceptorsRunInOrder()
    {
        var log = new List<string>();
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoAsyncProcedure(),
            new AsyncInterceptor<EchoRequest, EchoResponse>[]
            {
                new TrackingAsyncInterceptor(log, "outer"),
                new TrackingAsyncInterceptor(log, "inner"),
            });

        await pipeline.ExecuteAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, log);
    }

    [Fact]
    public async Task ExecuteAsync_ShortCircuit_DoesNotCallProcedure()
    {
        var procedureCalled = false;
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new TrackingAsyncProcedure(() => procedureCalled = true),
            new AsyncInterceptor<EchoRequest, EchoResponse>[]
            {
                new ShortCircuitAsyncInterceptor(new EchoResponse("short"))
            });

        var result = await pipeline.ExecuteAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.False(procedureCalled);
        Assert.Equal("short", result.Value);
    }

    [Fact]
    public async Task ExecuteAsync_ProcedureThrows_PropagatesException()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingAsyncProcedure(),
            Array.Empty<AsyncInterceptor<EchoRequest, EchoResponse>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pipeline.ExecuteAsync(new EchoRequest("x"), CancellationToken.None));
    }

    [Fact]
    public async Task ExecuteAsync_RecoveringInterceptor_CatchesProcedureException()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingAsyncProcedure(),
            new AsyncInterceptor<EchoRequest, EchoResponse>[]
            {
                new RecoveringAsyncInterceptor(new EchoResponse("recovered"))
            });

        var result = await pipeline.ExecuteAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.Equal("recovered", result.Value);
    }

    private sealed class EchoAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> ExecuteAsync(EchoRequest arguments, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new EchoResponse(arguments.Value));
    }

    private sealed class TrackingAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        private readonly Action _onCall;
        public TrackingAsyncProcedure(Action onCall) => _onCall = onCall;
        public ValueTask<EchoResponse> ExecuteAsync(EchoRequest arguments, CancellationToken cancellationToken = default)
        {
            _onCall();
            return ValueTask.FromResult(new EchoResponse(arguments.Value));
        }
    }

    private sealed class ThrowingAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> ExecuteAsync(EchoRequest arguments, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }

    private sealed class TrackingAsyncInterceptor : AsyncInterceptor<EchoRequest, EchoResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;
        public TrackingAsyncInterceptor(List<string> log, string name) { _log = log; _name = name; }
        public override async ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
        {
            _log.Add($"{_name}:before");
            var result = await next(arguments, cancellationToken);
            _log.Add($"{_name}:after");
            return result;
        }
    }

    private sealed class ShortCircuitAsyncInterceptor : AsyncInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _response;
        public ShortCircuitAsyncInterceptor(EchoResponse response) => _response = response;
        public override ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
            => ValueTask.FromResult(_response);
    }

    private sealed class RecoveringAsyncInterceptor : AsyncInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _fallback;
        public RecoveringAsyncInterceptor(EchoResponse fallback) => _fallback = fallback;
        public override async ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken cancellationToken)
        {
            try { return await next(arguments, cancellationToken); }
            catch { return _fallback; }
        }
    }
}
