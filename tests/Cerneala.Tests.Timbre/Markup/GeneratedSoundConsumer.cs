using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using Cerneala.SourceGen;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Cerneala.Tests.Timbre.Markup;

// Runs the real UiMarkupGenerator on .crn input, compiles the output as an
// ordinary consumer assembly (no internals access, no audio device) and loads
// it into a collectible context so each fixture releases its generated code.
internal sealed class GeneratedSoundConsumer : IDisposable
{
    private readonly AssemblyLoadContext context;

    private GeneratedSoundConsumer(Assembly assembly, AssemblyLoadContext context, string generatedSource, Compilation compilation)
    {
        Assembly = assembly;
        this.context = context;
        GeneratedSource = generatedSource;
        Compilation = compilation;
    }

    public Assembly Assembly { get; }

    public string GeneratedSource { get; }

    public Compilation Compilation { get; }

    public static GeneratedSoundConsumer Compile(
        IReadOnlyList<(string Path, string Markup)> files,
        string companionSource = "namespace SoundConsumer { public static class Anchor { } }",
        string companionPath = "")
    {
        SyntaxTree input = CSharpSyntaxTree.ParseText(
            companionSource,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            path: companionPath);
        string assemblyName = "SoundConsumer" + Guid.NewGuid().ToString("N");
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            [input],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UiMarkupGenerator().AsSourceGenerator()],
            files.Select(file => (AdditionalText)new InMemoryAdditionalText(file.Path, file.Markup)).ToArray(),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation output, out _);
        GeneratorRunResult result = driver.GetRunResult().Results.Single();
        string generated = string.Join(
            Environment.NewLine + "-----" + Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Diagnostic[] errors = result.Diagnostics
            .Concat(output.GetDiagnostics())
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                "Generated consumer failed to build (harness failure, not an audio RED):" + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(error => error.ToString())) + Environment.NewLine + generated);
        }

        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = output.Emit(stream);
        if (!emit.Success)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics));
        }

        stream.Position = 0;
        AssemblyLoadContext context = new(assemblyName, isCollectible: true);
        return new GeneratedSoundConsumer(context.LoadFromStream(stream), context, generated, output);
    }

    public UIElement Create(string factoryType)
    {
        MethodInfo method = Assembly.GetType(factoryType, throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 0);
        return (UIElement)method.Invoke(null, null)!;
    }

    public object CreateInstance(string typeName) =>
        Activator.CreateInstance(Assembly.GetType(typeName, throwOnError: true)!)!;

    public void Dispose() => context.Unload();

    private static MetadataReference[] References()
    {
        string trusted = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trusted
            .Split(Path.PathSeparator)
            .Where(path => !Path.GetFileName(path).StartsWith("Cerneala.Tests", StringComparison.Ordinal))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .GroupBy(reference => reference.Display, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private sealed class InMemoryAdditionalText(string path, string text) : AdditionalText
    {
        private readonly SourceText text = SourceText.From(text, Encoding.UTF8);

        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => text;
    }
}
