using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Markup;

public sealed class UiFactoryTests
{
    [Fact]
    public void CreateBuildsNestedRetainedTreeInChildOrder()
    {
        UiMarkupDocument document = Read("<StackPanel><TextBlock Text=\"One\" /><Button>Two</Button></StackPanel>");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.False(result.HasErrors);
        StackPanel panel = Assert.IsType<StackPanel>(result.Value);
        Assert.Equal(2, panel.VisualChildren.Count);
        Assert.Equal(2, panel.LogicalChildren.Count);
        Assert.IsType<TextBlock>(panel.VisualChildren[0]);
        Button button = Assert.IsType<Button>(panel.VisualChildren[1]);
        Assert.Equal("Two", button.Content);
    }

    [Fact]
    public void CreateReportsUnknownElement()
    {
        UiMarkupDocument document = Read("<Missing />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP021", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CreateReportsUnknownProperty()
    {
        UiMarkupDocument document = Read("<TextBlock Nope=\"what\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP023", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CreateUsesTypedPropertyValidation()
    {
        UiMarkupDocument document = Read("<TextBlock FontSize=\"-1\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP027", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CreateRejectsEmptyThicknessComponents()
    {
        UiMarkupDocument document = Read("<Border BorderThickness=\"1,,2,3,4\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP027", Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("8", 8f, 8f, 8f, 8f)]
    [InlineData("8,0,0,8", 8f, 0f, 0f, 8f)]
    [InlineData(" 1.5 , 2 , 3 , 4 ", 1.5f, 2f, 3f, 4f)]
    public void CreateParsesBorderCornerRadius(string value, float topLeft, float topRight, float bottomRight, float bottomLeft)
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(Read($"<Border CornerRadius=\"{value}\" />"));

        Assert.False(result.HasErrors);
        Border border = Assert.IsType<Border>(result.Value);
        Assert.Equal(new CornerRadius(topLeft, topRight, bottomRight, bottomLeft), border.CornerRadius);
        Assert.Equal(topLeft, border.CornerRadius.TopLeft);
        Assert.Equal(topRight, border.CornerRadius.TopRight);
        Assert.Equal(bottomRight, border.CornerRadius.BottomRight);
        Assert.Equal(bottomLeft, border.CornerRadius.BottomLeft);
    }

    [Theory]
    [InlineData("1,2")]
    [InlineData("1,2,3")]
    [InlineData("1,2,3,4,5")]
    [InlineData("1,,2,3")]
    [InlineData("-1")]
    [InlineData("8,-1,0,0")]
    [InlineData("big")]
    public void CreateRejectsInvalidBorderCornerRadius(string value)
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(Read($"<Border CornerRadius=\"{value}\" />"));

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP027", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CreateDefaultAppliesEveryDocumentedElementSpecificProperty()
    {
        // The element-specific properties listed on docs-site/documentation/classes/Cerneala.UI.Markup.UiMarkupSchema.md.
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        Border border = Assert.IsType<Border>(factory.Create(Read("<Border CornerRadius=\"4\" />")).Value);
        Assert.Equal(new CornerRadius(4f), border.CornerRadius);
        Button button = Assert.IsType<Button>(factory.Create(Read("<Button Content=\"Save\" />")).Value);
        Assert.Equal("Save", button.Content);
        Cerneala.UI.Controls.Primitives.RepeatButton repeat = Assert.IsType<Cerneala.UI.Controls.Primitives.RepeatButton>(
            factory.Create(Read("<RepeatButton Content=\"x\" Delay=\"300\" Interval=\"50\" />")).Value);
        Assert.Equal("x", repeat.Content);
        Assert.Equal(300, repeat.Delay);
        Assert.Equal(50, repeat.Interval);
        TextBlock text = Assert.IsType<TextBlock>(factory.Create(Read("<TextBlock Text=\"One\" />")).Value);
        Assert.Equal("One", text.Text);
    }

    [Theory]
    [InlineData("Delay", "1.5")]
    [InlineData("Delay", "-1")]
    [InlineData("Interval", "0")]
    public void CreateReportsInvalidRepeatButtonTimingAsTypedValueDiagnostic(string property, string value)
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(Read($"<RepeatButton {property}=\"{value}\" />"));

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP027", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CreateRejectsCornerRadiusOnElementsThatAreNotBorders()
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(Read("<Button CornerRadius=\"8\" />"));

        Assert.True(result.HasErrors);
        Assert.Equal("MARKUP023", Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("Panel", "Background", "Red")]
    [InlineData("Panel", "Foreground", "Red")]
    [InlineData("Panel", "BorderBrush", "Red")]
    [InlineData("Panel", "BorderThickness", "1")]
    [InlineData("Panel", "Padding", "2")]
    [InlineData("Panel", "FontFamily", "Segoe UI")]
    [InlineData("Panel", "FontSize", "12")]
    [InlineData("StackPanel", "Background", "Red")]
    [InlineData("StackPanel", "Foreground", "Red")]
    [InlineData("StackPanel", "BorderBrush", "Red")]
    [InlineData("StackPanel", "BorderThickness", "1")]
    [InlineData("StackPanel", "Padding", "2")]
    [InlineData("StackPanel", "FontFamily", "Segoe UI")]
    [InlineData("StackPanel", "FontSize", "12")]
    public void CreateReportsControlPropertiesOnPanelsAsUnknownProperties(string element, string property, string value)
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());
        UiMarkupDocument document = Read($"<{element} {property}=\"{value}\" />");

        MarkupResult<UIElement> strict = factory.Create(document);
        MarkupResult<UIElement> recovered = factory.Create(document, MarkupLoadOptions.Recover);

        Assert.Null(strict.Value);
        MarkupDiagnostic diagnostic = Assert.Single(strict.Diagnostics);
        Assert.Equal("MARKUP023", diagnostic.Code);
        Assert.Contains($"'{element}.{property}'", diagnostic.Message);
        Assert.IsAssignableFrom<Cerneala.UI.Layout.Panels.Panel>(recovered.Value);
        Assert.Equal("MARKUP023", Assert.Single(recovered.Diagnostics).Code);
    }

    [Theory]
    [InlineData("Panel")]
    [InlineData("StackPanel")]
    public void CreateAppliesElementPropertiesOnPanels(string element)
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(Read($"<{element} Margin=\"4\" IsEnabled=\"False\" IsVisible=\"False\" />"));

        Assert.False(result.HasErrors);
        UIElement panel = Assert.IsAssignableFrom<Cerneala.UI.Layout.Panels.Panel>(result.Value);
        Assert.Equal(new Thickness(4), panel.Margin);
        Assert.False(panel.IsEnabled);
        Assert.False(panel.IsVisible);
    }

    [Fact]
    public void CreateRejectsEmptyColorComponents()
    {
        UiMarkupDocument document = Read("<Border Background=\"255,,255,255\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP027", Assert.Single(result.Diagnostics).Code);
    }

    [Theory]
    [InlineData("Background")]
    [InlineData("Foreground")]
    [InlineData("BorderBrush")]
    public void CreateReportsMissingBrushResourceAsDiagnostic(string property)
    {
        UiMarkupDocument document = Read($"<Button {property}=\"$Missing\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> strict = factory.Create(document);
        MarkupResult<UIElement> recovered = factory.Create(document, MarkupLoadOptions.Recover);

        Assert.Null(strict.Value);
        MarkupDiagnostic diagnostic = Assert.Single(strict.Diagnostics);
        Assert.Equal("MARKUP027", diagnostic.Code);
        Assert.Equal(MarkupDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains($"'{property}'", diagnostic.Message);
        Assert.Contains("'Missing'", diagnostic.Message);
        Assert.IsType<Button>(recovered.Value);
        Assert.Equal("MARKUP027", Assert.Single(recovered.Diagnostics).Code);
    }

    [Fact]
    public void CreateParsesShortAndCompositeBorderBrushValues()
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        Border solid = Assert.IsType<Border>(factory.Create(Read("<Border BorderBrush=\"Tomato\" />")).Value);
        Border gradient = Assert.IsType<Border>(factory.Create(Read("""
            <Border>
              <Border.BorderBrush>
                <LinearGradientBrush StartPoint="0,0" EndPoint="10,0">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </LinearGradientBrush>
              </Border.BorderBrush>
            </Border>
            """)).Value);

        Assert.Equal(Color.Tomato, Assert.IsType<SolidColorBrush>(solid.BorderBrush).Color);
        Assert.IsType<LinearGradientBrush>(gradient.BorderBrush);
    }

    [Fact]
    public void CreateResolvesBorderBrushResourceOnItsOwner()
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());
        MarkupResult<UIElement> result = factory.Create(Read("""
            <Border BorderBrush="$Accent">
              <Border.Resources>
                <RadialGradientBrush Name="Accent" Center="5,5" RadiusX="5" RadiusY="5">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </RadialGradientBrush>
              </Border.Resources>
            </Border>
            """));

        Assert.False(result.HasErrors);
        Border border = Assert.IsType<Border>(result.Value);
        Assert.IsType<RadialGradientBrush>(border.BorderBrush);
        Assert.Same(border.BorderBrush, border.Resources["Accent"]);
    }

    [Fact]
    public void CreateParsesBackgroundShorthandResourceAndPropertyElement()
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());
        Border shorthand = Assert.IsType<Border>(factory.Create(Read("<Border Background=\"Tomato\" />")).Value);
        Border resource = Assert.IsType<Border>(factory.Create(Read("""
            <Border Background="$Fill">
              <Border.Resources>
                <ImageBrush Name="Fill" Source="fill.png" />
              </Border.Resources>
            </Border>
            """)).Value);
        Border property = Assert.IsType<Border>(factory.Create(Read("""
            <Border>
              <Border.Background>
                <LinearGradientBrush StartPoint="0,0" EndPoint="10,0">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </LinearGradientBrush>
              </Border.Background>
            </Border>
            """)).Value);

        Assert.Equal(Color.Tomato, Assert.IsType<SolidColorBrush>(shorthand.Background).Color);
        Assert.IsType<ImageBrush>(resource.Background);
        Assert.IsType<LinearGradientBrush>(property.Background);
    }

