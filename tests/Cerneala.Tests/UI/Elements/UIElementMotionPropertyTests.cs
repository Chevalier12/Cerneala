using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Elements;

public sealed class UIElementMotionPropertyTests
{
    [Fact]
    public void ElementMotionPropertiesHaveStableDefaults()
    {
        UIElement element = new();

        Assert.Equal(Transform.Identity, element.RenderTransform);
        Assert.Equal(new LayoutPoint(0.5f, 0.5f), element.RenderTransformOrigin);
        Assert.Equal(1, element.Opacity);
        Assert.Equal(0, element.TranslateX);
        Assert.Equal(0, element.TranslateY);
        Assert.Equal(1, element.Scale);
        Assert.Equal(1, element.ScaleX);
        Assert.Equal(1, element.ScaleY);
        Assert.Equal(0, element.Rotation);
        Assert.Equal(0, element.SkewX);
        Assert.Equal(0, element.SkewY);
        Assert.False(element.ClipToBounds);
    }

    [Fact]
    public void OpacityAndRenderTransformDirtyRenderButNotLayout()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        int layoutVersion = element.LayoutVersion;
        int renderScopeVersion = element.RenderScopeVersion;

        element.Opacity = 0.5f;
        element.RenderTransform = new Transform(Matrix3x2.CreateTranslation(4, 6));

        Assert.Equal(layoutVersion, element.LayoutVersion);
        Assert.True(element.RenderScopeVersion > renderScopeVersion);
        Assert.True(element.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.Contains(element, root.RenderQueue.Snapshot());
    }

    [Fact]
    public void TransformChannelsDirtyRenderButNotLayout()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        int layoutVersion = element.LayoutVersion;

        element.TranslateX = 10;
        element.TranslateY = 12;
        element.Scale = 1.2f;
        element.ScaleX = 1.3f;
        element.ScaleY = 0.8f;
        element.Rotation = 0.5f;
        element.SkewX = 0.1f;
        element.SkewY = 0.2f;
        element.RenderTransformOrigin = new LayoutPoint(0, 1);

        Assert.Equal(layoutVersion, element.LayoutVersion);
        Assert.True(element.DirtyState.Has(InvalidationFlags.Render));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Arrange));
    }

    [Fact]
    public void InvalidMotionPropertyValuesAreRejected()
    {
        UIElement element = new();

        Assert.Throws<ArgumentException>(() => element.Opacity = -0.01f);
        Assert.Throws<ArgumentException>(() => element.Opacity = 1.01f);
        Assert.Throws<ArgumentException>(() => element.Opacity = float.NaN);
        Assert.Throws<ArgumentException>(() => element.RenderTransform = null!);
        Assert.Throws<ArgumentException>(() => element.RenderTransformOrigin = new LayoutPoint(-0.01f, 0.5f));
        Assert.Throws<ArgumentException>(() => element.RenderTransformOrigin = new LayoutPoint(0.5f, 1.01f));
        Assert.Throws<ArgumentException>(() => element.TranslateX = float.PositiveInfinity);
        Assert.Throws<ArgumentException>(() => element.Scale = float.NaN);
    }

    [Fact]
    public void ClipToBoundsDirtiesRenderAndHitTestOnly()
    {
        UIRoot root = new();
        UIElement element = new();
        root.VisualChildren.Add(element);
        root.ProcessFrame();
        int layoutVersion = element.LayoutVersion;

        element.ClipToBounds = true;

        Assert.Equal(layoutVersion, element.LayoutVersion);
        Assert.True(element.DirtyState.Has(InvalidationFlags.Render));
        Assert.True(element.DirtyState.Has(InvalidationFlags.HitTest));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Measure));
        Assert.False(element.DirtyState.Has(InvalidationFlags.Arrange));
        Assert.Contains(element, root.HitTestQueue.Snapshot());
    }
}
