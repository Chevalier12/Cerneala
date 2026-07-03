using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

namespace Cerneala.Tests.UI.Data;

public sealed class TypedBindingTests
{
    [Fact]
    public void PropertyAdapterReadsAndWritesTypedValues()
    {
        Model model = new() { Count = 3 };
        PropertyAdapter<Model, int> adapter = new(owner => owner.Count, (owner, value) => owner.Count = value);

        adapter.Write(model, 7);

        Assert.Equal(7, adapter.Read(model));
    }

    [Fact]
    public void PropertyAdapterWritesRetainedUiPropertyThroughExistingInvalidation()
    {
        TestElement element = new();
        PropertyAdapter<TestElement, int> adapter = PropertyAdapter<TestElement, int>.ForUiProperty<TestElement>(TestElement.CountProperty);

        adapter.Write(element, 4);

        Assert.Equal(4, adapter.Read(element));
        Assert.True(element.Invalidated);
    }

    [Fact]
    public void OneWayBindingUpdatesTargetFromSource()
    {
        ObservableValue<int> source = new(1);
        int target = 0;
        using Binding<int> binding = Binding.OneWay(source, value => target = value);

        source.Value = 9;

        Assert.Equal(9, target);
        Assert.False(binding.IsDisposed);
    }

    [Fact]
    public void BindingDisposalStopsUpdates()
    {
        ObservableValue<int> source = new(1);
        int target = 0;
        Binding<int> binding = Binding.OneWay(source, value => target = value);

        binding.Dispose();
        source.Value = 2;

        Assert.Equal(1, target);
        Assert.True(binding.IsDisposed);
    }

    [Fact]
    public void FailedImmediateUpdateDoesNotLeaveSourceSubscribed()
    {
        ObservableValue<int> source = new(1);
        int targetCalls = 0;

        Assert.Throws<InvalidOperationException>(() =>
            Binding.OneWay(
                source,
                _ =>
                {
                    targetCalls++;
                    throw new InvalidOperationException("Target rejected value.");
                }));

        source.Value = 2;

        Assert.Equal(1, targetCalls);
    }

    [Fact]
    public void TwoWayBindingCommitsTargetValueToSource()
    {
        ObservableValue<int> source = new(1);
        using Binding<int> binding = Binding.TwoWay(source, _ => { });

        binding.CommitTargetValue(5);

        Assert.Equal(5, source.Value);
    }

    [Fact]
    public void OneWayBindingUsesTypedConverter()
    {
        ObservableValue<int> source = new(2);
        string target = "";
        using Binding<int> binding = Binding<int>.OneWayConverted(source, value => target = value, new IntToStringConverter());

        source.Value = 12;

        Assert.Equal("12", target);
    }

    private sealed class Model
    {
        public int Count { get; set; }
    }

    private sealed class TestElement : UIElement
    {
        public static readonly UiProperty<int> CountProperty = UiProperty<int>.Register(
            "Count",
            typeof(TestElement),
            new UiPropertyMetadata<int>(0, UiPropertyOptions.AffectsRender));

        public bool Invalidated { get; private set; }

        public override void Invalidate(InvalidationRequest request)
        {
            Invalidated = true;
            base.Invalidate(request);
        }
    }

    private sealed class IntToStringConverter : IValueConverter<int, string>
    {
        public string Convert(int value)
        {
            return value.ToString();
        }

        public int ConvertBack(string value)
        {
            return int.Parse(value);
        }
    }
}
