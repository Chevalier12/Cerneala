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
    private const string SoundClipResources = """
        <SoundClip Name="Tone">
          Source = "audio/tone.wav";
        </SoundClip>
        <SoundClip Name="ConfirmSound">
          Source = "audio/confirm.wav";
          Volume = 0.8;
          @parameter ToneCutoff: float = 1200;
          @parameter EchoMix: float = 0.15;
          @modifier LowPass { Cutoff = ToneCutoff; }
          @modifier Delay { Time = 120ms; Feedback = 0.20; Mix = EchoMix; }
        </SoundClip>
        <SoundClip Name="Music">
          Source = "audio/music.ogg";
          Loop = true;
        </SoundClip>
        """;

    [Fact]
    public void SoundClipAndEventSoundActionGenerateWithoutDiagnostics()
    {
        const string markup = """
            <Button Content="Play">
              <Button.Resources>
                <SoundClip Name="Tone">
                  Source = "audio/tone.wav";
                </SoundClip>
              </Button.Resources>
              <Button.Aspect>
                @on Click { @sound $Tone; }
              </Button.Aspect>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("SoundMinimal.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
    }

    [Fact]
    public void SoundClipFactoryResourcesBindToCoreConstructors()
    {
        string markup = "<StackPanel><StackPanel.Resources>" + SoundClipResources + "</StackPanel.Resources></StackPanel>";

        GeneratorRunResult result = RunGenerator("SoundFactory.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertSoundClipLoweringBindsCoreApi(compilation, result);
        UIElement panel = InvokeCreate(Emit(compilation), "Cerneala.GeneratedUi.SoundFactoryFactory");
        AssertSoundClipResourcesMatchManualDefinitions(panel);
    }

    [Fact]
    public void SoundClipPairedUserControlResourcesBindToCoreConstructors()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class SoundPanel : UserControl { }
            """;
        string markup = "<UserControl><UserControl.Resources>" + SoundClipResources +
            "</UserControl.Resources><TextBlock Text=\"Sounds\" /></UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/SoundPanel.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertSoundClipLoweringBindsCoreApi(compilation, result);
        Assembly assembly = Assembly.Load(Emit(compilation).ToArray());
        UserControl view = Assert.IsAssignableFrom<UserControl>(
            Activator.CreateInstance(assembly.GetType("TestInput.Views.SoundPanel", throwOnError: true)!));
        AssertSoundClipResourcesMatchManualDefinitions(view);
    }

    [Fact]
    public void SoundClipReferencedFromTwoElementsAndWindowsCompilesWithoutPlaybackOrIo()
    {
        MarkupFile application = new(
            "App.crn",
            """
            <Application StartupWindow="ShellWindow">
              <Application.Resources>
                <SoundClip Name="Chime">
                  Source = "audio/missing-chime.wav";
                </SoundClip>
              </Application.Resources>
            </Application>
            """);
        MarkupFile first = new(
            "First.crn",
            """
            <StackPanel>
              <StackPanel.Resources>
                <SoundClip Name="Tone">Source = "audio/missing-tone.wav";</SoundClip>
                <Aspect Name="Clicky" TargetType="Button">@on Click { @sound $Tone; @sound $Chime; }</Aspect>
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
                <Button.Aspect>@on Click { @sound $Chime; }</Button.Aspect>
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
        Assert.True(panel.TryFindResource(new ResourceId<SoundClip>("Tone"), out SoundClip tone));
        Assert.Equal("audio/missing-tone.wav", tone.Source.Name);

        // Declaring and referencing a clip opens nothing: the factory ran and
        // the missing files were never touched.
        CountingSoundOutput output = new();
        using SoundRuntime runtime = new(new SoundRuntimeOptions { Output = output });
        UIRoot root = new();
        root.SetSoundRuntime(runtime);
        root.VisualChildren.Add(panel);
        Assert.Equal(0, output.OpenCount);
        Assert.Equal(0, root.Detective.CaptureSound()!.ActivePlaybacks);
        Assert.Equal(0, root.Detective.CaptureSound()!.PlaybacksFailed);
    }

    [Fact]
    public void SoundClipInsideTemplatesAndShadowingScopesCompiles()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SoundClip Name="Tone">Source = "audio/outer.wav";</SoundClip>
              </StackPanel.Resources>
              <Border>
                <Border.Resources>
                  <SoundClip Name="Tone">Source = "audio/inner.wav"; @parameter Cut: float = 900; @modifier LowPass { Cutoff = Cut; }</SoundClip>
                </Border.Resources>
                <Button Content="Inner">
                  <Button.Aspect>@on Click { @sound $Tone(Cut = 400); }</Button.Aspect>
                </Button>
              </Border>
              <ItemsControl>
                @templates
                {
                  <ContentTemplate DataType="System.String">
                    <Border>
                      <Border.Resources>
                        <SoundClip Name="Item">Source = "audio/item.wav";</SoundClip>
                      </Border.Resources>
                      <Border.Aspect>@on Loaded { @sound $Item; }</Border.Aspect>
                    </Border>
                  </ContentTemplate>
                }
              </ItemsControl>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("SoundScopes.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        UIElement panel = InvokeCreate(Emit(compilation), "Cerneala.GeneratedUi.SoundScopesFactory");
        Border inner = Assert.IsType<Border>(panel.VisualChildren[0]);
        Assert.True(panel.TryFindResource(new ResourceId<SoundClip>("Tone"), out SoundClip outer));
        Assert.True(inner.TryFindResource(new ResourceId<SoundClip>("Tone"), out SoundClip shadowing));
        Assert.Equal("audio/outer.wav", outer.Source.Name);
        Assert.Equal("audio/inner.wav", shadowing.Source.Name);
        Assert.Single(shadowing.Parameters);
    }

    private const string SoundActionAspect = """
        @handle Playback;
        @on Click { @sound $ConfirmSound(Volume = 0.2, Loop = true, ToneCutoff = 800) as Playback; @animate with Tween(200ms, EaseOut) { @to { Opacity = 0.5; } } @sound $Tone; }
        @on MouseEnter { @pause Playback; @resume Playback; @seek Playback to 30s; @cancel Playback; }
        @when $self.IsEnabled { @if value == false { @sound $Music as Playback; } }
        """;

    [Fact]
    public void SoundActionsInFactoryBindToGeneratedMarkupHelpersAndCoreStartOptions()
    {
        string markup = "<StackPanel><StackPanel.Resources>" + SoundClipResources + "</StackPanel.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>" + SoundActionAspect + "</Button.Aspect></Button>" +
            "<Button Content=\"Templated\"><Button.Aspect>@template { <Border><Border.Aspect>@on Loaded { @sound $Tone; }</Border.Aspect></Border> }</Button.Aspect></Button>" +
            "</StackPanel>";

        GeneratorRunResult result = RunGenerator("SoundActions.crn", markup, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertSoundActionsBindCoreOperations(compilation, result, expectTemplateLifetime: true);
    }

    [Fact]
    public void SoundActionsInPairedPartialBindToGeneratedMarkupHelpersAndCoreStartOptions()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class SoundActionsView : UserControl { }
            """;
        string markup = "<UserControl><UserControl.Resources>" + SoundClipResources + "</UserControl.Resources>" +
            "<Button Content=\"Play\"><Button.Aspect>" + SoundActionAspect + "</Button.Aspect></Button></UserControl>";

        GeneratorRunResult result = RunPairedGenerator("Views/SoundActionsView.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        AssertSoundActionsBindCoreOperations(compilation, result, expectTemplateLifetime: false);
    }

    [Theory]
    [InlineData("UserControl")]
    [InlineData("Window")]
    public void PairedRootAspectEmitsEachReactiveSoundActivationOnce(string rootType)
    {
        string inputSource = $$"""
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class RootSound : {{rootType}} { }
            """;
        string markup = $"<{rootType}><{rootType}.Resources>" + SoundClipResources + $"</{rootType}.Resources>" +
            $"<{rootType}.Aspect>@when IsEnabled {{ @sound $Tone; }}</{rootType}.Aspect><TextBlock Text=\"Sounds\" /></{rootType}>";

        GeneratorRunResult result = RunPairedGenerator("Views/RootSound.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        string generated = string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString()));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"PlaySound\("));
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(generated, @"visualActivation => \{ soundAction\d+\(\);"));
    }

    // The sound examples of the conceptual guides must stay compilable: their
    // markup runs through the real generator and their C# against the core API.
    [Theory]
    [InlineData("timbre-guide.md")]
    [InlineData("CernealaMarkupGuide.md")]
    public void DocumentedSoundExamplesCompile(string guide)
    {
        string text = File.ReadAllText(Path.Combine(DocumentationRoot(), guide)).Replace("\r\n", "\n");
        string[] xml = System.Text.RegularExpressions.Regex.Matches(text, "```xml\n(.*?)```", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(block => block.Contains("SoundClip", StringComparison.Ordinal) || block.Contains("@sound", StringComparison.Ordinal))
            .ToArray();
        string[] csharp = System.Text.RegularExpressions.Regex.Matches(text, "```csharp\n(.*?)```", System.Text.RegularExpressions.RegexOptions.Singleline)
            .Select(match => match.Groups[1].Value)
            .Where(block => block.Contains("SoundClip", StringComparison.Ordinal) || block.Contains("Sounds.", StringComparison.Ordinal))
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
            "using Cerneala.Timbre;\nusing Cerneala.UI.Controls;\nnamespace TestInput.Views;\n" +
            "public partial class GuideView : UserControl\n{\n" +
            string.Concat(csharp.Select((block, index) => $"    private static void Example{index}(Button button)\n    {{\n{block}\n    }}\n")) +
            "}\n";

        GeneratorRunResult result = RunPairedGenerator("Views/GuideView.crn", markup, inputSource, out Compilation compilation);

        AssertNoGeneratorOrCompilationErrors(result, compilation);
        Assert.Contains("PlaySound(", string.Concat(result.GeneratedSources.Select(source => source.SourceText.ToString())), StringComparison.Ordinal);
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

    private static void AssertSoundActionsBindCoreOperations(Compilation compilation, GeneratorRunResult result, bool expectTemplateLifetime)
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
        foreach (string helper in new[] { "AttachSoundSession", "AddSoundTrigger", "PlaySound", "GetSoundParameter", "PauseSound", "ResumeSound", "SeekSound", "CancelSound", "CanStartMotionExecution" })
        {
            Assert.Contains(helper, helpers);
        }

        Assert.Equal(expectTemplateLifetime ? 4 : 3, helpers.Count(name => name == "PlaySound"));
        IMethodSymbol set = Assert.Single(calls, symbol => symbol.Name == "Set" && symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.SoundStartOptions");
        Assert.Equal("float", set.TypeArguments.Single().ToDisplayString());
        IPropertySymbol[] assigned = tree.GetRoot().DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Select(assignment => model.GetSymbolInfo(assignment.Left).Symbol)
            .OfType<IPropertySymbol>()
            .Where(property => property.ContainingType.ToDisplayString() == "Cerneala.Timbre.SoundStartOptions")
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
        Assert.DoesNotContain("SoundPlayback ", generated, StringComparison.Ordinal);
    }

    private static void AssertSoundClipLoweringBindsCoreApi(Compilation compilation, GeneratorRunResult result)
    {
        SyntaxTree tree = compilation.SyntaxTrees.Single(candidate =>
            result.GeneratedSources.Any(source => candidate.FilePath.EndsWith(source.HintName, StringComparison.Ordinal)));
        SemanticModel model = compilation.GetSemanticModel(tree);
        IAssemblySymbol core = compilation.GetTypeByMetadataName("Cerneala.Timbre.SoundClip")!.ContainingAssembly;
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
                "Cerneala.Timbre.SoundClip",
                "Cerneala.Timbre.SoundInput<float>",
                "Cerneala.Timbre.SoundParameter<float>"
            ],
            constructed);
        Assert.All(constructors, symbol => Assert.Equal(core, symbol.ContainingAssembly, SymbolEqualityComparer.Default));
        Assert.Equal(3, constructors.Count(symbol => symbol.ContainingType.Name == "SoundClip"));

        IMethodSymbol[] invocations = tree.GetRoot().DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Select(invocation => model.GetSymbolInfo(invocation).Symbol)
            .OfType<IMethodSymbol>()
            .ToArray();
        Assert.Equal(3, invocations.Count(symbol => symbol.ContainingType.ToDisplayString() == "Cerneala.Timbre.SoundSource" && symbol.Name == "FromFile"));
        Assert.Equal(3, invocations.Count(symbol =>
            symbol.Name == "SetResource" &&
            symbol.TypeArguments.SingleOrDefault()?.ToDisplayString() == "Cerneala.Timbre.SoundClip"));

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

    private static void AssertSoundClipResourcesMatchManualDefinitions(UIElement owner)
    {
        SoundParameter<float> toneCutoff = new("ToneCutoff", 1200f);
        SoundParameter<float> echoMix = new("EchoMix", 0.15f);
        SoundClip manualConfirm = new(
            "audio/confirm.wav",
            volume: 0.8f,
            parameters: [toneCutoff, echoMix],
            modifiers: [new LowPass(cutoff: toneCutoff), new Delay(time: 0.12f, feedback: 0.20f, mix: echoMix)]);
        AssertSameDefinition(new SoundClip("audio/tone.wav"), Resource(owner, "Tone"));
        AssertSameDefinition(manualConfirm, Resource(owner, "ConfirmSound"));
        AssertSameDefinition(new SoundClip("audio/music.ogg", loop: true), Resource(owner, "Music"));
    }

    private static SoundClip Resource(UIElement owner, string name)
    {
        Assert.True(owner.TryFindResource(new ResourceId<SoundClip>(name), out SoundClip clip), name);
        return clip;
    }

    private static void AssertSameDefinition(SoundClip expected, SoundClip actual)
    {
        Assert.Equal(expected.Source.Name, actual.Source.Name);
        Assert.Equal(expected.Volume, actual.Volume);
        Assert.Equal(expected.Loop, actual.Loop);
        Assert.Equal(expected.Loading, actual.Loading);
        Assert.Equal(
            expected.Parameters.Cast<SoundParameter<float>>().Select(parameter => (parameter.Name, parameter.DefaultValue)),
            actual.Parameters.Cast<SoundParameter<float>>().Select(parameter => (parameter.Name, parameter.DefaultValue)));
        Assert.Equal(DescribeModifiers(expected), DescribeModifiers(actual));
    }

    private static IEnumerable<string> DescribeModifiers(SoundClip clip) => clip.Modifiers.Select(modifier => modifier switch
    {
        LowPass lowPass => "LowPass(" + DescribeInput(lowPass.Cutoff) + ")",
        Delay delay => "Delay(" + DescribeInput(delay.Time) + ", " + DescribeInput(delay.Feedback) + ", " + DescribeInput(delay.Mix) + ")",
        _ => modifier.GetType().Name
    });

    private static string DescribeInput(SoundInput<float> input) =>
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

    private sealed class CountingSoundOutput : ISoundOutput
    {
        public int OpenCount { get; private set; }

        public int QueuedFrames => 0;

        public void Open(ISoundOutputClient client) => OpenCount++;

        public void Submit(ReadOnlySpan<float> samples)
        {
        }

        public void Close()
        {
        }
    }
}
