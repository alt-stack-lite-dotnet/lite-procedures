using BenchmarkDotNet.Running;
using Lite.Procedures.Benchmark;

// Lite.Procedures vs MediatR vs MessagePipe. One thesis per class.
//   DispatchBenchmarks   - raw dispatch, 0 middleware
//   ChainBenchmarks      - 3 pass-through middleware
//   ThroughputBenchmarks - 10k requests, time + memory + GC
// No args -> run all. Filter a subset with e.g. --filter *ChainBenchmarks*
BenchmarkSwitcher
    .FromTypes(new[] { typeof(DispatchBenchmarks), typeof(ChainBenchmarks), typeof(ThroughputBenchmarks) })
    .Run(args);
