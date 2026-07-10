using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cerneala.SourceGen;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Cerneala.Tests.SourceGen;

public sealed class UiMarkupGeneratorTests
{
    [Fact]
    public void TemplatePartPathEmitsReactivePartObservation()
    {
        const string markup = """
            <StackPanel>
              <Button Name="Host">
                @template
                {
                  <Border Name="Chrome" IsEnabled="True" />
                }
              </Button>
              <TextBlock Text="pending">
                @when $Host.parts.$Chrome.IsEnabled
                {
                  @if value == True
                  {
                    Text = "matched";
                  }
                }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("part-path.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generatedSource = SingleGeneratedSource(result);
        Assert.Contains("ObserveTemplatePartProperty(Host, \"Chrome\"", generatedSource);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.PartPathFactory"));
        Button host = Assert.IsType<Button>(panel.VisualChildren[0]);
        TextBlock status = Assert.IsType<TextBlock>(panel.VisualChildren[1]);
        Assert.Equal("matched", status.Text);

        Border chrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);
        chrome.IsEnabled = false;
        Assert.Equal("pending", status.Text);

        host.ComponentTemplate = new Cerneala.UI.Controls.Templates.ComponentTemplate<Button>(
            "replacement",
            context =>
            {
                Border replacement = new() { IsEnabled = false };
                context.RequirePart("Chrome", replacement);
                return replacement;
            });
        Border replacementChrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);
        replacementChrome.IsEnabled = true;
        Assert.Equal("matched", status.Text);
    }

    [Fact]
    public void SupportedMarkupEmitsCompilableFactory()
    {
        const string markup = """
            <StackPanel>
              <TextBlock Text="Hello" FontSize="18" />
              <Button Content="Go" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("Sample.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public static partial class SampleFactory", generatedSource);
        Assert.Contains("global::Cerneala.UI.Controls.StackPanel", generatedSource);
        Assert.Contains(".Text = \"Hello\";", generatedSource);
        Assert.Contains(".Content = \"Go\";", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        UIElement root = InvokeCreate(stream, "Cerneala.GeneratedUi.SampleFactory");
        StackPanel panel = Assert.IsType<StackPanel>(root);
        Assert.Equal(2, panel.VisualChildren.Count);
        TextBlock text = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Assert.Equal("Hello", text.Text);
        Assert.Equal(18, text.FontSize);
        Button button = Assert.IsType<Button>(panel.VisualChildren[1]);
        Assert.Equal("Go", button.Content);
    }

    [Fact]
    public void GeneratedSourceUsesPublicTypedPropertiesWithoutRuntimeMarkupParser()
    {
        const string markup = """
            <Border BorderColor="white" BorderThickness="1" Padding="4">
              <TextBlock Text="Typed" Foreground="0, 0, 0" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("typed-view.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.Contains("public static partial class TypedViewFactory", generatedSource);
        Assert.Contains(".BorderColor = global::Cerneala.Drawing.DrawColor.White;", generatedSource);
        Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
        Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(4f);", generatedSource);
        Assert.Contains(".Foreground = new global::Cerneala.Drawing.DrawColor(0, 0, 0);", generatedSource);
        Assert.DoesNotContain("UiMarkupReader", generatedSource);
        Assert.DoesNotContain("UiMarkupParser", generatedSource);
        Assert.DoesNotContain("UiMarkupSerializer", generatedSource);
        Assert.DoesNotContain("SetValue(", generatedSource);
        Assert.DoesNotContain("propertyStore", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.TypedViewFactory"));
        Assert.Equal(1, border.BorderThickness.Left);
        Assert.Equal(4, border.Padding.Left);
        TextBlock child = Assert.IsType<TextBlock>(border.Child);
        Assert.Equal("Typed", child.Text);
    }

    [Fact]
    public void RefactoredPropertySpecsPreserveExistingDirectAssignments()
    {
        const string markup = """
            <Border Background="White" BorderColor="0, 1, 2, 3" BorderThickness="1" Padding="2">
              <TextBlock Text="Typed" FontFamily="Consolas" FontSize="12" Foreground="Black" Margin="1,2,3,4" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("DirectAssignments.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(".Background = global::Cerneala.Drawing.DrawColor.White;", generatedSource);
        Assert.Contains(".BorderColor = new global::Cerneala.Drawing.DrawColor(0, 1, 2, 3);", generatedSource);
        Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
        Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(2f);", generatedSource);
        Assert.Contains(".FontFamily = \"Consolas\";", generatedSource);
        Assert.Contains(".FontSize = 12f;", generatedSource);
        Assert.Contains(".Foreground = global::Cerneala.Drawing.DrawColor.Black;", generatedSource);
        Assert.Contains(".Margin = new global::Cerneala.UI.Layout.Thickness(1f, 2f, 3f, 4f);", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void ElementResourcesDoNotEmitVisualChildren()
    {
        const string markup = """
            <TextBlock Text="Hello">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("ResourceFragment.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public static partial class ResourceFragmentFactory", generatedSource);
        Assert.DoesNotContain("global::Cerneala.UI.Controls.Resources", generatedSource);
        Assert.Contains("global::Cerneala.UI.Controls.TextBlock", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.ResourceFragmentFactory"));
        Assert.Equal("Hello", root.Text);
        SolidColorBrush brush = root.FindResource<SolidColorBrush>("PulseColor");
        Assert.Same(brush, root.Resources["PulseColor"]);
        Assert.Equal(new Cerneala.Drawing.DrawColor(255, 93, 115), brush.Color);
    }

    [Fact]
    public void GeneratedResourcesAreStoredOnTheirActualOwnerAndFollowRuntimeLookup()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="#FFFF0000" />
              </StackPanel.Resources>
              <Border>
                <Border.Resources>
                  <SolidColorBrush Name="Accent" Color="#FF00FF00" />
                  <Aspect Name="Card" Target="Border">
                    @default { Background = $Accent; }
                  </Aspect>
                </Border.Resources>
                <TextBlock Text="Inner" />
              </Border>
              <TextBlock Text="Outer" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("RuntimeResources.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.RuntimeResourcesFactory"));
        Border border = Assert.IsType<Border>(panel.VisualChildren[0]);
        TextBlock inner = Assert.IsType<TextBlock>(border.Child);
        TextBlock outer = Assert.IsType<TextBlock>(panel.VisualChildren[1]);

        Assert.Equal(new Cerneala.Drawing.DrawColor(0, 255, 0), inner.FindResource<SolidColorBrush>("Accent").Color);
        Assert.Equal(new Cerneala.Drawing.DrawColor(255, 0, 0), outer.FindResource<SolidColorBrush>("Accent").Color);
        MarkupAspectResource aspect = border.FindResource<MarkupAspectResource>("Card");
        Assert.Equal(typeof(Border), aspect.TargetType);
        Assert.Equal(new[] { "Background" }, aspect.DefaultPropertyNames);
        Assert.Single(panel.Resources);
        Assert.Equal(2, border.Resources.Count);
    }

    [Fact]
    public void TopLevelResourcesReportsMigrationDiagnostic()
    {
        const string markup = """
            <Resources>
              <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
            </Resources>
            <TextBlock Text="Hello" />
            """;

        GeneratorRunResult result = RunGenerator("TopLevelResources.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "TopLevelResources.cui.xml");
        Assert.Contains("<RootType.Resources>", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void LiteralRelationalComparatorsCompileInsideXmlText()
    {
        const string markup = """
            <StackPanel>
              <TextBlock FontSize="12">
                @when FontSize { @if value < 13 { Text = "lt"; } }
              </TextBlock>
              <TextBlock FontSize="12">
                @when FontSize { @if value <= 12 { Text = "lte"; } }
              </TextBlock>
              <TextBlock FontSize="12">
                @when FontSize { @if value > 11 { Text = "gt"; } }
              </TextBlock>
              <TextBlock FontSize="12">
                @when FontSize { @if value >= 12 { Text = "gte"; } }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("KeywordComparators.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.KeywordComparatorsFactory"));
        Assert.Equal(new[] { "lt", "lte", "gt", "gte" }, panel.VisualChildren.Cast<TextBlock>().Select(text => text.Text));
    }

    [Fact]
    public void NearestElementResourceScopeShadowsAncestorResources()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="#FFFF0000" />
                <Aspect Name="Label" Target="TextBlock">
                  @default { Foreground = $Accent; }
                </Aspect>
              </StackPanel.Resources>

              <TextBlock Aspect="$Label" Text="Outer" />
              <Border>
                <Border.Resources>
                  <SolidColorBrush Name="Accent" Color="#FF00FF00" />
                  <Aspect Name="Label" Target="TextBlock">
                    @default { Foreground = $Accent; }
                  </Aspect>
                </Border.Resources>
                <TextBlock Aspect="$Label" Text="Inner" />
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ScopedResources.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.ScopedResourcesFactory"));
        TextBlock outer = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Border border = Assert.IsType<Border>(panel.VisualChildren[1]);
        TextBlock inner = Assert.IsType<TextBlock>(border.Child);
        Assert.Equal(new Cerneala.Drawing.DrawColor(255, 0, 0), outer.Foreground);
        Assert.Equal(new Cerneala.Drawing.DrawColor(0, 255, 0), inner.Foreground);
    }

    [Fact]
    public void ResourcePropertyElementMustMatchItsOwnerTag()
    {
        const string markup = """
            <StackPanel>
              <Border.Resources>
                <SolidColorBrush Name="Accent" Color="#FFFF0000" />
              </Border.Resources>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("WrongResourceOwner.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "WrongResourceOwner.cui.xml");
        Assert.Contains("StackPanel.Resources", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void SolidColorBrushResourceEmitsNamedBrushVariable()
    {
        const string markup = """
            <TextBlock Text="Hello">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("BrushResource.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::Cerneala.UI.Media.SolidColorBrush PulseColorResource0 = new(new global::Cerneala.Drawing.DrawColor(255, 93, 115));", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void InvalidSolidColorBrushColorReportsDiagnostic()
    {
        const string markup = """
            <TextBlock Text="Hello">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#NOPE" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("BadBrush.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "BadBrush.cui.xml");
        Assert.Contains("SolidColorBrush.Color", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AspectTypeAttributeIsNotAcceptedAsTarget()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Aspect Type="TextBlock">
                  @default { FontSize = 12; }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("LegacyAspectType.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "LegacyAspectType.cui.xml");
        Assert.Contains("Aspect.Target", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ControlCanDeclareInlineComponentTemplateWithOwnerBindingsAndParts()
    {
        const string markup = """
            <Button Content="Close" Background="Black">
              @template
              {
                <Border Name="Bd" Background="$owner.Background">
                  <ContentPresenter Content="$owner.Content" />
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("InlineTemplate.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("ComponentTemplate<global::Cerneala.UI.Controls.Button>", generatedSource);
        Assert.Contains(".Bind(global::Cerneala.UI.Controls.Control.BackgroundProperty", generatedSource);
        Assert.Contains(".Bind(global::Cerneala.UI.Controls.ContentControl.ContentProperty", generatedSource);
        Assert.Contains(".RequirePart(\"Bd\"", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.InlineTemplateFactory"));
        Assert.Equal("Close", button.Content);
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.Same(border, button.ComponentTemplateInstance.Parts["Bd"]);
        Assert.Equal(button.Background, border.Background);
        ContentPresenter presenter = Assert.IsType<ContentPresenter>(border.Child);
        Assert.Equal("Close", presenter.Content);

        button.Background = Cerneala.Drawing.DrawColor.White;
        button.Content = "Changed";
        Assert.Equal(Cerneala.Drawing.DrawColor.White, border.Background);
        Assert.Equal("Changed", presenter.Content);
    }

    [Fact]
    public void InlineTemplateBooleanShorthandRestoresOwnerBindingWhenFalse()
    {
        const string markup = """
            <Button Background="Black" IsEnabled="False">
              @template
              {
                <Border Background="$owner.Background">
                  @when $owner.IsEnabled
                  {
                    Background = "White";
                  }
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateBoolean.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateBooleanFactory"));
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);

        button.IsEnabled = true;
        Assert.Equal(Cerneala.Drawing.DrawColor.White, border.Background);
        button.IsEnabled = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);
    }

    [Fact]
    public void InlineTemplateWhenSupportsMultipleIfBranches()
    {
        const string markup = """
            <Button IsEnabled="True">
              @template
              {
                <Border>
                  @when $owner.IsEnabled
                  {
                    @if value == True { Background = "White"; }
                    @if value == False { Background = "Black"; }
                  }
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateBranches.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateBranchesFactory"));
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.White, border.Background);
        button.IsEnabled = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);
    }

    [Fact]
    public void InlineTemplateBooleanShorthandCanCreateConditionalChildren()
    {
        const string markup = """
            <Button IsEnabled="False">
              @template
              {
                <StackPanel>
                  @when $owner.IsEnabled
                  {
                    <TextBlock Text="Enabled" />
                  }
                </StackPanel>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateConditionalChild.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateConditionalChildFactory"));
        button.ApplyTemplate();
        StackPanel panel = Assert.IsType<StackPanel>(button.ComponentTemplateInstance!.Root);
        Assert.Empty(panel.VisualChildren);
        button.IsEnabled = true;
        Assert.Equal("Enabled", Assert.IsType<TextBlock>(Assert.Single(panel.VisualChildren)).Text);
        button.IsEnabled = false;
        Assert.Empty(panel.VisualChildren);
    }

    [Fact]
    public void AspectCanProvideComponentTemplateWithoutChangingNameTargetContract()
    {
        const string markup = """
            <Button Aspect="$GhostButton" Content="Ghost">
              <Button.Resources>
                <Aspect Name="GhostButton" Target="Button">
                  @default { Background = "Black"; }
                  @template
                  {
                    <Border Background="$owner.Background">
                      <ContentPresenter Content="$owner.Content" />
                    </Border>
                  }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("AspectTemplate.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.AspectTemplateFactory"));
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, button.Background);
        Assert.Contains("ComponentTemplate", button.FindResource<MarkupAspectResource>("GhostButton").DefaultPropertyNames);
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.IsType<ContentPresenter>(border.Child);
    }

    [Fact]
    public void DirectTemplateOverridesAspectTemplateWithoutDroppingAspectDefaults()
    {
        const string markup = """
            <Button Background="White" IsEnabled="False">
              <Button.Resources>
                <Aspect Target="Button">
                  @default { Foreground = "White"; }
                  @when IsEnabled
                  {
                    @if value == False { Foreground = "Black"; }
                  }
                  @template { <TextBlock Text="Aspect" /> }
                </Aspect>
              </Button.Resources>
              @template { <TextBlock Text="Direct" /> }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplatePrecedence.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplatePrecedenceFactory"));
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, button.Foreground);
        button.ApplyTemplate();
        TextBlock root = Assert.IsType<TextBlock>(button.ComponentTemplateInstance!.Root);
        Assert.Equal("Direct", root.Text);
    }

    [Fact]
    public void TemplateOnNonControlReportsDedicatedDiagnostic()
    {
        const string markup = """
            <StackPanel>
              @template { <Border /> }
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("PanelTemplate.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "PanelTemplate.cui.xml");
        Assert.Contains("Control", diagnostic.GetMessage(), StringComparison.Ordinal);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void InlineTemplateKeepsOrdinaryVisualContentAndNestedTemplatesIndependent()
    {
        const string markup = """
            <Button>
              @template
              {
                <Border>
                  <ContentPresenter Content="$owner.Content" />
                </Border>
              }
              <Button Content="Nested">
                @template { <TextBlock Text="Inner template" /> }
              </Button>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("NestedTemplates.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button outer = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.NestedTemplatesFactory"));
        Button inner = Assert.IsType<Button>(outer.Content);
        outer.ApplyTemplate();
        Border outerRoot = Assert.IsType<Border>(outer.ComponentTemplateInstance!.Root);
        Assert.Same(inner, Assert.IsType<ContentPresenter>(outerRoot.Child).Content);

        inner.ApplyTemplate();
        TextBlock innerRoot = Assert.IsType<TextBlock>(inner.ComponentTemplateInstance!.Root);
        Assert.Equal("Inner template", innerRoot.Text);
    }

    [Fact]
    public void TemplateExpressionsDistinguishOwnerSelfAndLexicalResources()
    {
        const string markup = """
            <Button IsEnabled="True">
              <Button.Resources>
                <SolidColorBrush Name="Accent" Color="#FF123456" />
              </Button.Resources>
              @template
              {
                <StackPanel>
                  <Border Name="OwnerPart" IsEnabled="False">
                    @when IsEnabled { Background = $Accent; }
                  </Border>
                  <Border Name="SelfPart" IsEnabled="False">
                    @when $self.IsEnabled { Background = "White"; }
                  </Border>
                </StackPanel>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateScopes.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateScopesFactory"));
        button.ApplyTemplate();
        Border ownerPart = Assert.IsType<Border>(button.ComponentTemplateInstance!.Parts["OwnerPart"]);
        Border selfPart = Assert.IsType<Border>(button.ComponentTemplateInstance.Parts["SelfPart"]);
        Assert.Equal(new Cerneala.Drawing.DrawColor(18, 52, 86), ownerPart.Background);
        Assert.Equal(Cerneala.Drawing.DrawColor.Transparent, selfPart.Background);

        selfPart.IsEnabled = true;
        Assert.Equal(Cerneala.Drawing.DrawColor.White, selfPart.Background);
        button.IsEnabled = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Transparent, ownerPart.Background);
    }

    [Fact]
    public void InlineAspectCanProvideTemplateAndNamedAspectWinsOverDefaultTemplate()
    {
        const string inlineMarkup = """
            <Button>
              <Button.Aspect>
                @template { <TextBlock Text="Inline" /> }
              </Button.Aspect>
            </Button>
            """;
        GeneratorRunResult inline = RunGenerator("InlineAspectTemplate.cui.xml", inlineMarkup, out Compilation inlineCompilation);
        Assert.DoesNotContain(inline.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream inlineStream = new();
        Assert.True(inlineCompilation.Emit(inlineStream).Success);
        Button inlineButton = Assert.IsType<Button>(InvokeCreate(inlineStream, "Cerneala.GeneratedUi.InlineAspectTemplateFactory"));
        inlineButton.ApplyTemplate();
        Assert.Equal("Inline", Assert.IsType<TextBlock>(inlineButton.ComponentTemplateInstance!.Root).Text);

        const string namedMarkup = """
            <Button Aspect="$Named">
              <Button.Resources>
                <Aspect Target="Button">@template { <TextBlock Text="Default" /> }</Aspect>
                <Aspect Name="Named" Target="Button">@template { <TextBlock Text="Named" /> }</Aspect>
              </Button.Resources>
            </Button>
            """;
        GeneratorRunResult named = RunGenerator("NamedAspectTemplate.cui.xml", namedMarkup, out Compilation namedCompilation);
        Assert.DoesNotContain(named.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream namedStream = new();
        Assert.True(namedCompilation.Emit(namedStream).Success);
        Button namedButton = Assert.IsType<Button>(InvokeCreate(namedStream, "Cerneala.GeneratedUi.NamedAspectTemplateFactory"));
        namedButton.ApplyTemplate();
        Assert.Equal("Named", Assert.IsType<TextBlock>(namedButton.ComponentTemplateInstance!.Root).Text);
    }

    [Fact]
    public void SharedAspectTemplateCreatesIndependentPartsForEveryControl()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Target="Button">
                  @template { <Border Name="Chrome" /> }
                </Aspect>
              </StackPanel.Resources>
              <Button Content="One" />
              <Button Content="Two" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("TemplatePartIsolation.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplatePartIsolationFactory"));
        Button first = Assert.IsType<Button>(panel.VisualChildren[0]);
        Button second = Assert.IsType<Button>(panel.VisualChildren[1]);
        Assert.Same(first.ComponentTemplate, second.ComponentTemplate);
        first.ApplyTemplate();
        second.ApplyTemplate();

        Assert.NotSame(first.ComponentTemplateInstance, second.ComponentTemplateInstance);
        Assert.NotSame(first.ComponentTemplateInstance!.Parts["Chrome"], second.ComponentTemplateInstance!.Parts["Chrome"]);
        Assert.NotSame(first.ComponentTemplateInstance.Root, second.ComponentTemplateInstance.Root);
    }

    [Fact]
    public void DirectTemplateWinsOverInlineAspectTemplate()
    {
        const string markup = """
            <Button>
              <Button.Aspect>
                @default { Foreground = "White"; }
                @template { <TextBlock Text="Inline Aspect" /> }
              </Button.Aspect>
              @template { <TextBlock Text="Direct" /> }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("InlineAspectPrecedence.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.InlineAspectPrecedenceFactory"));
        Assert.Equal(Cerneala.Drawing.DrawColor.White, button.Foreground);
        button.ApplyTemplate();
        Assert.Equal("Direct", Assert.IsType<TextBlock>(button.ComponentTemplateInstance!.Root).Text);
    }

    [Theory]
    [InlineData("<Button>@template { }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { raw text }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { <Border /><TextBlock /> }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { <Border /> } @template { <TextBlock /> }</Button>", "CERNEALAUI012", "only one")]
    [InlineData("<Button>@when IsEnabled { @if value == True { @template { <Border /> } } }</Button>", "CERNEALAUI006", "not allowed")]
    [InlineData("<Button>@template { <Border Name=\"Part\"><Button Name=\"Part\" /></Border> }</Button>", "CERNEALAUI012", "Duplicate")]
    [InlineData("<Button>@template { <Border Background=\"$owner.FontSize\" /> }</Button>", "CERNEALAUI012", "type")]
    [InlineData("<Button>@template { <Border>@when $owner.Unknown { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "template owner")]
    [InlineData("<Button>@template { <Border>@when $self.Unknown { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "template element")]
    [InlineData("<Button>@template { <Border>@when $owner.FontSize { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "Boolean")]
    [InlineData("<StackPanel><StackPanel.Resources><Aspect Target=\"StackPanel\">@template { <Border /> }</Aspect></StackPanel.Resources></StackPanel>", "CERNEALAUI012", "not a Control")]
    [InlineData("<Button><Button.Resources><SolidColorBrush Name=\"owner\" Color=\"#FF000000\" /></Button.Resources></Button>", "CERNEALAUI005", "reserved")]
    public void InvalidTemplateShapesReportFocusedDiagnostics(string markup, string diagnosticId, string message)
    {
        GeneratorRunResult result = RunGenerator("InvalidTemplate.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, diagnosticId, "InvalidTemplate.cui.xml");
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ReplacingGeneratedTemplateDetachesOldReactiveObservations()
    {
        const string markup = """
            <Button IsEnabled="True">
              @template
              {
                <Border>
                  @when $owner.IsEnabled { Background = "White"; }
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateLifecycle.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateLifecycleFactory"));
        button.ApplyTemplate();
        Border oldRoot = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.White, oldRoot.Background);

        button.ComponentTemplate = new Cerneala.UI.Controls.Templates.ComponentTemplate<Button>("replacement", _ => new Border());
        Assert.Equal(Cerneala.Drawing.DrawColor.Transparent, oldRoot.Background);
        button.IsEnabled = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Transparent, oldRoot.Background);
        Assert.NotSame(oldRoot, button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void UnnamedAspectAppliesToEveryMatchingElement()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect Target="TextBlock">
                  @default
                  {
                    FontFamily = "Consolas";
                    FontSize = 12;
                  }
                </Aspect>
              </StackPanel.Resources>
              <TextBlock Text="One" />
              <TextBlock Text="Two" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("DefaultTextAspect.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2, Count(generatedSource, ".FontFamily = \"Consolas\";"));
        Assert.Equal(2, Count(generatedSource, ".FontSize = 12f;"));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void NamedAspectAppliesAfterUnnamedDefault()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText" Text="HELLO">
              <TextBlock.Resources>
                <Aspect Target="TextBlock">
                  @default
                  {
                    FontSize = 14;
                    Foreground = Black;
                  }
                </Aspect>
                <Aspect Name="KickerText" Target="TextBlock">
                  @default
                  {
                    FontSize = 12;
                    Margin = "0,0,0,12";
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("NamedAspect.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.True(generatedSource.IndexOf(".FontSize = 14f;", StringComparison.Ordinal) < generatedSource.IndexOf(".FontSize = 12f;", StringComparison.Ordinal));
        Assert.True(generatedSource.IndexOf(".FontSize = 12f;", StringComparison.Ordinal) < generatedSource.IndexOf(".Text = \"HELLO\";", StringComparison.Ordinal));
        Assert.Contains(".Margin = new global::Cerneala.UI.Layout.Thickness(0f, 0f, 0f, 12f);", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void AspectCanReferenceSolidColorBrushForDrawColorProperty()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText" Text="HELLO">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
                <Aspect Name="KickerText" Target="TextBlock">
                  @default
                  {
                    Foreground = $PulseColor;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("AspectBrushReference.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(".Foreground = new global::Cerneala.Drawing.DrawColor(255, 93, 115);", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.AspectBrushReferenceFactory"));
        Assert.Equal(new Cerneala.Drawing.DrawColor(255, 93, 115), root.Foreground);
    }

    [Fact]
    public void UnknownNameReferenceReportsDiagnostic()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText">
              <TextBlock.Resources>
                <Aspect Name="KickerText" Target="TextBlock">
                  @default
                  {
                    Foreground = $MissingColor;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("UnknownReference.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "UnknownReference.cui.xml");
        Assert.Contains("MissingColor", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ElementNameRegistersGeneratedVariableSymbol()
    {
        const string markup = """
            <TextBlock Name="KickerLabel" Text="HELLO" />
            """;

        GeneratorRunResult result = RunGenerator("NamedElement.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::Cerneala.UI.Controls.TextBlock KickerLabel = new();", generatedSource);
        Assert.Contains("KickerLabel.Text = \"HELLO\";", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void DuplicateResourceNameInSameScopeReportsDiagnostic()
    {
        const string markup = """
            <TextBlock Text="HELLO">
              <TextBlock.Resources>
                <SolidColorBrush Name="Duplicate" Color="#FF5D73" />
                <SolidColorBrush Name="Duplicate" Color="#FFFFFFFF" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("DuplicateName.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateName.cui.xml");
        Assert.Contains("Duplicate", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AspectTargetMismatchReportsDiagnostic()
    {
        const string markup = """
            <Button Aspect="$KickerText">
              <Button.Resources>
                <Aspect Name="KickerText" Target="TextBlock">
                  @default
                  {
                    FontSize = 12;
                  }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("AspectMismatch.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "AspectMismatch.cui.xml");
        Assert.Contains("Button.Aspect", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void DuplicateUnnamedAspectForTargetReportsDiagnostic()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Aspect Target="TextBlock">
                  @default { FontSize = 12; }
                </Aspect>
                <Aspect Target="TextBlock">
                  @default { FontSize = 14; }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("DuplicateDefaultAspect.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateDefaultAspect.cui.xml");
        Assert.Contains("TextBlock", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedAspectPropertyReportsDiagnostic()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Aspect Target="TextBlock">
                  @default
                  {
                    Width = 100;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("UnsupportedAspectProperty.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedAspectProperty.cui.xml");
        Assert.Contains("TextBlock.Width", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedResourceDeclarationReportsDiagnostic()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Resources />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("NestedResources.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI002", "NestedResources.cui.xml");
        Assert.Contains("Resources", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void MultipleUiRootsReportMalformedMarkupDiagnostic()
    {
        const string markup = """
            <TextBlock Text="One" />
            <TextBlock Text="Two" />
            """;

        GeneratorRunResult result = RunGenerator("MultipleRoots.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "MultipleRoots.cui.xml");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void FragmentDiagnosticsPreserveElementLineInformation()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
              </StackPanel.Resources>
              <TextBlock Width="12" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("FragmentDiagnosticLocation.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "FragmentDiagnosticLocation.cui.xml");
        Assert.Equal(4, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void FirstLineFragmentDiagnosticsUseOriginalMarkupColumn()
    {
        GeneratorRunResult result = RunGenerator("FirstLineDiagnosticLocation.cui.xml", "<TextBlock Width=\"12\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "FirstLineDiagnosticLocation.cui.xml");
        Assert.Equal(11, diagnostic.Location.GetLineSpan().StartLinePosition.Character);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void XmlDeclarationCanPrecedeFragmentMarkup()
    {
        const string markup = """
            <?xml version="1.0"?>
            <TextBlock Text="Hello">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("XmlDeclarationFragment.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public static partial class XmlDeclarationFragmentFactory", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void TopLevelTextReportsMalformedMarkupDiagnostic()
    {
        const string markup = """
            stray text
            <TextBlock Text="Hello" />
            """;

        GeneratorRunResult result = RunGenerator("TopLevelText.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "TopLevelText.cui.xml");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AdjacentMultipleUiRootsReportMalformedMarkupDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("AdjacentRoots.cui.xml", "<TextBlock Text=\"One\" /><TextBlock Text=\"Two\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "AdjacentRoots.cui.xml");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void TopLevelCDataAfterRootReportsMalformedMarkupDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("TrailingCData.cui.xml", "<TextBlock Text=\"Hello\" /><![CDATA[stray]]>", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "TrailingCData.cui.xml");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void MalformedMarkupReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Broken.cui.xml", "<StackPanel>", out _);

        AssertDiagnostic(result, "CERNEALAUI001", "Broken.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedElementReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Unsupported.cui.xml", "<Grid />", out _);

        AssertDiagnostic(result, "CERNEALAUI002", "Unsupported.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedPropertyReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("UnsupportedProperty.cui.xml", "<TextBlock Width=\"12\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedProperty.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ControlPropertiesOnStackPanelReportDiagnosticInsteadOfGeneratingInvalidCode()
    {
        GeneratorRunResult result = RunGenerator("BadPanel.cui.xml", "<StackPanel Padding=\"4\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "BadPanel.cui.xml");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void InvalidRuntimeValidatedValuesReportDiagnostics()
    {
        GeneratorRunResult fontResult = RunGenerator("BadFont.cui.xml", "<TextBlock FontSize=\"0\" />", out _);
        GeneratorRunResult paddingResult = RunGenerator("BadPadding.cui.xml", "<Border Padding=\"-1\" />", out _);

        AssertDiagnostic(fontResult, "CERNEALAUI004", "BadFont.cui.xml");
        AssertDiagnostic(paddingResult, "CERNEALAUI004", "BadPadding.cui.xml");
    }

    [Fact]
    public void InvalidBooleanPropertyValueDiagnosticNamesMarkupProperty()
    {
        GeneratorRunResult result = RunGenerator("BadVisibility.cui.xml", "<TextBlock IsVisible=\"maybe\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "BadVisibility.cui.xml");
        Assert.Contains("TextBlock.IsVisible", diagnostic.GetMessage());
    }

    [Fact]
    public void DistinctMarkupFilesWithSameBaseNameEmitUniqueFactories()
    {
        GeneratorRunResult result = RunGenerator(
            new MarkupFile("Views/Main.cui.xml", "<TextBlock Text=\"View\" />"),
            new MarkupFile("Dialogs/Main.cui.xml", "<TextBlock Text=\"Dialog\" />"));

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2, result.GeneratedSources.Length);
        Assert.Equal(2, result.GeneratedSources.Select(source => source.HintName).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("ViewsMainFactory"));
        Assert.Contains(result.GeneratedSources, source => source.SourceText.ToString().Contains("DialogsMainFactory"));
    }

    [Fact]
    public void WhenReevaluatesIndependentIfBranchesAndRestoresMarkupBase()
    {
        const string markup = """
            <Border Background="Black">
              @when IsMouseOver
              {
                @if value == True
                {
                  Background = White;
                }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("ReactiveBorder.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.ReactiveBorderFactory"));
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);

        border.IsPointerOver = true;
        Assert.Equal(Cerneala.Drawing.DrawColor.White, border.Background);

        border.IsPointerOver = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);
    }

    [Fact]
    public void ConditionalChildrenAreLazyCachedAndKeepMarkupOrder()
    {
        const string markup = """
            <StackPanel>
              <TextBlock Text="Before" />
              @when IsMouseOver
              {
                @if value == True
                {
                  <TextBlock Text="Conditional" />
                }
              }
              <TextBlock Text="After" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ConditionalChildren.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.ConditionalChildrenFactory"));
        Assert.Equal(new[] { "Before", "After" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));

        panel.IsPointerOver = true;
        Assert.Equal(new[] { "Before", "Conditional", "After" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));
        Assert.Equal(new[] { "Before", "Conditional", "After" }, panel.LogicalChildren.Cast<TextBlock>().Select(child => child.Text));
        TextBlock cached = Assert.IsType<TextBlock>(panel.VisualChildren[1]);

        panel.IsPointerOver = false;
        Assert.Equal(new[] { "Before", "After" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));

        panel.IsPointerOver = true;
        Assert.Same(cached, panel.VisualChildren[1]);
    }

    [Fact]
    public void ConditionalButtonChildFallsBackToStaticContentAndRespectsLocalContent()
    {
        const string markup = """
            <Button Content="Static">
              @when IsMouseOver
              {
                @if value == True
                {
                  <TextBlock Text="Conditional" />
                }
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("ConditionalButton.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.ConditionalButtonFactory"));
        Assert.Equal("Static", button.Content);
        button.IsPointerOver = true;
        Assert.Equal("Conditional", Assert.IsType<TextBlock>(button.Content).Text);
        button.IsPointerOver = false;
        Assert.Equal("Static", button.Content);

        button.Content = "CodeBehind";
        button.IsPointerOver = true;
        Assert.Equal("CodeBehind", button.Content);
    }

    [Fact]
    public void DataContextSourceEmitsTypedFactoryAndTracksUiObjectChanges()
    {
        const string markup = """
            <TextBlock DataType="Cerneala.UI.Elements.UIElement" Text="Off">
              @when $DataContext.IsEnabled
              {
                @if value == True
                {
                  Text = "On";
                }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("TypedContext.cui.xml", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("Create(global::Cerneala.UI.Elements.UIElement dataContext)", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        UIElement context = new() { IsEnabled = false };
        TextBlock text = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.TypedContextFactory", context));
        Assert.Equal("Off", text.Text);

        context.IsEnabled = true;
        Assert.Equal("On", text.Text);

        context.IsEnabled = false;
        Assert.Equal("Off", text.Text);
    }

    [Fact]
    public void NullAndInheritedDataContextsAreSafeAndRebindOnReplacement()
    {
        const string markup = """
            <StackPanel DataType="Cerneala.UI.Elements.UIElement">
              <TextBlock Text="Off">
                @when $DataContext.IsEnabled
                {
                  @if value == True { Text = "On"; }
                }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("InheritedContext.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel withoutContext = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.InheritedContextFactory"));
        Assert.Equal("Off", Assert.IsType<TextBlock>(withoutContext.VisualChildren[0]).Text);

        UIElement first = new() { IsEnabled = false };
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.InheritedContextFactory", first));
        TextBlock child = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Assert.Equal("Off", child.Text);

        first.IsEnabled = true;
        Assert.Equal("On", child.Text);

        UIElement second = new() { IsEnabled = false };
        panel.DataContext = second;
        Assert.Equal("Off", child.Text);

        first.IsEnabled = false;
        first.IsEnabled = true;
        Assert.Equal("Off", child.Text);

        second.IsEnabled = true;
        Assert.Equal("On", child.Text);
    }

    [Fact]
    public void NestedNotifyPropertyChangedPathRebindsEverySegment()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class RootViewModel : INotifyPropertyChanged
            {
                private ChildViewModel? child;
                public event PropertyChangedEventHandler? PropertyChanged;
                public ChildViewModel? Child
                {
                    get => child;
                    set
                    {
                        child = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
                    }
                }
            }

            public sealed class ChildViewModel : INotifyPropertyChanged
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
            <TextBlock DataType="TestInput.RootViewModel" Text="Off">
              @when $DataContext.Child.Active
              {
                @if value == True { Text = "On"; }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("NestedPath.cui.xml", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type rootType = assembly.GetType("TestInput.RootViewModel", throwOnError: true)!;
        Type childType = assembly.GetType("TestInput.ChildViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(rootType)!;
        object first = Activator.CreateInstance(childType)!;
        rootType.GetProperty("Child")!.SetValue(viewModel, first);
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.NestedPathFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        TextBlock text = Assert.IsType<TextBlock>(create.Invoke(null, new[] { viewModel }));
        Assert.Equal("Off", text.Text);

        childType.GetProperty("Active")!.SetValue(first, true);
        Assert.Equal("On", text.Text);

        object second = Activator.CreateInstance(childType)!;
        rootType.GetProperty("Child")!.SetValue(viewModel, second);
        Assert.Equal("Off", text.Text);

        childType.GetProperty("Active")!.SetValue(first, false);
        childType.GetProperty("Active")!.SetValue(first, true);
        Assert.Equal("Off", text.Text);

        childType.GetProperty("Active")!.SetValue(second, true);
        Assert.Equal("On", text.Text);
    }

    [Fact]
    public void DataContextOperandIsTypedAndObserved()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;

            public sealed class PairViewModel : INotifyPropertyChanged
            {
                private string left = "A";
                private string right = "B";
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Left
                {
                    get => left;
                    set { left = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Left))); }
                }
                public string Right
                {
                    get => right;
                    set { right = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Right))); }
                }
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.PairViewModel" Text="Different">
              @when $DataContext.Left
              {
                @if value == $DataContext.Right { Text = "Same"; }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("ContextOperand.cui.xml", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewModelType = assembly.GetType("TestInput.PairViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.ContextOperandFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        TextBlock text = Assert.IsType<TextBlock>(create.Invoke(null, new[] { viewModel }));
        Assert.Equal("Different", text.Text);

        viewModelType.GetProperty("Right")!.SetValue(viewModel, "A");
        Assert.Equal("Same", text.Text);
        viewModelType.GetProperty("Left")!.SetValue(viewModel, "C");
        Assert.Equal("Different", text.Text);
    }

    [Fact]
    public void AllComparisonOperatorsAreTypedAndReactive()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class NumberViewModel : INotifyPropertyChanged
            {
                private int value = 10;
                public event PropertyChangedEventHandler? PropertyChanged;
                public int Value
                {
                    get => value;
                    set { this.value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); }
                }
            }
            """;
        const string markup = """
            <StackPanel DataType="TestInput.NumberViewModel">
              <TextBlock Text="F">@when $DataContext.Value { @if value == 10 { Text = "T"; } }</TextBlock>
              <TextBlock Text="F">@when $DataContext.Value { @if value != 10 { Text = "T"; } }</TextBlock>
              <TextBlock Text="F">@when $DataContext.Value { @if value &lt; 10 { Text = "T"; } }</TextBlock>
              <TextBlock Text="F">@when $DataContext.Value { @if value &lt;= 10 { Text = "T"; } }</TextBlock>
              <TextBlock Text="F">@when $DataContext.Value { @if value &gt; 10 { Text = "T"; } }</TextBlock>
              <TextBlock Text="F">@when $DataContext.Value { @if value &gt;= 10 { Text = "T"; } }</TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("Comparators.cui.xml", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewModelType = assembly.GetType("TestInput.NumberViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.ComparatorsFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        StackPanel panel = Assert.IsType<StackPanel>(create.Invoke(null, new[] { viewModel }));

        Assert.Equal(new[] { "T", "F", "F", "T", "F", "T" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));
        viewModelType.GetProperty("Value")!.SetValue(viewModel, 5);
        Assert.Equal(new[] { "F", "T", "T", "T", "F", "F" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));
        viewModelType.GetProperty("Value")!.SetValue(viewModel, 15);
        Assert.Equal(new[] { "F", "T", "F", "F", "T", "T" }, panel.VisualChildren.Cast<TextBlock>().Select(child => child.Text));
    }

    [Fact]
    public void LocalCodeBehindValueStaysAboveConditionalMarkup()
    {
        const string markup = """
            <Border Background="Black">
              @when IsMouseOver
              {
                @if value == True { Background = White; }
              }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("LocalWins.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.LocalWinsFactory"));
        Cerneala.Drawing.DrawColor local = new(12, 34, 56);
        border.Background = local;
        border.IsPointerOver = true;
        Assert.Equal(local, border.Background);
        border.IsPointerOver = false;
        Assert.Equal(local, border.Background);
    }

    [Fact]
    public void NestedWhenConditionsAreCombinedWithAnd()
    {
        const string markup = """
            <TextBlock Text="Base" IsEnabled="False">
              @when IsMouseOver
              {
                @if value == True
                {
                  @when IsEnabled
                  {
                    @if value == True { Text = "Both"; }
                  }
                }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("NestedWhen.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock text = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.NestedWhenFactory"));
        text.IsPointerOver = true;
        Assert.Equal("Base", text.Text);
        text.IsEnabled = true;
        Assert.Equal("Both", text.Text);
        text.IsPointerOver = false;
        Assert.Equal("Base", text.Text);
    }

    [Fact]
    public void ConditionalAspectFallsBackWhenConditionStopsMatching()
    {
        const string markup = """
            <Border Aspect="$Hover">
              <Border.Resources>
                <Aspect Name="Hover" Target="Border">
                  @default { Background = Black; }
                  @when IsMouseOver
                  {
                    @if value == True { Background = White; }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("ReactiveAspect.cui.xml", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.ReactiveAspectFactory"));
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);
        border.IsPointerOver = true;
        Assert.Equal(Cerneala.Drawing.DrawColor.White, border.Background);
        border.IsPointerOver = false;
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, border.Background);
    }

    [Fact]
    public void UnsupportedDirectiveReportsGeneratorDiagnostic()
    {
        const string markup = """
            <Border>
              @animate $base { Background = White; }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("UnsupportedDirective.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI006", "UnsupportedDirective.cui.xml");
        Assert.Contains("@animate", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void DataTypeBelowRootReportsGeneratorDiagnostic()
    {
        const string markup = """
            <StackPanel>
              <TextBlock DataType="Cerneala.UI.Elements.UIElement" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("NestedDataType.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI007", "NestedDataType.cui.xml");
        Assert.Contains("only on the root", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void PairedTypedUserControlEmitsConstructorsNamesEventsAndNoFactory()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Input;
            namespace TestInput.Views;

            public sealed class MainWindowViewModel
            {
                public int SaveCount { get; private set; }
                public void Save() => SaveCount++;
            }

            public partial class MainWindow : UserControl<MainWindowViewModel>
            {
                private void OnSave(UiElementId sender, RoutedEventArgs args) => ViewModel.Save();
            }
            """;
        const string markup = """
            <UserControl>
              <StackPanel>
                <Button Name="SaveButton" Content="Save" Click="OnSave" />
              </StackPanel>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public MainWindow()", generatedSource);
        Assert.Contains("public MainWindow(global::TestInput.Views.MainWindowViewModel viewModel)", generatedSource);
        Assert.Contains("private global::Cerneala.UI.Controls.Button SaveButton", generatedSource);
        Assert.Contains("SaveButton.Click += this.OnSave;", generatedSource);
        Assert.DoesNotContain("MainWindowFactory", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewModelType = assembly.GetType("TestInput.Views.MainWindowViewModel", throwOnError: true)!;
        Type windowType = assembly.GetType("TestInput.Views.MainWindow", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        UserControl window = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(windowType, viewModel));
        Assert.Same(viewModel, window.DataContext);
        StackPanel panel = Assert.IsType<StackPanel>(window.ComponentTemplateInstance!.Root);
        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        object namedButton = windowType.GetProperty("SaveButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Assert.Same(button, namedButton);

        button.RaiseEvent(new Cerneala.UI.Input.RoutedEventArgs(
            Cerneala.UI.Controls.Primitives.ButtonBase.ClickEvent,
            button));
        Assert.Equal(1, viewModelType.GetProperty("SaveCount")!.GetValue(viewModel));

        UserControl withoutContext = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(windowType));
        Assert.Null(withoutContext.DataContext);
        Assert.NotNull(withoutContext.ComponentTemplateInstance);
    }

    [Fact]
    public void PairedUserControlRootTemplateUsesGeneratedTemplateContextAndPreservesNamedMembers()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class TemplateView : UserControl { }
            """;
        const string markup = """
            <UserControl Background="Black">
              @template
              {
                <Border Background="$owner.Background">
                  <Button Name="ActionButton" Content="Action" />
                </Border>
              }
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/TemplateView.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("__CernealaCreateContent(context)", generatedSource);
        Assert.Contains("private global::Cerneala.UI.Controls.Button ActionButton", generatedSource);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.Views.TemplateView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(type));
        Border root = Assert.IsType<Border>(view.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, root.Background);
        object named = type.GetProperty("ActionButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
        Assert.Same(root.Child, named);
    }

    [Fact]
    public void PairedUserControlRootTemplateConflictsWithDirectVisualChild()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class InvalidTemplateView : UserControl { }
            """;
        const string markup = """
            <UserControl>
              @template { <Border /> }
              <TextBlock Text="Second root" />
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/InvalidTemplateView.cui.xml",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "Views/InvalidTemplateView.cui.xml");
        Assert.Contains("direct visual child", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void SemanticCustomControlCanDeclareInlineTemplate()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public sealed class FancyButton : Button { }
            public partial class CustomTemplateView : UserControl { }
            """;
        const string markup = """
            <UserControl>
              <FancyButton Content="Fancy">
                @template
                {
                  <ContentPresenter Content="$owner.Content" />
                }
              </FancyButton>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/CustomTemplateView.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("ComponentTemplate<global::TestInput.Views.FancyButton>", generatedSource);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.Views.CustomTemplateView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(type));
        Button button = Assert.IsAssignableFrom<Button>(view.ComponentTemplateInstance!.Root);
        button.ApplyTemplate();
        Assert.Equal("Fancy", Assert.IsType<ContentPresenter>(button.ComponentTemplateInstance!.Root).Content);
    }

    [Fact]
    public void NestedInlineTemplateCanWireCodeBehindEventsWithoutLeakingPartMembers()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Input;
            namespace TestInput.Views;
            public partial class EventTemplateView : UserControl
            {
                public int ClickCount { get; private set; }
                private void OnInnerClick(UiElementId sender, RoutedEventArgs args) => ClickCount++;
            }
            """;
        const string markup = """
            <UserControl>
              <Button Content="Outer">
                @template
                {
                  <Button Name="InnerPart" Content="Inner" Click="OnInnerClick" />
                }
              </Button>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/EventTemplateView.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(".RequirePart(\"InnerPart\"", generatedSource);
        Assert.DoesNotContain("private global::Cerneala.UI.Controls.Button InnerPart", generatedSource);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.Views.EventTemplateView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(type));
        Button outer = Assert.IsType<Button>(view.ComponentTemplateInstance!.Root);
        outer.ApplyTemplate();
        Button inner = Assert.IsType<Button>(outer.ComponentTemplateInstance!.Parts["InnerPart"]);
        inner.RaiseEvent(new Cerneala.UI.Input.RoutedEventArgs(
            Cerneala.UI.Controls.Primitives.ButtonBase.ClickEvent,
            inner));
        Assert.Equal(1, type.GetProperty("ClickCount")!.GetValue(view));
    }

    [Fact]
    public void TemplateBindingToReadOnlyCustomPropertyReportsFocusedDiagnostic()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Core;
            namespace TestInput.Views;
            public sealed class ReadOnlyPart : Border
            {
                public static readonly UiProperty<float> MirrorFontSizeProperty = UiProperty<float>.Register(
                    nameof(MirrorFontSize),
                    typeof(ReadOnlyPart),
                    new UiPropertyMetadata<float>(0));
                public float MirrorFontSize => GetValue(MirrorFontSizeProperty);
            }
            public partial class ReadOnlyTemplateView : UserControl { }
            """;
        const string markup = """
            <UserControl>
              <Button>
                @template { <ReadOnlyPart MirrorFontSize="$owner.FontSize" /> }
              </Button>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/ReadOnlyTemplateView.cui.xml",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "Views/ReadOnlyTemplateView.cui.xml");
        Assert.Contains("read-only", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConditionalNameIsNullableWhileBranchIsInactiveAndReusesCachedInstance()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class ConditionalView : UserControl { }
            """;
        const string markup = """
            <UserControl>
              <StackPanel>
                @when IsEnabled
                {
                  @if value == True { <Button Name="ConditionalButton" Content="Save" /> }
                }
              </StackPanel>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/ConditionalView.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewType = assembly.GetType("TestInput.Views.ConditionalView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(viewType));
        StackPanel panel = Assert.IsType<StackPanel>(view.ComponentTemplateInstance!.Root);
        PropertyInfo member = viewType.GetProperty("ConditionalButton", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object first = member.GetValue(view)!;
        Assert.Single(panel.VisualChildren);

        panel.IsEnabled = false;
        Assert.Null(member.GetValue(view));
        Assert.Empty(panel.VisualChildren);

        panel.IsEnabled = true;
        Assert.Same(first, member.GetValue(view));
        Assert.Same(first, panel.VisualChildren[0]);

        UIRoot root = new();
        root.VisualChildren.Add(view);
        root.VisualChildren.Remove(view);
        Assert.Null(member.GetValue(view));

        root.VisualChildren.Add(view);
        Assert.Same(first, member.GetValue(view));
        Assert.Same(first, panel.VisualChildren[0]);
    }

    [Fact]
    public void PairedMarkupResolvesCustomControlThroughCompanionUsingScope()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Cerneala.UI.Core;
            using TestInput.Components;

            namespace TestInput.Components
            {
                public class ProfileCard : UserControl
                {
                    public static readonly UiProperty<int> ScoreProperty = UiProperty<int>.Register(
                        nameof(Score),
                        typeof(ProfileCard),
                        new UiPropertyMetadata<int>(0));

                    public int Score
                    {
                        get => GetValue(ScoreProperty);
                        set => SetValue(ScoreProperty, value);
                    }
                }
            }

            namespace TestInput.Views
            {
                public partial class MainView : UserControl { }
            }
            """;
        const string markup = """
            <UserControl>
              <ProfileCard Score="7" />
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainView.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::TestInput.Components.ProfileCard", SingleGeneratedSource(result));
        Assert.Contains(".Score = 7;", SingleGeneratedSource(result));
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void SemanticDiscoveryResolvesBuiltInInheritedAndEnumProperties()
    {
        const string markup = """
            <StackPanel>
              <Slider Minimum="1" Maximum="10" Value="4" SmallChange="0.5" Orientation="Vertical" />
              <TextBox Text="hello" CaretColor="18,52,86" />
              <CheckBox IsChecked="True" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("SemanticControls.cui.xml", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.SemanticControlsFactory"));
        Slider slider = Assert.IsType<Slider>(panel.VisualChildren[0]);
        Assert.Equal(1, slider.Minimum);
        Assert.Equal(10, slider.Maximum);
        Assert.Equal(4, slider.Value);
        Assert.Equal(0.5f, slider.SmallChange);
        Assert.Equal(Cerneala.UI.Layout.Orientation.Vertical, slider.Orientation);

        TextBox textBox = Assert.IsType<TextBox>(panel.VisualChildren[1]);
        Assert.Equal("hello", textBox.Text);
        Assert.Equal(new Cerneala.Drawing.DrawColor(0x12, 0x34, 0x56), textBox.CaretColor);

        CheckBox checkBox = Assert.IsType<CheckBox>(panel.VisualChildren[2]);
        Assert.True(checkBox.IsChecked);
    }

    [Fact]
    public void InlineAspectPropertyAppliesToAnyControlWithoutResourceReference()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Aspect>
                @default {
                  Margin = 8;
                }
                @when IsMouseOver {
                  @if value == True { Margin = 12; }
                }
              </StackPanel.Aspect>
              <Button>
                <Button.Aspect>
                  @default { Foreground = White; }
                </Button.Aspect>
              </Button>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("InlineAspect.cui.xml", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.InlineAspectFactory"));
        Assert.NotNull(panel.Aspect);
        Assert.True(panel.Aspect.IsConditional);
        Assert.Equal(new[] { "Margin" }, panel.Aspect.DefaultValues.Select(value => value.Property.Name));
        Assert.Equal(new Cerneala.UI.Layout.Thickness(8), panel.Margin);
        Assert.Single(panel.VisualChildren);

        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        Assert.NotNull(button.Aspect);
        Assert.Equal(Cerneala.Drawing.DrawColor.White, button.Foreground);

        panel.IsPointerOver = true;
        Assert.Equal(new Cerneala.UI.Layout.Thickness(12), panel.Margin);
        panel.IsPointerOver = false;
        Assert.Equal(new Cerneala.UI.Layout.Thickness(8), panel.Margin);

        panel.Aspect = null;
        panel.IsPointerOver = true;
        Assert.Equal(Cerneala.UI.Layout.Thickness.Zero, panel.Margin);
    }

    [Fact]
    public void InlineAspectRejectsAttributeCombination()
    {
        const string markup = """
            <StackPanel Aspect="$Shared">
              <StackPanel.Aspect>
                @default { Background = Black; }
              </StackPanel.Aspect>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ConflictingAspect.cui.xml", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "ConflictingAspect.cui.xml");
        Assert.Contains("cannot combine", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void PairedMarkupRejectsUserConstructorMissingHandlerAndConditionalRoot()
    {
        const string constructorSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class BadView : UserControl
            {
                public BadView() { }
            }
            """;
        GeneratorRunResult constructorResult = RunPairedGenerator(
            "Views/BadView.cui.xml",
            "<UserControl />",
            constructorSource,
            out _);
        Assert.Contains(constructorResult.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI008");
        Assert.Empty(constructorResult.GeneratedSources);

        const string handlerSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class EventView : UserControl { }
            """;
        GeneratorRunResult handlerResult = RunPairedGenerator(
            "Views/EventView.cui.xml",
            "<UserControl><Button Click=\"Missing\" /></UserControl>",
            handlerSource,
            out _);
        Assert.Contains(handlerResult.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI009");
        Assert.Empty(handlerResult.GeneratedSources);

        const string rootSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class RootView : UserControl { }
            """;
        const string conditionalRoot = """
            <UserControl>
              @when IsEnabled { @if value == True { <Button /> } }
            </UserControl>
            """;
        GeneratorRunResult rootResult = RunPairedGenerator(
            "Views/RootView.cui.xml",
            conditionalRoot,
            rootSource,
            out _);
        Assert.Contains(rootResult.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI008");
        Assert.Empty(rootResult.GeneratedSources);
    }

    [Fact]
    public void PairedTypedWindowBuildsContentPropertiesNamesAndEvents()
    {
        const string inputSource = """
            using System;
            using Cerneala.UI.Controls;
            namespace TestInput.Views;

            public sealed class MainWindowViewModel { }

            public partial class MainWindow : Window<MainWindowViewModel>
            {
                public int SourceInitializationCount { get; private set; }
                private void OnSourceInitialized(object? sender, EventArgs args) => SourceInitializationCount++;
            }
            """;
        const string markup = """
            <Window Title="Cerneala"
                    Width="1100"
                    Height="720"
                    MinWidth="640"
                    MinHeight="480"
                    WindowState="Normal"
                    ResizeMode="CanResize"
                    WindowStartupLocation="CenterScreen"
                    Topmost="False"
                    ShowInTaskbar="True"
                    SourceInitialized="OnSourceInitialized">
              @when WindowState
              {
                @if value == Normal { Title = "Normal state"; }
              }
              <StackPanel>
                <Button Name="SaveButton" Content="Save" />
              </StackPanel>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("public MainWindow(global::TestInput.Views.MainWindowViewModel viewModel)", generatedSource);
        Assert.DoesNotContain("public MainWindow()", generatedSource);
        Assert.Contains("this.SourceInitialized += this.OnSourceInitialized;", generatedSource);
        Assert.Contains("private global::Cerneala.UI.Controls.Button SaveButton", generatedSource);
        Assert.DoesNotContain("MainWindowFactory", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewModelType = assembly.GetType("TestInput.Views.MainWindowViewModel", throwOnError: true)!;
        Type windowType = assembly.GetType("TestInput.Views.MainWindow", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(viewModelType)!;
        Window window = Assert.IsAssignableFrom<Window>(Activator.CreateInstance(windowType, viewModel));
        Assert.Same(viewModel, window.DataContext);
        Assert.Equal("Normal state", window.Title);
        window.WindowState = WindowState.Maximized;
        Assert.Equal("Cerneala", window.Title);
        Assert.Equal(1100, window.Width);
        Assert.Equal(720, window.Height);
        StackPanel panel = Assert.IsType<StackPanel>(window.Content);
        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        object namedButton = windowType.GetProperty("SaveButton", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        Assert.Same(button, namedButton);
    }

    [Fact]
    public void PairedWindowCanCombineLocalTemplateWithOrdinaryContent()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class TemplateWindow : Window { }
            """;
        const string markup = """
            <Window Background="Black">
              @template
              {
                <Border Name="Chrome" Background="$owner.Background" />
              }
              <StackPanel>
                <TextBlock Text="Window content" />
              </StackPanel>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/TemplateWindow.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.Views.TemplateWindow", throwOnError: true)!;
        Window window = Assert.IsAssignableFrom<Window>(Activator.CreateInstance(type));
        StackPanel content = Assert.IsType<StackPanel>(window.Content);
        Assert.Equal("Window content", Assert.IsType<TextBlock>(content.VisualChildren[0]).Text);
        Border chrome = Assert.IsType<Border>(window.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, chrome.Background);
        Assert.Same(chrome, window.ComponentTemplateInstance.Parts["Chrome"]);
    }

    [Fact]
    public void WindowAspectCanProvideComponentTemplate()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class AspectWindow : Window { }
            """;
        const string markup = """
            <Window Background="Black">
              <Window.Resources>
                <Aspect Target="Window">
                  @template { <Border Name="AspectChrome" Background="$owner.Background" /> }
                </Aspect>
              </Window.Resources>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/AspectWindow.cui.xml",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.Views.AspectWindow", throwOnError: true)!;
        Window window = Assert.IsAssignableFrom<Window>(Activator.CreateInstance(type));
        Border chrome = Assert.IsType<Border>(window.ComponentTemplateInstance!.Root);
        Assert.Equal(Cerneala.Drawing.DrawColor.Black, chrome.Background);
        Assert.Same(chrome, window.ComponentTemplateInstance.Parts["AspectChrome"]);
    }

    [Fact]
    public void PairedWindowRejectsWrongRootConstructorAndConditionalRoot()
    {
        const string validSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class MainWindow : Window { }
            """;
        GeneratorRunResult wrongRoot = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            "<UserControl />",
            validSource,
            out _);
        Assert.Contains(wrongRoot.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI010");

        const string constructorSource = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class DialogWindow : Window
            {
                public DialogWindow() { }
            }
            """;
        GeneratorRunResult constructor = RunPairedGenerator(
            "Views/DialogWindow.cui.xml",
            "<Window />",
            constructorSource,
            out _);
        Assert.Contains(constructor.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI010");

        GeneratorRunResult conditionalRoot = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            "<Window>@when IsEnabled { @if value == True { <Button /> } }</Window>",
            validSource,
            out _);
        Assert.Contains(conditionalRoot.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI010");
    }

    [Fact]
    public void ExecutableMainWindowEmitsAutomaticEntryPointOrHostedDescriptor()
    {
        const string noEntryPoint = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class MainWindow : Window { }
            """;
        GeneratorRunResult standalone = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            "<Window Title=\"Standalone\" />",
            noEntryPoint,
            out Compilation standaloneCompilation,
            OutputKind.WindowsApplication);
        string standaloneSource = SingleGeneratedSource(standalone);
        Assert.Contains("[global::System.STAThreadAttribute]", standaloneSource);
        Assert.Contains("GeneratedWindowApplication.Run(CreateDescriptor())", standaloneSource);
        using MemoryStream standaloneStream = new();
        EmitResult standaloneEmit = standaloneCompilation.Emit(standaloneStream);
        Assert.True(standaloneEmit.Success, string.Join(Environment.NewLine, standaloneEmit.Diagnostics));

        const string existingEntryPoint = """
            using Cerneala.UI.Controls;
            namespace TestInput.Views;
            public partial class MainWindow : Window { }
            public static class Program { public static void Main() { } }
            """;
        GeneratorRunResult hosted = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            "<Window Title=\"Hosted\" />",
            existingEntryPoint,
            out Compilation hostedCompilation,
            OutputKind.ConsoleApplication);
        string hostedSource = SingleGeneratedSource(hosted);
        Assert.Contains("[global::System.Runtime.CompilerServices.ModuleInitializerAttribute]", hostedSource);
        Assert.Contains("GeneratedWindowApplication.RegisterStartup(CreateDescriptor())", hostedSource);
        using MemoryStream hostedStream = new();
        EmitResult hostedEmit = hostedCompilation.Emit(hostedStream);
        Assert.True(hostedEmit.Success, string.Join(Environment.NewLine, hostedEmit.Diagnostics));
    }

    [Fact]
    public void GeneratedStartupRegistersViewModelDependenciesAndCallsAppHookLast()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;
            using Microsoft.Extensions.DependencyInjection;
            namespace TestInput.Views;

            public interface IClock { }
            public sealed class Clock : IClock { }
            public sealed class MainWindowViewModel
            {
                public MainWindowViewModel(IClock clock) { }
            }
            public partial class MainWindow : Window<MainWindowViewModel> { }
            public static class App
            {
                public static void ConfigureServices(IServiceCollection services)
                    => services.AddSingleton<IClock, Clock>();
            }
            public static class Program { public static void Main() { } }
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainWindow.cui.xml",
            "<Window />",
            inputSource,
            out Compilation compilation,
            OutputKind.ConsoleApplication);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("AddTransient<global::TestInput.Views.MainWindowViewModel>", generatedSource);
        Assert.Contains("global::TestInput.Views.App.ConfigureServices(services);", generatedSource);
        Assert.True(
            generatedSource.IndexOf("AddTransient<global::TestInput.Views.MainWindow>", StringComparison.Ordinal) <
            generatedSource.IndexOf("App.ConfigureServices(services)", StringComparison.Ordinal));
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    private static GeneratorRunResult RunGenerator(string fileName, string markup, out Compilation outputCompilation)
    {
        return RunGenerator(new[] { new MarkupFile(fileName, markup) }, out outputCompilation);
    }

    private static GeneratorRunResult RunGeneratorWithInput(
        string fileName,
        string markup,
        string inputSource,
        out Compilation outputCompilation)
    {
        return RunGenerator(new[] { new MarkupFile(fileName, markup) }, out outputCompilation, inputSource);
    }

    private static GeneratorRunResult RunPairedGenerator(
        string fileName,
        string markup,
        string inputSource,
        out Compilation outputCompilation,
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        return RunGenerator(
            new[] { new MarkupFile(fileName, markup) },
            out outputCompilation,
            inputSource,
            fileName + ".cs",
            outputKind);
    }

    private static GeneratorRunResult RunGenerator(params MarkupFile[] files)
    {
        return RunGenerator(files, out _);
    }

    private static GeneratorRunResult RunGenerator(MarkupFile[] files, out Compilation outputCompilation)
    {
        return RunGenerator(files, out outputCompilation, "namespace TestInput { public static class Anchor { } }");
    }

    private static GeneratorRunResult RunGenerator(
        MarkupFile[] files,
        out Compilation outputCompilation,
        string inputSource,
        string inputPath = "",
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            inputSource,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            path: inputPath);

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            new[] { syntaxTree },
            References(),
            new CSharpCompilationOptions(outputKind));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new ISourceGenerator[] { new UiMarkupGenerator().AsSourceGenerator() },
            files.Select(file => new InMemoryAdditionalText(file.Path, file.Text)).ToArray(),
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out _);
        return driver.GetRunResult().Results.Single();
    }

    private static MetadataReference[] References()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location))
            .ToArray();
    }

    private static string SingleGeneratedSource(GeneratorRunResult result)
    {
        return result.GeneratedSources.Single().SourceText.ToString();
    }

    private static Diagnostic AssertDiagnostic(GeneratorRunResult result, string id, string path)
    {
        Diagnostic diagnostic = Assert.Single(result.Diagnostics, diagnostic => diagnostic.Id == id);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(path, diagnostic.Location.GetLineSpan().Path);
        return diagnostic;
    }

    private static UIElement InvokeCreate(MemoryStream stream, string typeName)
    {
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 0);
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, null));
    }

    private static UIElement InvokeCreate(MemoryStream stream, string typeName, object dataContext)
    {
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType(typeName, throwOnError: true)!;
        MethodInfo method = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        return Assert.IsAssignableFrom<UIElement>(method.Invoke(null, new[] { dataContext }));
    }

    private static int Count(string text, string value)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private readonly record struct MarkupFile(string Path, string Text);

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText text;

        public InMemoryAdditionalText(string path, string text)
        {
            Path = path;
            this.text = SourceText.From(text, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(System.Threading.CancellationToken cancellationToken = default)
        {
            return text;
        }
    }
}
