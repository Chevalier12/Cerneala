using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cerneala.Drawing;
using Cerneala.SourceGen;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Grid = Cerneala.UI.Layout.Panels.Grid;
using GridLength = Cerneala.UI.Layout.Panels.GridLength;

namespace Cerneala.Tests.SourceGen;

public sealed partial class UiMarkupGeneratorTests
{
    private const string DefaultBackendSelectionSource = """
        [assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
            typeof(TestInput.TestApplicationBackend))]

        namespace TestInput
        {
            public static class TestApplicationBackend
            {
                public static void EnsureRegistered() { }
            }
        }
        """;

    [Fact]
    public void ContentTemplateResourceIsRejected()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <ContentTemplate DataType="System.String">
                  <Border Background="#FF123456">
                    <TextBlock Text="TEMPLATE" />
                  </Border>
                </ContentTemplate>
              </StackPanel.Resources>
              <ItemsControl />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "DeclarativeContentTemplate.crn",
            markup,
            out Compilation compilation);
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Id == "CERNEALAUI005");
        Assert.Contains("cannot be declared in Resources", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ItemsControlOwnsMultipleInlineContentTemplates()
    {
        const string markup = """
            <ItemsControl>
              @templates
              {
                <ContentTemplate DataType="System.String">
                  <TextBlock Text="STRING" />
                </ContentTemplate>
                <ContentTemplate DataType="System.Int32">
                  <TextBlock Text="INTEGER" />
                </ContentTemplate>
              }
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGenerator(
            "ItemsControlTemplates.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        UIElement items = InvokeCreate(stream, "Cerneala.GeneratedUi.ItemsControlTemplatesFactory");
        Assert.Equal("ItemsControl", items.GetType().Name);
        IReadOnlyList<Cerneala.UI.Controls.Templates.ContentTemplate> templates =
            Assert.IsAssignableFrom<IReadOnlyList<Cerneala.UI.Controls.Templates.ContentTemplate>>(
                items.GetType().GetProperty("Templates")!.GetValue(items));
        Assert.Equal(2, templates.Count);
        Assert.Equal(typeof(string), templates[0].DataType);
        Assert.Equal(typeof(int), templates[1].DataType);
    }

    [Fact]
    public void LegacyTemplatesPropertyElementIsRejected()
    {
        const string markup = """
            <ItemsControl>
              <ItemsControl.Templates>
                <ContentTemplate DataType="System.String">
                  <TextBlock Text="LEGACY" />
                </ContentTemplate>
              </ItemsControl.Templates>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGenerator(
            "LegacyItemsControlTemplates.crn",
            markup,
            out Compilation compilation);
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Id == "CERNEALAUI005");
        Assert.Contains("use @templates", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<ItemsControl>@templates { }</ItemsControl>", "CERNEALAUI006", "one or more")]
    [InlineData("<Button>@templates { <ContentTemplate DataType=\"System.String\"><TextBlock /></ContentTemplate> }</Button>", "CERNEALAUI005", "only inside")]
    [InlineData("<ItemsControl>@templates { <TextBlock /> }</ItemsControl>", "CERNEALAUI005", "only ContentTemplate")]
    [InlineData("<StackPanel><StackPanel.Resources><Aspect TargetType=\"Button\">@templates { <ContentTemplate DataType=\"System.String\"><TextBlock /></ContentTemplate> }</Aspect></StackPanel.Resources></StackPanel>", "CERNEALAUI006", "Aspect bodies may contain only")]
    public void TemplatesDirectiveRejectsInvalidShape(string markup, string diagnosticId, string message)
    {
        GeneratorRunResult result = RunGenerator(
            "InvalidTemplatesDirective.crn",
            markup,
            out Compilation compilation);

        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Id == diagnosticId);
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void ComponentTemplateCanContainItemsControlTemplatesDirective()
    {
        const string markup = """
            <Button>
              @template
              {
                <ItemsControl>
                  @templates
                  {
                    <ContentTemplate DataType="System.String">
                      <TextBlock Text="ITEM" />
                    </ContentTemplate>
                  }
                </ItemsControl>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator(
            "NestedTemplatesDirective.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Fact]
    public void RenderSurfaceOwnsDeclarativeSceneWithTemplatedItems()
    {
        const string inputSource = """
            using System.Collections;
            using System.ComponentModel;
            using Cerneala.Drawing;

            namespace TestInput;

            public sealed class RootModel : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public SceneModel Scene { get; } = new();
            }

            public sealed class SceneModel
            {
                public IEnumerable Items { get; } = new SpriteModel[0];
                public IDrawImage? CurrentImage { get; set; }
                public DrawRect CurrentDestination { get; set; }
            }

            public sealed class SpriteModel
            {
                public IDrawImage? Image { get; set; }
                public DrawRect Destination { get; set; }
            }
            """;
        const string markup = """
            <RenderSurface2D
                xmlns:models="clr-namespace:TestInput;assembly=GeneratorTests"
                DataType="TestInput.RootModel"
                DataContext="$DataContext.Scene"
                Stretch="Uniform">
              <RenderSurface2D.Scene>
                <Scene2D>
                  <SceneItems2D ItemsSource="$DataContext.Items">
                    @templates
                    {
                      <ContentTemplate DataType="models:SpriteModel">
                        <Sprite2D Source="$DataContext.Image" Destination="$DataContext.Destination" />
                      </ContentTemplate>
                    }
                  </SceneItems2D>
                  <Sprite2D Name="CurrentPiece" Source="$DataContext.CurrentImage" Destination="$DataContext.CurrentDestination" />
                </Scene2D>
              </RenderSurface2D.Scene>
            </RenderSurface2D>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "RenderSurfaceScene.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        object dataContext = Activator.CreateInstance(
            assembly.GetType("TestInput.RootModel", throwOnError: true)!)!;
        Type factoryType = assembly.GetType(
            "Cerneala.GeneratedUi.RenderSurfaceSceneFactory",
            throwOnError: true)!;
        MethodInfo create = factoryType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        RenderSurface2D surface = Assert.IsType<RenderSurface2D>(
            create.Invoke(null, new[] { dataContext }));
        Assert.NotNull(surface.Scene);
        Assert.Equal(2, surface.Scene.Children.Count);
        SceneItems2D items = Assert.IsType<SceneItems2D>(surface.Scene.Children[0]);
        Assert.Single(items.Templates);
        Assert.Equal("TestInput.SpriteModel", items.Templates[0].DataType?.FullName);
        Assert.IsType<Sprite2D>(surface.Scene.Children[1]);
    }

    [Fact]
    public void ItemsControlOwnsDirectItemsPanelFromMarkup()
    {
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemsPanel>
                <StackPanel Orientation="Horizontal" />
              </ItemsControl.ItemsPanel>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGenerator(
            "ItemsControlPanel.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        ItemsControl items = Assert.IsType<ItemsControl>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.ItemsControlPanelFactory"));
        StackPanel panel = Assert.IsType<StackPanel>(items.ItemsPanel);
        Assert.Equal(Orientation.Horizontal, panel.Orientation);
    }

    [Fact]
    public void DerivedItemsControlOwnsInlineContentTemplates()
    {
        const string markup = """
            <ComboBox>
              @templates
              {
                <ContentTemplate DataType="System.String">
                  <TextBlock Text="STRING" />
                </ContentTemplate>
              }
            </ComboBox>
            """;

        GeneratorRunResult result = RunGenerator(
            "ComboBoxTemplates.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        ComboBox comboBox = Assert.IsType<ComboBox>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.ComboBoxTemplatesFactory"));
        Assert.Single(comboBox.Templates);
        Assert.Equal(typeof(string), comboBox.Templates[0].DataType);
    }

    [Fact]
    public void ContentTemplateDataTypeResolvesClrNamespaceAlias()
    {
        const string inputSource = """
            namespace TestInput;

            public sealed class PropertyRow
            {
                public string Label { get; set; } = "BOUND";
            }
            """;
        const string markup = """
            <ItemsControl
                xmlns:rows="clr-namespace:TestInput"
                xmlns:controls="clr-namespace:Cerneala.UI.Controls;assembly=Cerneala">
              @templates
              {
                <ContentTemplate DataType="rows:PropertyRow">
                  <TextBlock Text="ROW" />
                </ContentTemplate>
                <ContentTemplate DataType="controls:Button">
                  <TextBlock Text="BUTTON" />
                </ContentTemplate>
              }
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "AliasedContentTemplate.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        ItemsControl items = Assert.IsType<ItemsControl>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.AliasedContentTemplateFactory"));
        Assert.Equal(2, items.Templates.Count);
        Assert.Equal("TestInput.PropertyRow", items.Templates[0].DataType?.FullName);
        Assert.Equal(typeof(Button), items.Templates[1].DataType);
    }

    [Fact]
    public void CustomElementRequiresClrNamespaceAlias()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;

            namespace TestInput;

            public sealed class FancyControl : Border
            {
            }
            """;
        const string importedMarkup = """
            <views:FancyControl xmlns:views="clr-namespace:TestInput;assembly=GeneratorTests" />
            """;

        GeneratorRunResult imported = RunGeneratorWithInput(
            "ImportedFancyControl.crn",
            importedMarkup,
            inputSource,
            out Compilation importedCompilation);

        AssertNoGeneratorOrCompilationErrors(imported, importedCompilation);
        Assert.Contains("global::TestInput.FancyControl", SingleGeneratedSource(imported), StringComparison.Ordinal);

        GeneratorRunResult implicitType = RunGeneratorWithInput(
            "ImplicitFancyControl.crn",
            "<FancyControl />",
            inputSource,
            out _);

        Assert.Contains(implicitType.Diagnostics, diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            diagnostic.GetMessage().Contains("FancyControl", StringComparison.Ordinal));
    }

    [Fact]
    public void NamedContentTemplateResourceIsRejected()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <ContentTemplate Name="PropertyRow" DataType="System.String">
                  <TextBlock Text="ROW" />
                </ContentTemplate>
              </StackPanel.Resources>
              <ItemsControl ItemTemplate="$PropertyRow" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "NamedContentTemplate.crn",
            markup,
            out Compilation compilation);
        Diagnostic diagnostic = Assert.Single(
            result.Diagnostics,
            candidate => candidate.Id == "CERNEALAUI005");
        Assert.Contains("cannot be declared in Resources", diagnostic.GetMessage(), StringComparison.Ordinal);
    }

    [Fact]
    public void InlineContentTemplateBindsAgainstItsOwnDataType()
    {
        const string inputSource = """
            using System.ComponentModel;

            namespace TestInput;

            public sealed class PropertyRow : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Label { get; set; } = "BOUND";
                public string AutomationId { get; set; } = "bound-row";
            }
            """;
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemTemplate>
                <ContentTemplate DataType="TestInput.PropertyRow">
                  <TextBlock
                    AutomationProperties.AutomationId="$DataContext.AutomationId"
                    Text="$DataContext.Label" />
                </ContentTemplate>
              </ItemsControl.ItemTemplate>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "InlineContentTemplate.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("ObserveDataPath(contentTemplateContext", SingleGeneratedSource(result), StringComparison.Ordinal);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        ItemsControl items = Assert.IsType<ItemsControl>(
            assembly.GetType("Cerneala.GeneratedUi.InlineContentTemplateFactory", throwOnError: true)!
                .GetMethod("Create", Type.EmptyTypes)!
                .Invoke(null, null));
        object row = Activator.CreateInstance(assembly.GetType("TestInput.PropertyRow", throwOnError: true)!)!;
        TextBlock text = Assert.IsType<TextBlock>(items.ItemTemplate!.Create(
            new Cerneala.UI.Controls.Templates.ContentTemplateContext(row, owner: items)));

        Assert.Equal(row, text.DataContext);
        Assert.Equal("BOUND", text.Text);
        Assert.Equal("bound-row", Cerneala.UI.Automation.AutomationProperties.GetAutomationId(text));
    }

    [Fact]
    public void ContentTemplateRootDataContextScopesDescendantBindingsAndRetargets()
    {
        const string inputSource = """
            using System.ComponentModel;

            namespace TestInput;

            public sealed class PropertyRow : INotifyPropertyChanged
            {
                private RowDetails details = new() { Name = "FIRST" };
                public event PropertyChangedEventHandler? PropertyChanged;
                public RowDetails Details
                {
                    get => details;
                    set
                    {
                        details = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Details)));
                    }
                }
            }

            public sealed class RowDetails : INotifyPropertyChanged
            {
                private string name = string.Empty;
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name
                {
                    get => name;
                    set
                    {
                        name = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                    }
                }
            }
            """;
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemTemplate>
                <ContentTemplate DataType="TestInput.PropertyRow">
                  <StackPanel DataContext="$DataContext.Details:OneWay">
                    <TextBlock Text="$DataContext.Name:OneWay" />
                  </StackPanel>
                </ContentTemplate>
              </ItemsControl.ItemTemplate>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "ScopedRootDataContext.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(
            ".DataContext = contentTemplateContext",
            SingleGeneratedSource(result),
            StringComparison.Ordinal);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        ItemsControl items = Assert.IsType<ItemsControl>(
            assembly.GetType("Cerneala.GeneratedUi.ScopedRootDataContextFactory", throwOnError: true)!
                .GetMethod("Create", Type.EmptyTypes)!
                .Invoke(null, null));
        Type rowType = assembly.GetType("TestInput.PropertyRow", throwOnError: true)!;
        Type detailsType = assembly.GetType("TestInput.RowDetails", throwOnError: true)!;
        object row = Activator.CreateInstance(rowType)!;
        object firstDetails = rowType.GetProperty("Details")!.GetValue(row)!;
        StackPanel panel = Assert.IsType<StackPanel>(items.ItemTemplate!.Create(
            new Cerneala.UI.Controls.Templates.ContentTemplateContext(row, owner: items)));
        TextBlock text = Assert.IsType<TextBlock>(panel.VisualChildren[0]);

        Assert.Same(firstDetails, panel.DataContext);
        Assert.Equal("FIRST", text.Text);

        detailsType.GetProperty("Name")!.SetValue(firstDetails, "UPDATED");
        Assert.Equal("UPDATED", text.Text);

        object replacement = Activator.CreateInstance(detailsType)!;
        detailsType.GetProperty("Name")!.SetValue(replacement, "REPLACED");
        rowType.GetProperty("Details")!.SetValue(row, replacement);

        Assert.Same(replacement, panel.DataContext);
        Assert.Equal("REPLACED", text.Text);
    }

    [Fact]
    public void NestedDataContextScopesOnlyItsSubtree()
    {
        const string inputSource = """
            using System.ComponentModel;

            namespace TestInput;

            public sealed class PropertyRow : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Label { get; set; } = "ROW";
                public RowDetails Details { get; set; } = new() { Name = "DETAILS" };
            }

            public sealed class RowDetails : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public string Name { get; set; } = string.Empty;
            }
            """;
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemTemplate>
                <ContentTemplate DataType="TestInput.PropertyRow">
                  <StackPanel>
                    <TextBlock Text="$DataContext.Label" />
                    <Border DataContext="$DataContext.Details">
                      <TextBlock Text="$DataContext.Name" />
                    </Border>
                    <TextBlock Text="$DataContext.Label" />
                  </StackPanel>
                </ContentTemplate>
              </ItemsControl.ItemTemplate>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "NestedScopedDataContext.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        ItemsControl items = Assert.IsType<ItemsControl>(
            assembly.GetType("Cerneala.GeneratedUi.NestedScopedDataContextFactory", throwOnError: true)!
                .GetMethod("Create", Type.EmptyTypes)!
                .Invoke(null, null));
        Type rowType = assembly.GetType("TestInput.PropertyRow", throwOnError: true)!;
        object row = Activator.CreateInstance(rowType)!;
        object details = rowType.GetProperty("Details")!.GetValue(row)!;
        StackPanel panel = Assert.IsType<StackPanel>(items.ItemTemplate!.Create(
            new Cerneala.UI.Controls.Templates.ContentTemplateContext(row, owner: items)));

        Assert.Equal("ROW", Assert.IsType<TextBlock>(panel.VisualChildren[0]).Text);
        Border border = Assert.IsType<Border>(panel.VisualChildren[1]);
        Assert.Same(details, border.DataContext);
        Assert.Equal("DETAILS", Assert.IsType<TextBlock>(border.Child).Text);
        Assert.Equal("ROW", Assert.IsType<TextBlock>(panel.VisualChildren[2]).Text);
    }

    [Fact]
    public void InlineContentTemplateTwoWayBindingWritesBackToItsDataItem()
    {
        const string inputSource = """
            using System.ComponentModel;

            namespace TestInput;

            public sealed class PropertyRow : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Enabled { get; set; }
            }
            """;
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemTemplate>
                <ContentTemplate DataType="TestInput.PropertyRow">
                  <CheckBox IsChecked="$DataContext.Enabled:TwoWay" />
                </ContentTemplate>
              </ItemsControl.ItemTemplate>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "TwoWayContentTemplate.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        ItemsControl items = Assert.IsType<ItemsControl>(
            assembly.GetType("Cerneala.GeneratedUi.TwoWayContentTemplateFactory", throwOnError: true)!
                .GetMethod("Create", Type.EmptyTypes)!
                .Invoke(null, null));
        Type rowType = assembly.GetType("TestInput.PropertyRow", throwOnError: true)!;
        object row = Activator.CreateInstance(rowType)!;
        CheckBox checkBox = Assert.IsType<CheckBox>(items.ItemTemplate!.Create(
            new Cerneala.UI.Controls.Templates.ContentTemplateContext(row, owner: items)));

        checkBox.IsChecked = true;

        Assert.True((bool)rowType.GetProperty("Enabled")!.GetValue(row)!);
    }

    [Fact]
    public void NestedContentTemplateTwoWayBindingRemainsActiveAfterAttach()
    {
        const string inputSource = """
            using System.ComponentModel;

            namespace TestInput;

            public sealed class PropertyRow : INotifyPropertyChanged
            {
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Enabled { get; set; }
            }
            """;
        const string markup = """
            <ItemsControl>
              <ItemsControl.ItemTemplate>
                <ContentTemplate DataType="TestInput.PropertyRow">
                  <Border>
                    <CheckBox IsChecked="$DataContext.Enabled:TwoWay" />
                  </Border>
                </ContentTemplate>
              </ItemsControl.ItemTemplate>
            </ItemsControl>
            """;

        GeneratorRunResult result = RunGeneratorWithInput(
            "NestedTwoWayContentTemplate.crn",
            markup,
            inputSource,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        ItemsControl items = Assert.IsType<ItemsControl>(
            assembly.GetType("Cerneala.GeneratedUi.NestedTwoWayContentTemplateFactory", throwOnError: true)!
                .GetMethod("Create", Type.EmptyTypes)!
                .Invoke(null, null));
        Type rowType = assembly.GetType("TestInput.PropertyRow", throwOnError: true)!;
        object row = Activator.CreateInstance(rowType)!;
        items.ItemsSource = new[] { row };
        UIRoot root = new(320, 200);
        root.VisualChildren.Add(items);
        root.ProcessFrame();
        root.ProcessFrame();
        CheckBox checkBox = Assert.Single(Descendants(items).OfType<CheckBox>());

        checkBox.IsChecked = true;

        Assert.True((bool)rowType.GetProperty("Enabled")!.GetValue(row)!);
    }

    private static System.Collections.Generic.IEnumerable<UIElement> Descendants(UIElement element)
    {
        foreach (UIElement child in element.VisualChildren)
        {
            yield return child;
            foreach (UIElement descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    [Fact]
    public void LocalDefaultAspectAppliesToDynamicallyAddedDescendants()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @default
                  {
                    Background = "#FF123456";
                    Foreground = "#FFF0F1F2";
                  }
                </Aspect>
              </StackPanel.Resources>
              <Button Content="STATIC" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator(
            "DynamicLocalAspect.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(
            InvokeCreate(stream, "Cerneala.GeneratedUi.DynamicLocalAspectFactory"));
        UIRoot root = new(320, 200);
        root.VisualChildren.Add(panel);
        Button dynamicButton = new() { Content = "DYNAMIC" };

        panel.VisualChildren.Add(dynamicButton);
        root.ProcessFrame();

        Assert.True(dynamicButton.IsAttached);
        AssertSolidBackground(new Color(18, 52, 86), dynamicButton);
        Assert.Equal(
            new Color(240, 241, 242),
            Assert.IsType<SolidColorBrush>(dynamicButton.Foreground).Color);
    }

    [Fact]
    public void GridMarkupEmitsDefinitionsPlacementsSpansAndLayout()
    {
        const string markup = """
            <Grid>
              <Grid.ColumnDefinitions>
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="2*" />
                <ColumnDefinition Width="120" />
              </Grid.ColumnDefinitions>
              <Grid.RowDefinitions>
                <RowDefinition Height="*" />
                <RowDefinition Height="Auto" />
              </Grid.RowDefinitions>
              <TextBlock Text="first" Grid.Row="0" Grid.Column="0" />
              <TextBlock Text="second" Grid.Row="1" Grid.Column="1" Grid.ColumnSpan="2" />
            </Grid>
            """;

        GeneratorRunResult result = RunGenerator("GridView.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generatedSource = SingleGeneratedSource(result);
        Assert.Contains("GridLength.Auto", generatedSource);
        Assert.Contains("GridLength.Stars(2f)", generatedSource);
        Assert.Contains("GridLength.Pixels(120f)", generatedSource);
        Assert.Contains("Grid.SetColumnSpan", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Grid grid = Assert.IsType<Grid>(InvokeCreate(stream, "Cerneala.GeneratedUi.GridViewFactory"));
        Assert.Equal(GridLength.Auto, grid.ColumnDefinitions[0].Width);
        Assert.Equal(GridLength.Stars(2), grid.ColumnDefinitions[1].Width);
        Assert.Equal(GridLength.Pixels(120), grid.ColumnDefinitions[2].Width);
        Assert.Equal(GridLength.Star, grid.RowDefinitions[0].Height);
        Assert.Equal(GridLength.Auto, grid.RowDefinitions[1].Height);
        TextBlock first = Assert.IsType<TextBlock>(grid.VisualChildren[0]);
        TextBlock second = Assert.IsType<TextBlock>(grid.VisualChildren[1]);
        Assert.Equal(0, Grid.GetRow(first));
        Assert.Equal(0, Grid.GetColumn(first));
        Assert.Equal(1, Grid.GetRow(second));
        Assert.Equal(1, Grid.GetColumn(second));
        Assert.Equal(2, Grid.GetColumnSpan(second));

        grid.Measure(new MeasureContext(new LayoutSize(600, 300)));
        grid.Arrange(new ArrangeContext(new LayoutRect(0, 0, 600, 300)));
        Assert.Equal(120, grid.ColumnDefinitions[2].Width.Value);
        Assert.True(second.ArrangedBounds.Width > 120);
    }

    [Fact]
    public void GridMarkupWorksInsideTemplatesWithConditionalChildren()
    {
        const string markup = """
            <Button IsEnabled="True">
              @template
              {
                <Grid Name="Chrome">
                  <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" />
                    <ColumnDefinition Width="Auto" />
                  </Grid.ColumnDefinitions>
                  <TextBlock Text="static" Grid.Column="0" />
                  @when $owner.IsEnabled
                  {
                    @if value == True { <TextBlock Text="conditional" Grid.Column="1" /> }
                  }
                </Grid>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("GridTemplate.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.GridTemplateFactory"));
        Grid grid = Assert.IsType<Grid>(button.ComponentTemplateInstance!.Root);
        Assert.Equal(2, grid.ColumnDefinitions.Count);
        Assert.Equal(2, grid.VisualChildren.Count);
        Assert.Equal(1, Grid.GetColumn(grid.VisualChildren[1]));

        button.IsEnabled = false;
        Assert.Single(grid.VisualChildren);
        button.IsEnabled = true;
        Assert.Equal(2, grid.VisualChildren.Count);
    }

    [Theory]
    [InlineData("<Grid><Grid.ColumnDefinitions><ColumnDefinition Width=\"-1\" /></Grid.ColumnDefinitions></Grid>")]
    [InlineData("<Grid><TextBlock Grid.Row=\"-1\" /></Grid>")]
    [InlineData("<Grid><TextBlock Grid.ColumnSpan=\"0\" /></Grid>")]
    public void InvalidGridMarkupReportsPropertyValueDiagnostic(string markup)
    {
        GeneratorRunResult result = RunGenerator("InvalidGrid.crn", markup, out _);

        AssertDiagnostic(result, "CERNEALAUI004", "InvalidGrid.crn");
        Assert.Empty(result.GeneratedSources);
    }

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

        GeneratorRunResult result = RunGenerator("part-path.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("Sample.crn", markup, out Compilation compilation);
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
    public void ExplicitElementSizeMarkupEmitsAndRuns()
    {
        const string markup = "<TextBlock Width=\"40\" Height=\"20\" />";

        GeneratorRunResult result = RunGenerator("ExplicitSize.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        UIElement element = InvokeCreate(stream, "Cerneala.GeneratedUi.ExplicitSizeFactory");
        Assert.Equal(40, element.Width);
        Assert.Equal(20, element.Height);
    }

    [Fact]
    public void GeneratedSourceUsesPublicTypedPropertiesWithoutRuntimeMarkupParser()
    {
        const string markup = """
            <Border BorderBrush="white" BorderThickness="1" Padding="4">
              <TextBlock Text="Typed" Foreground="0, 0, 0" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("typed-view.crn", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.Contains("public static partial class TypedViewFactory", generatedSource);
        Assert.Contains(".BorderBrush = new global::Cerneala.UI.Media.SolidColorBrush(global::Cerneala.Drawing.Color.White);", generatedSource);
        Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
        Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(4f);", generatedSource);
        Assert.Contains(".Foreground = new global::Cerneala.UI.Media.SolidColorBrush(new global::Cerneala.Drawing.Color(0, 0, 0));", generatedSource);
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
    public void SourceGeneratorEmitsAllNamedColorsThroughColorApi()
    {
        const string markup = """
            <Border Background="AliceBlue" BorderBrush="YellowGreen">
              <TextBlock Text="Named" Foreground="Tomato" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("NamedColors.crn", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::Cerneala.Drawing.Color.AliceBlue", generatedSource);
        Assert.Contains("global::Cerneala.Drawing.Color.YellowGreen", generatedSource);
        Assert.Contains("global::Cerneala.Drawing.Color.Tomato", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.NamedColorsFactory"));
        AssertSolidBackground(Color.AliceBlue, border);
        Assert.Equal(Color.YellowGreen, Assert.IsType<SolidColorBrush>(border.BorderBrush).Color);
        Assert.Equal(Color.Tomato, Assert.IsType<SolidColorBrush>(Assert.IsType<TextBlock>(border.Child).Foreground).Color);
    }

    [Fact]
    public void BorderBrushSupportsResourceAndPropertyElementBrushes()
    {
        const string resourceMarkup = """
            <Border BorderBrush="$Accent">
              <Border.Resources>
                <ImageBrush Name="Accent" Source="accent.png" Stretch="Uniform" />
              </Border.Resources>
            </Border>
            """;
        const string propertyMarkup = """
            <Border BorderThickness="2">
              <Border.BorderBrush>
                <LinearGradientBrush StartPoint="0,0" EndPoint="10,0">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </LinearGradientBrush>
              </Border.BorderBrush>
            </Border>
            """;

        GeneratorRunResult resourceResult = RunGenerator("BorderResource.crn", resourceMarkup, out Compilation resourceCompilation);
        GeneratorRunResult propertyResult = RunGenerator("BorderProperty.crn", propertyMarkup, out Compilation propertyCompilation);

        Assert.DoesNotContain(resourceResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(propertyResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream resourceStream = new();
        using MemoryStream propertyStream = new();
        Assert.True(resourceCompilation.Emit(resourceStream).Success);
        Assert.True(propertyCompilation.Emit(propertyStream).Success);

        Border resourceBorder = Assert.IsType<Border>(InvokeCreate(resourceStream, "Cerneala.GeneratedUi.BorderResourceFactory"));
        Border propertyBorder = Assert.IsType<Border>(InvokeCreate(propertyStream, "Cerneala.GeneratedUi.BorderPropertyFactory"));
        Assert.IsType<ImageBrush>(resourceBorder.BorderBrush);
        Assert.IsType<LinearGradientBrush>(propertyBorder.BorderBrush);
    }

    [Fact]
    public void BackgroundSupportsResourceAndPropertyElementBrushes()
    {
        const string resourceMarkup = """
            <Border Background="$Fill">
              <Border.Resources>
                <RadialGradientBrush Name="Fill" Center="5,5" RadiusX="5" RadiusY="5">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </RadialGradientBrush>
              </Border.Resources>
            </Border>
            """;
        const string propertyMarkup = """
            <Border>
              <Border.Background>
                <ImageBrush Source="background.png" Stretch="Uniform" />
              </Border.Background>
            </Border>
            """;

        GeneratorRunResult resourceResult = RunGenerator("BackgroundResource.crn", resourceMarkup, out Compilation resourceCompilation);
        GeneratorRunResult propertyResult = RunGenerator("BackgroundProperty.crn", propertyMarkup, out Compilation propertyCompilation);

        Assert.DoesNotContain(resourceResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(propertyResult.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream resourceStream = new();
        using MemoryStream propertyStream = new();
        Assert.True(resourceCompilation.Emit(resourceStream).Success);
        Assert.True(propertyCompilation.Emit(propertyStream).Success);

        Border resourceBorder = Assert.IsType<Border>(InvokeCreate(resourceStream, "Cerneala.GeneratedUi.BackgroundResourceFactory"));
        Border propertyBorder = Assert.IsType<Border>(InvokeCreate(propertyStream, "Cerneala.GeneratedUi.BackgroundPropertyFactory"));
        Assert.IsType<RadialGradientBrush>(resourceBorder.Background);
        Assert.IsType<ImageBrush>(propertyBorder.Background);
    }

    [Fact]
    public void RefactoredPropertySpecsPreserveExistingDirectAssignments()
    {
        const string markup = """
            <Border Background="White" BorderBrush="0, 1, 2, 3" BorderThickness="1" Padding="2">
              <TextBlock Text="Typed" FontFamily="Consolas" FontSize="12" Foreground="Black" Margin="1,2,3,4" />
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("DirectAssignments.crn", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains(".Background = new global::Cerneala.UI.Media.SolidColorBrush(global::Cerneala.Drawing.Color.White);", generatedSource);
        Assert.Contains(".BorderBrush = new global::Cerneala.UI.Media.SolidColorBrush(new global::Cerneala.Drawing.Color(0, 1, 2, 3));", generatedSource);
        Assert.Contains(".BorderThickness = new global::Cerneala.UI.Layout.Thickness(1f);", generatedSource);
        Assert.Contains(".Padding = new global::Cerneala.UI.Layout.Thickness(2f);", generatedSource);
        Assert.Contains(".FontFamily = \"Consolas\";", generatedSource);
        Assert.Contains(".FontSize = 12f;", generatedSource);
        Assert.Contains(".Foreground = new global::Cerneala.UI.Media.SolidColorBrush(global::Cerneala.Drawing.Color.Black);", generatedSource);
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

        GeneratorRunResult result = RunGenerator("ResourceFragment.crn", markup, out Compilation compilation);
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
        Assert.Equal(new Cerneala.Drawing.Color(255, 93, 115), brush.Color);
    }

    [Fact]
    public void CompositeBrushResourcesAreTypedAndAssignedWithoutColorFlattening()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <LinearGradientBrush Name="Linear" StartPoint="0,0" EndPoint="10,0" Opacity="0.5">
                  <GradientStop Offset="0" Color="#FFFFFFFF" />
                  <GradientStop Offset="1" Color="#FF000000" />
                </LinearGradientBrush>
                <RadialGradientBrush Name="Radial" Center="5,5" RadiusX="5" RadiusY="5">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </RadialGradientBrush>
                <DrawingBrush Name="Drawing" ContentBounds="0,0,10,10">
                  <FillRectangle Rect="0,0,10,10" Color="White" />
                </DrawingBrush>
                <ImageBrush Name="Image" Source="accent.png" Stretch="Uniform" TileMode="FlipX" />
              </StackPanel.Resources>
              <Rectangle Fill="$Linear" Stroke="$Radial" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("CompositeBrushes.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generatedSource = SingleGeneratedSource(result);
        Assert.Contains("global::Cerneala.UI.Media.LinearGradientBrush", generatedSource);
        Assert.Contains(".Fill = LinearResource", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel root = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.CompositeBrushesFactory"));
        Cerneala.UI.Controls.Shapes.Rectangle shape = Assert.IsType<Cerneala.UI.Controls.Shapes.Rectangle>(root.VisualChildren[0]);
        Assert.IsType<LinearGradientBrush>(shape.Fill);
        Assert.IsType<RadialGradientBrush>(shape.Stroke);
        Assert.IsType<DrawingBrush>(root.Resources["Drawing"]);
        Assert.IsType<ImageBrush>(root.Resources["Image"]);
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
                  <Aspect Name="Card" TargetType="Border">
                    @default { Background = $Accent; }
                    @when IsMouseOver { BorderBrush = $Accent; }
                  </Aspect>
                </Border.Resources>
                <TextBlock Text="Inner" />
              </Border>
              <TextBlock Text="Outer" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("RuntimeResources.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.RuntimeResourcesFactory"));
        Border border = Assert.IsType<Border>(panel.VisualChildren[0]);
        TextBlock inner = Assert.IsType<TextBlock>(border.Child);
        TextBlock outer = Assert.IsType<TextBlock>(panel.VisualChildren[1]);

        Assert.Equal(new Cerneala.Drawing.Color(0, 255, 0), inner.FindResource<SolidColorBrush>("Accent").Color);
        Assert.Equal(new Cerneala.Drawing.Color(255, 0, 0), outer.FindResource<SolidColorBrush>("Accent").Color);
        ElementAspect aspect = border.FindResource<ElementAspect>("Card");
        Assert.Equal(typeof(Border), aspect.TargetType);
        Border dynamicBorder = new();
        dynamicBorder.Aspect = aspect;
        UIRoot dynamicRoot = AttachAndProcess(dynamicBorder);
        Assert.Equal(new Cerneala.Drawing.Color(0, 255, 0), Assert.IsType<SolidColorBrush>(dynamicBorder.Background).Color);
        dynamicBorder.SetValue(Cerneala.UI.Elements.UIElement.IsMouseOverProperty, true);
        dynamicRoot.ProcessFrame();
        Assert.Equal(new Cerneala.Drawing.Color(0, 255, 0), Assert.IsType<SolidColorBrush>(dynamicBorder.BorderBrush).Color);
        Assert.Single(panel.Resources);
        Assert.Equal(2, border.Resources.Count);
    }

    [Fact]
    public void NamedTemplatedAspectCanBeAppliedToADynamicControl()
    {
        const string markup = """
            <Border>
              <Border.Resources>
                <Aspect Name="DynamicButton" TargetType="Button">
                  @default { Background = "#FF10282D"; }
                  @template
                  {
                    <Border Name="Chrome" Background="$owner.Background:OneWay" />
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator(
            "DynamicTemplatedAspect.crn",
            markup,
            out Compilation compilation);
        Assert.DoesNotContain(
            result.Diagnostics,
            diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border owner = Assert.IsType<Border>(InvokeCreate(
            stream,
            "Cerneala.GeneratedUi.DynamicTemplatedAspectFactory"));
        ElementAspect aspect = owner.FindResource<ElementAspect>("DynamicButton");
        Button dynamicButton = new();

        dynamicButton.Aspect = aspect;
        AttachAndProcess(dynamicButton);
        dynamicButton.ApplyTemplate();

        Assert.DoesNotContain("Button.Default", dynamicButton.ComponentTemplate?.Name, StringComparison.Ordinal);
        Border chrome = Assert.IsType<Border>(dynamicButton.ComponentTemplateInstance!.Root);
        Assert.Same(chrome, dynamicButton.ComponentTemplateInstance.Parts["Chrome"]);
        AssertSolidBackground(new Color(16, 40, 45), chrome);
    }

    [Fact]
    public void ReferencedNamedAspectRemainsLocalAfterThemeProcessing()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <SolidColorBrush Name="Ink" Color="#FF0A0B0E" />
                <SolidColorBrush Name="Lime" Color="#FFC6FF3D" />
                <SolidColorBrush Name="Cyan" Color="#FF00E8FF" />
                <Aspect Name="PrimaryButton" TargetType="Button">
                  @default
                  {
                    Background = $Lime;
                    Foreground = $Ink;
                    BorderBrush = $Lime;
                  }
                  @when IsMouseOver
                  {
                    Background = $Cyan;
                  }
                </Aspect>
              </StackPanel.Resources>
              <Button Aspect="$PrimaryButton" Content="Continue" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("NamedLocalAspect.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.NamedLocalAspectFactory"));
        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        UIRoot root = new();
        root.VisualChildren.Add(panel);
        root.ProcessFrame();

        Assert.NotNull(button.Aspect);
        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        AssertSolidBackground(new Color(198, 255, 61), button);
        Assert.Equal(new Color(10, 11, 14), Assert.IsType<SolidColorBrush>(button.Foreground).Color);

        button.IsPointerOver = true;
        root.ProcessFrame();

        Assert.Equal(Cerneala.UI.Core.UiPropertyValueSource.AspectBase, button.GetValueSource(Control.BackgroundProperty));
        AssertSolidBackground(new Color(0, 232, 255), button);
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

        GeneratorRunResult result = RunGenerator("TopLevelResources.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "TopLevelResources.crn");
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

        GeneratorRunResult result = RunGenerator("KeywordComparators.crn", markup, out Compilation compilation);
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
                <Aspect Name="Label" TargetType="TextBlock">
                  @default { Foreground = $Accent; }
                </Aspect>
              </StackPanel.Resources>

              <TextBlock Aspect="$Label" Text="Outer" />
              <Border>
                <Border.Resources>
                  <SolidColorBrush Name="Accent" Color="#FF00FF00" />
                  <Aspect Name="Label" TargetType="TextBlock">
                    @default { Foreground = $Accent; }
                  </Aspect>
                </Border.Resources>
                <TextBlock Aspect="$Label" Text="Inner" />
              </Border>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("ScopedResources.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.ScopedResourcesFactory"));
        AttachAndProcess(panel);
        TextBlock outer = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Border border = Assert.IsType<Border>(panel.VisualChildren[1]);
        TextBlock inner = Assert.IsType<TextBlock>(border.Child);
        Assert.Equal(new Cerneala.Drawing.Color(255, 0, 0), Assert.IsType<SolidColorBrush>(outer.Foreground).Color);
        Assert.Equal(new Cerneala.Drawing.Color(0, 255, 0), Assert.IsType<SolidColorBrush>(inner.Foreground).Color);
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

        GeneratorRunResult result = RunGenerator("WrongResourceOwner.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "WrongResourceOwner.crn");
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

        GeneratorRunResult result = RunGenerator("BrushResource.crn", markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("global::Cerneala.UI.Media.SolidColorBrush PulseColorResource0 = new(new global::Cerneala.Drawing.Color(255, 93, 115));", generatedSource);

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

        GeneratorRunResult result = RunGenerator("BadBrush.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "BadBrush.crn");
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

        GeneratorRunResult result = RunGenerator("LegacyAspectType.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "LegacyAspectType.crn");
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
                <Border Name="Bd" Background="$owner.Background:OneWay">
                  <ContentPresenter Content="$owner.Content:OneWay" />
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("InlineTemplate.crn", markup, out Compilation compilation);
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

        button.Background = new SolidColorBrush(Cerneala.Drawing.Color.White);
        button.Content = "Changed";
        AssertSolidBackground(Cerneala.Drawing.Color.White, border);
        Assert.Equal("Changed", presenter.Content);
    }

    [Fact]
    public void InlineTemplateBooleanShorthandRestoresOwnerBindingWhenFalse()
    {
        const string markup = """
            <Button Background="Black" IsEnabled="False">
              @template
              {
                <Border Background="$owner.Background:OneWay">
                  @when $owner.IsEnabled
                  {
                    Background = "White";
                  }
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateBoolean.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateBooleanFactory"));
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);

        button.IsEnabled = true;
        AssertSolidBackground(Cerneala.Drawing.Color.White, border);
        button.IsEnabled = false;
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);
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

        GeneratorRunResult result = RunGenerator("TemplateBranches.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateBranchesFactory"));
        button.ApplyTemplate();
        Border border = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        AssertSolidBackground(Cerneala.Drawing.Color.White, border);
        button.IsEnabled = false;
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);
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

        GeneratorRunResult result = RunGenerator("TemplateConditionalChild.crn", markup, out Compilation compilation);
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
                <Aspect Name="GhostButton" TargetType="Button">
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

        GeneratorRunResult result = RunGenerator("AspectTemplate.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.AspectTemplateFactory"));
        AttachAndProcess(button);
        AssertSolidBackground(Cerneala.Drawing.Color.Black, button);
        Assert.Contains(
            Control.ComponentTemplateProperty,
            button.FindResource<ElementAspect>("GhostButton").DefaultValues.Select(value => value.Property));
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
                <Aspect TargetType="Button">
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

        GeneratorRunResult result = RunGenerator("TemplatePrecedence.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplatePrecedenceFactory"));
        AttachAndProcess(button);
        Assert.Equal(Cerneala.Drawing.Color.Black, Assert.IsType<SolidColorBrush>(button.Foreground).Color);
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

        GeneratorRunResult result = RunGenerator("PanelTemplate.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "PanelTemplate.crn");
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
                  <ContentPresenter Content="$owner.Content:OneWay" />
                </Border>
              }
              <Button Content="Nested">
                @template { <TextBlock Text="Inner template" /> }
              </Button>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("NestedTemplates.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("TemplateScopes.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateScopesFactory"));
        button.ApplyTemplate();
        Border ownerPart = Assert.IsType<Border>(button.ComponentTemplateInstance!.Parts["OwnerPart"]);
        Border selfPart = Assert.IsType<Border>(button.ComponentTemplateInstance.Parts["SelfPart"]);
        AssertSolidBackground(new Cerneala.Drawing.Color(18, 52, 86), ownerPart);
        Assert.Null(selfPart.Background);

        selfPart.IsEnabled = true;
        AssertSolidBackground(Cerneala.Drawing.Color.White, selfPart);
        button.IsEnabled = false;
        Assert.Null(ownerPart.Background);
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
        GeneratorRunResult inline = RunGenerator("InlineAspectTemplate.crn", inlineMarkup, out Compilation inlineCompilation);
        Assert.DoesNotContain(inline.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream inlineStream = new();
        Assert.True(inlineCompilation.Emit(inlineStream).Success);
        Button inlineButton = Assert.IsType<Button>(InvokeCreate(inlineStream, "Cerneala.GeneratedUi.InlineAspectTemplateFactory"));
        AttachAndProcess(inlineButton);
        inlineButton.ApplyTemplate();
        Assert.Equal("Inline", Assert.IsType<TextBlock>(inlineButton.ComponentTemplateInstance!.Root).Text);

        const string namedMarkup = """
            <Button Aspect="$Named">
              <Button.Resources>
                <Aspect TargetType="Button">@template { <TextBlock Text="Default" /> }</Aspect>
                <Aspect Name="Named" TargetType="Button">@template { <TextBlock Text="Named" /> }</Aspect>
              </Button.Resources>
            </Button>
            """;
        GeneratorRunResult named = RunGenerator("NamedAspectTemplate.crn", namedMarkup, out Compilation namedCompilation);
        Assert.DoesNotContain(named.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream namedStream = new();
        Assert.True(namedCompilation.Emit(namedStream).Success);
        Button namedButton = Assert.IsType<Button>(InvokeCreate(namedStream, "Cerneala.GeneratedUi.NamedAspectTemplateFactory"));
        AttachAndProcess(namedButton);
        namedButton.ApplyTemplate();
        Assert.Equal("Named", Assert.IsType<TextBlock>(namedButton.ComponentTemplateInstance!.Root).Text);
    }

    [Fact]
    public void SharedAspectTemplateCreatesIndependentPartsForEveryControl()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="Button">
                  @template { <Border Name="Chrome" /> }
                </Aspect>
              </StackPanel.Resources>
              <Button Content="One" />
              <Button Content="Two" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("TemplatePartIsolation.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplatePartIsolationFactory"));
        AttachAndProcess(panel);
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

        GeneratorRunResult result = RunGenerator("InlineAspectPrecedence.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.InlineAspectPrecedenceFactory"));
        AttachAndProcess(button);
        Assert.Equal(Cerneala.Drawing.Color.White, Assert.IsType<SolidColorBrush>(button.Foreground).Color);
        button.ApplyTemplate();
        Assert.Equal("Direct", Assert.IsType<TextBlock>(button.ComponentTemplateInstance!.Root).Text);
    }

    [Theory]
    [InlineData(
        "ScrollViewerParts.crn",
        "<ScrollViewer>@template { <StackPanel><ScrollContentPresenter Name=\"PART_ScrollContentPresenter\" /><ScrollBar Name=\"PART_HorizontalScrollBar\" /><ScrollBar Name=\"PART_VerticalScrollBar\" /></StackPanel> }</ScrollViewer>",
        "PART_ScrollContentPresenter,PART_HorizontalScrollBar,PART_VerticalScrollBar")]
    [InlineData(
        "ScrollBarParts.crn",
        "<ScrollBar>@template { <StackPanel><RepeatButton Name=\"PART_DecreaseButton\" /><Track Name=\"PART_Track\" /><RepeatButton Name=\"PART_IncreaseButton\" /></StackPanel> }</ScrollBar>",
        "PART_DecreaseButton,PART_Track,PART_IncreaseButton")]
    [InlineData(
        "TrackParts.crn",
        "<Track>@template { <Thumb Name=\"PART_Thumb\" /> }</Track>",
        "PART_Thumb")]
    public void GeneratedScrollingTemplatesRegisterDeclaredPartNames(
        string fileName,
        string markup,
        string expectedPartNames)
    {
        GeneratorRunResult result = RunGenerator(fileName, markup, out Compilation compilation);
        string generatedSource = SingleGeneratedSource(result);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        foreach (string partName in expectedPartNames.Split(','))
        {
            Assert.Contains(".RequirePart(\"" + partName + "\"", generatedSource, StringComparison.Ordinal);
        }

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
    }

    [Theory]
    [InlineData("<Button>@template { }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { raw text }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { <Border /><TextBlock /> }</Button>", "CERNEALAUI006", "exactly one")]
    [InlineData("<Button>@template { <Border /> } @template { <TextBlock /> }</Button>", "CERNEALAUI012", "only one")]
    [InlineData("<Button>@when IsEnabled { @if value == True { @template { <Border /> } } }</Button>", "CERNEALAUI006", "not allowed")]
    [InlineData("<Button>@template { <Border Name=\"Part\"><Button Name=\"Part\" /></Border> }</Button>", "CERNEALAUI012", "Duplicate")]
    [InlineData("<Button>@template { <Border Background=\"$owner.FontSize\" /> }</Button>", "CERNEALAUI012", "type")]
    [InlineData("<Track>@template { <Border Name=\"PART_Thumb\" /> }</Track>", "CERNEALAUI012", "PART_Thumb")]
    [InlineData("<Button>@template { <Border>@when $owner.Unknown { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "template owner")]
    [InlineData("<Button>@template { <Border>@when $self.Unknown { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "template element")]
    [InlineData("<Button>@template { <Border>@when $owner.FontSize { Background = \"White\"; }</Border> }</Button>", "CERNEALAUI007", "Boolean")]
    [InlineData("<StackPanel><StackPanel.Resources><Aspect TargetType=\"StackPanel\">@template { <Border /> }</Aspect></StackPanel.Resources></StackPanel>", "CERNEALAUI012", "not a Control")]
    [InlineData("<Button><Button.Resources><SolidColorBrush Name=\"owner\" Color=\"#FF000000\" /></Button.Resources></Button>", "CERNEALAUI005", "reserved")]
    public void InvalidTemplateShapesReportFocusedDiagnostics(string markup, string diagnosticId, string message)
    {
        GeneratorRunResult result = RunGenerator("InvalidTemplate.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, diagnosticId, "InvalidTemplate.crn");
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
                <Border IsEnabled="True">
                  @when $owner.IsEnabled and $self.IsEnabled { Background = "White"; }
                </Border>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("TemplateLifecycle.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.TemplateLifecycleFactory"));
        button.ApplyTemplate();
        Border oldRoot = Assert.IsType<Border>(button.ComponentTemplateInstance!.Root);
        AssertSolidBackground(Cerneala.Drawing.Color.White, oldRoot);

        button.ComponentTemplate = new Cerneala.UI.Controls.Templates.ComponentTemplate<Button>("replacement", _ => new Border());
        Assert.Null(oldRoot.Background);
        button.IsEnabled = false;
        oldRoot.IsEnabled = false;
        Assert.Null(oldRoot.Background);
        Assert.NotSame(oldRoot, button.ComponentTemplateInstance!.Root);
    }

    [Fact]
    public void LogicalConditionalContentSurvivesRepeatedTemplateAttachDetachCycles()
    {
        const string markup = """
            <Button IsEnabled="True">
              @template
              {
                <StackPanel IsEnabled="True">
                  @when $owner.IsEnabled and $self.IsEnabled
                  {
                    <TextBlock Text="Attached" />
                  }
                </StackPanel>
              }
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("LogicalAttachDetach.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);

        Button button = Assert.IsType<Button>(InvokeCreate(stream, "Cerneala.GeneratedUi.LogicalAttachDetachFactory"));
        Cerneala.UI.Controls.Templates.ComponentTemplate? generatedTemplate = button.ComponentTemplate;
        Assert.NotNull(generatedTemplate);

        for (int cycle = 0; cycle < 3; cycle++)
        {
            button.IsEnabled = true;
            button.ComponentTemplate = generatedTemplate;
            StackPanel generatedRoot = Assert.IsType<StackPanel>(button.ComponentTemplateInstance!.Root);
            Assert.Single(generatedRoot.VisualChildren);

            button.ComponentTemplate = new Cerneala.UI.Controls.Templates.ComponentTemplate<Button>(
                "replacement-" + cycle,
                _ => new Border());
            int detachedCount = generatedRoot.VisualChildren.Count;
            button.IsEnabled = false;
            generatedRoot.IsEnabled = false;
            Assert.Equal(detachedCount, generatedRoot.VisualChildren.Count);
        }
    }

    [Fact]
    public void UnnamedAspectAppliesToEveryMatchingElement()
    {
        const string markup = """
            <StackPanel>
              <StackPanel.Resources>
                <Aspect TargetType="TextBlock">
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

        GeneratorRunResult result = RunGenerator("DefaultTextAspect.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.DefaultTextAspectFactory"));
        AttachAndProcess(panel);
        Assert.All(panel.VisualChildren.Cast<TextBlock>(), text =>
        {
            Assert.Equal("Consolas", text.FontFamily);
            Assert.Equal(12, text.FontSize);
        });
    }

    [Fact]
    public void NamedAspectAppliesAfterUnnamedDefault()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText" Text="HELLO">
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
                  @default
                  {
                    FontSize = 14;
                    Foreground = Black;
                  }
                </Aspect>
                <Aspect Name="KickerText" TargetType="TextBlock">
                  @default
                  {
                    FontSize = 12;
                    Margin = "0,0,0,12";
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("NamedAspect.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock text = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.NamedAspectFactory"));
        AttachAndProcess(text);
        Assert.Equal(12, text.FontSize);
        Assert.Equal(new Thickness(0, 0, 0, 12), text.Margin);
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(text.Foreground).Color);
    }

    [Fact]
    public void LayoutPointPropertyParsesFromMarkup()
    {
        const string markup = """
            <Border RenderTransformOrigin="0,0.5" />
            """;

        GeneratorRunResult result = RunGenerator("LayoutPoint.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.LayoutPointFactory"));
        Assert.Equal(new LayoutPoint(0, 0.5f), border.RenderTransformOrigin);
    }

    [Fact]
    public void AspectCanReferenceSolidColorBrushForBrushProperty()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText" Text="HELLO">
              <TextBlock.Resources>
                <SolidColorBrush Name="PulseColor" Color="#FF5D73" />
                <Aspect Name="KickerText" TargetType="TextBlock">
                  @default
                  {
                    Foreground = $PulseColor;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("AspectBrushReference.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.AspectBrushReferenceFactory"));
        AttachAndProcess(root);
        Assert.Equal(new Cerneala.Drawing.Color(255, 93, 115), Assert.IsType<SolidColorBrush>(root.Foreground).Color);
    }

    [Fact]
    public void ForegroundSupportsCompositeBrushPropertyElement()
    {
        const string markup = """
            <TextBlock Text="Gradient">
              <TextBlock.Foreground>
                <LinearGradientBrush StartPoint="0,0" EndPoint="100,0">
                  <GradientStop Offset="0" Color="Tomato" />
                  <GradientStop Offset="1" Color="AliceBlue" />
                </LinearGradientBrush>
              </TextBlock.Foreground>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("ForegroundProperty.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        TextBlock root = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.ForegroundPropertyFactory"));
        Assert.IsType<LinearGradientBrush>(root.Foreground);
    }

    [Fact]
    public void UnknownNameReferenceReportsDiagnostic()
    {
        const string markup = """
            <TextBlock Aspect="$KickerText">
              <TextBlock.Resources>
                <Aspect Name="KickerText" TargetType="TextBlock">
                  @default
                  {
                    Foreground = $MissingColor;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("UnknownReference.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "UnknownReference.crn");
        Assert.Contains("MissingColor", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ElementNameRegistersGeneratedVariableSymbol()
    {
        const string markup = """
            <TextBlock Name="KickerLabel" Text="HELLO" />
            """;

        GeneratorRunResult result = RunGenerator("NamedElement.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("DuplicateName.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateName.crn");
        Assert.Contains("Duplicate", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AspectTargetMismatchReportsDiagnostic()
    {
        const string markup = """
            <Button Aspect="$KickerText">
              <Button.Resources>
                <Aspect Name="KickerText" TargetType="TextBlock">
                  @default
                  {
                    FontSize = 12;
                  }
                </Aspect>
              </Button.Resources>
            </Button>
            """;

        GeneratorRunResult result = RunGenerator("AspectMismatch.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "AspectMismatch.crn");
        Assert.Contains("Button.Aspect", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void DuplicateUnnamedAspectForTargetReportsDiagnostic()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
                  @default { FontSize = 12; }
                </Aspect>
                <Aspect TargetType="TextBlock">
                  @default { FontSize = 14; }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("DuplicateDefaultAspect.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "DuplicateDefaultAspect.crn");
        Assert.Contains("TextBlock", diagnostic.GetMessage());
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedAspectPropertyReportsDiagnostic()
    {
        const string markup = """
            <TextBlock>
              <TextBlock.Resources>
                <Aspect TargetType="TextBlock">
                  @default
                  {
                    Bogus = 100;
                  }
                </Aspect>
              </TextBlock.Resources>
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("UnsupportedAspectProperty.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedAspectProperty.crn");
        Assert.Contains("TextBlock.Bogus", diagnostic.GetMessage());
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

        GeneratorRunResult result = RunGenerator("NestedResources.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI002", "NestedResources.crn");
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

        GeneratorRunResult result = RunGenerator("MultipleRoots.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "MultipleRoots.crn");
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
              <TextBlock Bogus="12" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("FragmentDiagnosticLocation.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "FragmentDiagnosticLocation.crn");
        Assert.Equal(4, diagnostic.Location.GetLineSpan().StartLinePosition.Line);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void FirstLineFragmentDiagnosticsUseOriginalMarkupColumn()
    {
        GeneratorRunResult result = RunGenerator("FirstLineDiagnosticLocation.crn", "<TextBlock Bogus=\"12\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI003", "FirstLineDiagnosticLocation.crn");
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

        GeneratorRunResult result = RunGenerator("XmlDeclarationFragment.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("TopLevelText.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "TopLevelText.crn");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void AdjacentMultipleUiRootsReportMalformedMarkupDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("AdjacentRoots.crn", "<TextBlock Text=\"One\" /><TextBlock Text=\"Two\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "AdjacentRoots.crn");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void TopLevelCDataAfterRootReportsMalformedMarkupDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("TrailingCData.crn", "<TextBlock Text=\"Hello\" /><![CDATA[stray]]>", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI001", "TrailingCData.crn");
        Assert.Contains("exactly one UI root", diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void MalformedMarkupReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Broken.crn", "<StackPanel>", out _);

        AssertDiagnostic(result, "CERNEALAUI001", "Broken.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedElementReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("Unsupported.crn", "<BogusWidget />", out _);

        AssertDiagnostic(result, "CERNEALAUI002", "Unsupported.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void UnsupportedPropertyReportsDiagnostic()
    {
        GeneratorRunResult result = RunGenerator("UnsupportedProperty.crn", "<TextBlock Bogus=\"12\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "UnsupportedProperty.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void ControlPropertiesOnStackPanelReportDiagnosticInsteadOfGeneratingInvalidCode()
    {
        GeneratorRunResult result = RunGenerator("BadPanel.crn", "<StackPanel Padding=\"4\" />", out _);

        AssertDiagnostic(result, "CERNEALAUI003", "BadPanel.crn");
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void InvalidRuntimeValidatedValuesReportDiagnostics()
    {
        GeneratorRunResult fontResult = RunGenerator("BadFont.crn", "<TextBlock FontSize=\"0\" />", out _);
        GeneratorRunResult paddingResult = RunGenerator("BadPadding.crn", "<Border Padding=\"-1\" />", out _);

        AssertDiagnostic(fontResult, "CERNEALAUI004", "BadFont.crn");
        AssertDiagnostic(paddingResult, "CERNEALAUI004", "BadPadding.crn");
    }

    [Fact]
    public void InvalidBooleanPropertyValueDiagnosticNamesMarkupProperty()
    {
        GeneratorRunResult result = RunGenerator("BadVisibility.crn", "<TextBlock IsVisible=\"maybe\" />", out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI004", "BadVisibility.crn");
        Assert.Contains("TextBlock.IsVisible", diagnostic.GetMessage());
    }

    [Fact]
    public void DistinctMarkupFilesWithSameBaseNameEmitUniqueFactories()
    {
        GeneratorRunResult result = RunGenerator(
            new MarkupFile("Views/Main.crn", "<TextBlock Text=\"View\" />"),
            new MarkupFile("Dialogs/Main.crn", "<TextBlock Text=\"Dialog\" />"));

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

        GeneratorRunResult result = RunGenerator("ReactiveBorder.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.ReactiveBorderFactory"));
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);

        border.IsPointerOver = true;
        AssertSolidBackground(Cerneala.Drawing.Color.White, border);

        border.IsPointerOver = false;
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);
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

        GeneratorRunResult result = RunGenerator("ConditionalChildren.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("ConditionalButton.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("TypedContext.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGenerator("InheritedContext.crn", markup, out Compilation compilation);
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

        GeneratorRunResult result = RunGeneratorWithInput("NestedPath.crn", markup, inputSource, out Compilation compilation);
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

        GeneratorRunResult result = RunGeneratorWithInput("ContextOperand.crn", markup, inputSource, out Compilation compilation);
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

        GeneratorRunResult result = RunGeneratorWithInput("Comparators.crn", markup, inputSource, out Compilation compilation);
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
    public void LogicalWhenSupportsAndOrPrecedenceParenthesesAndMultilineWhitespace()
    {
        const string markup = """
            <StackPanel>
              <TextBlock Text="Precedence" IsEnabled="False" IsVisible="False">
                @when IsEnabled or
                      IsMouseOver   and   IsVisible
                {
                  Text = "Matched";
                }
              </TextBlock>
              <TextBlock Text="Grouped" IsEnabled="False" IsVisible="False">
                @when ( IsEnabled or IsMouseOver ) and IsVisible
                {
                  Text = "Matched";
                }
              </TextBlock>
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("LogicalPrecedence.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.LogicalPrecedenceFactory"));
        TextBlock precedence = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        TextBlock grouped = Assert.IsType<TextBlock>(panel.VisualChildren[1]);

        precedence.IsEnabled = true;
        grouped.IsEnabled = true;
        Assert.Equal("Matched", precedence.Text);
        Assert.Equal("Grouped", grouped.Text);

        grouped.IsVisible = true;
        Assert.Equal("Matched", grouped.Text);
        precedence.IsEnabled = false;
        precedence.IsPointerOver = true;
        Assert.Equal("Precedence", precedence.Text);
        precedence.IsVisible = true;
        Assert.Equal("Matched", precedence.Text);
    }

    [Fact]
    public void LogicalWhenObservesShortCircuitedBranchesAndRestoresBaseValue()
    {
        const string markup = """
            <TextBlock Text="Base" IsEnabled="True">
              @when IsEnabled or IsMouseOver
              {
                Text = "Active";
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("ShortCircuit.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        TextBlock text = Assert.IsType<TextBlock>(InvokeCreate(stream, "Cerneala.GeneratedUi.ShortCircuitFactory"));
        Assert.Equal("Active", text.Text);

        text.IsPointerOver = true;
        text.IsEnabled = false;
        Assert.Equal("Active", text.Text);
        text.IsPointerOver = false;
        Assert.Equal("Base", text.Text);
        text.IsEnabled = true;
        Assert.Equal("Active", text.Text);
    }

    [Fact]
    public void LogicalIfRangesObserveEveryTypedOperand()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class RangeViewModel : INotifyPropertyChanged
            {
                private int value = 15;
                private int min = 10;
                private int max = 20;
                public event PropertyChangedEventHandler? PropertyChanged;
                public int Value { get => value; set { this.value = value; Changed(nameof(Value)); } }
                public int Min { get => min; set { min = value; Changed(nameof(Min)); } }
                public int Max { get => max; set { max = value; Changed(nameof(Max)); } }
                private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.RangeViewModel" Text="Outside">
              @when $DataContext.Value
              {
                @if (value >= $DataContext.Min and value <= $DataContext.Max)
                {
                  Text = "Inside";
                }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("LogicalRange.crn", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type type = assembly.GetType("TestInput.RangeViewModel", throwOnError: true)!;
        object viewModel = Activator.CreateInstance(type)!;
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.LogicalRangeFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        TextBlock text = Assert.IsType<TextBlock>(create.Invoke(null, new[] { viewModel }));
        Assert.Equal("Inside", text.Text);

        type.GetProperty("Min")!.SetValue(viewModel, 16);
        Assert.Equal("Outside", text.Text);
        type.GetProperty("Value")!.SetValue(viewModel, 18);
        Assert.Equal("Inside", text.Text);
        type.GetProperty("Max")!.SetValue(viewModel, 17);
        Assert.Equal("Outside", text.Text);
    }

    [Fact]
    public void LogicalWhenCombinesElementDataContextOwnerSelfAndTemplatePartSources()
    {
        const string markup = """
            <StackPanel DataType="Cerneala.UI.Elements.UIElement">
              <TextBlock Text="Off" IsEnabled="True">
                @when IsEnabled and $DataContext.IsEnabled { Text = "On"; }
              </TextBlock>
              <Button Name="Host" IsEnabled="False">
                @template
                {
                  <Border Name="Chrome" IsEnabled="True" Background="Black">
                    @when $owner.IsEnabled and $self.IsEnabled { Background = "White"; }
                  </Border>
                }
              </Button>
              <TextBlock Text="Part off" IsEnabled="True">
                @when $Host.parts.$Chrome.IsEnabled and IsEnabled { Text = "Part on"; }
              </TextBlock>
            </StackPanel>
            """;

        UIElement context = new() { IsEnabled = false };
        GeneratorRunResult result = RunGenerator("LogicalSources.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Contains("RegisterLifetime", SingleGeneratedSource(result));

        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.LogicalSourcesFactory", context));
        TextBlock dataText = Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Button host = Assert.IsType<Button>(panel.VisualChildren[1]);
        TextBlock partText = Assert.IsType<TextBlock>(panel.VisualChildren[2]);
        Border chrome = Assert.IsType<Border>(host.ComponentTemplateInstance!.Parts["Chrome"]);

        Assert.Equal("Off", dataText.Text);
        context.IsEnabled = true;
        Assert.Equal("On", dataText.Text);
        Assert.Equal("Part on", partText.Text);
        host.IsEnabled = true;
        AssertSolidBackground(Cerneala.Drawing.Color.White, chrome);
        chrome.IsEnabled = false;
        AssertSolidBackground(Cerneala.Drawing.Color.Black, chrome);
        Assert.Equal("Part off", partText.Text);
    }

    [Fact]
    public void LogicalOrUsesPerLeafNullableGuards()
    {
        const string inputSource = """
            using System.ComponentModel;
            namespace TestInput;
            public sealed class NullableRoot : INotifyPropertyChanged
            {
                private NullableChild? child;
                private bool fallback = true;
                public event PropertyChangedEventHandler? PropertyChanged;
                public NullableChild? Child { get => child; set { child = value; Changed(nameof(Child)); } }
                public bool Fallback { get => fallback; set { fallback = value; Changed(nameof(Fallback)); } }
                private void Changed(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
            public sealed class NullableChild : INotifyPropertyChanged
            {
                private bool active;
                public event PropertyChangedEventHandler? PropertyChanged;
                public bool Active { get => active; set { active = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Active))); } }
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.NullableRoot" Text="Off">
              @when $DataContext.Child.Active or $DataContext.Fallback { Text = "On"; }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("NullableOr.crn", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        Assembly assembly = Assembly.Load(stream.ToArray());
        Type rootType = assembly.GetType("TestInput.NullableRoot", throwOnError: true)!;
        Type childType = assembly.GetType("TestInput.NullableChild", throwOnError: true)!;
        object root = Activator.CreateInstance(rootType)!;
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.NullableOrFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        TextBlock text = Assert.IsType<TextBlock>(create.Invoke(null, new[] { root }));
        Assert.Equal("On", text.Text);

        rootType.GetProperty("Fallback")!.SetValue(root, false);
        Assert.Equal("Off", text.Text);
        object child = Activator.CreateInstance(childType)!;
        childType.GetProperty("Active")!.SetValue(child, true);
        rootType.GetProperty("Child")!.SetValue(root, child);
        Assert.Equal("On", text.Text);
    }

    [Fact]
    public void LogicalConditionalChildrenReactivateAndDetachSubscriptions()
    {
        const string markup = """
            <StackPanel DataType="Cerneala.UI.Elements.UIElement">
              <StackPanel IsEnabled="False">
                @when IsEnabled or $DataContext.IsEnabled
                {
                  <TextBlock Text="Conditional" />
                }
              </StackPanel>
            </StackPanel>
            """;

        UIElement context = new() { IsEnabled = false };
        GeneratorRunResult result = RunGenerator("LogicalLifecycle.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        Assert.True(compilation.Emit(stream).Success);
        StackPanel root = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.LogicalLifecycleFactory", context));
        StackPanel conditionalHost = Assert.IsType<StackPanel>(root.VisualChildren[0]);
        Assert.Empty(conditionalHost.VisualChildren);

        context.IsEnabled = true;
        TextBlock cached = Assert.IsType<TextBlock>(Assert.Single(conditionalHost.VisualChildren));
        context.IsEnabled = false;
        Assert.Empty(conditionalHost.VisualChildren);
        conditionalHost.IsEnabled = true;
        Assert.Same(cached, Assert.Single(conditionalHost.VisualChildren));
    }

    [Fact]
    public void LogicalExpressionsDeduplicateSourcesAndKeepKeywordsInsideAtoms()
    {
        const string inputSource = """
            namespace TestInput;
            public sealed class KeywordViewModel
            {
                public bool IsAndroidReady { get; set; } = true;
                public string Label { get; set; } = "salt and pepper or sugar";
            }
            """;
        const string markup = """
            <TextBlock DataType="TestInput.KeywordViewModel" Text="Off">
              @when $DataContext.IsAndroidReady and $DataContext.IsAndroidReady
              {
                @if value == True and $DataContext.Label == "salt and pepper or sugar"
                {
                  Text = "On";
                }
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGeneratorWithInput("KeywordAtoms.crn", markup, inputSource, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        string generatedSource = SingleGeneratedSource(result);
        Assert.Equal(2, Count(generatedSource, "ObserveDataPath("));
        Assert.Contains("&&", generatedSource);
        Assert.DoesNotContain("observation0Value", generatedSource);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));
        Assembly assembly = Assembly.Load(stream.ToArray());
        object viewModel = Activator.CreateInstance(assembly.GetType("TestInput.KeywordViewModel", throwOnError: true)!)!;
        MethodInfo create = assembly.GetType("Cerneala.GeneratedUi.KeywordAtomsFactory", throwOnError: true)!
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate => candidate.Name == "Create" && candidate.GetParameters().Length == 1);
        Assert.Equal("On", Assert.IsType<TextBlock>(create.Invoke(null, new[] { viewModel })).Text);
    }

    [Theory]
    [InlineData("<TextBlock>@when { Text = \"x\"; }</TextBlock>", "empty")]
    [InlineData("<TextBlock>@when IsEnabled IsMouseOver { Text = \"x\"; }</TextBlock>", "operator")]
    [InlineData("<TextBlock>@when IsEnabled and { Text = \"x\"; }</TextBlock>", "operand")]
    [InlineData("<TextBlock>@when (IsEnabled or IsMouseOver { Text = \"x\"; }</TextBlock>", "closing")]
    [InlineData("<TextBlock>@when IsEnabled) { Text = \"x\"; }</TextBlock>", "parenthesis")]
    [InlineData("<TextBlock>@when FontSize and IsEnabled { Text = \"x\"; }</TextBlock>", "Boolean")]
    [InlineData("<TextBlock>@when FontSize { @if value == \"large\" { Text = \"x\"; } }</TextBlock>", "type")]
    public void InvalidLogicalExpressionsReportFocusedDiagnostics(string markup, string message)
    {
        GeneratorRunResult result = RunGenerator("InvalidLogical.crn", markup, out _);

        Diagnostic diagnostic = Assert.Single(result.Diagnostics, item =>
            item.Id is "CERNEALAUI006" or "CERNEALAUI007");
        Assert.Contains(message, diagnostic.GetMessage(), StringComparison.OrdinalIgnoreCase);
        Assert.Empty(result.GeneratedSources);
    }

    [Fact]
    public void LogicalExpressionDiagnosticsPointAtTheFailingToken()
    {
        const string markup = """
            <TextBlock>
              @when IsEnabled and
                    FontSize
              {
                Text = "x";
              }
            </TextBlock>
            """;

        GeneratorRunResult result = RunGenerator("LogicalLocation.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI007", "LogicalLocation.crn");
        FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
        Assert.Equal(2, span.StartLinePosition.Line);
        Assert.Equal(8, span.StartLinePosition.Character);
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

        GeneratorRunResult result = RunGenerator("LocalWins.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.LocalWinsFactory"));
        SolidColorBrush local = new(new Cerneala.Drawing.Color(12, 34, 56));
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

        GeneratorRunResult result = RunGenerator("NestedWhen.crn", markup, out Compilation compilation);
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
                <Aspect Name="Hover" TargetType="Border">
                  @default { Background = Black; }
                  @when IsMouseOver
                  {
                    @if value == True { Background = White; }
                  }
                </Aspect>
              </Border.Resources>
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("ReactiveAspect.crn", markup, out Compilation compilation);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Border border = Assert.IsType<Border>(InvokeCreate(stream, "Cerneala.GeneratedUi.ReactiveAspectFactory"));
        UIRoot root = AttachAndProcess(border);
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);
        border.IsPointerOver = true;
        root.ProcessFrame();
        AssertSolidBackground(Cerneala.Drawing.Color.White, border);
        border.IsPointerOver = false;
        root.ProcessFrame();
        AssertSolidBackground(Cerneala.Drawing.Color.Black, border);
    }

    [Fact]
    public void UnsupportedDirectiveReportsGeneratorDiagnostic()
    {
        const string markup = """
            <Border>
              @animate $base { Background = White; }
            </Border>
            """;

        GeneratorRunResult result = RunGenerator("UnsupportedDirective.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI006", "UnsupportedDirective.crn");
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

        GeneratorRunResult result = RunGenerator("NestedDataType.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI007", "NestedDataType.crn");
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
            "Views/MainWindow.crn",
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
            "Views/TemplateView.crn",
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
        AssertSolidBackground(Cerneala.Drawing.Color.Black, root);
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
            "Views/InvalidTemplateView.crn",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "Views/InvalidTemplateView.crn");
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
            <UserControl xmlns:views="clr-namespace:TestInput.Views;assembly=GeneratorTests">
              <views:FancyButton Content="Fancy">
                @template
                {
                  <ContentPresenter Content="$owner.Content:OneWay" />
                }
              </views:FancyButton>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/CustomTemplateView.crn",
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
            "Views/EventTemplateView.crn",
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
            <UserControl xmlns:views="clr-namespace:TestInput.Views;assembly=GeneratorTests">
              <Button>
                @template { <views:ReadOnlyPart MirrorFontSize="$owner.FontSize" /> }
              </Button>
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/ReadOnlyTemplateView.crn",
            markup,
            inputSource,
            out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI012", "Views/ReadOnlyTemplateView.crn");
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
            "Views/ConditionalView.crn",
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
    public void PairedMarkupResolvesCustomControlThroughClrNamespaceAlias()
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
            <UserControl xmlns:components="clr-namespace:TestInput.Components;assembly=GeneratorTests">
              <components:ProfileCard Score="7" />
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainView.crn",
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
    public void PairedMarkupSupportsCustomClrPropertiesAndInheritedContentContract()
    {
        const string inputSource = """
            using Cerneala.UI.Controls;

            namespace TestInput.Views;

            public sealed class GaugeBorder : Border
            {
                public float TrackLength { get; set; }
            }

            public partial class MainWindow : Window { }
            """;
        const string markup = """
            <Window xmlns:views="clr-namespace:TestInput.Views;assembly=GeneratorTests">
              <views:GaugeBorder TrackLength="42">
                <TextBlock Text="custom content" />
              </views:GaugeBorder>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/MainWindow.crn",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type windowType = assembly.GetType("TestInput.Views.MainWindow", throwOnError: true)!;
        Window window = Assert.IsAssignableFrom<Window>(Activator.CreateInstance(windowType));
        Border border = Assert.IsAssignableFrom<Border>(window.Content);
        Assert.Equal(42f, (float)border.GetType().GetProperty("TrackLength")!.GetValue(border)!);
        Assert.Equal("custom content", Assert.IsType<TextBlock>(border.Child).Text);
    }

    [Fact]
    public void SemanticDiscoveryResolvesBuiltInInheritedAndEnumProperties()
    {
        const string markup = """
            <StackPanel>
              <Slider Minimum="1" Maximum="10" Value="4" SmallChange="0.5" Orientation="Vertical" />
              <TextBox Text="hello" CaretBrush="#FF123456" />
              <CheckBox IsChecked="True" />
            </StackPanel>
            """;

        GeneratorRunResult result = RunGenerator("SemanticControls.crn", markup, out Compilation compilation);

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
        Assert.Equal(
            new Cerneala.Drawing.Color(0x12, 0x34, 0x56),
            Assert.IsType<SolidColorBrush>(textBox.CaretBrush).Color);

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

        GeneratorRunResult result = RunGenerator("InlineAspect.crn", markup, out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        StackPanel panel = Assert.IsType<StackPanel>(InvokeCreate(stream, "Cerneala.GeneratedUi.InlineAspectFactory"));
        UIRoot root = AttachAndProcess(panel);
        Assert.NotNull(panel.Aspect);
        Assert.True(panel.Aspect.IsConditional);
        Assert.Equal(new[] { "Margin" }, panel.Aspect.DefaultValues.Select(value => value.Property.Name));
        Assert.Equal(new Cerneala.UI.Layout.Thickness(8), panel.Margin);
        Assert.Single(panel.VisualChildren);

        Button button = Assert.IsType<Button>(panel.VisualChildren[0]);
        Assert.NotNull(button.Aspect);
        Assert.Equal(Cerneala.Drawing.Color.White, Assert.IsType<SolidColorBrush>(button.Foreground).Color);

        panel.IsPointerOver = true;
        root.ProcessFrame();
        Assert.Equal(new Cerneala.UI.Layout.Thickness(12), panel.Margin);
        panel.IsPointerOver = false;
        root.ProcessFrame();
        Assert.Equal(new Cerneala.UI.Layout.Thickness(8), panel.Margin);

        panel.Aspect = null;
        root.ProcessFrame();
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

        GeneratorRunResult result = RunGenerator("ConflictingAspect.crn", markup, out _);

        Diagnostic diagnostic = AssertDiagnostic(result, "CERNEALAUI005", "ConflictingAspect.crn");
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
            "Views/BadView.crn",
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
            "Views/EventView.crn",
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
            "Views/RootView.crn",
            conditionalRoot,
            rootSource,
            out _);
        Assert.Contains(rootResult.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI008");
        Assert.Empty(rootResult.GeneratedSources);
    }

    [Fact]
    public void PairedMarkupCanBindChildToRootUiProperty()
    {
        const string inputSource = """
            using System.Collections;
            using Cerneala.UI.Controls;
            using Cerneala.UI.Core;

            namespace TestInput.Views;

            public partial class RootItemsView : UserControl
            {
                public static readonly UiProperty<IEnumerable?> RowsProperty = UiProperty<IEnumerable?>.Register(
                    nameof(Rows),
                    typeof(RootItemsView),
                    new UiPropertyMetadata<IEnumerable?>(null));

                public IEnumerable? Rows
                {
                    get => GetValue(RowsProperty);
                    set => SetValue(RowsProperty, value);
                }
            }
            """;
        const string markup = """
            <UserControl>
              <ItemsControl Name="Items" ItemsSource="$root.Rows:OneWay" />
            </UserControl>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/RootItemsView.crn",
            markup,
            inputSource,
            out Compilation compilation);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);
        Assert.True(emit.Success, string.Join(Environment.NewLine, emit.Diagnostics));

        Assembly assembly = Assembly.Load(stream.ToArray());
        Type viewType = assembly.GetType("TestInput.Views.RootItemsView", throwOnError: true)!;
        UserControl view = Assert.IsAssignableFrom<UserControl>(Activator.CreateInstance(viewType));
        ItemsControl items = Assert.IsType<ItemsControl>(viewType
            .GetProperty("Items", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(view));
        string[] rows = ["one", "two"];

        viewType.GetProperty("Rows")!.SetValue(view, rows);

        Assert.Same(rows, items.ItemsSource);
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
            "Views/MainWindow.crn",
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
                <Border Name="Chrome" Background="$owner.Background:OneWay" />
              }
              <StackPanel>
                <TextBlock Text="Window content" />
              </StackPanel>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/TemplateWindow.crn",
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
        AssertSolidBackground(Cerneala.Drawing.Color.Black, chrome);
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
                <Aspect TargetType="Window">
                  @template { <Border Name="AspectChrome" Background="$owner.Background:OneWay" /> }
                </Aspect>
              </Window.Resources>
            </Window>
            """;

        GeneratorRunResult result = RunPairedGenerator(
            "Views/AspectWindow.crn",
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
        AttachAndProcess(window);
        Border chrome = Assert.IsType<Border>(window.ComponentTemplateInstance!.Root);
        AssertSolidBackground(Cerneala.Drawing.Color.Black, chrome);
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
            "Views/MainWindow.crn",
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
            "Views/DialogWindow.crn",
            "<Window />",
            constructorSource,
            out _);
        Assert.Contains(constructor.Diagnostics, diagnostic => diagnostic.Id == "CERNEALAUI010");

        GeneratorRunResult conditionalRoot = RunPairedGenerator(
            "Views/MainWindow.crn",
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
            "Views/MainWindow.crn",
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
            "Views/MainWindow.crn",
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
            "Views/MainWindow.crn",
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

    [Fact]
    public void ChangingOneMarkupFileReusesIndependentSemanticModels()
    {
        InMemoryAdditionalText first = new("First.crn", "<Button Content=\"First\" />");
        InMemoryAdditionalText second = new("Second.crn", "<Button Content=\"Second\" />");
        CSharpCompilation compilation = CSharpCompilation.Create(
            "IncrementalGeneratorTests",
            [CSharpSyntaxTree.ParseText("namespace TestInput { public static class Anchor { } }")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new UiMarkupGenerator().AsSourceGenerator()],
            [first, second],
            parseOptions: CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            driverOptions: new GeneratorDriverOptions(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        InMemoryAdditionalText changed = new("First.crn", "<Button Content=\"Changed\" />");
        driver = driver.ReplaceAdditionalText(first, changed).RunGenerators(compilation);

        var outputs = driver.GetRunResult().Results.Single()
            .TrackedSteps["CernealaLanguageSemanticModel"]
            .SelectMany(step => step.Outputs)
            .ToArray();
        Assert.Equal(2, outputs.Length);
        Assert.Single(outputs, output => output.Reason == IncrementalStepRunReason.Modified);
        Assert.Single(outputs, output => output.Reason == IncrementalStepRunReason.Cached);
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
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool addDefaultBackendSelection = true)
    {
        return RunGenerator(
            new[] { new MarkupFile(fileName, markup) },
            out outputCompilation,
            inputSource,
            fileName + ".cs",
            outputKind,
            addDefaultBackendSelection);
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
        OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary,
        bool addDefaultBackendSelection = true)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(
            inputSource,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
            path: inputPath);
        SyntaxTree[] syntaxTrees = outputKind != OutputKind.DynamicallyLinkedLibrary && addDefaultBackendSelection
            ?
            [
                syntaxTree,
                CSharpSyntaxTree.ParseText(
                    DefaultBackendSelectionSource,
                    CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
                    path: "TestApplicationBackend.cs")
            ]
            : [syntaxTree];

        CSharpCompilation compilation = CSharpCompilation.Create(
            "GeneratorTests",
            syntaxTrees,
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

    private static void AssertSolidBackground(Color expected, Control control)
    {
        Assert.Equal(expected, Assert.IsType<SolidColorBrush>(control.Background).Color);
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
