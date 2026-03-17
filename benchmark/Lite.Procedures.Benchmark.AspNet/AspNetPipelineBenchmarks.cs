using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Lite.Procedures;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interceptors;
using Microsoft.Extensions.DependencyInjection;
using OneOf;
using OneOf.Types;

namespace Lite.Procedures.Benchmark.AspNet;

[Config(typeof(Config))]
[MemoryDiagnoser]
public class AspNetPipelineBenchmarks
{
    private IServiceProvider _rootServices = null!;
    private IAsyncProcedure<string, string> _cachedPipeline = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddLiteProcedures(b =>
        {
            b.AddGlobalInterceptor<NoOpAspNetInterceptor>().AddProcedure<EchoProcedure>();
            b.Build();
        });
        var app = builder.Build();
        app.MapGet("/echo", async (string? q, IAsyncProcedure<string, string> pipeline, CancellationToken ct) =>
        {
            OneOf<string, Exception> result = await pipeline.InvokeAsync(q ?? "", ct);
            return result.IsT0 ? Results.Ok(result.AsT0) : Results.BadRequest();
        });
        const string url = "http://127.0.0.1:56789";
        app.Urls.Add(url);
        app.Start();

        _rootServices = app.Services;
        _cachedPipeline = _rootServices.GetRequiredService<IAsyncProcedure<string, string>>();
        _httpClient = new HttpClient { BaseAddress = new Uri(url) };
        _app = app;
    }

    private HttpClient _httpClient = null!;
    private WebApplication _app = null!;

    [GlobalCleanup]
    public void Cleanup() => _app?.StopAsync().GetAwaiter().GetResult();

    [Benchmark(Baseline = true)]
    public async ValueTask<OneOf<string, Exception>> DirectPipelineInvoke()
    {
        return await _cachedPipeline.InvokeAsync("hello", CancellationToken.None);
    }

    [Benchmark]
    public async ValueTask<OneOf<string, Exception>> ResolveFromScope_AndInvoke()
    {
        await using var scope = _rootServices.CreateAsyncScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IAsyncProcedure<string, string>>();
        return await pipeline.InvokeAsync("hello", CancellationToken.None);
    }

    [Benchmark]
    public async Task<int> HttpGet_MinimalApi()
    {
        var r = await _httpClient.GetAsync("/echo?q=hello", CancellationToken.None);
        return (int)r.StatusCode;
    }

    private class Config : ManualConfig
    {
        public Config()
        {
            AddJob(Job.Default.WithWarmupCount(4).WithIterationCount(12));
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }
}

internal sealed class EchoProcedure : IAsyncProcedure<string, string>
{
    public ValueTask<string> InvokeAsync(string arguments, CancellationToken cancellationToken)
        => ValueTask.FromResult(arguments);
}

internal sealed class NoOpAspNetInterceptor : IAsyncProcedureInterceptor<string, string>
{
    public ValueTask<OneOf<Success, string, Exception>> InvokeBeforeExecutionAsync(string arguments, CancellationToken cancellationToken)
        => ValueTask.FromResult<OneOf<Success, string, Exception>>(new Success());

    public ValueTask<OneOf<string, Exception>> InvokeAfterExecutionAsync(string arguments, OneOf<string, Exception> resultOrError, CancellationToken cancellationToken)
        => ValueTask.FromResult(resultOrError);
}
