using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;

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

        border.Background = DrawColor.Black;
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Single(commands);
        Assert.Equal(DrawColor.Black, commands[0].Color);
    }

    [Fact]
    public void GeneratedFactoryCreatesRetainedTree()
    {
        GeneratedUiFactory factory = new((Func<UIElement>)(() => new Border { Background = DrawColor.White }));

        MarkupResult<UIElement> result = factory.Create();

        Assert.False(result.HasErrors);
        Border border = Assert.IsType<Border>(result.Value);
        Assert.Equal(DrawColor.White, border.Background);
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
