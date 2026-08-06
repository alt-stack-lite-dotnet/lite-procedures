using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lite.Procedures.Test.Generators
{
    /// <summary>
    /// Minimal harness to drive a source generator directly (not through a real csproj build) and
    /// inspect what it actually produced: diagnostics, generated source text, and whether the
    /// resulting compilation is error-free.
    /// </summary>
    internal static class GeneratorTestHost
    {
        public readonly struct Result
        {
            public Result(ImmutableArray<Diagnostic> diagnostics, string[] generatedSources, Compilation updatedCompilation)
            {
                Diagnostics = diagnostics;
                GeneratedSources = generatedSources;
                UpdatedCompilation = updatedCompilation;
            }

            public ImmutableArray<Diagnostic> Diagnostics { get; }
            public string[] GeneratedSources { get; }
            public Compilation UpdatedCompilation { get; }
        }

        public static Result Run(IIncrementalGenerator generator, string source, params Type[] extraReferenceTypes)
        {
            var references = PlatformReferences()
                .Concat(extraReferenceTypes.Select(t => (MetadataReference)MetadataReference.CreateFromFile(t.Assembly.Location)))
                .ToArray();

            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratorTests_" + Guid.NewGuid().ToString("N"),
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var driver = CSharpGeneratorDriver.Create(generator);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out var updatedCompilation, out var diagnostics);

            var generatedSources = updatedCompilation.SyntaxTrees
                .Where(t => t.FilePath.EndsWith(".g.cs", StringComparison.Ordinal))
                .Select(t => t.ToString())
                .ToArray();

            return new Result(diagnostics, generatedSources, updatedCompilation);
        }

        private static MetadataReference[] PlatformReferences()
        {
            var tpaPaths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
                .Split(Path.PathSeparator);
            return tpaPaths.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p)).ToArray();
        }
    }
}
