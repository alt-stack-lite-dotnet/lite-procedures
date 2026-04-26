using Lite.Procedures;
using Lite.Procedures.Interceptors;
using Lite.Procedures.Pipeline;

namespace Lite.Procedures.Test;

public sealed record EchoRequest(string Value);
public sealed record EchoResponse(string Value);

public class SyncPipelineTests
{
    [Fact]
    public void Invoke_NoInterceptors_CallsProcedure()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoProcedure(),
            Array.Empty<IProcedureInterceptor<EchoRequest, EchoResponse>>());

        var result = pipeline.Invoke(new EchoRequest("hello"));

        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public void Invoke_InterceptorsRunInOrder()
    {
        var log = new List<string>();
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoProcedure(),
            new IProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new TrackingInterceptor(log, "outer"),
                new TrackingInterceptor(log, "inner"),
            });

        pipeline.Invoke(new EchoRequest("x"));

        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, log);
    }

    [Fact]
    public void Invoke_ShortCircuit_DoesNotCallProcedure()
    {
        var procedureCalled = false;
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new TrackingProcedure(() => procedureCalled = true),
            new IProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new ShortCircuitInterceptor(new EchoResponse("short"))
            });

        var result = pipeline.Invoke(new EchoRequest("x"));

        Assert.False(procedureCalled);
        Assert.Equal("short", result.Value);
    }

    [Fact]
    public void Invoke_ProcedureThrows_PropagatesException()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingProcedure(),
            Array.Empty<IProcedureInterceptor<EchoRequest, EchoResponse>>());

        Assert.Throws<InvalidOperationException>(() => pipeline.Invoke(new EchoRequest("x")));
    }

    [Fact]
    public void Invoke_RecoveringInterceptor_CatchesProcedureException()
    {
        var pipeline = new ProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingProcedure(),
            new IProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new RecoveringInterceptor(new EchoResponse("recovered"))
            });

        var result = pipeline.Invoke(new EchoRequest("x"));

        Assert.Equal("recovered", result.Value);
    }

    private sealed class EchoProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        public EchoResponse Invoke(EchoRequest arguments) => new(arguments.Value);
    }

    private sealed class TrackingProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        private readonly Action _onCall;
        public TrackingProcedure(Action onCall) => _onCall = onCall;
        public EchoResponse Invoke(EchoRequest arguments) { _onCall(); return new EchoResponse(arguments.Value); }
    }

    private sealed class ThrowingProcedure : IProcedure<EchoRequest, EchoResponse>
    {
        public EchoResponse Invoke(EchoRequest arguments) => throw new InvalidOperationException("boom");
    }

    private sealed class TrackingInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;
        public TrackingInterceptor(List<string> log, string name) { _log = log; _name = name; }
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
        {
            _log.Add($"{_name}:before");
            var result = next(arguments);
            _log.Add($"{_name}:after");
            return result;
        }
    }

    private sealed class ShortCircuitInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _response;
        public ShortCircuitInterceptor(EchoResponse response) => _response = response;
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next) => _response;
    }

    private sealed class RecoveringInterceptor : IProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _fallback;
        public RecoveringInterceptor(EchoResponse fallback) => _fallback = fallback;
        public EchoResponse Invoke(EchoRequest arguments, Func<EchoRequest, EchoResponse> next)
        {
            try { return next(arguments); }
            catch { return _fallback; }
        }
    }
}

public class AsyncPipelineTests
{
    [Fact]
    public async Task InvokeAsync_NoInterceptors_CallsProcedure()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoAsyncProcedure(),
            Array.Empty<IAsyncProcedureInterceptor<EchoRequest, EchoResponse>>());

        var result = await pipeline.InvokeAsync(new EchoRequest("hello"), CancellationToken.None);

        Assert.Equal("hello", result.Value);
    }

    [Fact]
    public async Task InvokeAsync_InterceptorsRunInOrder()
    {
        var log = new List<string>();
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new EchoAsyncProcedure(),
            new IAsyncProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new TrackingAsyncInterceptor(log, "outer"),
                new TrackingAsyncInterceptor(log, "inner"),
            });

        await pipeline.InvokeAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.Equal(new[] { "outer:before", "inner:before", "inner:after", "outer:after" }, log);
    }

    [Fact]
    public async Task InvokeAsync_ShortCircuit_DoesNotCallProcedure()
    {
        var procedureCalled = false;
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new TrackingAsyncProcedure(() => procedureCalled = true),
            new IAsyncProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new ShortCircuitAsyncInterceptor(new EchoResponse("short"))
            });

        var result = await pipeline.InvokeAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.False(procedureCalled);
        Assert.Equal("short", result.Value);
    }

    [Fact]
    public async Task InvokeAsync_ProcedureThrows_PropagatesException()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingAsyncProcedure(),
            Array.Empty<IAsyncProcedureInterceptor<EchoRequest, EchoResponse>>());

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await pipeline.InvokeAsync(new EchoRequest("x"), CancellationToken.None));
    }

    [Fact]
    public async Task InvokeAsync_RecoveringInterceptor_CatchesProcedureException()
    {
        var pipeline = new AsyncProcedurePipeline<EchoRequest, EchoResponse>(
            new ThrowingAsyncProcedure(),
            new IAsyncProcedureInterceptor<EchoRequest, EchoResponse>[]
            {
                new RecoveringAsyncInterceptor(new EchoResponse("recovered"))
            });

        var result = await pipeline.InvokeAsync(new EchoRequest("x"), CancellationToken.None);

        Assert.Equal("recovered", result.Value);
    }

    private sealed class EchoAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> InvokeAsync(EchoRequest arguments, CancellationToken ct)
            => ValueTask.FromResult(new EchoResponse(arguments.Value));
    }

    private sealed class TrackingAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        private readonly Action _onCall;
        public TrackingAsyncProcedure(Action onCall) => _onCall = onCall;
        public ValueTask<EchoResponse> InvokeAsync(EchoRequest arguments, CancellationToken ct)
        {
            _onCall();
            return ValueTask.FromResult(new EchoResponse(arguments.Value));
        }
    }

    private sealed class ThrowingAsyncProcedure : IAsyncProcedure<EchoRequest, EchoResponse>
    {
        public ValueTask<EchoResponse> InvokeAsync(EchoRequest arguments, CancellationToken ct)
            => throw new InvalidOperationException("boom");
    }

    private sealed class TrackingAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly List<string> _log;
        private readonly string _name;
        public TrackingAsyncInterceptor(List<string> log, string name) { _log = log; _name = name; }
        public async ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken ct)
        {
            _log.Add($"{_name}:before");
            var result = await next(arguments, ct);
            _log.Add($"{_name}:after");
            return result;
        }
    }

    private sealed class ShortCircuitAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _response;
        public ShortCircuitAsyncInterceptor(EchoResponse response) => _response = response;
        public ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken ct)
            => ValueTask.FromResult(_response);
    }

    private sealed class RecoveringAsyncInterceptor : IAsyncProcedureInterceptor<EchoRequest, EchoResponse>
    {
        private readonly EchoResponse _fallback;
        public RecoveringAsyncInterceptor(EchoResponse fallback) => _fallback = fallback;
        public async ValueTask<EchoResponse> InvokeAsync(
            EchoRequest arguments,
            Func<EchoRequest, CancellationToken, ValueTask<EchoResponse>> next,
            CancellationToken ct)
        {
            try { return await next(arguments, ct); }
            catch { return _fallback; }
        }
    }
}
