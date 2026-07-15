using Cerneala.UI.Layout;
using Cerneala.UI.Text;

namespace Cerneala.Tests.UI.Text;

public sealed class TextLayoutCacheTests
{
    [Fact]
    public void UnchangedLayoutHitsCache()
    {
        TextLayoutCache cache = new();
        TextLayoutKey key = new("Hello", "Default:16", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);

        TextMeasureResult first = cache.GetOrAdd(key, CreateResult);
        TextMeasureResult second = cache.GetOrAdd(key, CreateResult);

        Assert.Same(first, second);
        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Hits);
    }

    [Fact]
    public void ColorDoesNotAffectCacheIdentity()
    {
        TextLayoutKey first = new("Hello", "Default:16", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);
        TextLayoutKey second = new("Hello", "Default:16", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void TextContentChangesCacheIdentity()
    {
        TextLayoutKey first = new("Hello", "Default:16", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);
        TextLayoutKey second = first with { Text = "World" };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void CacheEvictsTheLeastRecentlyUsedLayoutAtCapacity()
    {
        TextLayoutCache cache = new(capacity: 2);
        TextLayoutKey first = new("first", "Default:16", 16, TextWrapping.NoWrap, float.PositiveInfinity, TextTrimming.None, 1);
        TextLayoutKey second = first with { Text = "second" };
        TextLayoutKey third = first with { Text = "third" };

        cache.GetOrAdd(first, CreateResult);
        cache.GetOrAdd(second, CreateResult);
        cache.GetOrAdd(first, CreateResult);
        cache.GetOrAdd(third, CreateResult);

        Assert.Equal(2, cache.Count);
        Assert.True(cache.Contains(first));
        Assert.False(cache.Contains(second));
        Assert.True(cache.Contains(third));
    }

    private static TextMeasureResult CreateResult(TextLayoutKey key)
    {
        return new TextMeasureResult(new LayoutSize(10, 16), 1, key, key.FontIdentity, [new TextLine(key.Text, 10)]);
    }
}
