using System.Reflection;
using Cerneala.Timbre;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string TimbreClipResources = """
        <TimbreClip Name="Tone">
          Source = "audio/tone.wav";
        </TimbreClip>
        <TimbreClip Name="ConfirmTimbre">
          Source = "audio/confirm.wav";
          Volume = 0.8;
          @parameter ToneCutoff: float = 1200;
          @parameter EchoMix: float = 0.15;
          @modifier LowPass { Cutoff = ToneCutoff; }
          @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
        </TimbreClip>
        <TimbreClip Name="Music">
          Source = "audio/music.ogg";
          Loop = true;
        </TimbreClip>
        """;

    [Fact]
    public void TimbreClipAndEventTimbreActionGenerateWithoutDiagnostics()
    {
        const string markup = """
            <Button Content="Play">
              <Button.Resources>
                <TimbreClip Name="Tone">
                  Source = "audio/tone.wav";
                </TimbreClip>
              </Button.Resources>
              <Button.Aspect>
                @on Click { @timbre $Tone; }
              </Button.Aspect>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TimbreMinimal.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void TimbreClipFactoryResourcesBindToCoreConstructors()
    {
        string markup = "<StackPanel><StackPanel.Resources>" + TimbreClipResources + "</StackPanel.Resources></StackPanel>";

        GeneratorRunResult result = RunGenerator("TimbreFactory.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreClipLoweringBindsCoreApi(compilation, result);
        UIElement panel = InvokeCreate(Emit(compilation), "Cerneala.GeneratedUi.TimbreFactoryFactory");
        AssertTimbreClipResourcesMatchManualDefinitions(panel);
    }

    [Fact]
    public void TimbreClipPairedUserControlResourcesBindToCoreConstructors()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class TimbrePanel : UserControl { }
            """;
        string markup = "<UserControl><UserControl.Resources>" + TimbreClipResources +
            "</UserControl.Resources><TextBlock Text=\"Timbre\" /></UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/TimbrePanel.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreClipLoweringBindsCoreApi(compilation, result);
        Assembly assembly = Assembly.Load(Emit(compilation).ToArray());
        UserControl view = Assert.IsAssignableFrom<UserControl>(
            Activator.CreateInstance(assembly.GetType("TestInput.Views.TimbrePanel", throwOnError: true)!));
        AssertTimbreClipResourcesMatchManualDefinitions(view);
    }

    [Fact]
    public void TimbreClipReferencedFromTwoElementsAndWindowsCompilesWithoutPlaybackOrIo()
    {
        MarkupFile application = new(
            "App.crn",
            """
            <Application StartupWindow="ShellWindow">
              <Application.Resources>
                <TimbreClip Name="Chime">
                  Source = "audio/missing-chime.wav";
                </TimbreClip>
              </Application.Resources>
            </Application>
            """);
        MarkupFile first = new(
            "First.crn",
            """
            <StackPanel>
              <StackPanel.Resources>
                <TimbreClip Name="Tone">Source = "audio/missing-tone.wav";</TimbreClip>
                <Aspect Name="Clicky" TargetType="Button">@on Click { @timbre $Tone; @timbre $Chime; }</Aspect>
              </StackPanel.Resources>
              <Button Content="A" Aspect="$Clicky" />
              <Button Content="B" Aspect="$Clicky" />
            </StackPanel>
            """);
        MarkupFile second = new(
            "Second.crn",
            """
            <Border>
              <Button Content="C">
                <Button.Aspect>@on Click { @timbre $Chime; }</Button.Aspect>
              </Button>
            </Border>
            """);

        GeneratorRunResult result = RunGenerator(
            [application, first, second],
            out Compilation compilation,
            "namespace TestInput { public partial class App : Cerneala.UI.Application { } public partial class ShellWindow : Cerneala.UI.Controls.Window { } }",
            "App.crn.cs");

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assembly assembly = Assembly.Load(Emit(compilation).ToArray());
        UIElement panel = InvokeFactory(assembly, "Cerneala.GeneratedUi.FirstFactory");
        Assert.IsType<Border>(InvokeFactory(assembly, "Cerneala.GeneratedUi.SecondFactory"));
        Assert.True(panel.TryFindResource(new ResourceId<TimbreClip>("Tone"), out TimbreClip tone));
        Assert.Equal("audio/missing-tone.wav", tone.Source.Name);

        // Declaring and referencing a clip opens nothing: the factory ran and
        // the missing files were never touched.
        CountingTimbreOutput output = new();
        using TimbreRuntime runtime = new(new TimbreRuntimeOptions { Output = output });
        UIRoot root = new();
        root.SetTimbreRuntime(runtime);
        root.VisualChildren.Add(panel);
        Assert.Equal(0, output.OpenCount);
        Assert.Equal(0, root.Detective.CaptureTimbre()!.ActivePlaybacks);
        Assert.Equal(0, root.Detective.CaptureTimbre()!.PlaybacksFailed);
    }

    [Fact]
    public void TimbreClipInsideTemplatesAndShadowingScopesCompiles()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <TimbreClip Name="Tone">Source = "audio/outer.wav";</TimbreClip>
              </StackPanel.Resources>
              <Border>
                <Border.Resources>
                  <TimbreClip Name="Tone">Source = "audio/inner.wav"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</TimbreClip>
                </Border.Resources>
                <Button Content="Inner">
                  <Button.Aspect>@on Click { @timbre $Tone(Cut = 400); }</Button.Aspect>
                </Button>
              </Border>
              <ItemsControl>
                @templates
                {
                  <ContentTemplate DataType="System.String">
                    <Border>
                      <Border.Resources>
                        <TimbreClip Name="Item">Source = "audio/item.wav";</TimbreClip>
                      </Border.Resources>
                      <Border.Aspect>@on Loaded { @timbre $Item; }</Border.Aspect>
                    </Border>
                  </ContentTemplate>
                }
              </ItemsControl>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("TimbreScopes.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        UIElement panel = InvokeCreate(Emit(compilation), "Cerneala.GeneratedUi.TimbreScopesFactory");
        Border inner = Assert.IsType<Border>(panel.VisualChildren[0]);
        Assert.True(panel.TryFindResource(new ResourceId<TimbreClip>("Tone"), out TimbreClip outer));
        Assert.True(inner.TryFindResource(new ResourceId<TimbreClip>("Tone"), out TimbreClip shadowing));
        Assert.Equal("audio/outer.wav", outer.Source.Name);
        Assert.Equal("audio/inner.wav", shadowing.Source.Name);
        Assert.Single(shadowing.Parameters);
    }

    private const string TimbreActionAspect = """
        @handle Playback;
        @on Click { @timbre $ConfirmTimbre(Volume = 0.2, Loop = true, ToneCutoff = 800) as Playback; @animate with Tween(200ms, EaseOut) { @to { Opacity = 0.5; } } @timbre $Tone; }
        @on MouseEnter { @pause Playback; @resume Playback; @seek Playback to 30s; @cancel Playback; }
        @when $self.IsEnabled { @if value == false { @timbre $Music as Playback; } }
        """;

    [Fact]
    public void TimbreActionsInFactoryBindToGeneratedMarkupHelpersAndCoreStartOptions()
    {
        string markup = "<StackPanel><StackPanel.Resources>" + TimbreClipResources + "</StackPanel.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>" + TimbreActionAspect + "</Button.Aspect></Button>" +
            "<Button Content=\"Templated\"><Button.Aspect>@template { <Border><Border.Aspect>@on Loaded { @timbre $Tone; }</Border.Aspect></Border> }</Button.Aspect></Button>" +
            "</StackPanel>";

        GeneratorRunResult result = RunGenerator("TimbreActions.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreActionsBindCoreOperations(compilation, result, expectTemplateLifetime: true);
    }

    [Fact]
    public void TimbreActionsInPairedPartialBindToGeneratedMarkupHelpersAndCoreStartOptions()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class TimbreActionsView : UserControl { }
            """;
        string markup = "<UserControl><UserControl.Resources>" + TimbreClipResources + "</UserControl.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>" + TimbreActionAspect + "</Button.Aspect></Button></UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/TimbreActionsView.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreActionsBindCoreOperations(compilation, result, expectTemplateLifetime: false);
    }

    [Theory]
    [InlineData("UserControl")]
    [InlineData("Window")]
    public void PairedRootAspectEmitsEachReactiveTimbreActivationOnce(string rootType)
    {
        string inputSource = $$"""
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class RootTimbre : {{rootType}} { }
            """;
        string markup = $"<{rootType}><{rootType}.Resources>" + TimbreClipResources + $"</{rootType}.Resources>" +
            $"<{rootType}.Aspect>@when IsEnabled {{ @timbre $Tone; }}</{rootType}.Aspect><TextBlock Text=\"Timbre\" /></{rootType}>";

        GeneratorRunResult result = RunPairedGenerator("Views/RootTimbre.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"PlayTimbre\("));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"visualActivation => \{ timbreAction\d+\(\);"));
    }

    // The sound examples of the conceptual guides must stay compilable: their
    // markup runs through the real generator and their C# against the core API.
    [Theory]
    [InlineData("timbre-guide.md")]
    [InlineData("CernealaMarkupGuide.md")]
    public void DocumentedTimbreExamplesCompile(string guide)
    {
        string text = File.ReadAllText(Path.Combine(DocumentationRoot(), guide)).Replace("\r\n", "\n");
        string[] xml = System.Text.RegularExpressions.Regex.Matches(text, "```xml\n(.*?)```", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(block => block.Contains("TimbreClip", StringComparison.Ordinal) || block.Contains("@timbre", StringComparison.Ordinal))
            .ToArray();
        string[] csharp = System.Text.RegularExpressions.Regex.Matches(text, "```csharp\n(.*?)```", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(block => block.Contains("TimbreClip", StringComparison.Ordinal) || block.Contains("Timbre.", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(xml);

        string joined = string.Concat(xml);
        System.Text.RegularExpressions.MatchCollection resources = System.Text.RegularExpressions.Regex.Matches(
            joined,
            "<UserControl.Resources>(.*?)</UserControl.Resources>",
            System.Text.RegularExpressions.RegexOptions.Singleline);
        string elements = System.Text.RegularExpressions.Regex.Replace(
            joined,
            "<UserControl.Resources>.*?</UserControl.Resources>",
            string.Empty,
            System.Text.RegularExpressions.RegexOptions.Singleline);
        string markup = "<UserControl><UserControl.Resources>" +
            string.Concat(resources.Select(match => match.Groups[1].Value)) +
            "</UserControl.Resources><StackPanel>" + elements + "</StackPanel></UserControl>";
        string inputSource =
            "using System;\nusing Cerneala.Timbre;\nusing Cerneala.UI.Controls;\nusing Cerneala.UI.Motion;\nusing Cerneala.UI.Motion.Core;\nusing Cerneala.UI.Motion.Specs;\nnamespace TestInput.Views;\n" +
            "public partial class GuideView : UserControl\n{\n" +
            string.Concat(csharp.Select((block, index) => $"    private static void Example{index}(Button button)\n    {{\n{block}\n    }}\n")) +
            "}\n";

        GeneratorRunResult result = RunPairedGenerator("Views/GuideView.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assert.Contains("PlayTimbre(", string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString())), StringComparison.Ordinal);
    }

    private static string DocumentationRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return Path.Combine(
            directory?.FullName ?? throw new InvalidOperationException("Could not locate the Cerneala repository root."),
            "docs");
    }

    private static void AssertTimbreActionsBindCoreOperations(Compilation compilation, GeneratorRunResult result, bool expectTemplateLifetime)
    {
        SyntaxTree tree = compilation.SyntaxTrees.Single(candidate =>
            result.GeneratedSources.Any(source => candidate.FilePath.EndsWith(source.HintName, StringComparison.Ordinal)));
        SemanticModel model = compilation.GetSemanticModel(tree);
        IMethodSymbol[] calls = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>()
            .ToArray();
        string[] helpers = calls
            .Where(symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.UI.Markup.GeneratedMarkup")
            .Select(symbol => symbol.Name)
            .ToArray();
        foreach (string helper in new[] { "AttachTimbreSession", "AddTimbreTrigger", "PlayTimbre", "GetTimbreParameter", "PauseTimbre", "ResumeTimbre", "SeekTimbre", "CancelTimbre", "CanStartMotionExecution" })
        {
            Assert.Contains(helper, helpers);
        }

        Assert.Equal(expectTemplateLifetime ? 4 : 3, helpers.Count(name => name == "PlayTimbre"));
        IMethodSymbol set = Assert.Single(calls, symbol => symbol.Name == "Set" && symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.TimbreStartOptions");
        Assert.Equal("float", set.TypeArguments.Single().ToDisplayString());
        IPropertySymbol[] assigned = tree.GetRoot().DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(assignment => model.GetSymbolInfo(assignment.Left).Symbol)
            .OfType<IPropertySymbol>()
            .Where(property => property.ContainingType.ToDisplayString() == "Cerneala.Timbre.TimbreStartOptions")
            .ToArray();
        Assert.Equal(["Loop", "Volume"], assigned.Select(property => property.Name).OrderBy(name => name));
        Assert.Contains(
            tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Select(creation => model.GetSymbolInfo(creation).Symbol)
                .OfType<IMethodSymbol>(),
            constructor => constructor.ContainingType.ToDisplayString() == "Cerneala.UI.Markup.MarkupConditionRule" && constructor.Parameters.Length == 8);
        Assert.Equal(expectTemplateLifetime, calls.Any(symbol => symbol.Name == "RegisterLifetime"));

        string generated = string.Join(Environment.NewLine, result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Sdl", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("TimbrePlayback ", generated, StringComparison.Ordinal);
    }

    private static void AssertTimbreClipLoweringBindsCoreApi(Compilation compilation, GeneratorRunResult result)
    {
        SyntaxTree tree = compilation.SyntaxTrees.Single(candidate =>
            result.GeneratedSources.Any(source => candidate.FilePath.EndsWith(source.HintName, StringComparison.Ordinal)));
        SemanticModel model = compilation.GetSemanticModel(tree);
        IAssemblySymbol core = compilation.GetTypeByMetadataName("Cerneala.Timbre.TimbreClip")!.ContainingAssembly;
        IMethodSymbol[] constructors = tree.GetRoot().DescendantNodes()
            .OfType<BaseObjectCreationExpressionSyntax>()
            .Select(creation => model.GetSymbolInfo(creation).Symbol)
            .OfType<IMethodSymbol>()
            .Where(symbol => symbol.ContainingNamespace.ToDisplayString() == "Cerneala.Timbre")
            .ToArray();
        string[] constructed = constructors.Select(symbol => symbol.ContainingType.ToDisplayString()).Distinct().OrderBy(name => name).ToArray();
        Assert.Equal(
            [
                "Cerneala.Timbre.Delay",
                "Cerneala.Timbre.LowPass",
                "Cerneala.Timbre.TimbreClip",
                "Cerneala.Timbre.TimbreInput<float>",
                "Cerneala.Timbre.TimbreParameter<float>"
            ],
            constructed);
        Assert.All(constructors, symbol => Assert.Equal(core, symbol.ContainingAssembly, SymbolEqualityComparer.Default));
        Assert.Equal(3, constructors.Count(symbol => symbol.ContainingType.Name == "TimbreClip"));

        IMethodSymbol[] invocations = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>()
            .ToArray();
        Assert.Equal(3, invocations.Count(symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.TimbreSource" && symbol.Name == "FromFile"));
        Assert.Equal(3, invocations.Count(symbol =>
            symbol.Name == "SetResource" &&
            symbol.TypeArguments.SingleOrDefault()?.ToDisplayString() == "Cerneala.Timbre.TimbreClip"));

        ObjectCreationExpressionSyntax confirm = tree.GetRoot().DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>()
            .Single(creation => creation.ArgumentList!.Arguments.Count == 5);
        Assert.Equal(0.8f, model.GetConstantValue(confirm.ArgumentList!.Arguments.Single(argument => argument.NameColon?.Name.Identifier.Text == "volume").Expression).Value);
        Assert.Equal(false, model.GetConstantValue(confirm.ArgumentList.Arguments.Single(argument => argument.NameColon?.Name.Identifier.Text == "loop").Expression).Value);

        string generated = string.Join(Environment.NewLine, result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.DoesNotContain("System.Reflection", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("dynamic", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("Sdl", generated, StringComparison.Ordinal);
        Assert.DoesNotContain("@modifier", generated, StringComparison.Ordinal);
    }

    private static void AssertTimbreClipResourcesMatchManualDefinitions(UIElement owner)
    {
        TimbreParameter<float> toneCutoff = new("ToneCutoff", 1200f);
        TimbreParameter<float> echoMix = new("EchoMix", 0.15f);
        TimbreClip manualConfirm = new(
            "audio/confirm.wav",
            volume: 0.8f,
            parameters: [toneCutoff, echoMix],
            modifiers: [new LowPass(cutoff: toneCutoff), new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
        AssertSameDefinition(new TimbreClip("audio/tone.wav"), Resource(owner, "Tone"));
        AssertSameDefinition(manualConfirm, Resource(owner, "ConfirmTimbre"));
        AssertSameDefinition(new TimbreClip("audio/music.ogg", loop: true), Resource(owner, "Music"));
    }

    private static TimbreClip Resource(UIElement owner, string name)
    {
        Assert.True(owner.TryFindResource(new ResourceId<TimbreClip>(name), out TimbreClip clip), name);
        return clip;
    }

    private static void AssertSameDefinition(TimbreClip expected, TimbreClip actual)
    {
        Assert.Equal(expected.Source.Name, actual.Source.Name);
        Assert.Equal(expected.Volume, actual.Volume);
        Assert.Equal(expected.Loop, actual.Loop);
        Assert.Equal(expected.Loading, actual.Loading);
        Assert.Equal(
            expected.Parameters.Cast<TimbreParameter<float>>().Select(parameter => (parameter.Name, parameter.DefaultValue)),
            actual.Parameters.Cast<TimbreParameter<float>>().Select(parameter => (parameter.Name, parameter.DefaultValue)));
        Assert.Equal(DescribeModifiers(expected), DescribeModifiers(actual));
    }

    private static IEnumerable<string> DescribeModifiers(TimbreClip clip) => clip.Modifiers.Select(modifier => modifier switch
    {
        LowPass lowPass => "LowPass(" + DescribeInput(lowPass.Cutoff) + ")",
        Delay delay => "Delay(" + DescribeInput(delay.Time) + ", " + DescribeInput(delay.Feedback) + ", " + DescribeInput(delay.Mix) + ")",
        _ => modifier.GetType().Name
    });

    private static string DescribeInput(TimbreInput<float> input) =>
        input.Parameter is { } parameter ? "$" + parameter.Name : input.Value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    private static MemoryStream Emit(Compilation compilation)
    {
        MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        return stream;
    }

    private static UIElement InvokeFactory(Assembly assembly, string typeName)
    {
        MethodInfo method = assembly.GetType(typeName, throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 0);
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, null));
    }

    private sealed class CountingTimbreOutput : ITimbreOutput
    {
        public int OpenCount { get; private set; }

        public int QueuedFrames => 0;

        public void Open(ITimbreOutputClient client) => OpenCount++;

        public void Submit(ReadOnlySpan<float> samples)
        {
        }

        public void Close()
        {
        }
    }
}
