using System.ComponentModel;
using System.Globalization;
using System.Threading;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Core;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.UI.Markup;

public sealed class GeneratedMarkupBindingTests
{
    [Fact]
    public void OneWayUsesMarkupBaseAndKeepsCurrentValueBehindConditionalMask()
    {
        StringEndpoint source = new() { Value = "one" };
        TextBlock target = new();
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, StringEndpoint.ValueProperty);

        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => "projected:" + (string?)value,
            "$source.Value");

        Assert.Equal("projected:one", target.Text);
        Assert.Equal(UiPropertyValueSource.MarkupBase, target.GetValueSource(TextBlock.TextProperty));
        source.Value = "two";
        Assert.Equal("projected:two", target.Text);

        target.SetValue(TextBlock.TextProperty, "conditional", UiPropertyValueSource.MarkupConditional);
        source.Value = "three";
        Assert.Equal("conditional", target.Text);
        target.ClearValue(TextBlock.TextProperty, UiPropertyValueSource.MarkupConditional);
        Assert.Equal("projected:three", target.Text);

        binding.Dispose();
        Assert.Equal(string.Empty, target.Text);
        source.Value = "ignored";
        Assert.Equal(string.Empty, target.Text);
    }

    [Fact]
    public void OneWayStringProjectionUsesCurrentCultureAndHandlesEnumObjectAndNull()
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            ObjectEndpoint source = new() { Value = 12.5m };
            TextBlock target = new();
            MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, ObjectEndpoint.ValueProperty);
            using Binding binding = GeneratedMarkup.AttachPropertyBinding(
                target,
                target,
                TextBlock.TextProperty,
                observation,
                BindingMode.OneWay,
                GeneratedMarkup.FormatStringValue,
                "$source.Value");

            Assert.Equal(Convert.ToString(12.5m, CultureInfo.CurrentCulture), target.Text);
            source.Value = DayOfWeek.Monday;
            Assert.Equal("Monday", target.Text);
            source.Value = new CustomTextValue();
            Assert.Equal("custom text", target.Text);
            source.Value = null;
            Assert.Equal(string.Empty, target.Text);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void InterpolationRefreshesEverySourceAndConditionalReactivationReadsCurrentValues()
    {
        ObjectEndpoint first = new() { Value = "A" };
        ObjectEndpoint second = new() { Value = 2 };
        MarkupObservation firstObservation = GeneratedMarkup.ObserveProperty(first, ObjectEndpoint.ValueProperty);
        MarkupObservation secondObservation = GeneratedMarkup.ObserveProperty(second, ObjectEndpoint.ValueProperty);
        TextBlock direct = new();
        int composeCount = 0;
        using Binding directBinding = GeneratedMarkup.AttachInterpolatedStringBinding(
            direct,
            direct,
            TextBlock.TextProperty,
            [firstObservation, secondObservation, firstObservation],
            () =>
            {
                composeCount++;
                return $"{GeneratedMarkup.FormatStringValue(firstObservation.Value)}:{GeneratedMarkup.FormatStringValue(secondObservation.Value)}";
            },
            "direct interpolation");

        Assert.Equal("A:2", direct.Text);
        composeCount = 0;
        first.Value = "B";
        Assert.Equal(1, composeCount);
        second.Value = null;
        Assert.Equal("B:", direct.Text);

        ObjectEndpoint conditionalFirst = new() { Value = "one" };
        ObjectEndpoint conditionalSecond = new() { Value = 1 };
        MarkupObservation conditionalFirstObservation = GeneratedMarkup.ObserveProperty(
            conditionalFirst,
            ObjectEndpoint.ValueProperty);
        MarkupObservation conditionalSecondObservation = GeneratedMarkup.ObserveProperty(
            conditionalSecond,
            ObjectEndpoint.ValueProperty);
        UIElement condition = new() { IsEnabled = false };
        MarkupObservation conditionObservation = GeneratedMarkup.ObserveProperty(condition, UIElement.IsEnabledProperty);
        TextBlock target = new();
        target.SetValue(TextBlock.TextProperty, "base", UiPropertyValueSource.MarkupBase);
        MarkupConditionalValue interpolated = GeneratedMarkup.CreateConditionalInterpolatedStringBinding(
            target,
            TextBlock.TextProperty,
            [conditionalFirstObservation, conditionalSecondObservation],
            () => $"{GeneratedMarkup.FormatStringValue(conditionalFirstObservation.Value)}-" +
                GeneratedMarkup.FormatStringValue(conditionalSecondObservation.Value),
            "conditional interpolation");
        MarkupConditionRule rule = new(
            0,
            () => (bool)conditionObservation.Value!,
            [interpolated]);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(target, [conditionObservation], [rule]);

        Assert.Equal("base", target.Text);
        conditionalFirst.Value = "two";
        conditionalSecond.Value = 2;
        Assert.Equal("base", target.Text);
        condition.IsEnabled = true;
        Assert.Equal("two-2", target.Text);
        conditionalSecond.Value = 3;
        Assert.Equal("two-3", target.Text);
        condition.IsEnabled = false;
        Assert.Equal("base", target.Text);
        conditionalFirst.Value = "three";
        conditionalSecond.Value = 4;
        condition.IsEnabled = true;
        Assert.Equal("three-4", target.Text);
    }

    [Fact]
    public void TwoWayWritesOnlyLocalChangesAndNormalizesTargetBackToMarkupBase()
    {
        StringEndpoint source = new() { Value = "initial" };
        TextBox target = new();
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, StringEndpoint.ValueProperty);
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBox.TextProperty,
            observation,
            BindingMode.TwoWay,
            value => (string)value!,
            "$source.Value:TwoWay");
        int sourceChanges = 0;
        source.PropertyChanged += (_, args) =>
        {
            if (ReferenceEquals(args.Property, StringEndpoint.ValueProperty))
            {
                sourceChanges++;
            }
        };

        Assert.Equal("initial", target.Text);
        target.Text = "local";
        Assert.Equal("local", source.Value);
        Assert.Equal(1, sourceChanges);
        Assert.Equal("local", target.Text);
        Assert.Equal(UiPropertyValueSource.MarkupBase, target.GetValueSource(TextBox.TextProperty));

        source.Value = "source";
        Assert.Equal("source", target.Text);
        target.SetValue(TextBox.TextProperty, "conditional", UiPropertyValueSource.MarkupConditional);
        Assert.Equal("source", source.Value);
        source.Value = "behind conditional";
        Assert.Equal("conditional", target.Text);
        target.ClearValue(TextBox.TextProperty, UiPropertyValueSource.MarkupConditional);
        Assert.Equal("behind conditional", target.Text);

        target.SetValue(TextBox.TextProperty, "animated", UiPropertyValueSource.Animation);
        Assert.Equal("behind conditional", source.Value);
        target.ClearValue(TextBox.TextProperty, UiPropertyValueSource.Animation);
        Assert.Equal("behind conditional", target.Text);
        target.SetValue(TextBox.TextProperty, "aspect", UiPropertyValueSource.AspectVisualState);
        Assert.Equal("behind conditional", source.Value);
        target.ClearValue(TextBox.TextProperty, UiPropertyValueSource.AspectVisualState);
        Assert.Equal("behind conditional", target.Text);
    }

    [Fact]
    public void TwoWayUnavailablePathIgnoresWriteAndRefreshesWhenTerminalReturns()
    {
        BindingRoot source = new(null);
        TextBox target = new();
        target.DataContext = source;
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Child", owner => ((BindingRoot)owner!).Child),
            new MarkupDataPathSegment(
                "Name",
                owner => ((BindingChild)owner!).Name,
                (owner, value) => ((BindingChild)owner!).Name = (string)value!));
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBox.TextProperty,
            observation,
            BindingMode.TwoWay,
            value => (string)value!,
            "$DataContext.Child.Name:TwoWay");

        Assert.Equal(string.Empty, target.Text);
        target.Text = "ignored";
        Assert.Null(source.Child);
        Assert.Equal(string.Empty, target.Text);
        Assert.Equal(UiPropertyValueSource.Default, target.GetValueSource(TextBox.TextProperty));

        source.Child = new BindingChild("ready");
        Assert.Equal("ready", target.Text);
        target.Text = "written";
        Assert.Equal("written", source.Child.Name);
        Assert.Equal(UiPropertyValueSource.MarkupBase, target.GetValueSource(TextBox.TextProperty));

        source.Child = null;
        Assert.Equal(string.Empty, target.Text);
        source.Child = new BindingChild("restored");
        Assert.Equal("restored", target.Text);
    }

    [Fact]
    public void ConditionalBranchesActivateOnlyWinnerAndGateTwoWayWrites()
    {
        StringEndpoint baseSource = new() { Value = "base" };
        StringEndpoint firstSource = new() { Value = "first" };
        StringEndpoint secondSource = new() { Value = "second" };
        TextBox target = new();
        MarkupObservation baseObservation = GeneratedMarkup.ObserveProperty(baseSource, StringEndpoint.ValueProperty);
        using Binding baseBinding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBox.TextProperty,
            baseObservation,
            BindingMode.OneWay,
            value => (string)value!,
            "base binding");

        UIElement firstCondition = new() { IsEnabled = true };
        UIElement secondCondition = new() { IsEnabled = false };
        MarkupObservation firstConditionObservation = GeneratedMarkup.ObserveProperty(
            firstCondition,
            UIElement.IsEnabledProperty);
        MarkupObservation secondConditionObservation = GeneratedMarkup.ObserveProperty(
            secondCondition,
            UIElement.IsEnabledProperty);
        MarkupObservation firstSourceObservation = GeneratedMarkup.ObserveProperty(firstSource, StringEndpoint.ValueProperty);
        MarkupObservation secondSourceObservation = GeneratedMarkup.ObserveProperty(secondSource, StringEndpoint.ValueProperty);
        MarkupConditionalValue firstProvider = GeneratedMarkup.CreateConditionalPropertyBinding(
            target,
            TextBox.TextProperty,
            firstSourceObservation,
            BindingMode.TwoWay,
            value => (string)value!,
            "first provider");
        MarkupConditionalValue secondProvider = GeneratedMarkup.CreateConditionalPropertyBinding(
            target,
            TextBox.TextProperty,
            secondSourceObservation,
            BindingMode.TwoWay,
            value => (string)value!,
            "second provider");
        MarkupConditionRule firstRule = new(
            0,
            () => (bool)firstConditionObservation.Value!,
            [firstProvider]);
        MarkupConditionRule secondRule = new(
            1,
            () => (bool)secondConditionObservation.Value!,
            [secondProvider]);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(
            target,
            [firstConditionObservation, secondConditionObservation],
            [firstRule, secondRule]);

        Assert.Equal("first", target.Text);
        baseSource.Value = "base current";
        Assert.Equal("first", target.Text);
        secondCondition.IsEnabled = true;
        Assert.Equal("second", target.Text);
        firstSource.Value = "first inactive";
        Assert.Equal("second", target.Text);
        target.Text = "write second";
        Assert.Equal("write second", secondSource.Value);
        Assert.Equal("first inactive", firstSource.Value);

        secondCondition.IsEnabled = false;
        Assert.Equal("first inactive", target.Text);
        secondSource.Value = "second inactive";
        Assert.Equal("first inactive", target.Text);
        target.Text = "write first";
        Assert.Equal("write first", firstSource.Value);
        Assert.Equal("second inactive", secondSource.Value);

        firstCondition.IsEnabled = false;
        Assert.Equal("base current", target.Text);
        target.Text = "local without provider";
        Assert.Equal("write first", firstSource.Value);
        Assert.Equal("second inactive", secondSource.Value);
    }

    [Fact]
    public void ActiveConditionalBindingFallsBackToMarkupBaseWhilePathIsUnavailable()
    {
        StringEndpoint baseSource = new() { Value = "base" };
        TextBlock target = new();
        MarkupObservation baseObservation = GeneratedMarkup.ObserveProperty(baseSource, StringEndpoint.ValueProperty);
        using Binding baseBinding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            baseObservation,
            BindingMode.OneWay,
            value => (string)value!,
            "base binding");

        BindingRoot source = new(new BindingChild("conditional"));
        target.DataContext = source;
        MarkupObservation valueObservation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Child", owner => ((BindingRoot)owner!).Child),
            new MarkupDataPathSegment("Name", owner => ((BindingChild)owner!).Name));
        UIElement condition = new() { IsEnabled = true };
        MarkupObservation conditionObservation = GeneratedMarkup.ObserveProperty(condition, UIElement.IsEnabledProperty);
        MarkupConditionalValue provider = GeneratedMarkup.CreateConditionalPropertyBinding(
            target,
            TextBlock.TextProperty,
            valueObservation,
            BindingMode.OneWay,
            value => (string)value!,
            "conditional data path");
        MarkupConditionRule rule = new(0, () => (bool)conditionObservation.Value!, [provider]);
        using IDisposable conditions = GeneratedMarkup.AttachConditions(target, [conditionObservation], [rule]);

        Assert.Equal("conditional", target.Text);
        source.Child = null;
        Assert.Equal("base", target.Text);
        Assert.Equal(UiPropertyValueSource.MarkupBase, target.GetValueSource(TextBlock.TextProperty));
        baseSource.Value = "base current";
        Assert.Equal("base current", target.Text);
        source.Child = new BindingChild("restored conditional");
        Assert.Equal("restored conditional", target.Text);
        condition.IsEnabled = false;
        Assert.Equal("base current", target.Text);
    }

    [Fact]
    public void BindingStopsOnDetachRefreshesOnReattachAndDisposesIdempotently()
    {
        StringEndpoint source = new() { Value = "one" };
        TextBlock target = new();
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, StringEndpoint.ValueProperty);
        Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "lifecycle binding");
        UIRoot root = new();
        root.VisualChildren.Add(target);
        Assert.Equal("one", target.Text);

        root.VisualChildren.Remove(target);
        source.Value = "detached";
        Assert.Equal("one", target.Text);
        root.VisualChildren.Add(target);
        Assert.Equal("detached", target.Text);
        IElementLifecycleBehavior lifecycle = Assert.IsAssignableFrom<IElementLifecycleBehavior>(binding);
        lifecycle.Attach();
        lifecycle.Attach();
        source.Value = "reattached";
        Assert.Equal("reattached", target.Text);

        binding.Dispose();
        binding.Dispose();
        Assert.Equal(string.Empty, target.Text);
        source.Value = "disposed";
        Assert.Equal(string.Empty, target.Text);
        root.VisualChildren.Remove(target);
        root.VisualChildren.Add(target);
        Assert.Equal(string.Empty, target.Text);
    }

    [Fact]
    public void BindingReconnectsTemplatePartAndStopsAfterDisposal()
    {
        StringEndpoint first = new() { Value = "first" };
        Button owner = new() { ComponentTemplate = PartTemplate("first", first) };
        TextBlock target = new();
        MarkupObservation observation = GeneratedMarkup.ObserveTemplatePartProperty(
            owner,
            "Part",
            StringEndpoint.ValueProperty);
        Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "$owner.parts.$Part.Value");

        Assert.Equal("first", target.Text);
        StringEndpoint second = new() { Value = "second" };
        owner.ComponentTemplate = PartTemplate("second", second);
        Assert.Equal("second", target.Text);
        first.Value = "stale";
        Assert.Equal("second", target.Text);
        second.Value = "current";
        Assert.Equal("current", target.Text);

        binding.Dispose();
        Assert.Equal(string.Empty, target.Text);
        second.Value = "ignored";
        Assert.Equal(string.Empty, target.Text);
    }

    [Fact]
    public void OffThreadSourceNotificationFailsBeforeGetterOrTargetAccess()
    {
        ThreadedSource source = new();
        TextBlock target = new() { DataContext = source };
        MarkupObservation observation = GeneratedMarkup.ObserveDataPath(
            target,
            new MarkupDataPathSegment("Value", owner => ((ThreadedSource)owner!).Value));
        using Binding binding = GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            TextBlock.TextProperty,
            observation,
            BindingMode.OneWay,
            value => (string)value!,
            "$DataContext.Value");
        int uiThreadId = Environment.CurrentManagedThreadId;
        source.ResetReads();

        Exception failure = Assert.IsType<InvalidOperationException>(source.RaiseFromWorker());

        Assert.Equal(0, source.ReadCount);
        Assert.Equal("initial", target.Text);
        Assert.Contains("$DataContext.Value", failure.Message, StringComparison.Ordinal);
        Assert.Contains(uiThreadId.ToString(CultureInfo.InvariantCulture), failure.Message, StringComparison.Ordinal);
        Assert.Contains(source.WorkerThreadId.ToString(CultureInfo.InvariantCulture), failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BindingRejectsReadOnlyTargetAndReadOnlyTwoWaySource()
    {
        StringEndpoint source = new() { Value = "value" };
        MarkupObservation writable = GeneratedMarkup.ObserveProperty(source, StringEndpoint.ValueProperty);
        ReadOnlyEndpoint target = new();
        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.AttachPropertyBinding(
            target,
            target,
            ReadOnlyEndpoint.ValueProperty,
            writable,
            BindingMode.OneWay,
            value => (string)value!,
            "read-only target"));

        MarkupObservation readOnlySource = GeneratedMarkup.ObserveProperty(target, ReadOnlyEndpoint.ValueProperty);
        TextBox writableTarget = new();
        Assert.Throws<InvalidOperationException>(() => GeneratedMarkup.AttachPropertyBinding(
            writableTarget,
            writableTarget,
            TextBox.TextProperty,
            readOnlySource,
            BindingMode.TwoWay,
            value => (string)value!,
            "read-only source"));
    }

    private static ComponentTemplate<Button> PartTemplate(string name, StringEndpoint part)
    {
        return new ComponentTemplate<Button>(name, context => context.RequirePart("Part", part));
    }

    private sealed class StringEndpoint : UIElement
    {
        public static readonly UiProperty<string> ValueProperty = UiProperty<string>.Register(
            nameof(Value),
            typeof(StringEndpoint),
            new UiPropertyMetadata<string>(string.Empty));

        public string Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }

    private sealed class ObjectEndpoint : UIElement
    {
        public static readonly UiProperty<object?> ValueProperty = UiProperty<object?>.Register(
            nameof(Value),
            typeof(ObjectEndpoint),
            new UiPropertyMetadata<object?>(null));

        public object? Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
    }

    private sealed class ReadOnlyEndpoint : UIElement
    {
        private static readonly UiPropertyKey<string> ValueKey = UiProperty<string>.RegisterReadOnly(
            nameof(Value),
            typeof(ReadOnlyEndpoint),
            new UiPropertyMetadata<string>("read-only"));

        public static UiProperty<string> ValueProperty => ValueKey.Property;

        public string Value => GetValue(ValueProperty);
    }

    private sealed class BindingRoot : INotifyPropertyChanged
    {
        private BindingChild? child;

        public BindingRoot(BindingChild? child)
        {
            this.child = child;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public BindingChild? Child
        {
            get => child;
            set
            {
                child = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Child)));
            }
        }
    }

    private sealed class BindingChild : INotifyPropertyChanged
    {
        private string name;

        public BindingChild(string name)
        {
            this.name = name;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => name;
            set
            {
                name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
    }

    private sealed class ThreadedSource : INotifyPropertyChanged
    {
        private int readCount;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Value
        {
            get
            {
                readCount++;
                return "initial";
            }
        }

        public int ReadCount => readCount;

        public int WorkerThreadId { get; private set; }

        public void ResetReads()
        {
            readCount = 0;
        }

        public Exception? RaiseFromWorker()
        {
            Exception? failure = null;
            using ManualResetEventSlim complete = new(false);
            Thread worker = new(() =>
            {
                WorkerThreadId = Environment.CurrentManagedThreadId;
                try
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
                catch (Exception error)
                {
                    failure = error;
                }
                finally
                {
                    complete.Set();
                }
            });
            worker.Start();
            complete.Wait();
            worker.Join();
            return failure;
        }
    }

    private sealed class CustomTextValue
    {
        public override string ToString()
        {
            return "custom text";
        }
    }
}
