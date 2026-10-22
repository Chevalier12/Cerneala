using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Presence;
using MotionFactory = Cerneala.UI.Motion.Specs.Motion;

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
    public void ProgressBarFrameworkDefaultForegroundPropagatesToTemplateText()
    {
        TextBlock? text = null;
        ProgressBar bar = new()
        {
            ComponentTemplate = new ComponentTemplate<ProgressBar>("Probe", _ => text = new TextBlock { Text = "x" })
        };
        UIRoot root = new(200, 50);
        root.VisualChildren.Add(bar);

        root.ProcessFrame();
        root.ProcessFrame();

        Assert.NotNull(text);
        Assert.Equal(UiPropertyValueSource.Default, bar.GetValueSource(Control.ForegroundProperty));
        Assert.Same(bar.Foreground, text.Foreground);
        Assert.Equal(new Color(65, 135, 230), ((SolidColorBrush)text.Foreground!).Color);
        Assert.Equal(UiPropertyValueSource.Inherited, text.GetValueSource(Control.ForegroundProperty));
    }

    [Fact]
    public void FrameworkDefaultFontSizePropagatesAndComesBackAfterALocalValueIsCleared()
    {
        UIRoot root = new();
        FontSizeDefaultControl parent = new();
        TextBlock child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();

        Assert.Equal(24, child.FontSize);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.FontSizeProperty));

        parent.FontSize = 30;
        root.ProcessFrame();

        Assert.Equal(30, child.FontSize);

        parent.ClearValue(Control.FontSizeProperty);
        root.ProcessFrame();

        Assert.Equal(24, child.FontSize);
        Assert.Equal(UiPropertyValueSource.Inherited, child.GetValueSource(Control.FontSizeProperty));
    }

    [Fact]
    public void ParentWithOnlyTheMetadataDefaultClearsTheChildInheritedValue()
    {
        UIRoot root = new();
        Control parent = new() { FontSize = 20 };
        TextBlock child = new();
        parent.VisualChildren.Add(child);
        root.VisualChildren.Add(parent);
        root.ProcessFrame();

        parent.ClearValue(Control.FontSizeProperty);
        root.ProcessFrame();

        Assert.Equal(Control.FontSizeProperty.Metadata.DefaultValue, child.FontSize);
        Assert.Equal(UiPropertyValueSource.Default, child.GetValueSource(Control.FontSizeProperty));
        Assert.Null(child.GetSourceValue(Control.FontSizeProperty, UiPropertyValueSource.Inherited));
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

    // An inherited write raises PropertyChanged, and the handler can change the tree. The frame's Inherited phase and
    // the ItemsPresenter, which writes the inherited values of the rows that it realizes during its measure, both
    // finish: a removed element gets no further writes, and the elements that stay attached get the inherited value.
    public enum InheritedWritePath
    {
        FrameInheritedPhase,
        FrameInheritedPhaseInsideItemsControl,
        ItemsPresenterMeasure,
    }

    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void HandlerThatRemovesItsOwnElementDuringInheritedPropagationDoesNotThrow(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        Button button = new();
        Border border = new() { Child = button };
        Button sibling = new();
        StackPanel row = new();
        row.Children.Add(border);
        row.Children.Add(sibling);
        button.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.DataContextProperty) && button.IsAttached)
            {
                border.Child = null;
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.False(button.IsAttached);
        Assert.Null(border.Child);
        Assert.Equal("row", border.DataContext);
        Assert.Equal("row", sibling.DataContext);
        Assert.Equal(UiPropertyValueSource.Inherited, sibling.GetValueSource(UIElement.DataContextProperty));
    }

    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void ElementThatItsHandlerRemovesGetsNoFurtherInheritedWrites(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        TextBlock text = new();
        Border border = new() { Child = text };
        TextBlock sibling = new();
        StackPanel row = new();
        row.Children.Add(border);
        row.Children.Add(sibling);
        text.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.DataContextProperty) && text.IsAttached)
            {
                border.Child = null;
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.False(text.IsAttached);
        Assert.Equal("row", text.DataContext);
        Assert.NotEqual(UiPropertyValueSource.Inherited, text.GetValueSource(Control.FontSizeProperty));
        Assert.Equal(30f, sibling.FontSize);
        Assert.Equal(UiPropertyValueSource.Inherited, sibling.GetValueSource(Control.FontSizeProperty));
    }

    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void HandlerThatRemovesALaterSiblingLeavesItWithoutTheInheritedValue(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        TextBlock first = new();
        TextBlock second = new();
        TextBlock third = new();
        StackPanel row = new();
        row.Children.Add(first);
        row.Children.Add(second);
        row.Children.Add(third);
        first.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.DataContextProperty) && second.IsAttached)
            {
                row.Children.Remove(second);
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.False(second.IsAttached);
        Assert.Null(second.DataContext);
        Assert.Equal(UiPropertyValueSource.Default, second.GetValueSource(UIElement.DataContextProperty));
        Assert.Equal("row", first.DataContext);
        Assert.Equal("row", third.DataContext);
        Assert.Equal(30f, third.FontSize);
    }

    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void HandlerThatRemovesAndReaddsItsOwnElementLeavesItWithTheInheritedValues(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        Button button = new();
        Border border = new() { Child = button };
        TextBlock sibling = new();
        StackPanel row = new();
        row.Children.Add(border);
        row.Children.Add(sibling);
        bool readded = false;
        button.PropertyChanged += (_, args) =>
        {
            if (!readded && ReferenceEquals(args.Property, UIElement.DataContextProperty) && button.IsAttached)
            {
                readded = true;
                border.Child = null;
                border.Child = button;
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.True(readded);
        Assert.True(button.IsAttached);
        Assert.Equal("row", button.DataContext);
        Assert.Equal(UiPropertyValueSource.Inherited, button.GetValueSource(UIElement.DataContextProperty));
        Assert.Equal("row", sibling.DataContext);
        Assert.Equal(30f, sibling.FontSize);
    }

    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void HandlerThatRemovesAnAncestorStopsTheWritesIntoTheRemovedSubtree(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        TextBlock first = new();
        TextBlock second = new();
        StackPanel inner = new();
        inner.Children.Add(first);
        inner.Children.Add(second);
        TextBlock after = new();
        StackPanel row = new();
        row.Children.Add(inner);
        row.Children.Add(after);
        first.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, UIElement.DataContextProperty) && inner.IsAttached)
            {
                row.Children.Remove(inner);
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.False(inner.IsAttached);
        Assert.Null(second.DataContext);
        Assert.Equal(UiPropertyValueSource.Default, second.GetValueSource(UIElement.DataContextProperty));
        Assert.Equal("row", after.DataContext);
        Assert.Equal(30f, after.FontSize);
    }

    // A removed ancestor with Presence stays attached while it exits, so the walk cannot tell the removal from the
    // elements' root alone.
    [Theory]
    [InlineData(InheritedWritePath.FrameInheritedPhase)]
    [InlineData(InheritedWritePath.FrameInheritedPhaseInsideItemsControl)]
    [InlineData(InheritedWritePath.ItemsPresenterMeasure)]
    public void HandlerThatRemovesAnExitingAncestorStopsTheWritesIntoTheRemovedSubtree(InheritedWritePath path)
    {
        UIRoot root = new(300, 200);
        TextBlock first = new();
        TextBlock second = new();
        StackPanel inner = new()
        {
            Presence = PresenceOptions.FadeAndScale(
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)),
                MotionFactory.Tween<float>(TimeSpan.FromMilliseconds(100)))
        };
        inner.Children.Add(first);
        inner.Children.Add(second);
        TextBlock after = new();
        StackPanel row = new();
        row.Children.Add(inner);
        row.Children.Add(after);
        bool removed = false;
        first.PropertyChanged += (_, args) =>
        {
            if (!removed && ReferenceEquals(args.Property, UIElement.DataContextProperty) && inner.IsAttached)
            {
                removed = true;
                row.Children.Remove(inner);
            }
        };

        Exception? failure = WriteInheritedValuesToRow(root, row, path);

        Assert.Null(failure);
        Assert.True(removed);
        Assert.Null(inner.VisualParent);
        Assert.Null(second.DataContext);
        Assert.Equal(UiPropertyValueSource.Default, second.GetValueSource(UIElement.DataContextProperty));
        Assert.Equal("row", after.DataContext);
        Assert.Equal(30f, after.FontSize);
    }

    [Fact]
    public void HandlerThatRemovesAnAncestorDuringADetachedWalkStopsTheWritesIntoTheRemovedSubtree()
    {
        TextBlock first = new();
        TextBlock second = new();
        StackPanel inner = new();
        inner.Children.Add(first);
        inner.Children.Add(second);
        TextBlock after = new();
        StackPanel row = new();
        row.Children.Add(inner);
        row.Children.Add(after);
        bool removed = false;
        first.PropertyChanged += (_, args) =>
        {
            if (!removed && ReferenceEquals(args.Property, UIElement.DataContextProperty))
            {
                removed = true;
                row.Children.Remove(inner);
            }
        };
        SetInheritedValues(row);

        Exception? failure = Record.Exception(() => new InheritedPropertyPropagator().PropagateFrom(row));

        Assert.Null(failure);
        Assert.True(removed);
        Assert.Null(second.DataContext);
        Assert.Equal(UiPropertyValueSource.Default, second.GetValueSource(UIElement.DataContextProperty));
        Assert.Equal("row", after.DataContext);
        Assert.Equal(30f, after.FontSize);
    }

    // Writes DataContext "row" and FontSize 30 to the row's elements through the given path and returns what the frame
    // that writes them threw.
    private static Exception? WriteInheritedValuesToRow(UIRoot root, UIElement row, InheritedWritePath path)
    {
        if (path == InheritedWritePath.ItemsPresenterMeasure)
        {
            // The ItemsControl's values reach the presenter in this frame. The row is realized during the presenter's
            // measure in the next frame, so the presenter's walk writes its first inherited values.
            ItemsControl owner = new();
            SetInheritedValues(owner);
            root.VisualChildren.Add(owner);
            root.ProcessFrame();
            owner.SetItems(new object[] { row });
        }
        else
        {
            if (path == InheritedWritePath.FrameInheritedPhaseInsideItemsControl)
            {
                ItemsControl items = new();
                items.SetItems(new object[] { row });
                root.VisualChildren.Add(items);
            }
            else
            {
                root.VisualChildren.Add(row);
            }

            root.ProcessFrame();
            SetInheritedValues(row);
        }

        return Record.Exception(() => root.ProcessFrame());
    }

    private static void SetInheritedValues(UIElement source)
    {
        source.DataContext = "row";
        source.SetValue(Control.FontSizeProperty, 30f);
    }

    private sealed class FontSizeDefaultControl : Control
    {
        public FontSizeDefaultControl()
        {
            SetFrameworkDefault(FontSizeProperty, 24f);
        }
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
