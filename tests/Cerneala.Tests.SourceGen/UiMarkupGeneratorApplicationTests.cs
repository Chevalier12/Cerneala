using System;
using System.IO;
using System.Linq;
using Cerneala.SourceGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string ApplicationInput = """
        using Cerneala.UI;
        using Cerneala.UI.Controls;

        namespace TestInput
        {
            public partial class App : Application { }
            public partial class ShellWindow : Window { }
        }
        """;

    [Fact]
    public void PairedApplicationOwnsStandaloneEntryPointForArbitraryStartupWindow()
    {
        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" ShutdownMode=\"OnLastWindowClose\" />",
            ApplicationInput,
            OutputKind.WindowsApplication,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);
        Assert.Contains("partial class App", source);
        Assert.Contains("[global::System.STAThreadAttribute]", source);
        Assert.Contains("private static int Main(string[] args)", source);
        Assert.Contains("return global::Cerneala.UI.Hosting.Windows.GeneratedWindowApplication.Run(CreateDescriptor(), args);", source);
        Assert.Contains("static () => new global::TestInput.App()", source);
        Assert.Contains("global::TestInput.ShellWindow", source);
        Assert.Contains("\"global::TestInput.ShellWindow\"", source);
        Assert.DoesNotContain("MainWindow", source);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void PairedApplicationOwnsHostedModuleInitializer()
    {
        const string program = """
            public static class Program
            {
                public static void Main() { }
            }
            """;
        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            ApplicationInput + program,
            OutputKind.ConsoleApplication,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);
        Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]", source);
        Assert.Contains("GeneratedWindowApplication.RegisterStartup", source);
        Assert.Contains("static () => new global::TestInput.App()", source);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void ApplicationDefinitionDisablesLegacyMainWindowStartup()
    {
        const string input = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;

            namespace TestInput
            {
                public partial class App : Application { }
                public partial class ShellWindow : Window { }
                public partial class MainWindow : Window { }
            }
            """;
        MarkupFile[] files = [new("App.cui.xml", "<Application StartupWindow=\"ShellWindow\" />")];

        GeneratorRunResult result = RunGenerator(
            files,
            out _,
            input,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string allSource = string.Join(Environment.NewLine, result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Equal(1, Count(allSource, "[global::System.STAThreadAttribute]"));
        Assert.Contains("global::TestInput.ShellWindow", allSource);
    }

    [Fact]
    public void LegacyMainWindowStillOwnsStartupWithoutApplicationMarkup()
    {
        const string input = """
            using Cerneala.UI.Controls;
            namespace TestInput;
            public partial class MainWindow : Window { }
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "MainWindow.cui.xml",
            "<Window />",
            input,
            out Compilation compilation,
            OutputKind.WindowsApplication);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("[global::System.STAThreadAttribute]", SingleGeneratedSource(result));
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
    }

    [Theory]
    [InlineData("", "StartupWindow")]
    [InlineData(" StartupWindow=\"MissingWindow\"", "MissingWindow")]
    [InlineData(" StartupWindow=\"AbstractWindow\"", "concrete")]
    [InlineData(" StartupWindow=\"NotAWindow\"", "Window")]
    [InlineData(" StartupWindow=\"App\"", "Application")]
    public void ApplicationRejectsInvalidStartupWindowTargets(string attributes, string expectedMessage)
    {
        const string invalidTargets = """
            public abstract class AbstractWindow : Cerneala.UI.Controls.Window { }
            public sealed class NotAWindow { }
            """;
        GeneratorRunResult result = RunApplicationGenerator(
            $"<Application{attributes} />",
            ApplicationInput + invalidTargets,
            OutputKind.WindowsApplication,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains(expectedMessage, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ApplicationRejectsAmbiguousAndInaccessibleStartupWindowTargets()
    {
        const string ambiguousInput = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            using First;
            using Second;

            namespace First { public sealed class ShellWindow : Window { } }
            namespace Second { public sealed class ShellWindow : Window { } }
            namespace TestInput { public partial class App : Application { } }
            """;
        GeneratorRunResult ambiguous = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            ambiguousInput,
            OutputKind.WindowsApplication,
            out _);
        Diagnostic ambiguousDiagnostic = Assert.Single(
            ambiguous.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains("ambiguous", ambiguousDiagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);

        const string inaccessibleInput = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;

            namespace TestInput
            {
                public partial class App : Application { }
                public sealed class WindowContainer
                {
                    private sealed class HiddenWindow : Window { }
                }
            }
            """;
        GeneratorRunResult inaccessible = RunApplicationGenerator(
            "<Application StartupWindow=\"WindowContainer.HiddenWindow\" />",
            inaccessibleInput,
            OutputKind.WindowsApplication,
            out _);
        Diagnostic inaccessibleDiagnostic = Assert.Single(
            inaccessible.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains("accessible", inaccessibleDiagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("<Window />", "Application")]
    [InlineData("<Application StartupWindow=\"ShellWindow\"><Button /></Application>", "child")]
    [InlineData("<Application StartupWindow=\"ShellWindow\" Name=\"App\" />", "Name")]
    [InlineData("<Application StartupWindow=\"ShellWindow\" DataType=\"System.String\" />", "DataType")]
    public void ApplicationRejectsWrongRootAndElementOnlySyntax(string markup, string expectedMessage)
    {
        GeneratorRunResult result = RunApplicationGenerator(
            markup,
            ApplicationInput,
            OutputKind.WindowsApplication,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains(expectedMessage, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ApplicationRejectsMissingWrongBaseAndUserConstructorCompanions()
    {
        (string Source, string ExpectedMessage)[] cases =
        [
            ("namespace TestInput { public sealed class Anchor { } }", "companion"),
            (
                "namespace TestInput { public partial class App { } public partial class ShellWindow : Cerneala.UI.Controls.Window { } }",
                "derive"),
            (
                """
                namespace TestInput
                {
                    public partial class App : Cerneala.UI.Application { public App() { } }
                    public partial class ShellWindow : Cerneala.UI.Controls.Window { }
                }
                """,
                "constructor")
        ];

        foreach ((string source, string expectedMessage) in cases)
        {
            GeneratorRunResult result = RunApplicationGenerator(
                "<Application StartupWindow=\"ShellWindow\" />",
                source,
                OutputKind.WindowsApplication,
                out _);
            Diagnostic diagnostic = Assert.Single(
                result.Diagnostics,
                candidate => candidate.Severity == DiagnosticSeverity.Error);
            Assert.Contains(expectedMessage, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
            Assert.Empty(result.GeneratedSources);
        }
    }

    [Fact]
    public void ExecutableRejectsDuplicateApplicationDefinitions()
    {
        MarkupFile[] files =
        [
            new("App.cui.xml", "<Application StartupWindow=\"ShellWindow\" />"),
            new("SecondApp.cui.xml", "<Application StartupWindow=\"ShellWindow\" />")
        ];
        GeneratorRunResult result = RunGenerator(
            files,
            out _,
            ApplicationInput,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains("one Application", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Theory]
    [InlineData("ShellWindow")]
    [InlineData("External.ShellWindow")]
    [InlineData("global::External.ShellWindow")]
    public void StartupWindowUsesRoslynScopeAndQualifiedTypeResolution(string startupName)
    {
        const string input = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            using External;

            namespace External { public partial class ShellWindow : Window { } }
            namespace TestInput { public partial class App : Application { } }
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            $"<Application StartupWindow=\"{startupName}\" />",
            input,
            OutputKind.WindowsApplication,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::External.ShellWindow", SingleGeneratedSource(result));
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
    }

    [Fact]
    public void ApplicationStartupRegistersGenericWindowViewModel()
    {
        const string input = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            namespace TestInput;
            public partial class App : Application { }
            public sealed class ShellViewModel { }
            public partial class ShellWindow : Window<ShellViewModel> { }
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            input,
            OutputKind.WindowsApplication,
            out Compilation compilation);
        string source = SingleGeneratedSource(result);

        Assert.Contains("AddTransient<global::TestInput.ShellViewModel>", source);
        Assert.Contains("AddTransient<global::TestInput.ShellWindow>", source);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
    }

    [Theory]
    [InlineData("<Application StartupWindow=\"ShellWindow\" ShutdownMode=\"Whenever\" />", "ShutdownMode")]
    [InlineData("<Application StartupWindow=\"ShellWindow\">@when Ready { }</Application>", "directives")]
    public void ApplicationRejectsInvalidPolicyAndDirectives(string markup, string expectedMessage)
    {
        GeneratorRunResult result = RunApplicationGenerator(
            markup,
            ApplicationInput,
            OutputKind.WindowsApplication,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains(expectedMessage, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void NewApplicationRejectsLegacyStaticConfigureServicesHook()
    {
        const string input = """
            using Cerneala.UI;
            using Cerneala.UI.Controls;
            using Microsoft.Extensions.DependencyInjection;
            namespace TestInput;
            public partial class App : Application
            {
                public static void ConfigureServices(IServiceCollection services) { }
            }
            public partial class ShellWindow : Window { }
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            "<Application StartupWindow=\"ShellWindow\" />",
            input,
            OutputKind.WindowsApplication,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains("legacy static", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplicationGenerationIsDeterministicAcrossAdditionalFileOrder()
    {
        MarkupFile app = new("App.cui.xml", "<Application StartupWindow=\"ShellWindow\" />");
        MarkupFile fragment = new("Views/Fragment.cui.xml", "<Button Content=\"Stable\" />");

        GeneratorRunResult forward = RunGenerator(
            [app, fragment],
            out _,
            ApplicationInput,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);
        GeneratorRunResult reverse = RunGenerator(
            [fragment, app],
            out _,
            ApplicationInput,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);

        string[] forwardSources = forward.GeneratedSources
            .Select(source => source.HintName + "\n" + source.SourceText)
            .OrderBy(source => source, StringComparer.Ordinal)
            .ToArray();
        string[] reverseSources = reverse.GeneratedSources
            .Select(source => source.HintName + "\n" + source.SourceText)
            .OrderBy(source => source, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(forwardSources, reverseSources);
    }

    [Fact]
    public void ApplicationResourcesAreEmittedAndVisibleToOtherMarkupDocuments()
    {
        const string markup = """
            <Application StartupWindow="ShellWindow">
                <Application.Resources>
                    <SolidColorBrush Name="Accent" Color="#FF20A060" />
                    <Tween Name="Quick" Duration="150ms" Easing="EaseOut" />
                    <Spring Name="Soft" Stiffness="420" Damping="32" Mass="1" />
                    <Aspect Target="Border">
                        @default
                        {
                            Background = $Accent;
                        }
                        @presence
                        {
                            enter = $Quick;
                            exit = $Soft;
                        }
                    </Aspect>
                </Application.Resources>
            </Application>
            """;
        MarkupFile[] files =
        [
            new("App.cui.xml", markup),
            new("Views/Shared.cui.xml", "<Border />")
        ];

        GeneratorRunResult result = RunGenerator(
            files,
            out Compilation compilation,
            ApplicationInput,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Contains("ResourceId<global::Cerneala.UI.Media.Brush>(\"Accent\")", generated);
        Assert.Contains("GeneratedMarkup.AttachResource", generated);
        Assert.Contains("\"Accent\"", generated);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void ApplicationResourcesAreResolvedByPairedWindowsAndUserControls()
    {
        MarkupFile[] files =
        [
            new(
                "App.cui.xml",
                """
                <Application StartupWindow="ShellWindow">
                    <Application.Resources>
                        <SolidColorBrush Name="Accent" Color="#FF20A060" />
                    </Application.Resources>
                </Application>
                """),
            new("ShellWindow.cui.xml", "<Window><Border Background=\"$Accent\" /></Window>"),
            new("SharedView.cui.xml", "<UserControl><Border Background=\"$Accent\" /></UserControl>")
        ];
        (string Path, string Source)[] sources =
        [
            (
                "App.cui.xml.cs",
                """
                using Cerneala.UI;
                namespace TestInput;
                public partial class App : Application { }
                """),
            (
                "ShellWindow.cui.xml.cs",
                """
                using Cerneala.UI.Controls;
                namespace TestInput;
                public partial class ShellWindow : Window { }
                """),
            (
                "SharedView.cui.xml.cs",
                """
                using Cerneala.UI.Controls;
                namespace TestInput;
                public partial class SharedView : UserControl { }
                """)
        ];

        GeneratorRunResult result = RunApplicationViewsGenerator(files, sources, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generated = string.Join(
            Environment.NewLine,
            result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Equal(2, Count(generated, "GeneratedMarkup.AttachResource"));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void ApplicationResourceReferencesAreTypeCheckedAcrossDocuments()
    {
        MarkupFile[] files =
        [
            new(
                "App.cui.xml",
                """
                <Application StartupWindow="ShellWindow">
                    <Application.Resources>
                        <Tween Name="Quick" Duration="150ms" />
                    </Application.Resources>
                </Application>
                """),
            new("Views/Shared.cui.xml", "<Border Background=\"$Quick\" />")
        ];

        GeneratorRunResult result = RunGenerator(
            files,
            out _,
            ApplicationInput,
            "App.cui.xml.cs",
            OutputKind.WindowsApplication);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Equal("CERNEALAUI004", diagnostic.Id);
        Assert.Contains("$Quick", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ApplicationRejectsMotionClipResourcesWithoutAVisualNamescope()
    {
        GeneratorRunResult result = RunApplicationGenerator(
            """
            <Application StartupWindow="ShellWindow">
                <Application.Resources>
                    <MotionClip Name="Pulse" TargetType="Border">
                        @animate self.Opacity to 0.5 over tween(100ms);
                    </MotionClip>
                </Application.Resources>
            </Application>
            """,
            ApplicationInput,
            OutputKind.WindowsApplication,
            out _);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Severity == DiagnosticSeverity.Error);
        Assert.Contains("namescope", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }

    private static GeneratorRunResult RunApplicationGenerator(
        string markup,
        string inputSource,
        OutputKind outputKind,
        out Compilation compilation)
    {
        return RunGenerator(
            [new MarkupFile("App.cui.xml", markup)],
            out compilation,
            inputSource,
            "App.cui.xml.cs",
            outputKind);
    }

    private static GeneratorRunResult RunApplicationViewsGenerator(
        MarkupFile[] files,
        (string Path, string Source)[] sources,
        out Compilation outputCompilation)
    {
        CSharpParseOptions parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest);
        SyntaxTree[] syntaxTrees = sources
            .Select(source => CSharpSyntaxTree.ParseText(source.Source, parseOptions, path: source.Path))
            .ToArray();
        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            syntaxTrees,
            References(),
            new CSharpCompilationOptions(OutputKind.WindowsApplication));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new UiMarkupGenerator().AsSourceGenerator() },
            files.Select(file => new InMemoryAdditionalText(file.Path, file.Text)).ToArray(),
            parseOptions: parseOptions);

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
        return driver.GetRunResult().Results.Single();
    }
}
