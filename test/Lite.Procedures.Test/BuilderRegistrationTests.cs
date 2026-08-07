using Lite.Procedures;
using Lite.Procedures.Configuration;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interception;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Test;

public class BuilderRegistrationTests
{
    [Fact]
    public void AddProcedure_ResolvesAsAsyncProcedureInterface()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetService<IAsyncProcedure<TestRequest, TestResponse>>();

        Assert.NotNull(procedure);
    }

    [Fact]
    public async Task AddProcedure_NoInterceptors_Invokes()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();

        var result = await procedure.ExecuteAsync(new TestRequest("hi"), CancellationToken.None);

        Assert.Equal("hi", result.Value);
    }

    [Fact]
    public async Task UseInterceptor_Shared_Applies()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b
            .UseInterceptor<TrackingA>()
            .AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();

        await procedure.ExecuteAsync(new TestRequest("x"), CancellationToken.None);

        Assert.Equal(new[] { "A:before", "A:after" }, probe.Log);
    }

    [Fact]
    public async Task AddProcedure_SharedPlusPerProcedure_MergeAndOrderByPriority()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b
            .UseInterceptor<TrackingB>(priority: 10)
            .AddProcedure<EchoProcedure>(p => p.UseInterceptor<TrackingA>(priority: 0)));

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();

        await procedure.ExecuteAsync(new TestRequest("x"), CancellationToken.None);

        // Lower priority is outer: A(0) wraps B(10).
        Assert.Equal(new[] { "A:before", "B:before", "B:after", "A:after" }, probe.Log);
    }

    [Fact]
    public async Task ApplyConfiguration_EfStyle_Applies()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b.ApplyConfiguration(new EchoConfiguration()));

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();

        await procedure.ExecuteAsync(new TestRequest("x"), CancellationToken.None);

        Assert.Equal(new[] { "A:before", "A:after" }, probe.Log);
    }

    [Fact]
    public void AddProcedure_SameInterceptorType_SharedPlusAttribute_NoInstance_Throws()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddLiteProcedures(b => b
                .UseInterceptor<TrackingA>()
                .AddProcedure<EchoProcedure>(p => p.UseInterceptor<TrackingA>())));

        Assert.Contains("TrackingA", ex.Message);
        Assert.Contains("EchoProcedure", ex.Message);
    }

    [Fact]
    public void AddProcedure_SameInterceptorType_DistinctInstances_DoesNotThrow()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);

        // Two distinct explicit instances of the same type is plausibly intentional -> allowed.
        services.AddLiteProcedures(b => b.AddProcedure<EchoProcedure>(p =>
        {
            p.UseInterceptor(new TrackingA(probe), priority: 0);
            p.UseInterceptor(new TrackingA(probe), priority: 1);
        }));
    }

    [Fact]
    public async Task AddProcedure_CalledTwiceInSameScope_MergesInterceptors_DoesNotDropEither()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b =>
        {
            b.AddProcedure<EchoProcedure>(p => p.UseInterceptor<TrackingA>());
            // Same procedure, second call in the same scope — must merge with the first, not replace
            // or silently drop it.
            b.AddProcedure<EchoProcedure>(p => p.UseInterceptor<TrackingB>());
        });

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();

        await procedure.ExecuteAsync(new TestRequest("x"), CancellationToken.None);

        // Equal (default) priority -> stable order: first call's interceptor stays outermost.
        Assert.Equal(new[] { "A:before", "B:before", "B:after", "A:after" }, probe.Log);
    }

    [Fact]
    public void AddProcedure_NonGeneric_TypeMustImplementProcedureCore()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddLiteProcedures(b => b.AddProcedure(typeof(string))));
    }

    [Fact]
    public async Task MultipleAddLiteProcedures_SharedInterceptorScopedToItsCall()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);

        // Scope 1: A applies to Echo.
        services.AddLiteProcedures(b => b.UseInterceptor<TrackingA>().AddProcedure<EchoProcedure>());
        // Scope 2: B applies to Other — NOT A.
        services.AddLiteProcedures(b => b.UseInterceptor<TrackingB>().AddProcedure<OtherProcedure>());

        using var sp = services.BuildServiceProvider();
        var other = sp.GetRequiredService<IAsyncProcedure<TestRequest, TestResponse>>();
        // Other resolves to the last registration for the interface (OtherProcedure scope).
        await other.ExecuteAsync(new TestRequest("x"), CancellationToken.None);

        Assert.Equal(new[] { "B:before", "B:after" }, probe.Log);
    }

    // --- Fixtures ---

    public sealed record TestRequest(string Value);
    public sealed record TestResponse(string Value);

    public sealed class EchoProcedure : IAsyncProcedure<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> ExecuteAsync(TestRequest arguments, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new TestResponse(arguments.Value));
    }

    public sealed class OtherProcedure : IAsyncProcedure<TestRequest, TestResponse>
    {
        public ValueTask<TestResponse> ExecuteAsync(TestRequest arguments, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(new TestResponse(arguments.Value));
    }

    public sealed class EchoConfiguration : IProcedureConfiguration<EchoProcedure>
    {
        public void Configure(IProcedureBuilder builder) => builder.UseInterceptor<TrackingA>();
    }

    public sealed class InterceptorOrderProbe
    {
        public List<string> Log { get; } = new();
    }

    public sealed class TrackingA : AsyncInterceptor<TestRequest, TestResponse>
    {
        private readonly InterceptorOrderProbe _probe;
        public TrackingA(InterceptorOrderProbe probe) => _probe = probe;
        public override async ValueTask<TestResponse> InvokeAsync(
            TestRequest arguments,
            Func<TestRequest, CancellationToken, ValueTask<TestResponse>> next,
            CancellationToken cancellationToken)
        {
            _probe.Log.Add("A:before");
            var r = await next(arguments, cancellationToken);
            _probe.Log.Add("A:after");
            return r;
        }
    }

    public sealed class TrackingB : AsyncInterceptor<TestRequest, TestResponse>
    {
        private readonly InterceptorOrderProbe _probe;
        public TrackingB(InterceptorOrderProbe probe) => _probe = probe;
        public override async ValueTask<TestResponse> InvokeAsync(
            TestRequest arguments,
            Func<TestRequest, CancellationToken, ValueTask<TestResponse>> next,
            CancellationToken cancellationToken)
        {
            _probe.Log.Add("B:before");
            var r = await next(arguments, cancellationToken);
            _probe.Log.Add("B:after");
            return r;
        }
    }
}

