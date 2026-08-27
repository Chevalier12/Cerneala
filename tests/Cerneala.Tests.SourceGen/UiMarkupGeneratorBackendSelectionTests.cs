using System.Reflection;
using Cerneala.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string BackendAttributeName =
        "Cerneala.UI.Hosting.Windowing.ApplicationBackend";

    [Fact]
    public void ExecutableStartupRequiresAnExplicitBackendSelection()
    {
        const string source = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            namespace TestInput;
            public partial class App : Application { }
            public partial class ShellWindow : Window { }
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            source,
            OutputKind.WindowsApplication,
            out _,
            addDefaultBackendSelection: false);

        AssertBackendSelectionDiagnostic(result, BackendAttributeName, "Exactly one");
    }

    [Fact]
    public void ExecutableStartupRejectsDuplicateBackendSelections()
    {
        string source = $$"""
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            [assembly: {{BackendAttributeName}}(typeof(TestInput.BackendA))]
            [assembly: {{BackendAttributeName}}(typeof(TestInput.BackendB))]
            namespace TestInput;
            public static class BackendA { public static void EnsureRegistered() { } }
            public static class BackendB { public static void EnsureRegistered() { } }
            public partial class App : Application { }
            public partial class ShellWindow : Window { }
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            source,
            OutputKind.WindowsApplication,
            out _,
            addDefaultBackendSelection: false);

        AssertBackendSelectionDiagnostic(result, BackendAttributeName, "found 2");
    }

    [Theory]
    [InlineData("internal static class Backend { public static void EnsureRegistered() { } }")]
    [InlineData("public static class Backend<T> { public static void EnsureRegistered() { } }")]
    public void ExecutableStartupRejectsAnInaccessibleOrGenericBackend(string declaration)
    {
        GeneratorRunResult result = RunLegacyBackendGenerator(
            "Backend",
            declaration,
            hosted: false);

        AssertBackendSelectionDiagnostic(result, "global::TestInput.Backend", "public, non-generic");
    }

    [Theory]
    [InlineData("public sealed class Backend { }")]
    [InlineData("public sealed class Backend { internal static void EnsureRegistered() { } }")]
    [InlineData("public sealed class Backend { public void EnsureRegistered() { } }")]
    [InlineData("public sealed class Backend { public static int EnsureRegistered() => 0; }")]
    [InlineData("public sealed class Backend { public static void EnsureRegistered(int value) { } }")]
    public void ExecutableStartupRejectsAnInvalidEnsureRegisteredSignature(string declaration)
    {
        GeneratorRunResult result = RunLegacyBackendGenerator(
            "Backend",
            declaration,
            hosted: false);

        AssertBackendSelectionDiagnostic(result, "global::TestInput.Backend", "public static void EnsureRegistered()");
    }

    [Theory]
    [InlineData(true, false, "BackendA")]
    [InlineData(true, true, "BackendA")]
    [InlineData(true, false, "BackendB")]
    [InlineData(true, true, "BackendB")]
    [InlineData(false, false, "BackendA")]
    [InlineData(false, true, "BackendA")]
    [InlineData(false, false, "BackendB")]
    [InlineData(false, true, "BackendB")]
    public void ExplicitBackendSelectionControlsEveryGeneratedStartupPath(
        bool applicationMarkup,
        bool hosted,
        string backendName)
    {
        GeneratorRunResult result = applicationMarkup
            ? RunApplicationBackendGenerator(backendName, hosted)
            : RunLegacyBackendGenerator(
                backendName,
                "public static class BackendA { public static void EnsureRegistered() { } } " +
                "public static class BackendB { public static void EnsureRegistered() { } }",
                hosted);

        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = SingleGeneratedSource(result);
        Assert.Contains(
            $"global::TestInput.{backendName}.EnsureRegistered();",
            generated,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            $"global::TestInput.{(backendName == "BackendA" ? "BackendB" : "BackendA")}.EnsureRegistered();",
            generated,
            StringComparison.Ordinal);
        Assert.Contains(
            hosted
                ? "[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]"
                : "[global::System.STAThreadAttribute]",
            generated,
            StringComparison.Ordinal);
        int registration = generated.IndexOf(
            $"global::TestInput.{backendName}.EnsureRegistered();",
            StringComparison.Ordinal);
        int startup = generated.IndexOf(
            hosted ? "GeneratedWindowApplication.RegisterStartup" : "GeneratedWindowApplication.Run",
            StringComparison.Ordinal);
        Assert.True(registration >= 0 && registration < startup);
    }

    [Fact]
    public void SourceGeneratorProjectAndOutputHaveNoConcreteWindowsBackendCoupling()
    {
        string repositoryRoot = FindRepositoryRoot();
        string forbiddenNamespace = "Cerneala.UI.Hosting." + "Windows";
        string forbiddenAssembly = "Cerneala.Backends." + "MonoGame";
        string sourceGeneratorDirectory = Path.Combine(repositoryRoot, "Cerneala.SourceGen");

        foreach (string path in Directory.EnumerateFiles(
            sourceGeneratorDirectory,
            "*.cs",
            SearchOption.TopDirectoryOnly))
        {
            Assert.DoesNotContain(
                forbiddenNamespace,
                File.ReadAllText(path),
                StringComparison.Ordinal);
        }

        Assert.DoesNotContain(
            forbiddenAssembly,
            File.ReadAllText(Path.Combine(sourceGeneratorDirectory, "Cerneala.SourceGen.csproj")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            forbiddenAssembly,
            File.ReadAllText(Path.Combine(
                repositoryRoot,
                "tests",
                "Cerneala.Tests.SourceGen",
                "Cerneala.Tests.SourceGen.csproj")),
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            typeof(UiMarkupGenerator).Assembly.GetReferencedAssemblies(),
            reference => reference.Name?.Contains(forbiddenAssembly, StringComparison.Ordinal) == true);

        GeneratorRunResult result = RunLegacyBackendGenerator(
            "Backend",
            "public static class Backend { public static void EnsureRegistered() { } }",
            hosted: false);
        string generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.DoesNotContain(forbiddenNamespace, generated, StringComparison.Ordinal);
    }

    private static GeneratorRunResult RunApplicationBackendGenerator(string backendName, bool hosted)
    {
        string source = $$"""
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            [assembly: {{BackendAttributeName}}(typeof(TestInput.{{backendName}}))]
            namespace TestInput;
            public static class BackendA { public static void EnsureRegistered() { } }
            public sealed class BackendB { public static void EnsureRegistered() { } }
            public partial class App : Application { }
            public partial class ShellWindow : Window { }
            {{(hosted ? "public static class Program { public static void Main() { } }" : string.Empty)}}
            """;

        return RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            source,
            hosted ? OutputKind.ConsoleApplication : OutputKind.WindowsApplication,
            out _,
            addDefaultBackendSelection: false);
    }

    private static GeneratorRunResult RunLegacyBackendGenerator(
        string backendName,
        string declaration,
        bool hosted)
    {
        string source = $$"""
            using Cerneala.UI.Controls;
            [assembly: {{BackendAttributeName}}(typeof(TestInput.{{backendName}}))]
            namespace TestInput;
            {{declaration}}
            public partial class MainWindow : Window { }
            {{(hosted ? "public static class Program { public static void Main() { } }" : string.Empty)}}
            """;

        return RunPairedGenerator(
            "MainWindow.crn",
            "<Window Title=\"Backend selection\" />",
            source,
            out _,
            hosted ? OutputKind.ConsoleApplication : OutputKind.WindowsApplication,
            addDefaultBackendSelection: false);
    }

    private static void AssertBackendSelectionDiagnostic(
        GeneratorRunResult result,
        params string[] expectedMessageParts)
    {
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Id == "CERNEALAUI015");
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        foreach (string expected in expectedMessageParts)
        {
            Assert.Contains(expected, diagnostic.GetMessage(), StringComparison.Ordinal);
        }
        Assert.Empty(result.GeneratedSources);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
