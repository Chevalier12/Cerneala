using System.Text;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics;
using Cerneala.Language.Semantics.Symbols;
using Cerneala.Language.Text;
using Cerneala.UI.Elements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cerneala.Tests.Language;

public sealed class SemanticScopesTests
{
    private const string ViewModels = """
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.Runtime.CompilerServices;
        using Cerneala.UI.Controls;
        namespace Demo;
        public sealed class Details : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Title { get; set; } = string.Empty;
        }
        public sealed class Row : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Name { get; set; } = string.Empty;
        }
        public sealed class ViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string Title { get; set; } = string.Empty;
            public Details Details { get; set; } = new();
            public List<Row> Rows { get; set; } = new();
        }
        public partial class MainView : UserControl
        {
            public string Caption { get; set; } = string.Empty;
            public List<Row> Rows { get; set; } = new();
        }
        """;

    [Fact]
    public void BindingQueriesResolveTypedSourcesSegmentsModesAndLocalDataContext()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <StackPanel Name="Host" DataContext="$DataContext.Details">
                <TextBlock Text="$DataContext.Title:TwoWay" />
                <TextBlock Text="$root.Caption" />
                <TextBlock Text="$Host.IsEnabled" />
                <TextBlock Text="$self.IsEnabled" />
              </StackPanel>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        Assert.Equal("Demo.ViewModel", SymbolAt(model, markup, "ViewModel").ValueType);
        Assert.Equal("Demo.Details", SymbolAt(model, markup, "Details").ValueType);
        Assert.Equal("string", SymbolAt(model, markup, "Title:TwoWay").ValueType);
        Assert.Equal(CernealaSemanticSymbolKind.BindingMode, SymbolAt(model, markup, "TwoWay").Kind);
        Assert.Equal("Demo.MainView", SymbolAt(model, markup, "$root").ValueType);
        Assert.Equal("Cerneala.UI.Controls.StackPanel", SymbolAt(model, markup, "$Host").ValueType);
        Assert.Equal("Cerneala.UI.Controls.TextBlock", SymbolAt(model, markup, "$self").ValueType);
    }

    // Issue #1209: the build resolved the root DataType with GetTypeByMetadataName only, so an xmlns alias that the
    // editor resolves (and that ContentTemplate DataType already accepts in the build) failed with CERNEALAUI007.
    [Theory]
    [InlineData("xmlns:d=\"clr-namespace:Demo\" DataType=\"d:ViewModel\"", "")]
    [InlineData("DataType=\"Demo.ViewModel\"", "")]
    // The unresolved DataType and the then untyped $DataContext.Title binding.
    [InlineData("xmlns:d=\"clr-namespace:Demo\" DataType=\"d:Missing\"", "CERNEALAUI007,CERNEALAUI007")]
    public void RootDataTypeReportsTheSameDiagnosticsInEditorAndBuild(string rootAttributes, string expectedIds)
    {
        string markup = "<UserControl " + rootAttributes + "><TextBlock Text=\"$DataContext.Title\" /></UserControl>";

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("MainView.crn", markup, "MainView.crn.cs", ViewModels);

        Assert.Empty(result.Syntax.Diagnostics);
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        string[] build = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(editor, build);
        Assert.Equal(expectedIds, string.Join(",", build));
    }

    [Theory]
    [InlineData("")]
    [InlineData(":OneWay")]
    public void AttributeOneWayRequiresAnObservableClrPathOwner(string mode)
    {
        const string source = """
            namespace Demo;
            public sealed class PlainViewModel
            {
                public string Name { get; set; } = "Initial";
            }
            """;
        string markup = $$"""
            <TextBlock DataType="Demo.PlainViewModel" Text="$DataContext.Name{{mode}}" />
            """;

        LanguageDiagnostic diagnostic = Assert.Single(Model("AttributeBinding.crn", markup, source).Diagnostics);
        Assert.Equal("CERNEALAUI007", diagnostic.Id);
        Assert.Contains("INotifyPropertyChanged", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void UnknownBindingSourceProducesOneDiagnosticWithoutSegmentCascade()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <TextBlock Text="$Missing.First.Second.Third" />
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        LanguageDiagnostic diagnostic = Assert.Single(model.Diagnostics);
        Assert.Equal("CERNEALAUI007", diagnostic.Id);
        Assert.DoesNotContain(model.Symbols, symbol => symbol.Name is "First" or "Second" or "Third");
    }

    private const string ExpressionViewModels = """
        using System.ComponentModel;
        namespace Demo;
        public enum Density { Compact, Wide }
        public sealed class ExpressionViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public bool HasName { get; set; }
            public bool IsValid { get; set; }
            public int Count { get; set; }
            public string Name { get; set; } = string.Empty;
            public Density Density { get; set; }
        }
        """;

    [Theory]
    [InlineData("$DataContext.HasName and $DataContext.IsValid")]
    [InlineData("($DataContext.HasName or $DataContext.IsValid) and $Host.IsEnabled")]
    [InlineData("$root.IsEnabled and $DataContext.IsValid")]
    [InlineData("$DataContext.Count &gt; 0")]
    [InlineData("$DataContext.Count >= 2")]
    [InlineData("$DataContext.Count &lt; 3 or $DataContext.IsValid")]
    [InlineData("$DataContext.Density == Compact")]
    [InlineData("$DataContext.Name != &quot;Ada&quot; and $DataContext.HasName == true")]
    public void BoolPropertyExpressionResolvesEverySourceWithoutDiagnostics(string value)
    {
        string markup = "<StackPanel Name=\"Host\" DataType=\"Demo.ExpressionViewModel\"><TextBlock IsEnabled=\"" +
            value + "\" /></StackPanel>";
        CernealaSemanticModel model = Model("PropertyExpression.crn", markup, ExpressionViewModels);

        Assert.Empty(model.Diagnostics);
        int valueStart = markup.IndexOf(value, StringComparison.Ordinal);
        int sources = model.Symbols.Count(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.BindingSource &&
            symbol.Span.Start >= valueStart &&
            symbol.Span.End <= valueStart + value.Length);
        int segments = model.Symbols.Count(symbol =>
            symbol.Kind == CernealaSemanticSymbolKind.BindingSegment &&
            symbol.Span.Start >= valueStart &&
            symbol.Span.End <= valueStart + value.Length);
        Assert.Equal(value.Count(character => character == '$'), sources);
        Assert.Equal(sources, segments);
    }

    // These are errors, but only the generator knows them (syntax, bare identifiers,
    // 'value', modes, self-reads and type rules). The shared model must stay silent so
    // the generator's own CERNEALAUI007 message reaches the build.
    [Theory]
    [InlineData("IsFocused and $DataContext.IsValid")]
    [InlineData("value and $DataContext.IsValid")]
    [InlineData("$DataContext.HasName:TwoWay and $DataContext.IsValid")]
    [InlineData("($DataContext.HasName and $DataContext.IsValid):OneWay")]
    [InlineData("$DataContext.Name and $DataContext.IsValid")]
    [InlineData("$DataContext.Count == $DataContext.Name")]
    [InlineData("$DataContext.HasName and")]
    [InlineData("$self.IsEnabled and $DataContext.IsValid")]
    [InlineData("$Host and $DataContext.IsValid")]
    [InlineData("$Accent and $DataContext.IsValid")]
    public void BoolPropertyExpressionLeavesGeneratorOnlyRulesToTheGenerator(string value)
    {
        string markup = "<StackPanel Name=\"Host\" DataType=\"Demo.ExpressionViewModel\"><StackPanel.Resources>" +
            "<SolidColorBrush Name=\"Accent\" Color=\"Red\" /></StackPanel.Resources><TextBlock IsEnabled=\"" +
            value + "\" /></StackPanel>";

        Assert.Empty(Model("PropertyExpressionGeneratorRule.crn", markup, ExpressionViewModels).Diagnostics);
    }

    [Fact]
    public void BoolPropertyExpressionReportsEachUnresolvedMemberOnItsOwnPath()
    {
        const string markup = "<StackPanel DataType=\"Demo.ExpressionViewModel\">" +
            "<TextBlock IsEnabled=\"$DataContext.HasNam and $DataContext.IsVali\" /></StackPanel>";
        CernealaSemanticModel model = Model("PropertyExpressionUnresolved.crn", markup, ExpressionViewModels);

        Assert.Collection(
            model.Diagnostics.OrderBy(diagnostic => diagnostic.Span.Start),
            diagnostic =>
            {
                Assert.Equal("CERNEALAUI007", diagnostic.Id);
                Assert.Equal(markup.IndexOf("HasNam", StringComparison.Ordinal), diagnostic.Span.Start);
                Assert.Contains("Readable property 'HasNam' was not found on 'Demo.ExpressionViewModel'.", diagnostic.Message, StringComparison.Ordinal);
            },
            diagnostic =>
            {
                Assert.Equal("CERNEALAUI007", diagnostic.Id);
                Assert.Equal(markup.IndexOf("IsVali", StringComparison.Ordinal), diagnostic.Span.Start);
                Assert.Contains("Readable property 'IsVali' was not found on 'Demo.ExpressionViewModel'.", diagnostic.Message, StringComparison.Ordinal);
            });
    }

    [Theory]
    [InlineData("Text", "$DataContext.HasName and $DataContext.IsValid", "string")]
    [InlineData("Opacity", "$DataContext.HasName and $DataContext.IsValid", "float")]
    [InlineData("Opacity", "IsFocused or $DataContext.IsValid", "float")]
    [InlineData("Opacity", "$DataContext.Count &gt; 0", "float")]
    public void PropertyExpressionOnNonBoolTargetReportsTheGeneratorMessage(string property, string value, string targetType)
    {
        string markup = "<StackPanel DataType=\"Demo.ExpressionViewModel\"><TextBlock " + property + "=\"" +
            value + "\" /></StackPanel>";

        LanguageDiagnostic diagnostic = Assert.Single(
            Model("PropertyExpressionNonBool.crn", markup, ExpressionViewModels).Diagnostics);
        Assert.Equal("CERNEALAUI007", diagnostic.Id);
        Assert.Contains(
            "Logical expressions are supported only for bool targets; the target type is '" + targetType + "'.",
            diagnostic.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void NameScopesSeparateTemplatePartsAndResolvePartQueries()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <StackPanel>
                <Button Name="Card">
                  @template {
                <Border Name="Part" IsEnabled="$owner.IsEnabled" />
                  }
                </Button>
                <TextBlock Text="$Card.parts.$Part.IsEnabled" />
              </StackPanel>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol owner = SymbolAt(model, markup, "$owner");
        CernealaSemanticSymbol part = SymbolAt(model, markup, "$Part");
        Assert.Equal("Cerneala.UI.Controls.Button", owner.ValueType);
        Assert.Equal(CernealaSemanticSymbolKind.TemplatePart, part.Kind);
        Assert.Equal("Cerneala.UI.Controls.Border", part.ValueType);
        Assert.NotNull(part.DefinitionLocation);
    }

    [Fact]
    public void ResourcesShadowApplicationResourcesAndDuplicateNamesAreReported()
    {
        const string application = """
            <Application>
              <Application.Resources>
                <SolidColorBrush Name="Accent" Color="#112233" />
              </Application.Resources>
            </Application>
            """;
        const string local = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Accent" Color="#445566" />
              </StackPanel.Resources>
              <Border Background="$Accent" />
            </StackPanel>
            """;
        CernealaSemanticModel model = Model(
            "Local.crn",
            local,
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol reference = SymbolAt(model, local, "$Accent");
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, reference.Kind);
        Assert.Equal("Local.crn", reference.DefinitionLocation?.Path);

        const string duplicate = """
            <StackPanel><StackPanel.Resources>
              <SolidColorBrush Name="Accent" Color="#112233" />
              <SolidColorBrush Name="Accent" Color="#445566" />
            </StackPanel.Resources></StackPanel>
            """;
        Assert.Contains(Model("Duplicate.crn", duplicate, string.Empty).Diagnostics,
            diagnostic => diagnostic.Id == "CERNEALAUI005" && diagnostic.Message.Contains("Duplicate resource Name", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("SpriteAnimationStage", "2")]
    public void SpriteAnimationResourceBindsToSpriteWithoutLeakingClipNamesIntoTheElementNamescope()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Resources>
                <SpriteAnimationSet Name="Animations">
                  <SpriteAnimationClip Name="Walk">
                    <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="100ms" />
                  </SpriteAnimationClip>
                  <SpriteAnimationClip Name="Idle">
                    <SpriteAnimationFrame SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="100ms" />
                  </SpriteAnimationClip>
                </SpriteAnimationSet>
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Name="Walk" Animations="$Animations" AnimationState="Walk" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        CernealaSemanticModel model = Model("Animation.crn", markup, string.Empty);

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol reference = SymbolAt(model, markup, "$Animations");
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, reference.Kind);
        Assert.Equal("Cerneala.UI.Controls.SpriteAnimationSet", reference.ValueType);
    }

    [Fact]
    public void SpriteAnimationSetCellAttributesAndCellFramesProduceNoLanguageDiagnostics()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Resources>
                <SpriteAnimationSet Name="Animations" CellWidth="32" CellHeight="32" CellMargin="2" CellSpacing="1">
                  <SpriteAnimationClip Name="Walk">
                    <SpriteAnimationFrame Column="0" Row="1" Duration="150ms" />
                    <SpriteAnimationFrame Column="1" Row="1" Duration="150ms" Flip="Horizontal" />
                  </SpriteAnimationClip>
                </SpriteAnimationSet>
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Animations="$Animations" AnimationState="Walk" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        CernealaSemanticModel model = Model("CellAnimation.crn", markup, string.Empty);

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol reference = SymbolAt(model, markup, "$Animations");
        Assert.Equal("Cerneala.UI.Controls.SpriteAnimationSet", reference.ValueType);
    }

    [Fact]
    public void SpriteAnimationSetSourceAttributeProducesNoLanguageDiagnostics()
    {
        const string markup = """
            <RenderSurface2D>
              <RenderSurface2D.Resources>
                <SpriteAnimationSet Name="Animations" Source="Assets/hero.json" Version="2" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Animations="$Animations" AnimationState="Walk" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        CernealaSemanticModel model = Model("AsepriteAnimation.crn", markup, string.Empty);

        Assert.Empty(model.Diagnostics);
        CernealaSemanticSymbol reference = SymbolAt(model, markup, "$Animations");
        Assert.Equal("Cerneala.UI.Controls.SpriteAnimationSet", reference.ValueType);
    }

    [Fact]
    public void ItemsTemplatesInferOrOverrideDataTypeAndModelContentOwnership()
    {
        const string markup = """
            <UserControl xmlns:d="clr-namespace:Demo" DataType="d:ViewModel">
              <ItemsControl ItemsSource="$DataContext.Rows">
                @templates
                {
                  <ContentTemplate DataType="d:Row" Key="row" Priority="1">
                    <TextBlock Text="$DataContext.Name" />
                  </ContentTemplate>
                }
                <ItemsControl.ItemsPanel><StackPanel /></ItemsControl.ItemsPanel>
              </ItemsControl>
            </UserControl>
            """;
        CernealaSemanticModel model = Model("MainView.crn", markup, ViewModels);

        Assert.Empty(model.Diagnostics);
        Assert.Equal("Demo.Row", SymbolAt(model, markup, "Row\" Key").ValueType);
        Assert.Equal("string", SymbolAt(model, markup, "Name\"").ValueType);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.ContentTemplate && symbol.ValueType == "Demo.Row");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.PropertyElement && symbol.Name == "ItemsPanel");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.ContentOwner && symbol.Name == "Content");
    }

    [Fact]
    public void AspectResourcesAssignmentsConditionsTemplatesAndApplicationSiteAreBound()
    {
        const string application = """
            <Application>
              <Application.Resources>
                <Aspect Name="Primary" TargetType="Button">
                  @default { IsEnabled = true; }
                  @when ($self.IsEnabled) { IsEnabled = false; }
                  @template { <Border Name="Chrome" /> }
                </Aspect>
              </Application.Resources>
            </Application>
            """;
        const string local = "<Button Aspect=\"$Primary\" />";
        CernealaSemanticModel applicationModel = Model("App.crn", application, string.Empty);
        CernealaSemanticModel localModel = Model(
            "Local.crn",
            local,
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));

        Assert.Empty(applicationModel.Diagnostics);
        Assert.Empty(localModel.Diagnostics);
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.Aspect && symbol.Name == "Primary");
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectAssignment && symbol.Name == "IsEnabled");
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectCondition);
        Assert.Contains(applicationModel.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.TemplatePart && symbol.Name == "Chrome");
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, SymbolAt(localModel, local, "$Primary").Kind);

        CernealaSemanticModel invalid = Model(
            "Invalid.crn",
            "<TextBlock Aspect=\"$Primary\" />",
            string.Empty,
            new CernealaDocument("App.crn", SourceText.From(application)));
        Assert.Contains(invalid.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI004");
    }

    [Fact]
    public void ValidBindingTemplateAndAspectCorpusMatchesSourceGeneratorDiagnostics()
    {
        (string Path, string Markup)[] cases =
        [
            ("Bindings.crn", "<StackPanel><Button Name=\"Source\" IsEnabled=\"true\"/><TextBlock Text=\"$Source.IsEnabled\" /></StackPanel>"),
            ("Templates.crn", "<Button>@template { <Border Name=\"Chrome\" IsEnabled=\"$owner.IsEnabled\" /> }</Button>"),
            ("Aspects.crn", "<StackPanel><StackPanel.Resources><Aspect Name=\"Primary\" TargetType=\"Button\">@default { IsEnabled = true; }</Aspect></StackPanel.Resources><Button Aspect=\"$Primary\" /></StackPanel>")
        ];

        foreach ((string path, string markup) in cases)
        {
            CernealaSemanticModel model = Model(path, markup, string.Empty);
            LanguagePipelineResult sourceGenerator = LanguagePipelineHarness.Analyze(path, markup);
            Assert.Empty(model.Diagnostics);
            Assert.Empty(sourceGenerator.SourceGeneratorDiagnostics);
        }
    }

    private const string AttachedPropertyCompanionSource = """
        using System.ComponentModel;
        using Cerneala.UI.Core;
        using Cerneala.UI.Elements;

        namespace Demo;

        public static class Layering
        {
            public static readonly UiProperty<int> LayerProperty = UiProperty<int>.Register(
                "Layer",
                typeof(Layering),
                new UiPropertyMetadata<int>(0));

            public static void SetLayer(UIElement element, int layer) => element.SetValue(LayerProperty, layer);
            public static int GetLayer(UIElement element) => element.GetValue(LayerProperty);
        }

        public static class Tagger
        {
            public static void SetTag(UIElement element, string tag) { }
            public static string GetTag(UIElement element) => string.Empty;
            public static void SetWeight(UIElement element, float weight) { }
            public static float GetWeight(UIElement element) => 0f;
        }

        public sealed class Row : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            public string X { get; set; } = string.Empty;
            public float W { get; set; }
            public int L { get; set; }
        }

        public class SubGrid : Cerneala.UI.Layout.Panels.Grid { }
        """;

    private static void AssertEditorAndBuildReportTheSameDiagnostics(string markup, string expectedIds)
    {
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(
            "AttachedParity.crn",
            markup,
            "AttachedParity.cs",
            AttachedPropertyCompanionSource);

        Assert.Empty(result.Syntax.Diagnostics);
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        string[] build = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(editor, build);
        Assert.Equal(expectedIds, string.Join(",", build));
    }

    [Theory]
    [InlineData("Canvas.Left=\"12\" Canvas.Top=\"20\"", "")]
    [InlineData("Canvas.Foo=\"1\"", "CERNEALAUI003")]
    [InlineData("Canvas.Left=\"abc\"", "CERNEALAUI004")]
    [InlineData("Grid.Row=\"1\"", "")]
    [InlineData("Panel.ZIndex=\"2\"", "")]
    [InlineData("Servo.Id=\"x\"", "")]
    [InlineData("AccessibleName.Name=\"Save\"", "CERNEALAUI003")]
    [InlineData("LayoutBoundary.IsBoundary=\"True\"", "CERNEALAUI003")]
    [InlineData("Tagger.Tag=\"x\"", "")]
    [InlineData("Tagger.Weight=\"2.5\"", "")]
    [InlineData("Tagger.Weight=\"abc\"", "CERNEALAUI004")]
    [InlineData("Tagger.Missing=\"x\"", "CERNEALAUI003")]
    [InlineData("Layering.Layer=\"2\"", "")]
    [InlineData("Layering.Layer=\"abc\"", "CERNEALAUI004")]
    public void AttachedPropertyMarkupReportsTheSameDiagnosticsInEditorAndBuild(string attachedAttributes, string expectedIds)
    {
        AssertEditorAndBuildReportTheSameDiagnostics(
            "<Canvas><Border Width=\"10\" Height=\"10\" " + attachedAttributes + " /></Canvas>",
            expectedIds);
    }

    // An internal SetTag on a base type of another assembly is not callable from generated code (the build reports
    // CERNEALAUI003), so the editor must not accept it either.
    [Fact]
    public void InternalAttachedSetterOfAReferencedAssemblyIsRejectedInEditorAndBuild()
    {
        MetadataReference library = LanguagePipelineHarness.CreateLibraryReference(
            "TaggerLibrary",
            "public class LibTaggerBase { internal static void SetTag(Cerneala.UI.Elements.UIElement element, string value) { } }");

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(
            "InternalSetter.crn",
            "<Canvas><Border Width=\"10\" Height=\"10\" AppTagger.Tag=\"x\" /></Canvas>",
            "InternalSetter.cs",
            "public class AppTagger : LibTaggerBase { }",
            [library]);

        Assert.Empty(result.Syntax.Diagnostics);
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        string[] build = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(editor, build);
        Assert.Equal("CERNEALAUI003", string.Join(",", build));
    }

    // A UserControl pair is compiled in the same assembly as the markup, and the generator checks accessibility against
    // the pair type, which honors [InternalsVisibleTo]; the editor must accept the same internal setter.
    [Theory]
    [InlineData(MetadataImportOptions.All)]
    [InlineData(MetadataImportOptions.Public)]
    public void InternalAttachedSetterVisibleThroughInternalsVisibleToIsAcceptedInEditorAndBuildForAPairedUserControl(
        MetadataImportOptions importOptions)
    {
        MetadataReference library = LanguagePipelineHarness.CreateLibraryReference(
            "TaggerLibrary",
            "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"LanguageCorpus\")]\n" +
            "public class LibTaggerBase { internal static void SetTag(Cerneala.UI.Elements.UIElement element, string value) { } }");

        LanguagePipelineResult result = LanguagePipelineHarness.Analyze(
            "TaggedView.crn",
            "<UserControl><Border Width=\"10\" Height=\"10\" AppTagger.Tag=\"x\" /></UserControl>",
            "TaggedView.crn.cs",
            "public class AppTagger : LibTaggerBase { }\n" +
            "public partial class TaggedView : Cerneala.UI.Controls.UserControl { }",
            [library],
            importOptions);

        Assert.Empty(result.Syntax.Diagnostics);
        string[] editor = result.SemanticDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        string[] build = result.SourceGeneratorDiagnostics.Select(diagnostic => diagnostic.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal("build: editor:", "build:" + string.Join(",", build) + " editor:" + string.Join(",", editor));
    }

    // A binding is valid only on an attached property backed by a UiProperty (Canvas.Left); on a setter-only
    // one (Tagger.Tag, Grid.Row) the editor and the build both report CERNEALAUI004.
    [Theory]
    [InlineData("Canvas.Left=\"$DataContext.W\"", "")]
    [InlineData("Layering.Layer=\"$DataContext.L\"", "")]
    [InlineData("Tagger.Tag=\"$DataContext.X\"", "CERNEALAUI004")]
    [InlineData("Tagger.Weight=\"$DataContext.W\"", "CERNEALAUI004")]
    [InlineData("Grid.Row=\"$DataContext.W\"", "CERNEALAUI004")]
    public void AttachedPropertyBindingReportsTheSameDiagnosticsInEditorAndBuild(string attachedAttributes, string expectedIds)
    {
        AssertEditorAndBuildReportTheSameDiagnostics(
            "<ItemsControl><ItemsControl.ItemTemplate><ContentTemplate DataType=\"Demo.Row\">" +
            "<Border Width=\"10\" Height=\"10\" " + attachedAttributes + " />" +
            "</ContentTemplate></ItemsControl.ItemTemplate></ItemsControl>",
            expectedIds);
    }

    // Grid.SetRow/SetColumn throw for an index below 0 and Grid.SetRowSpan/SetColumnSpan for a span below 1, so the
    // build rejects those literals (CERNEALAUI004) and the editor must report the same diagnostic on the value. The rule
    // follows the setter, not the markup name: SubGrid.Row (SubGrid : Grid) calls the same Grid.SetRow.
    [Theory]
    [InlineData("Grid.Row", "-1", "CERNEALAUI004")]
    [InlineData("Grid.Column", "-1", "CERNEALAUI004")]
    [InlineData("Grid.RowSpan", "0", "CERNEALAUI004")]
    [InlineData("Grid.ColumnSpan", "0", "CERNEALAUI004")]
    [InlineData("Grid.Row", "0", "")]
    [InlineData("Grid.Column", "0", "")]
    [InlineData("Grid.RowSpan", "2", "")]
    [InlineData("Grid.ColumnSpan", "1", "")]
    [InlineData("SubGrid.Row", "-1", "CERNEALAUI004")]
    [InlineData("SubGrid.Column", "-1", "CERNEALAUI004")]
    [InlineData("SubGrid.RowSpan", "0", "CERNEALAUI004")]
    [InlineData("SubGrid.ColumnSpan", "0", "CERNEALAUI004")]
    [InlineData("SubGrid.Row", "1", "")]
    [InlineData("SubGrid.RowSpan", "2", "")]
    public void GridPlacementOutOfRangeReportsTheSameDiagnosticsInEditorAndBuild(
        string property,
        string value,
        string expectedIds)
    {
        string markup = "<Grid><Border " + property + "=\"" + value + "\" /></Grid>";
        AssertEditorAndBuildReportTheSameDiagnostics(markup, expectedIds);

        if (expectedIds.Length > 0)
        {
            LanguagePipelineResult result = LanguagePipelineHarness.Analyze(
                "AttachedParity.crn",
                markup,
                "AttachedParity.cs",
                AttachedPropertyCompanionSource);
            HarnessDiagnostic editor = Assert.Single(result.SemanticDiagnostics);
            int valueStart = markup.IndexOf("=\"" + value, StringComparison.Ordinal) + 1;
            Assert.Equal(
                (0, valueStart, 0, valueStart + value.Length + 2),
                (editor.StartLine, editor.StartCharacter, editor.EndLine, editor.EndCharacter));
            Assert.Equal(editor, Assert.Single(result.SourceGeneratorDiagnostics));
        }
    }

    [Theory]
    [InlineData("<Aspect>@default { Opacity = 0.5; }</Aspect>")]
    [InlineData("<Aspect />")]
    [InlineData("<!-- legacy wrapper --> <Aspect>@default { Opacity = 0.5; }</Aspect>")]
    public void InlineAspectRejectsRedundantWrapperWithSharedDiagnostic(string body)
    {
        string markup = "<Button><Button.Aspect>" + body + "</Button.Aspect></Button>";
        LanguagePipelineResult result = LanguagePipelineHarness.Analyze("InlineAspect.crn", markup);

        Assert.Empty(result.Syntax.Diagnostics);
        HarnessDiagnostic diagnostic = Assert.Single(result.SemanticDiagnostics);
        Assert.Equal("CERNEALAUI005", diagnostic.Id);
        Assert.Equal("Error", diagnostic.Severity);
        Assert.Contains("<Button.Aspect> already declares the inline Aspect", diagnostic.Message, StringComparison.Ordinal);
        Assert.Equal(markup.IndexOf("<Aspect", StringComparison.Ordinal) + 1, diagnostic.StartCharacter);
        Assert.Equal(diagnostic.StartCharacter + "Aspect".Length, diagnostic.EndCharacter);
        Assert.Equal(diagnostic, Assert.Single(result.SourceGeneratorDiagnostics));

        CernealaDocument document = new("InlineAspect.crn", SourceText.From(markup));
        using CernealaCompilation editor = new(
            new RoslynCompilationSymbols(CreateCompilation(string.Empty, "InlineAspect.crn.cs")),
            [document], AnalysisMode.Editor);
        LanguageDiagnostic editorDiagnostic = Assert.Single(editor.GetSemanticModel(document.Path).Diagnostics);
        Assert.Equal(diagnostic.Id, editorDiagnostic.Id);
        Assert.Equal(diagnostic.Message, editorDiagnostic.Message);
        Assert.Equal(LanguageDiagnosticSeverity.Error, editorDiagnostic.Severity);
        Assert.Equal(diagnostic.StartCharacter, editorDiagnostic.Span.Start);
        Assert.Equal("Aspect".Length, editorDiagnostic.Span.Length);
    }

    [Fact]
    public void DirectInlineAspectBindsDefaultsConditionsMotionAndTemplateDeclaration()
    {
        const string markup = """
            <Button>
              <Button.Aspect>
                @default { Opacity = 0.5; }
                @when IsMouseOver { Opacity = 0.8; }
                @on Click { @animate with Tween(100ms) { @from { Opacity = 0.5; } @to { Opacity = 1; } } }
                @template { <Border Name="Chrome" IsEnabled="$owner.IsEnabled" /> }
              </Button.Aspect>
            </Button>
            """;
        CernealaSemanticModel model = Model("DirectInlineAspect.crn", markup, string.Empty);

        Assert.Empty(model.Diagnostics);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectAssignment && symbol.Name == "Opacity");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.AspectCondition);
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.MotionEvent && symbol.Name == "Click");
        Assert.Contains(model.Symbols, symbol => symbol.Kind == CernealaSemanticSymbolKind.TemplatePart && symbol.Name == "Chrome");
    }

    [Fact]
    public void RealPresentationBindingTemplateAndAspectMarkupHasNoCommonSemanticDivergences()
    {
        string root = CorpusCatalog.RepositoryRoot();
        string presentation = Path.Combine(root, "CernealaPresentation");
        string[] paths = Directory.EnumerateFiles(presentation, "*.crn")
            .Where(path =>
            {
                string text = File.ReadAllText(path);
                return text.Contains('$') || text.Contains("@template", StringComparison.Ordinal) || text.Contains("<Aspect", StringComparison.Ordinal);
            })
            .ToArray();
        Assert.NotEmpty(paths);

        SyntaxTree[] trees = Directory.EnumerateFiles(presentation, "*.cs", SearchOption.TopDirectoryOnly)
            .Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path, encoding: Encoding.UTF8))
            .ToArray();
        List<MetadataReference> references = PlatformReferences().ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location));
        CSharpCompilation project = CSharpCompilation.Create(
            "CernealaPresentation.LanguageParity",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        CernealaDocument[] documents = Directory.EnumerateFiles(presentation, "*.crn")
            .Select(path => new CernealaDocument(path, SourceText.From(File.ReadAllText(path))))
            .ToArray();
        using CernealaCompilation workspace = new(new RoslynCompilationSymbols(project), documents, AnalysisMode.Build);
        foreach (string path in paths)
        {
            Assert.All(workspace.GetSemanticModel(path).Diagnostics,
                diagnostic => Assert.DoesNotContain(diagnostic.Id, new[] { "CERNEALAUI006", "CERNEALAUI007", "CERNEALAUI012" }));
        }
    }

    private static CernealaSemanticSymbol SymbolAt(CernealaSemanticModel model, string markup, string needle)
    {
        int offset = markup.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, "Needle not found: " + needle);
        offset += needle.StartsWith("$", StringComparison.Ordinal) ? 1 : 0;
        return Assert.IsType<CernealaSemanticSymbol>(model.GetSymbolAt(offset));
    }

    [Fact]
    public void SpriteImageReferenceAcceptsOnlyImageResourcesAndPreservesSemanticNavigation()
    {
        const string markup = """
            <RenderSurface2D xmlns:r="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
              <RenderSurface2D.Resources>
                <r:ImageResource Name="Atlas" Source="atlas.png" />
                <SolidColorBrush Name="Ink" Color="Black" />
              </RenderSurface2D.Resources>
              <RenderSurface2D.Scene>
                <Scene2D><Sprite2D Image="$Atlas" X="2" Y="3" SourceWidth="16" SourceHeight="12" /></Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;
        CernealaSemanticModel model = Model("SpriteImage.crn", markup, string.Empty);
        Assert.Empty(model.Diagnostics);
        Assert.Equal(CernealaSemanticSymbolKind.ResourceReference, SymbolAt(model, markup, "$Atlas").Kind);
        Assert.Contains(Model("InvalidSpriteImage.crn", markup.Replace("Image=\"$Atlas\"", "Image=\"$Ink\""), string.Empty).Diagnostics,
            d => d.Id == "CERNEALAUI004");
    }

    private static CernealaSemanticModel Model(
        string path,
        string markup,
        string source,
        params CernealaDocument[] additionalDocuments)
    {
        CSharpCompilation project = CreateCompilation(source, path + ".cs");
        CernealaDocument document = new(path, SourceText.From(markup));
        CernealaCompilation workspace = new(
            new RoslynCompilationSymbols(project),
            additionalDocuments.Prepend(document),
            AnalysisMode.Build);
        return workspace.GetSemanticModel(path);
    }

    private static CSharpCompilation CreateCompilation(string source, string path)
    {
        List<MetadataReference> references = PlatformReferences().ToList();
        references.Add(MetadataReference.CreateFromFile(typeof(UIElement).Assembly.Location));
        SyntaxTree[] trees = source.Length == 0
            ? []
            : [CSharpSyntaxTree.ParseText(source, path: path, encoding: Encoding.UTF8)];
        return CSharpCompilation.Create(
            "SemanticScopeTests",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static IEnumerable<MetadataReference> PlatformReferences()
    {
        string trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
            ?? throw new InvalidOperationException("Trusted platform assemblies are unavailable.");
        return trustedAssemblies.Split(Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
    }
}
