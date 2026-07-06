using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Data;

public sealed class UiPropertyBindingTests
{
    [Fact]
    public void OneWayUiPropertyBindingUpdatesTargetImmediately()
    {
        ObservableValue<string> source = new("hello");
        TextBlock target = new();

        using IDisposable binding = BindingOperations.BindOneWay(target, TextBlock.TextProperty, source);

        Assert.Equal("hello", target.Text);
    }

    [Fact]
    public void OneWayUiPropertyBindingInvalidatesThroughUiPropertyMetadata()
    {
        UIRoot root = new(200, 80);
        TextBlock target = new();
        root.VisualChildren.Add(target);
        ObservableValue<string> source = new("before");
        using IDisposable binding = BindingOperations.BindOneWay(target, TextBlock.TextProperty, source);
        root.ProcessFrame();

        source.Value = "after";
        FrameStats stats = root.ProcessFrame();

        Assert.Equal("after", target.Text);
        Assert.True(stats.MeasuredElements > 0);
        Assert.True(stats.RenderedElements > 0);
    }

    [Fact]
    public void SourceChangeAfterDisposeDoesNotUpdateTarget()
    {
        ObservableValue<string> source = new("one");
        TextBlock target = new();
        IDisposable binding = BindingOperations.BindOneWay(target, TextBlock.TextProperty, source);

        binding.Dispose();
        source.Value = "two";

        Assert.Equal("one", target.Text);
    }

    [Fact]
    public void FailedInitialTargetWriteUnsubscribesSource()
    {
        ObservableValue<int> source = new(-1);

        Assert.Throws<ArgumentException>(() => BindingOperations.BindOneWay(new ValidatingElement(), ValidatingElement.PositiveProperty, source));

        source.Value = 2;
    }

    [Fact]
    public void BindingOperationsRejectsReadOnlyTargetProperty()
    {
        ObservableValue<int> source = new(1);

        Assert.Throws<InvalidOperationException>(() => BindingOperations.BindOneWay(new ReadOnlyElement(), ReadOnlyElement.ReadOnlyCountProperty, source));
    }

    [Fact]
    public void ElementOwnedBindingDisposesOnDetachFromRoot()
    {
        UIRoot root = new();
        TextBlock target = new();
        root.VisualChildren.Add(target);
        ObservableValue<string> source = new("one");
        target.Bindings.Add(BindingOperations.BindOneWay(target, TextBlock.TextProperty, source));

        root.VisualChildren.Remove(target);
        source.Value = "two";

        Assert.Equal("one", target.Text);
    }

    [Fact]
    public void ElementOwnedBindingSurvivesUnchangedFramesWithoutExtraWork()
    {
        UIRoot root = new(200, 80);
        TextBlock target = new();
        root.VisualChildren.Add(target);
        ObservableValue<string> source = new("one");
        target.Bindings.Add(BindingOperations.BindOneWay(target, TextBlock.TextProperty, source));

        root.ProcessFrame();
        FrameStats unchanged = root.ProcessFrame();

        Assert.Equal("one", target.Text);
        Assert.Equal(1, unchanged.NoWorkFrames);
    }

    [Fact]
    public void ReplacingElementOwnedBindingDisposesPreviousSubscription()
    {
        TextBlock target = new();
        ObservableValue<string> first = new("first");
        ObservableValue<string> second = new("second");
        IDisposable firstBinding = BindingOperations.BindOneWay(target, TextBlock.TextProperty, first);
        target.Bindings.Add(firstBinding);

        target.Bindings.Remove(firstBinding);
        target.Bindings.Add(BindingOperations.BindOneWay(target, TextBlock.TextProperty, second));
        first.Value = "stale";
        second.Value = "fresh";

        Assert.Equal("fresh", target.Text);
    }

    private sealed class ValidatingElement : UIElement
    {
        public static readonly UiProperty<int> PositiveProperty = UiProperty<int>.Register(
            nameof(Positive),
            typeof(ValidatingElement),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.None, validateValue: value => value >= 0));

        public int Positive
        {
            get => GetValue(PositiveProperty);
            set => SetValue(PositiveProperty, value);
        }
    }

    private sealed class ReadOnlyElement : UIElement
    {
        private static readonly UiPropertyKey<int> ReadOnlyCountKey = UiProperty<int>.RegisterReadOnly(
            "ReadOnlyCount",
            typeof(ReadOnlyElement),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.None));

        public static readonly UiProperty<int> ReadOnlyCountProperty = ReadOnlyCountKey.Property;
    }
}
