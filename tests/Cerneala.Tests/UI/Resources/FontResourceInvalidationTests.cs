using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Resources;

public sealed class FontResourceInvalidationTests
{
    [Fact]
    public void FontResourceResolvesDrawFont()
    {
        TestFont font = new("Default", 16);
        FontResource resource = new(font);

        Assert.Same(font, resource.Resolve());
    }

    [Fact]
    public void ReplacingFontResourceInvalidatesTextMeasurementAndRender()
    {
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        store.SetResource(id, new FontResource(new TestFont("Default", 16)));
        UIRoot root = new(100, 100);
        root.SetResourceProvider(store);
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        textBlock.DirtyState.ClearAll();

        store.SetResource(id, new FontResource(new TestFont("Serif", 16)));

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
    }

    [Fact]
    public void FontResourceReplacementChangesTextCacheIdentity()
    {
        ResourceStore store = new();
        ResourceId<FontResource> id = new("Body");
        store.SetResource(id, new FontResource(new TestFont("Default", 16)));
        TextMeasurer measurer = new(new FontResolver(store), LineBreakService.Default, new TextLayoutCache());
        TextAspect aspect = new("Fallback", 16, fontResourceId: id);
        TextMeasureResult first = measurer.Measure("Hello", aspect, 100);

        store.SetResource(id, new FontResource(new TestFont("Serif", 16)));
        TextMeasureResult second = measurer.Measure("Hello", aspect, 100);

        Assert.NotEqual(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void ChangingFontResourceIdInvalidatesTextMeasurementAndRender()
    {
        ResourceStore store = new();
        ResourceId<FontResource> body = new("Body");
        ResourceId<FontResource> heading = new("Heading");
        store.SetResource(body, new FontResource(new TestFont("BodyFont", 16)));
        store.SetResource(heading, new FontResource(new TestFont("HeadingFont", 16)));
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontResourceId = body,
            ResourceProvider = store
        };
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));
        string firstResourceIdentity = textBlock.RenderDependencies.ResourceIdentity;
        textBlock.DirtyState.ClearAll();

        textBlock.FontResourceId = heading;
        textBlock.Measure(new MeasureContext(new LayoutSize(100, 100)));

        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Measure));
        Assert.True(textBlock.DirtyState.Has(InvalidationFlags.Render));
        Assert.NotEqual(firstResourceIdentity, textBlock.RenderDependencies.ResourceIdentity);
        Assert.Equal(heading.ToString(), textBlock.RenderDependencies.ResourceIdentity);
    }

    private sealed record TestFont(string FamilyName, float Size) : IDrawFont;
}
