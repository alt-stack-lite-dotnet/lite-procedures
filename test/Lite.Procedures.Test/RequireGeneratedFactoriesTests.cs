using Lite.Procedures;
using Lite.Procedures.Configuration;
using Lite.Procedures.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace Lite.Procedures.Test;

public class RequireGeneratedFactoriesTests
{
    // Generic procedures are never covered by the source generator (see LITE001 in
    // ProcedureFactoryGeneratorTests) — a reliable, deterministic way to exercise the
    // "no generated factory" path without relying on accessibility tricks.
    public sealed record Req<T>(T Value);

    public sealed class GenericProc<T> : IAsyncProcedure<Req<T>, T>
    {
        public ValueTask<T> ExecuteAsync(Req<T> arguments, CancellationToken cancellationToken = default)
            => new(arguments.Value);
    }

    [Fact]
    public async Task WithoutRequireGeneratedFactories_FallsBackToReflectionBridge_AndStillWorks()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<GenericProc<int>>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IAsyncProcedure<Req<int>, int>>();

        var result = await procedure.ExecuteAsync(new Req<int>(42), CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public void WithRequireGeneratedFactories_ThrowsInsteadOfSilentFallback()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddLiteProcedures(b => b
                .RequireGeneratedFactories()
                .AddProcedure<GenericProc<int>>()));

        Assert.Contains("GenericProc", ex.Message);
        Assert.Contains("RequireGeneratedFactories", ex.Message);
    }

    // Sync mirror: the reflection bridge used to hardcode IAsyncProcedure<,> and throw for any
    // sync procedure landing in the fallback path. Same "generic -> no generated factory" trick
    // to force the fallback deterministically.
    public sealed class GenericSyncProc<T> : IProcedure<Req<T>, T>
    {
        public T Execute(Req<T> arguments) => arguments.Value;
    }

    [Fact]
    public void Sync_WithoutRequireGeneratedFactories_FallsBackToReflectionBridge_AndStillWorks()
    {
        var services = new ServiceCollection();
        services.AddLiteProcedures(b => b.AddProcedure<GenericSyncProc<int>>());

        using var sp = services.BuildServiceProvider();
        var procedure = sp.GetRequiredService<IProcedure<Req<int>, int>>();

        var result = procedure.Execute(new Req<int>(42));

        Assert.Equal(42, result);
    }
}
