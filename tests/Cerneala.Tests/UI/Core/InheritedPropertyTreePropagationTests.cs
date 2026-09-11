using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;

namespace Cerneala.Tests.UI.Core;

public sealed class InheritedPropertyTreePropagationTests
{
    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void SingleInheritedChangeAppliesEachDescendantOnce(int depth)
    {
        UIRoot root = new();
        InheritanceCountingElement parent = new();
        parent.SetValue(InheritanceCountingElement.ValueProperty, 1);
        root.VisualChildren.Add(parent);
        List<InheritanceCountingElement> descendants = [];
        UIElement current = parent;
        for (int index = 0; index < depth; index++)
        {
            InheritanceCountingElement child = new();
            current.VisualChildren.Add(child);
            descendants.Add(child);
            current = child;
        }

        root.ProcessFrame();
        foreach (InheritanceCountingElement child in descendants)
        {
            child.ApplicationCount = 0;
        }

        parent.SetValue(InheritanceCountingElement.ValueProperty, 2);
        root.ProcessFrame();

        Assert.All(descendants, child => Assert.Equal(2, child.GetValue(InheritanceCountingElement.ValueProperty)));
        Assert.Equal(depth, descendants.Sum(child => child.ApplicationCount));
        Assert.All(descendants, child => Assert.Equal(1, child.ApplicationCount));
        Assert.False(root.InheritedPropertyQueue.HasWork);
    }

    [Fact]
    public void DescendantCallbackCanRequeueAnAncestorDuringPropagation()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 20 };
        Control child = new();
        Control grandchild = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        child.VisualChildren.Add(grandchild);
        root.ProcessFrame();
        grandchild.PropertyChanged += (_, args) =>
        {
            if (args.Property == Control.FontSizeProperty && grandchild.FontSize == 30)
            {
                parent.FontSize = 40;
            }
        };

        parent.FontSize = 30;
        root.ProcessFrame();

        Assert.Equal(40, child.FontSize);
        Assert.Equal(40, grandchild.FontSize);
        Assert.False(root.InheritedPropertyQueue.HasWork);
    }

    [Fact]
    public void LaterSiblingCallbackCanRequeueAnAlreadyPropagatedSubtree()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 20 };
        Control first = new();
        Control firstChild = new();
        Control second = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(first);
        first.VisualChildren.Add(firstChild);
        parent.VisualChildren.Add(second);
        root.ProcessFrame();
        second.PropertyChanged += (_, args) =>
        {
            if (args.Property == Control.FontSizeProperty && second.FontSize == 30)
            {
                first.FontSize = 40;
            }
        };

        parent.Invalidate(InvalidationFlags.Inherited | InvalidationFlags.Subtree, "queued subtree");
        parent.FontSize = 30;
        root.ProcessFrame();

        Assert.Equal(40, firstChild.FontSize);
        Assert.Equal(30, second.FontSize);
        Assert.False(root.InheritedPropertyQueue.HasWork);
    }

    [Fact]
    public void PropagationFailureKeepsSubtreeRetryable()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 20 };
        Control child = new();
        Control grandchild = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        child.VisualChildren.Add(grandchild);
        root.ProcessFrame();
        bool shouldThrow = true;
        grandchild.PropertyChanged += (_, args) =>
        {
            if (args.Property == Control.FontSizeProperty && shouldThrow)
            {
                shouldThrow = false;
                throw new InvalidOperationException("propagation callback failure");
            }
        };

        parent.FontSize = 30;
        Assert.Throws<InvalidOperationException>(() => root.ProcessFrame());
        Assert.Contains(parent, root.InheritedPropertyQueue.Snapshot());
        root.ProcessFrame();

        Assert.Equal(30, child.FontSize);
        Assert.Equal(30, grandchild.FontSize);
        Assert.False(root.InheritedPropertyQueue.HasWork);
        Assert.False(child.DirtyState.Flags.HasFlag(InvalidationFlags.Inherited));
        Assert.False(grandchild.DirtyState.Flags.HasFlag(InvalidationFlags.Inherited));
    }

    [Fact]
    public void DirectPropagationPreservesPendingSchedulerWork()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 20 };
        Control child = new();
        root.VisualChildren.Add(parent);
        parent.VisualChildren.Add(child);
        root.ProcessFrame();
        parent.FontSize = 30;

        root.InheritedPropertyPropagator.PropagateFrom(parent);

        Assert.Equal(30, child.FontSize);
        Assert.Contains(parent, root.InheritedPropertyQueue.Snapshot());
        Assert.Contains(child, root.InheritedPropertyQueue.Snapshot());
    }

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

    private sealed class InheritanceCountingElement : UIElement
    {
        public static readonly UiProperty<int> ValueProperty = UiProperty<int>.Register(
            nameof(ValueProperty),
            typeof(InheritanceCountingElement),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.Inherits, coerceValue: (owner, value) =>
            {
                if (owner is InheritanceCountingElement element)
                {
                    element.ApplicationCount++;
                }

                return value;
            }));

        public int ApplicationCount { get; set; }
    }
}
