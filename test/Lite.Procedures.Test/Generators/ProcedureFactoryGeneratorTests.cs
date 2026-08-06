using System.Linq;
using Lite.Procedures;
using Lite.Procedures;
using Lite.Procedures.Interception;
using Lite.Procedures.Pipeline.SourceGenerators;
using Microsoft.CodeAnalysis;

namespace Lite.Procedures.Test.Generators
{
    public class ProcedureFactoryGeneratorTests
    {
        private static readonly System.Type[] References =
        {
            typeof(IAsyncProcedure<,>), typeof(AsyncInterceptor<,>), typeof(AsyncProcedurePipeline<,>),
        };

        [Fact]
        public void AttributeChain_EmitsFastPipeline_NoWarnings_CompilesClean()
        {
            const string source = """
                using Lite.Procedures;
                using Lite.Procedures.Interception;

                namespace TestNs
                {
                    public sealed record Req(string Value);

                    public sealed class TrackA : AsyncInterceptor<Req, string>
                    {
                        public override System.Threading.Tasks.ValueTask<string> InvokeAsync(
                            Req a,
                            System.Func<Req, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<string>> next,
                            System.Threading.CancellationToken ct)
                            => next(a, ct);
                    }

                    [InterceptWith(typeof(TrackA))]
                    public sealed class Proc : IAsyncProcedure<Req, string>
                    {
                        public System.Threading.Tasks.ValueTask<string> ExecuteAsync(Req a, System.Threading.CancellationToken ct = default)
                            => new(a.Value);
                    }
                }
                """;

            var result = GeneratorTestHost.Run(new ProcedureFactoryGenerator(), source, References);

            Assert.Empty(result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
            Assert.Contains(result.GeneratedSources, g => g.Contains("FastPipeline_TestNs_Proc"));
            Assert.Empty(result.UpdatedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        }

        [Fact]
        public void GenericProcedure_SkippedWithLITE001_NothingGenerated()
        {
            const string source = """
                using Lite.Procedures;

                namespace TestNs
                {
                    public sealed record Req<T>(T Value);

                    public sealed class GenericProc<T> : IAsyncProcedure<Req<T>, T>
                    {
                        public System.Threading.Tasks.ValueTask<T> ExecuteAsync(Req<T> a, System.Threading.CancellationToken ct = default)
                            => new(a.Value);
                    }
                }
                """;

            var result = GeneratorTestHost.Run(new ProcedureFactoryGenerator(), source, References);

            var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "LITE001");
            Assert.Contains("GenericProc", diagnostic.GetMessage());
            Assert.Contains("open generic type", diagnostic.GetMessage());
            Assert.Empty(result.GeneratedSources);
        }

        [Fact]
        public void PrivateNestedProcedure_SkippedWithLITE001()
        {
            const string source = """
                using Lite.Procedures;

                namespace TestNs
                {
                    public sealed record Req(string Value);

                    public class Container
                    {
                        private sealed class HiddenProc : IAsyncProcedure<Req, string>
                        {
                            public System.Threading.Tasks.ValueTask<string> ExecuteAsync(Req a, System.Threading.CancellationToken ct = default)
                                => new(a.Value);
                        }
                    }
                }
                """;

            var result = GeneratorTestHost.Run(new ProcedureFactoryGenerator(), source, References);

            var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "LITE001");
            Assert.Contains("not visible outside its declaring assembly", diagnostic.GetMessage());
            Assert.Empty(result.GeneratedSources);
        }

        [Fact]
        public void InvisibleInterceptor_SkipsFastPipeline_ButStillEmitsFactory_WithLITE002()
        {
            const string source = """
                using Lite.Procedures;
                using Lite.Procedures.Interception;

                namespace TestNs
                {
                    public sealed record Req(string Value);

                    public class Container
                    {
                        private sealed class HiddenInterceptor : AsyncInterceptor<Req, string>
                        {
                            public override System.Threading.Tasks.ValueTask<string> InvokeAsync(
                                Req a,
                                System.Func<Req, System.Threading.CancellationToken, System.Threading.Tasks.ValueTask<string>> next,
                                System.Threading.CancellationToken ct)
                                => next(a, ct);
                        }

                        [InterceptWith(typeof(HiddenInterceptor))]
                        public sealed class Proc : IAsyncProcedure<Req, string>
                        {
                            public System.Threading.Tasks.ValueTask<string> ExecuteAsync(Req a, System.Threading.CancellationToken ct = default)
                                => new(a.Value);
                        }
                    }
                }
                """;

            var result = GeneratorTestHost.Run(new ProcedureFactoryGenerator(), source, References);

            var diagnostic = Assert.Single(result.Diagnostics, d => d.Id == "LITE002");
            Assert.Contains("HiddenInterceptor", diagnostic.GetMessage());

            // The factory is still generated (reflection-free fallback), just not the unrolled FastPipeline.
            Assert.Contains(result.GeneratedSources, g => g.Contains("Factory_TestNs_Container_Proc"));
            Assert.DoesNotContain(result.GeneratedSources, g => g.Contains("FastPipeline_"));
            Assert.Empty(result.UpdatedCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error));
        }
    }
}
