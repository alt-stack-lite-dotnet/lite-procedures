using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using Lite.Procedures;
using Lite.Procedures.DependencyInjection;
using Lite.Procedures.Interception;
using Microsoft.Extensions.DependencyInjection;

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
            b.UseInterceptor<NoOpAspNetInterceptor>().AddProcedure<EchoProcedure>();
        });
        var app = builder.Build();
        app.MapGet("/echo", async (string? q, IAsyncProcedure<string, string> pipeline, CancellationToken ct) =>
        {
            try
            {
                var result = await pipeline.ExecuteAsync(q ?? "", ct);
                return Results.Ok(result);
            }
            catch
            {
                return Results.BadRequest();
            }
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
    public ValueTask<string> DirectPipelineInvoke()
        => _cachedPipeline.ExecuteAsync("hello", CancellationToken.None);

    [Benchmark]
    public async ValueTask<string> ResolveFromScope_AndInvoke()
    {
        await using var scope = _rootServices.CreateAsyncScope();
        var pipeline = scope.ServiceProvider.GetRequiredService<IAsyncProcedure<string, string>>();
        return await pipeline.ExecuteAsync("hello", CancellationToken.None);
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
    public ValueTask<string> ExecuteAsync(string arguments, CancellationToken cancellationToken)
        => ValueTask.FromResult(arguments);
}

internal sealed class NoOpAspNetInterceptor : AsyncInterceptor<string, string>
{
    public override ValueTask<string> InvokeAsync(
        string arguments,
        Func<string, CancellationToken, ValueTask<string>> next,
        CancellationToken cancellationToken)
        => next(arguments, cancellationToken);
}
