using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Lite.Procedures.Benchmark.AspNet;

// Один процесс (InProcess), чтобы хост и бенчмарки работали в одной процессе — нет конфликта портов
var config = DefaultConfig.Instance
    .AddJob(Job.Default
        .WithToolchain(InProcessEmitToolchain.DontLogOutput)
        .WithIterationCount(12)
        .WithWarmupCount(4));

BenchmarkRunner.Run<AspNetPipelineBenchmarks>(config);