    [Fact]
    public void CreateParsesForegroundShorthandResourceAndPropertyElement()
    {
        UiFactory factory = new(UiMarkupSchema.CreateDefault());
        TextBlock shorthand = Assert.IsType<TextBlock>(factory.Create(Read("<TextBlock Foreground=\"Tomato\" />")).Value);
        TextBlock resource = Assert.IsType<TextBlock>(factory.Create(Read("""
            <TextBlock Foreground="$Fill">
              <TextBlock.Resources>
                <ImageBrush Name="Fill" Source="fill.png" />
              </TextBlock.Resources>
            </TextBlock>
            """)).Value);
        TextBlock property = Assert.IsType<TextBlock>(factory.Create(Read("""
            <TextBlock>
              <TextBlock.Foreground>
                <LinearGradientBrush StartPoint="0,0" EndPoint="10,0">
                  <GradientStop Offset="0" Color="White" />
                  <GradientStop Offset="1" Color="Black" />
                </LinearGradientBrush>
              </TextBlock.Foreground>
            </TextBlock>
            """)).Value);

        Assert.Equal(Color.Tomato, Assert.IsType<SolidColorBrush>(shorthand.Foreground).Color);
        Assert.IsType<ImageBrush>(resource.Foreground);
        Assert.IsType<LinearGradientBrush>(property.Foreground);
    }

