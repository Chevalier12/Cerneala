using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Core;

public sealed class InheritedPropertyTreePropagationTests
{
    [Fact]
    public void ParentForegroundPropagatesToDescendantDuringFrame()
    {
        UIRoot root = new();
        SolidColorBrush foreground = new(Color.White);
        Control parent = new() { Foreground = foreground };
        TextBlock child = new() { Text = "child" };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        root.ProcessFrame();

        Assert.Same(foreground, child.Foreground);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void LocalChildValueWinsOverInheritedValue()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 22 };
        TextBlock child = new() { FontSize = 11 };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        root.ProcessFrame();

        Assert.Equal(11, child.FontSize);
        Assert.Equal(UiPropertyValueSource.Local, child.GetValueSource(Control.FontSizeProperty));
    }

    [Fact]
    public void ChangingInheritedParentValueInvalidatesDescendantRender()
    {
        UIRoot root = new();
        Control parent = new() { Foreground = new SolidColorBrush(Color.Black) };
        TextBlock child = new() { Text = "child" };
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();
        child.DirtyState.Clear(InvalidationFlags.Render);

        SolidColorBrush foreground = new(Color.White);
        parent.Foreground = foreground;
        FrameStats stats = root.ProcessFrame();

        Assert.Same(foreground, child.Foreground);
        Assert.True(stats.InheritedElements > 0);
        Assert.True(child.RenderVersion > 0);
    }

    [Fact]
    public void NewlyAttachedSubtreeReceivesInheritedValuesOnNextFrame()
    {
        UIRoot root = new();
        Control parent = new() { FontFamily = "Body" };
        root.VisualChildren.Add(parent);
        root.ProcessFrame();
        TextBlock child = new();

        parent.VisualChildren.Add(child);
        root.ProcessFrame();

        Assert.Equal("Body", child.FontFamily);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.FontFamilyProperty));
    }

    [Fact]
    public void RemovedSubtreeKeepsInheritedValueButIgnoresFutureParentUpdates()
    {
        UIRoot root = new();
        Control parent = new() { FontFamily = "Body" };
        TextBlock child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();

        parent.VisualChildren.Remove(child);
        parent.FontFamily = "Title";
        root.ProcessFrame();

        Assert.Equal("Body", child.FontFamily);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.FontFamilyProperty));
    }

    [Fact]
    public void ReparentedSubtreeReceivesNewParentInheritedValues()
    {
        UIRoot root = new();
        Control firstParent = new() { Foreground = new SolidColorBrush(Color.White) };
        SolidColorBrush secondForeground = new(Color.Black);
        Control secondParent = new() { Foreground = secondForeground };
        TextBlock child = new();
        firstParent.VisualChildren.Add(child);
        root.VisualChildren.Add(firstParent);
        root.VisualChildren.Add(secondParent);
        root.ProcessFrame();

        firstParent.VisualChildren.Remove(child);
        secondParent.VisualChildren.Add(child);
        root.ProcessFrame();

        Assert.Same(secondForeground, child.Foreground);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void VisualContainerPropertiesDoNotInherit()
    {
        UIRoot root = new();
        Control parent = new() { Background = new Cerneala.UI.Media.SolidColorBrush(Color.White) };
        Control child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);

        root.ProcessFrame();

        Assert.Null(child.Background);
        Assert.Equal(UiPropertyValueSource.Default, child.GetValueSource(Control.BackgroundProperty));
    }
}
