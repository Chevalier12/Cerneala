using System.ComponentModel;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Markup;

public sealed class GeneratedMarkupObservationTests
{
    [Fact]
    public void DataPathSingleSegmentReadsWritesAndRebindsRoot()
    {
        EndpointChild firstChild = new("first");
        EndpointRoot firstRoot = new(firstChild);
        UIElement target = new() { DataContext = firstRoot };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment(
                "Child",
                owner => ((EndpointRoot)owner!).Child,
                (owner, value) => ((EndpointRoot)owner!).Child = (EndpointChild?)value));

        observation.Start();
        Assert.True(observation.IsResolved);
        Assert.Same(firstChild, observation.Value);
        Assert.True(observation.CanWrite);

        EndpointChild written = new("written");
        Assert.True(observation.TryWrite(written));
        Assert.Same(written, firstRoot.Child);
        Assert.Same(written, observation.Value);

        EndpointRoot secondRoot = new(new EndpointChild("second"));
        target.DataContext = secondRoot;
        Assert.Same(secondRoot.Child, observation.Value);
        Assert.Equal(0, firstRoot.SubscriberCount);
        Assert.Equal(1, secondRoot.SubscriberCount);

        observation.Stop();
        Assert.Equal(0, secondRoot.SubscriberCount);
    }

    [Fact]
    public void DataPathDistinguishesTerminalNullFromMissingIntermediateAndReconnectsSetter()
    {
        EndpointChild firstChild = new(null);
        EndpointRoot firstRoot = new(firstChild);
        StackPanel parent = new() { DataContext = firstRoot };
        UIElement target = new();
        parent.LogicalChildren.Add(target);
        parent.VisualChildren.Add(target);
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Child", owner => ((EndpointRoot)owner!).Child),
            new MarkupDataPathSegment(
                "Name",
                owner => ((EndpointChild)owner!).Name,
                (owner, value) => ((EndpointChild)owner!).Name = (string?)value));

        Assert.Equal(0, firstRoot.SubscriberCount);
        Assert.Equal(0, firstChild.SubscriberCount);
        observation.Start();

        Assert.Equal(1, firstRoot.SubscriberCount);
        Assert.Equal(1, firstChild.SubscriberCount);
        Assert.True(observation.IsResolved);
        Assert.Null(observation.Value);
        Assert.True(observation.CanWrite);
        Assert.True(observation.TryWrite("written"));
        Assert.Equal("written", firstChild.Name);
        Assert.Equal("written", observation.Value);

        firstRoot.Child = null;
        Assert.False(observation.IsResolved);
        Assert.Null(observation.Value);
        Assert.False(observation.CanWrite);
        Assert.False(observation.TryWrite("ignored"));
        Assert.Equal(0, firstChild.SubscriberCount);

        EndpointChild replacement = new("replacement");
        firstRoot.Child = replacement;
        Assert.Equal(1, replacement.SubscriberCount);
        Assert.True(observation.IsResolved);
        Assert.Equal("replacement", observation.Value);
        Assert.True(observation.TryWrite(null));
        Assert.True(observation.IsResolved);
        Assert.Null(observation.Value);

        firstChild.Name = "stale";
        Assert.Null(observation.Value);

        EndpointRoot secondRoot = new(new EndpointChild("second"));
        parent.DataContext = secondRoot;
        Assert.Equal(0, firstRoot.SubscriberCount);
        Assert.Equal(0, replacement.SubscriberCount);
        Assert.Equal(1, secondRoot.SubscriberCount);
        Assert.Equal(1, secondRoot.Child!.SubscriberCount);
        Assert.True(observation.IsResolved);
        Assert.Equal("second", observation.Value);
        firstRoot.Child = new EndpointChild("detached");
        Assert.Equal("second", observation.Value);

        observation.Stop();
        Assert.Equal(0, secondRoot.SubscriberCount);
        Assert.Equal(0, secondRoot.Child!.SubscriberCount);
        secondRoot.Child!.Name = "stopped";
        Assert.Equal("second", observation.Value);
    }

    [Fact]
    public void UiPropertyEndpointWritesOnlyWritablePropertiesAndTracksChanges()
    {
        EndpointElement source = new() { Value = "initial" };
        MarkupObservation writable = GeneratedMarkup.ObserveProperty(source, EndpointElement.ValueProperty);
        MarkupObservation readOnly = GeneratedMarkup.ObserveProperty(source, EndpointElement.ReadOnlyValueProperty);
        writable.Start();
        readOnly.Start();

        Assert.True(writable.IsResolved);
        Assert.True(writable.CanWrite);
        Assert.True(writable.TryWrite("written"));
        Assert.Equal("written", source.Value);
        Assert.Equal("written", writable.Value);

        Assert.True(readOnly.IsResolved);
        Assert.False(readOnly.CanWrite);
        Assert.False(readOnly.TryWrite("blocked"));
        Assert.Equal("read-only", source.ReadOnlyValue);
        source.SetReadOnlyValue("changed internally");
        Assert.Equal("changed internally", readOnly.Value);
    }

    [Fact]
    public void TemplatePartEndpointReconnectsAcrossPresentMissingAndReplacementParts()
    {
        EndpointElement first = new() { Value = "first" };
        Button owner = new() { ComponentTemplate = PartTemplate("first-template", first) };
        owner.ApplyTemplate();
        MarkupObservation writable = GeneratedMarkup.ObserveTemplatePartProperty(
            owner,
            "Part",
            EndpointElement.ValueProperty);
        MarkupObservation readOnly = GeneratedMarkup.ObserveTemplatePartProperty(
            owner,
            "Part",
            EndpointElement.ReadOnlyValueProperty);
        writable.Start();
        readOnly.Start();

        Assert.True(writable.IsResolved);
        Assert.Equal("first", writable.Value);
        Assert.True(writable.CanWrite);
        Assert.True(writable.TryWrite("written"));
        Assert.Equal("written", first.Value);
        Assert.False(readOnly.CanWrite);
        Assert.False(readOnly.TryWrite("blocked"));

        EndpointElement second = new() { Value = "second" };
        owner.ComponentTemplate = PartTemplate("second-template", second);
        Assert.True(writable.IsResolved);
        Assert.Equal("second", writable.Value);
        first.Value = "stale";
        Assert.Equal("second", writable.Value);
        second.Value = "current";
        Assert.Equal("current", writable.Value);

        owner.ComponentTemplate = new ComponentTemplate<Button>("missing", _ => new Border());
        Assert.False(writable.IsResolved);
        Assert.False(writable.CanWrite);
        Assert.False(writable.TryWrite("ignored"));

        EndpointElement third = new() { Value = "third" };
        owner.ComponentTemplate = PartTemplate("third-template", third);
        Assert.True(writable.IsResolved);
        Assert.Equal("third", writable.Value);
        Assert.True(writable.TryWrite("final"));
        Assert.Equal("final", third.Value);
    }

    private static ComponentTemplate<Button> PartTemplate(string name, EndpointElement part)
    {
        return new ComponentTemplate<Button>(name, context => context.RequirePart("Part", part));
    }

    private sealed class EndpointRoot : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? propertyChanged;
        private EndpointChild? child;

        public EndpointRoot(EndpointChild? child)
        {
            this.child = child;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                propertyChanged += value;
                SubscriberCount++;
            }
            remove
            {
                propertyChanged -= value;
                SubscriberCount--;
            }
        }

        public int SubscriberCount { get; private set; }

        public EndpointChild? Child
        {
            get => child;
            set
            {
                child = value;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
            }
        }
    }

    private sealed class EndpointChild : INotifyPropertyChanged
    {
        private PropertyChangedEventHandler? propertyChanged;
        private string? name;

        public EndpointChild(string? name)
        {
            this.name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add
            {
                propertyChanged += value;
                SubscriberCount++;
            }
            remove
            {
                propertyChanged -= value;
                SubscriberCount--;
            }
        }

        public int SubscriberCount { get; private set; }

        public string? Name
        {
            get => name;
            set
            {
                name = value;
                propertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    private sealed class EndpointElement : UIElement
    {
        private static readonly UiPropertyKey<string> ReadOnlyValueKey = UiProperty<string>.RegisterReadOnly(
            nameof(ReadOnlyValue),
            typeof(EndpointElement),
            new UiPropertyMetadata<string>("read-only"));

        public static readonly UiProperty<string> ValueProperty = UiProperty<string>.Register(
            nameof(Value),
            typeof(EndpointElement),
            new UiPropertyMetadata<string>(string.Empty));

        public static readonly UiProperty<string> ReadOnlyValueProperty = ReadOnlyValueKey.Property;

        public string Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public string ReadOnlyValue => GetValue(ReadOnlyValueProperty);

        public void SetReadOnlyValue(string value)
        {
            SetValue(ReadOnlyValueKey, value);
        }
    }
}