    [Fact]
    public void CreateUsesTypedPropertyCoercion()
    {
        UiMarkupDocument document = Read("<TextBlock />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());

        MarkupResult<UIElement> result = factory.Create(document);

        TextBlock textBlock = Assert.IsType<TextBlock>(result.Value);
        textBlock.Text = null!;
        Assert.Equal(string.Empty, textBlock.Text);
    }

    [Fact]
    public void MarkupCreatedTreeUsesRetainedRenderCacheInvalidation()
    {
        UiMarkupDocument document = Read("<Border Background=\"255,255,255\" />");
        UiFactory factory = new(UiMarkupSchema.CreateDefault());
        Border border = Assert.IsType<Border>(factory.Create(document).Value);
        UIRoot root = new(10, 10);
        root.VisualChildren.Add(border);
        border.Arrange(new ArrangeContext(new LayoutRect(0, 0, 10, 10)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();
        root.RetainedRenderer.Commit(root);

        border.Background = new SolidColorBrush(Color.Black);
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(Color.Black, Assert.IsType<SolidColorBrush>(commands[0].Brush).Color);
    }

    [Fact]
    public void GeneratedFactoryCreatesRetainedTree()
    {
        GeneratedUiFactory factory = new((Func<UIElement>)(() => new Border { Background = new SolidColorBrush(Color.White) }));

        MarkupResult<UIElement> result = factory.Create();

        Assert.False(result.HasErrors);
        Border border = Assert.IsType<Border>(result.Value);
        Assert.Equal(new SolidColorBrush(Color.White), border.Background);
    }

    [Fact]
    public void GeneratedFactoryReportsDiagnostics()
    {
        GeneratedUiFactory factory = new((Func<UIElement>)(() => throw new InvalidOperationException("boom")));

        MarkupResult<UIElement> result = factory.Create();

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP030", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void GeneratedFactoryReportsNullRoot()
    {
        GeneratedUiFactory factory = new((Func<UIElement>)(() => null!));

        MarkupResult<UIElement> result = factory.Create();

        Assert.True(result.HasErrors);
        Assert.Null(result.Value);
        Assert.Equal("MARKUP030", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void CodeFirstCreationDoesNotRequireMarkupServices()
    {
        Button button = new() { Content = "Code" };

        Assert.Equal("Code", button.Content);
    }

    private static UiMarkupDocument Read(string markup)
    {
        MarkupResult<UiMarkupDocument> result = new UiMarkupReader().Read(markup);
        Assert.False(result.HasErrors);
        Assert.NotNull(result.Value);
        return result.Value;
    }
}
