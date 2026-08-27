using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private static UIRoot AttachAndProcess(UIElement element, int frameCount = 2)
    {
        UIRoot root = new();
        root.VisualChildren.Add(element);
        for (int frame = 0; frame < frameCount; frame++)
        {
            root.ProcessFrame();
        }

        return root;
    }

    [Fact]
    public void GeneratedScopedNamedAndInlineAspectsLowerToCommonRuntimeTypes()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @default { Opacity = 0.40; }
                </Aspect>
                <Aspect Name="Named" TargetType="Button">
                  @default { Opacity = 0.60; }
                </Aspect>
              </StackPanel.Resources>
              <Button Aspect="$Named" />
              <Button>
                <Button.Aspect>
                  @default { Opacity = 0.80; }
                </Button.Aspect>
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("UnifiedAspects.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);

        Assert.Contains("global::Cerneala.UI.Aspect.AspectPackage", source, StringComparison.Ordinal);
        Assert.Contains("global::Cerneala.UI.Aspect.AspectRuleSet", source, StringComparison.Ordinal);
        Assert.Contains("global::Cerneala.UI.Aspect.ElementAspect", source, StringComparison.Ordinal);
        Assert.DoesNotContain("global::Cerneala.UI.Markup.MarkupAspectResource", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".ApplyTo(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.LocalAspectBase", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.LocalAspectConditional", source, StringComparison.Ordinal);
        Assert.Empty(compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.UnifiedAspectsFactory"));
        Button namedButton = Assert.IsType<Button>(panel.VisualChildren[0]);
        Button inlineButton = Assert.IsType<Button>(panel.VisualChildren[1]);
        UIRoot root = AttachAndProcess(panel);
        AspectOrigin namedOrigin = Assert.Single(root.AspectProcessor.Engine
            .GetDiagnostics(namedButton)
            .ResolutionSteps
            .Where(step => step.Origin.Kind == AspectAuthoringKind.MarkupNamed))
            .Origin;
        AspectOrigin inlineOrigin = Assert.Single(root.AspectProcessor.Engine
            .GetDiagnostics(inlineButton)
            .ResolutionSteps
            .Where(step => step.Origin.Kind == AspectAuthoringKind.MarkupInline))
            .Origin;
        Assert.Equal(("UnifiedAspects.crn", "Named"), (namedOrigin.Document, namedOrigin.Name));
        Assert.Equal("UnifiedAspects.crn", inlineOrigin.Document);
    }

    [Fact]
    public void GeneratedAspectConditionsUseCommonAspectConditionsInsteadOfStyleWrites()
    {
        const string markup = """
            <Button>
              <Button.Aspect>
                @default { Opacity = 0.40; }
                @when IsMouseOver
                {
                  Opacity = 0.80;
                }
              </Button.Aspect>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("UnifiedConditions.crn", markup, out _);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);

        Assert.Contains("global::Cerneala.UI.Aspect.AspectCondition", source, StringComparison.Ordinal);
        Assert.Contains("global::Cerneala.UI.Aspect.AspectConditionKey", source, StringComparison.Ordinal);
        Assert.Contains(".SetActive(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::Cerneala.UI.Markup.MarkupConditionalValue(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.LocalAspectConditional", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.ApplicationAspectVisualState", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedApplicationAspectIsAPackageWithoutExecutableMarkupResource()
    {
        const string markup = """
            <Application StartupWindow="ShellWindow">
              <Application.Resources>
                <Aspect TargetType="Button">
                  @default { Opacity = 0.42; }
                </Aspect>
              </Application.Resources>
            </Application>
            """;

        GeneratorRunResult result = RunApplicationGenerator(
            markup,
            ApplicationInput,
            OutputKind.WindowsApplication,
            out _);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);

        Assert.Contains("global::Cerneala.UI.Aspect.AspectPackage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("global::Cerneala.UI.Markup.MarkupAspectResource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.ApplicationAspectBase", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UiPropertyValueSource.ApplicationAspectVisualState", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedScopedConditionalAspectOwnsItsSidecarThroughThePackage()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @when IsMouseOver { Opacity = 0.80; }
                </Aspect>
              </StackPanel.Resources>
              <Button />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("UnifiedPackageBehavior.crn", markup, out _);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);

        Assert.Contains("components.AddBehavior(new global::Cerneala.UI.Aspect.AspectBehavior", source, StringComparison.Ordinal);
        Assert.DoesNotContain("global::Cerneala.UI.Markup.MarkupAspectResource", source, StringComparison.Ordinal);
        Assert.DoesNotContain("__cerneala.aspect.behavior", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedPackageBehaviorObservesRuntimeCreatedMatchingControls()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @when IsMouseOver { Opacity = 0.80; }
                </Aspect>
              </StackPanel.Resources>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("RuntimePackageBehavior.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.RuntimePackageBehaviorFactory"));
        UIRoot root = AttachAndProcess(panel);
        Button runtimeButton = new() { IsPointerOver = true };

        panel.VisualChildren.Add(runtimeButton);
        root.ProcessFrame();

        Assert.Equal(0.80f, runtimeButton.Opacity);
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.AspectBase, runtimeButton.GetValueSource(UIElement.OpacityProperty));
    }

    [Fact]
    public void CodeAndMarkupDiagnosticsUseTheSameCascadeAndRejectionSemantics()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @default { Opacity = 0.40; }
                  @when IsMouseOver { Opacity = 0.80; }
                </Aspect>
              </StackPanel.Resources>
              <Button />
            </StackPanel>
            """;
        GeneratorRunResult result = RunGenerator("DiagnosticParity.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel markupPanel = Assert.IsType<StackPanel>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.DiagnosticParityFactory"));
        Button markupButton = Assert.IsType<Button>(Assert.Single(markupPanel.VisualChildren));
        UIRoot markupRoot = AttachAndProcess(markupPanel);
        AspectResolutionStep[] markupSteps = markupRoot.AspectProcessor.Engine
            .GetDiagnostics(markupButton)
            .ResolutionSteps
            .Where(step => string.Equals(step.Origin.Document, "DiagnosticParity.crn", StringComparison.Ordinal))
            .ToArray();

        AspectConditionKey conditionKey = new("code.hover");
        AspectPackage codePackage = AspectPackage.Create("Code.DiagnosticParity")
            .Components(components =>
            {
                components.AddRule(new AspectRuleSet(
                    "base",
                    AspectLayer.App,
                    new AspectTarget(typeof(Button)),
                    [new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.40f))],
                    0));
                components.AddRule(new AspectRuleSet(
                    "hover",
                    AspectLayer.App,
                    new AspectTarget(typeof(Button), conditions: [AspectCondition.Signal(conditionKey)]),
                    [new AspectDeclaration(UIElement.OpacityProperty, AspectValue<float>.Literal(0.80f))],
                    1));
            });
        StackPanel codePanel = new();
        codePanel.Resources["Aspect"] = codePackage;
        Button codeButton = new();
        codePanel.VisualChildren.Add(codeButton);
        UIRoot codeRoot = AttachAndProcess(codePanel);
        AspectResolutionStep[] codeSteps = codeRoot.AspectProcessor.Engine
            .GetDiagnostics(codeButton)
            .ResolutionSteps
            .Where(step => string.Equals(step.PackageName, "Code.DiagnosticParity", StringComparison.Ordinal))
            .ToArray();

        Assert.Equal(AspectAuthoringKind.MarkupDefault, Assert.Single(markupSteps.Select(step => step.Origin.Kind).Distinct()));
        Assert.Equal("DiagnosticParity.crn", Assert.Single(markupSteps.Select(step => step.Origin.Document).Distinct()));
        Assert.Equal(AspectAuthoringKind.Code, Assert.Single(codeSteps.Select(step => step.Origin.Kind).Distinct()));
        Assert.Equal(
            codeSteps.Select(SemanticDiagnosticSignature),
            markupSteps.Select(SemanticDiagnosticSignature));
    }

    [Fact]
    public void AspectResourceReferenceRecomputesThroughEngineWhenScopeResourceChanges()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="#FFFF0000" />
                <Aspect TargetType="Button">
                  @default { Background = $Accent; }
                </Aspect>
              </StackPanel.Resources>
              <Button />
            </StackPanel>
            """;
        GeneratorRunResult result = RunGenerator("ReactiveAspectResource.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.ReactiveAspectResourceFactory"));
        Button button = Assert.IsType<Button>(Assert.Single(panel.VisualChildren));
        UIRoot root = AttachAndProcess(panel);
        Assert.Equal(Color.Red, Assert.IsType<SolidColorBrush>(button.Background).Color);

        panel.Resources["Accent"] = new SolidColorBrush(Color.Blue);
        root.ProcessFrame();

        Assert.Equal(Color.Blue, Assert.IsType<SolidColorBrush>(button.Background).Color);
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        Assert.False(root.ProcessFrame().HasWork);
    }

    private static string SemanticDiagnosticSignature(AspectResolutionStep step)
    {
        return string.Join(
            "|",
            step.Target,
            step.Layer.Order,
            step.Specificity,
            step.DeclarationOrder,
            step.SourceOrder,
            step.Scope,
            step.Outcome,
            string.Join(",", step.Conditions.Select(condition => condition.Matches)),
            string.Join(",", step.Dependencies.Select(dependency => dependency.Kind).Order()));
    }

    [Fact]
    public void DataConditionInvalidatesEngineAndUnsubscribesOnDetach()
    {
        const string input = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class State : INotifyPropertyChanged
            {
                private bool active;
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Active
                {
                    get => active;
                    set
                    {
                        active = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active)));
                    }
                }
            }
            """;
        const string markup = """
            <Button DataType="TestInput.State">
              <Button.Aspect>
                @default { Background = Black; }
                @when $DataContext.Active { Background = White; }
              </Button.Aspect>
            </Button>
            """;
        GeneratorRunResult result = RunGeneratorWithInput(
            "DataConditionAspect.crn",
            markup,
            input,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        System.Reflection.Assembly assembly = System.Reflection.Assembly.Load(stream.ToArray());
        object state = Activator.CreateInstance(assembly.GetType("TestInput.State", throwOnError: true)!)!;
        Type factory = assembly.GetType(
            "Cerneala.GeneratedUi.DataConditionAspectFactory",
            throwOnError: true)!;
        System.Reflection.MethodInfo create = factory.GetMethods(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Single(method => method.Name == "Create" && method.GetParameters().Length == 1);
        Button button = Assert.IsType<Button>(create.Invoke(null, [state]));
        UIRoot root = AttachAndProcess(button);
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(button.Background).Color);

        state.GetType().GetProperty("Active")!.SetValue(state, true);
        root.ProcessFrame();
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(button.Background).Color);

        root.VisualChildren.Remove(button);
        root.ProcessFrame();
        state.GetType().GetProperty("Active")!.SetValue(state, false);
        Assert.False(root.ProcessFrame().HasWork);
    }

    [Fact]
    public void AspectInsideTemplateObservesTemplateOwnerThroughEngineSignal()
    {
        const string markup = """
            <Button IsEnabled="True">
              @template
              {
                <Border>
                  <Border.Resources>
                    <Aspect Name="Inner" TargetType="TextBlock">
                      @default { Foreground = Black; }
                      @when $owner.IsEnabled { Foreground = White; }
                    </Aspect>
                  </Border.Resources>
                  <TextBlock Aspect="$Inner" Text="Owner state" />
                </Border>
              }
            </Button>
            """;
        GeneratorRunResult result = RunGenerator(
            "TemplateOwnerAspectCondition.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string source = SingleGeneratedSource(result);
        Assert.Contains("global::Cerneala.UI.Aspect.ElementAspectCondition", source, StringComparison.Ordinal);
        Assert.Contains("global::Cerneala.UI.Aspect.AspectConditionKey", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new global::Cerneala.UI.Markup.MarkupConditionalValue(", source, StringComparison.Ordinal);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Button button = Assert.IsType<Button>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.TemplateOwnerAspectConditionFactory"));
        UIRoot root = AttachAndProcess(button);
        Border chrome = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        TextBlock text = Assert.IsType<TextBlock>(chrome.Child);
        Assert.Equal(Color.White, Assert.IsType<SolidColorBrush>(text.Foreground).Color);

        button.IsEnabled = false;
        root.ProcessFrame();

        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(text.Foreground).Color);
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.AspectBase, text.GetValueSource(Control.ForegroundProperty));
    }
}
