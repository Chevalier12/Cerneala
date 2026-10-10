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
        <TimbreClip Name="Sounds">
          @parameter ToneCutoff: float = 1200;
          @parameter EchoMix: float = 0.15;
          @sound Tone { Source = "audio/tone.wav"; }
          @sound Confirm
          {
            Source = "audio/confirm.wav";
            Volume = 0.8;
            @modifier LowPass { Cutoff = ToneCutoff; }
            @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
          }
          @sound Music { Source = "audio/music.ogg"; Loop = true; AutoPlay = true; }
        </TimbreClip>
        """;

    [Fact]
    public void TimbreClipAttachmentAndCommandGenerateWithoutDiagnostics()
    {
        const string markup = """
            <Button Content="Play">
              <Button.Resources>
                <TimbreClip Name="Tone">
                  @sound Tone { Source = "audio/tone.wav"; }
                </TimbreClip>
              </Button.Resources>
              <Button.Aspect>
                @timbre $Tone;
                @on Click { @play $self.timbre.Tone; }
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
                  @sound Chime { Source = "audio/missing-chime.wav"; }
                </TimbreClip>
              </Application.Resources>
            </Application>
            """);
        MarkupFile first = new(
            "First.crn",
            """
            <StackPanel>
              <StackPanel.Resources>
                <TimbreClip Name="Tone">@sound Tone { Source = "audio/missing-tone.wav"; }</TimbreClip>
                <Aspect Name="Clicky" TargetType="Button">@timbre $Tone; @on Click { @play $self.timbre.Tone; }</Aspect>
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
                <Button.Aspect>@timbre $Chime; @on Click { @play $self.timbre.Chime; }</Button.Aspect>
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
        Assert.True(panel.TryFindResource(new ResourceId<TimbreClipDefinition>("Tone"), out TimbreClipDefinition tone));
        Assert.Equal("audio/missing-tone.wav", tone.Sounds["Tone"].Sound.Source.Name);

        // Declaring a clip and attaching it without AutoPlay opens nothing: the
        // Aspects are applied and the missing files are never touched.
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
                <TimbreClip Name="Tone">@sound Tone { Source = "audio/outer.wav"; }</TimbreClip>
              </StackPanel.Resources>
              <Border>
                <Border.Resources>
                  <TimbreClip Name="Tone">@parameter Cut: float = 900; @sound Tone { Source = "audio/inner.wav"; @modifier LowPass { Cutoff = Cut; } }</TimbreClip>
                </Border.Resources>
                <Button Content="Inner">
                  <Button.Aspect>@timbre $Tone(Cut = 400); @on Click { @play $self.timbre.Tone; }</Button.Aspect>
                </Button>
              </Border>
              <ItemsControl>
                @templates
                {
                  <ContentTemplate DataType="System.String">
                    <Border>
                      <Border.Resources>
                        <TimbreClip Name="Item">@sound Item { Source = "audio/item.wav"; }</TimbreClip>
                      </Border.Resources>
                      <Border.Aspect>@timbre $Item; @on Loaded { @play $self.timbre.Item; }</Border.Aspect>
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
        Assert.True(panel.TryFindResource(new ResourceId<TimbreClipDefinition>("Tone"), out TimbreClipDefinition outer));
        Assert.True(inner.TryFindResource(new ResourceId<TimbreClipDefinition>("Tone"), out TimbreClipDefinition shadowing));
        Assert.Equal("audio/outer.wav", outer.Sounds["Tone"].Sound.Source.Name);
        Assert.Equal("audio/inner.wav", shadowing.Sounds["Tone"].Sound.Source.Name);
        Assert.Single(shadowing.Sounds["Tone"].Sound.Parameters);
    }

    private const string TimbreActionAspect = """
        @timbre $Sounds(ToneCutoff = 800);
        @on Click { @play $self.timbre.Confirm; @animate with Tween(200ms, EaseOut) { @to { Opacity = 0.5; } } @play $self.timbre.Tone; }
        @on MouseEnter { @pause $self.timbre.Confirm; @resume $self.timbre.Confirm; @seek $self.timbre.Confirm to 30s; @stop $self.timbre.Confirm; }
        @when $self.IsEnabled { @if value == false { @play $self.timbre.Music; } }
        """;

    [Fact]
    public void TimbreCommandsInFactoryBindToGeneratedMarkupHelpers()
    {
        string markup = "<StackPanel><StackPanel.Resources>" + TimbreClipResources + "</StackPanel.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>" + TimbreActionAspect + "</Button.Aspect></Button>" +
            "<Button Content=\"Templated\"><Button.Aspect>@template { <Border><Border.Aspect>@timbre $Sounds; @on Loaded { @play $self.timbre.Tone; }</Border.Aspect></Border> }</Button.Aspect></Button>" +
            "</StackPanel>";

        GeneratorRunResult result = RunGenerator("TimbreActions.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertTimbreCommandsBindGeneratedMarkupHelpers(compilation, result, hasTemplateAspect: true);
    }

    [Fact]
    public void TimbreCommandsInPairedPartialBindToGeneratedMarkupHelpers()
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
        AssertTimbreCommandsBindGeneratedMarkupHelpers(compilation, result, hasTemplateAspect: false);
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
            $"<{rootType}.Aspect>@timbre $Sounds; @when IsEnabled {{ @play $self.timbre.Tone; }}</{rootType}.Aspect><TextBlock Text=\"Timbre\" /></{rootType}>";

        GeneratorRunResult result = RunPairedGenerator("Views/RootTimbre.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"PlayTimbre\("));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"AttachTimbre\("));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"visualActivation => \{ timbreAction\d+\(\);"));
    }

    // The sound examples of the conceptual guides must stay compilable: their
    // markup runs through the real generator and their C# against the core API.
    [Theory]
    [InlineData("guides/timbre-guide.md")]
    [InlineData("guides/markup-guide.md")]
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

    private static void AssertTimbreCommandsBindGeneratedMarkupHelpers(Compilation compilation, GeneratorRunResult result, bool hasTemplateAspect)
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
        foreach (string helper in new[] { "AttachTimbre", "AttachTimbreSession", "AddTimbreTrigger", "PlayTimbre", "StopTimbre", "PauseTimbre", "ResumeTimbre", "SeekTimbre", "CanStartMotionExecution" })
        {
            Assert.Contains(helper, helpers);
        }

        Assert.Equal(hasTemplateAspect ? 2 : 1, helpers.Count(name => name == "AttachTimbre"));
        Assert.Equal(hasTemplateAspect ? 4 : 3, helpers.Count(name => name == "PlayTimbre"));
        Assert.All(
            calls.Where(symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.UI.Markup.GeneratedMarkup" && symbol.Name.EndsWith("Timbre", StringComparison.Ordinal) && symbol.Name != "AttachTimbre"),
            symbol => Assert.Equal("Cerneala.UI.Elements.UIElement", symbol.Parameters[0].Type.ToDisplayString()));
        Assert.DoesNotContain(calls, symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.TimbreStartOptions");

        // `@timbre $Sounds(ToneCutoff = 800)` passes its arguments by parameter name.
        ObjectCreationExpressionSyntax arguments = Assert.Single(
            tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>(),
            creation => model.GetSymbolInfo(creation).Symbol is IMethodSymbol constructor &&
                constructor.ContainingType.ToDisplayString() == "System.Collections.Generic.Dictionary<string, float>");
        Assert.Contains("[\"ToneCutoff\"] = 800f", arguments.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            tree.GetRoot().DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
                .Select(creation => model.GetSymbolInfo(creation).Symbol)
                .OfType<IMethodSymbol>(),
            constructor => constructor.ContainingType.ToDisplayString() == "Cerneala.UI.Markup.MarkupConditionRule" && constructor.Parameters.Length == 8);
        // Timbre attachments and sessions are lifetimes of the Aspect behavior,
        // disposed when a template element detaches, never registered on the
        // template context.
        Assert.DoesNotContain(calls, symbol => symbol.Name == "RegisterLifetime");

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
        IAssemblySymbol core = compilation.GetTypeByMetadataName("Cerneala.Timbre.TimbreSound")!.ContainingAssembly;
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
                "Cerneala.Timbre.TimbreClipDefinition",
                "Cerneala.Timbre.TimbreClipSound",
                "Cerneala.Timbre.TimbreInput<float>",
                "Cerneala.Timbre.TimbreParameter<float>",
                "Cerneala.Timbre.TimbreSound"
            ],
            constructed);
        Assert.All(constructors, symbol => Assert.Equal(core, symbol.ContainingAssembly, SymbolEqualityComparer.Default));
        Assert.Equal(1, constructors.Count(symbol => symbol.ContainingType.Name == "TimbreClipDefinition"));
        Assert.Equal(3, constructors.Count(symbol => symbol.ContainingType.Name == "TimbreClipSound"));
        Assert.Equal(3, constructors.Count(symbol => symbol.ContainingType.Name == "TimbreSound"));

        IMethodSymbol[] invocations = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>()
            .ToArray();
        Assert.Equal(3, invocations.Count(symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.TimbreSource" && symbol.Name == "FromFile"));
        Assert.Equal(1, invocations.Count(symbol =>
            symbol.Name == "SetResource" &&
            symbol.TypeArguments.SingleOrDefault()?.ToDisplayString() == "Cerneala.Timbre.TimbreClipDefinition"));

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
        TimbreSound manualConfirm = new(
            "audio/confirm.wav",
            volume: 0.8f,
            parameters: [toneCutoff, echoMix],
            modifiers: [new LowPass(cutoff: toneCutoff), new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
        Assert.True(owner.TryFindResource(new ResourceId<TimbreClipDefinition>("Sounds"), out TimbreClipDefinition clip));
        Assert.Equal("Sounds", clip.Name);
        Assert.Equal(["ToneCutoff", "EchoMix"], clip.Parameters.Select(parameter => parameter.Name));
        Assert.Equal(["Tone", "Confirm", "Music"], clip.Sounds.Keys);
        Assert.Equal([false, false, true], clip.Sounds.Values.Select(sound => sound.AutoPlay));
        AssertSameDefinition(new TimbreSound("audio/tone.wav"), clip.Sounds["Tone"].Sound);
        AssertSameDefinition(manualConfirm, clip.Sounds["Confirm"].Sound);
        AssertSameDefinition(new TimbreSound("audio/music.ogg", loop: true), clip.Sounds["Music"].Sound);
        Assert.Same(clip.Parameters[0], clip.Sounds["Confirm"].Sound.Parameters[0]);
    }

    private static void AssertSameDefinition(TimbreSound expected, TimbreSound actual)
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

    private static IEnumerable<string> DescribeModifiers(TimbreSound sound) => sound.Modifiers.Select(modifier => modifier switch
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
