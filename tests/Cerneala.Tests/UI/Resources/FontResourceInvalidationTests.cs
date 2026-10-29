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

    [Theory]
    [InlineData("ResourceStore")]
    [InlineData("ResourceDictionary")]
    public void FontResourceReplacementChangesTextCacheIdentityForEveryProvider(string provider)
    {
        ResourceId<FontResource> id = new("Body");
        IResourceProvider resources;
        Action<FontResource> set;
        if (provider == "ResourceStore")
        {
            ResourceStore store = new();
            resources = store;
            set = resource => store.SetResource(id, resource);
        }
        else
        {
            ResourceDictionary dictionary = new();
            resources = dictionary;
            set = resource => dictionary.SetResource(id, resource);
        }

        set(new FontResource(new TestFont("Default", 16)));
        TextMeasurer measurer = new(new FontResolver(resources), LineBreakService.Default, new TextLayoutCache());
        TextAspect aspect = new("Fallback", 16, fontResourceId: id);
        TextMeasureResult first = measurer.Measure("Hello", aspect, 100);

        set(new FontResource(new TestFont("Serif", 16)));
        TextMeasureResult second = measurer.Measure("Hello", aspect, 100);

        Assert.NotEqual(first.CacheKey, second.CacheKey);
    }

    [Fact]
    public void ReplacingFontResourceInResourceDictionaryDrawsTextBlockWithNewFont()
    {
        ResourceDictionary resources = new();
        ResourceId<FontResource> id = new("Body");
        TestFont defaultFont = new("Default", 16);
        TestFont serifFont = new("Serif", 16);
        resources.SetResource(id, new FontResource(defaultFont));
        UIRoot root = new(400, 400);
        root.SetResourceProvider(resources);
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        Assert.Same(defaultFont, DrawnFont(root));

        resources.SetResource(id, new FontResource(serifFont));
        root.ProcessFrame();

        Assert.Same(serifFont, DrawnFont(root));
    }

    // Issue #1163: the two providers give the same font identity ("resource:Body:1"), so only the swap tells them apart.
    [Theory]
    [InlineData("ResourceStore")]
    [InlineData("ResourceDictionary")]
    public void SwappingTheRootResourceProviderDrawsTextBlockWithTheFontOfTheNewProvider(string providerType)
    {
        ResourceId<FontResource> id = new("Body");
        TestFont regularFont = new("Regular", 16);
        TestFont largeFont = new("Large", 32);
        IResourceProvider regular = CreateProvider(providerType, id, regularFont);
        IResourceProvider large = CreateProvider(providerType, id, largeFont);
        UIRoot root = new(400, 400);
        root.SetResourceProvider(regular);
        TextBlock textBlock = new()
        {
            Text = "Hello",
            FontResourceId = id
        };
        root.VisualChildren.Add(textBlock);
        root.ProcessFrame();
        Assert.Same(regularFont, DrawnFont(root));

        root.SetResourceProvider(large);
        root.ProcessFrame();

        Assert.Same(largeFont, DrawnFont(root));
    }

    private static IResourceProvider CreateProvider(string providerType, ResourceId<FontResource> id, IDrawFont font)
    {
        if (providerType == "ResourceStore")
        {
            ResourceStore store = new();
            store.SetResource(id, new FontResource(font));
            return store;
        }

        ResourceDictionary dictionary = new();
        dictionary.SetResource(id, new FontResource(font));
        return dictionary;
    }

    private static IDrawFont DrawnFont(UIRoot root)
    {
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        return Assert.Single(commands, static command => command.Kind == DrawCommandKind.DrawText).TextRun!.Font;
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
