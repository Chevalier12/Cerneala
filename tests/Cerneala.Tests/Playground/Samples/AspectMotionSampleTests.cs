using Cerneala.Drawing;
using Cerneala.Playground.Samples;
using Cerneala.Tests.UI.Motion.Core;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.Tests.Playground.Samples;

public sealed class AspectMotionSampleTests
{
    [Fact]
    public void AspectMotionSampleRegistersPackageAndDefaultSelectorTab()
    {
        AspectPackage package = AspectMotionSample.CreatePackage();
        AspectCatalog catalog = new AspectRegistry().Register(package).BuildCatalog();
        SampleSelector selector = SampleSelector.CreateDefault();

        Assert.Equal("Playground.AspectMotion", package.Name);
        Assert.NotEmpty(catalog.Rules);
        Assert.Equal("Aspect Motion", selector.Samples.Last().Name);
    }

    [Fact]
    public void AspectMotionSampleBuildsHoverMotionTarget()
    {
        UIElement sampleRoot = new AspectMotionSample().Build();

        AspectMotionSample.HoverMotionCard card = DescendantsAndSelf<AspectMotionSample.HoverMotionCard>(sampleRoot).Single();

        Assert.Contains(DescendantsAndSelf<TextBlock>(sampleRoot), block => block.Text.Contains("Hover", StringComparison.Ordinal));
        Assert.Equal(0f, card.TranslateX);
        Assert.Equal(1f, card.Scale);
    }

    [Fact]
    public void AspectMotionSampleHoverAnimatesCardMotion()
    {
        ManualMotionClock clock = new();
        UIRoot root = new(800, 600, motionClock: clock);
        root.AspectRegistry.Register(AspectMotionSample.CreatePackage());
        UIElement sampleRoot = new AspectMotionSample().Build();
        root.VisualChildren.Add(sampleRoot);
        root.ProcessFrame();
        AspectMotionSample.HoverMotionCard card = DescendantsAndSelf<AspectMotionSample.HoverMotionCard>(sampleRoot).Single();

        card.IsPointerOver = true;
        root.ProcessFrame();
        clock.Advance(TimeSpan.FromMilliseconds(90));
        root.ProcessFrame();

        Assert.InRange(card.Scale, 1f, 1.06f);
        Assert.InRange(card.TranslateX, 0.1f, 18f);
        Assert.Equal(new DrawColor(99, 102, 241), card.BorderColor);
    }

    private static IEnumerable<T> DescendantsAndSelf<T>(UIElement element)
        where T : UIElement
    {
        if (element is T match)
        {
            yield return match;
        }

        foreach (UIElement child in element.VisualChildren)
        {
            foreach (T descendant in DescendantsAndSelf<T>(child))
            {
                yield return descendant;
            }
        }
    }
}