// Sync mirror of BuilderRegistrationTests — same DI/builder scenarios, IProcedure<,> instead of
// IAsyncProcedure<,>. Sync procedures were never exercised through AddLiteProcedures before.
public class SyncBuilderRegistrationTests
{
    [Fact]
    public void AddProcedure_ResolvesAsProcedureInterface()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetService<IProcedure<TestRequest, TestResponse>>();

        Assert.NotNull(procedure);
    }

    [Fact]
    public void AddProcedure_NoInterceptors_Invokes()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IProcedure<TestRequest, TestResponse>>();

        var result = procedure.Execute(new TestRequest("hi"));

        Assert.Equal("hi", result.Value);
    }

    [Fact]
    public void UseInterceptor_Shared_Applies()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b
            .UseInterceptor<TrackingA>()
            .AddProcedure<EchoProcedure>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IProcedure<TestRequest, TestResponse>>();

        procedure.Execute(new TestRequest("x"));

        Assert.Equal(new[] { "A:before", "A:after" }, probe.Log);
    }

    [Fact]
    public void AddProcedure_SharedPlusPerProcedure_MergeAndOrderByPriority()
    {
        var probe = new InterceptorOrderProbe();
        var services = new ServiceCollection();
        services.AddSingleton(probe);
        services.AddLiteProcedures(b => b
            .UseInterceptor<TrackingB>(priority: 10)
            .AddProcedure<EchoProcedure>(p => p.UseInterceptor<TrackingA>(priority: 0)));

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IProcedure<TestRequest, TestResponse>>();

        procedure.Execute(new TestRequest("x"));

        // Lower priority is outer: A(0) wraps B(10).
        Assert.Equal(new[] { "A:before", "B:before", "B:after", "A:after" }, probe.Log);
    }

    // --- Fixtures ---

    public sealed record TestRequest(string Value);
    public sealed record TestResponse(string Value);

    public sealed class EchoProcedure : IProcedure<TestRequest, TestResponse>
    {
        public TestResponse Execute(TestRequest arguments) => new(arguments.Value);
    }

    public sealed class InterceptorOrderProbe
    {
        public List<string> Log { get; } = new();
    }

    public sealed class TrackingA : Interceptor<TestRequest, TestResponse>
    {
        private readonly InterceptorOrderProbe _probe;
        public TrackingA(InterceptorOrderProbe probe) => _probe = probe;
        public override TestResponse Invoke(TestRequest arguments, Func<TestRequest, TestResponse> next)
        {
            _probe.Log.Add("A:before");
            var r = next(arguments);
            _probe.Log.Add("A:after");
            return r;
        }
    }

    public sealed class TrackingB : Interceptor<TestRequest, TestResponse>
    {
        private readonly InterceptorOrderProbe _probe;
        public TrackingB(InterceptorOrderProbe probe) => _probe = probe;
        public override TestResponse Invoke(TestRequest arguments, Func<TestRequest, TestResponse> next)
        {
            _probe.Log.Add("B:before");
            var r = next(arguments);
            _probe.Log.Add("B:after");
            return r;
        }
    }
}
