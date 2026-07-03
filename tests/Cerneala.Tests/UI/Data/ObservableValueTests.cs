using Cerneala.UI.Data;

namespace Cerneala.Tests.UI.Data;

public sealed class ObservableValueTests
{
    [Fact]
    public void ValueChangesNotifyWithOldAndNewValues()
    {
        ObservableValue<int> value = new(1);
        ObservableValueChangedEventArgs<int>? observed = null;
        value.ValueChanged += (_, args) => observed = args;

        int oldValue = value.SetValue(2);

        Assert.Equal(1, oldValue);
        Assert.Equal(2, value.Value);
        Assert.NotNull(observed);
        Assert.Equal(1, observed.OldValue);
        Assert.Equal(2, observed.NewValue);
    }

    [Fact]
    public void EqualValuesDoNotNotify()
    {
        ObservableValue<string> value = new("same");
        int notifications = 0;
        value.ValueChanged += (_, _) => notifications++;

        value.Value = "same";

        Assert.Equal(0, notifications);
    }

    [Fact]
    public void NullDefaultAndNonNullTransitionsNotifyWithOldAndNewValues()
    {
        ObservableValue<string?> value = new();
        List<ObservableValueChangedEventArgs<string?>> observed = [];
        value.ValueChanged += (_, args) => observed.Add(args);

        value.Value = "set";
        value.Value = null;

        Assert.Equal(2, observed.Count);
        Assert.Null(observed[0].OldValue);
        Assert.Equal("set", observed[0].NewValue);
        Assert.Equal("set", observed[1].OldValue);
        Assert.Null(observed[1].NewValue);
        Assert.Null(value.Value);
    }

    [Fact]
    public void CustomComparerControlsEqualityAndSuppressedAssignments()
    {
        ObservableValue<string> value = new("alpha", StringComparer.OrdinalIgnoreCase);
        int notifications = 0;
        value.ValueChanged += (_, _) => notifications++;

        value.Value = "ALPHA";

        Assert.Equal(0, notifications);
        Assert.Equal("alpha", value.Value);

        value.Value = "beta";

        Assert.Equal(1, notifications);
        Assert.Equal("beta", value.Value);
    }

    [Fact]
    public void ValueIsUpdatedBeforeValueChangedObserversRun()
    {
        ObservableValue<int> value = new(1);
        int observedValueDuringNotification = 0;
        value.ValueChanged += (_, _) => observedValueDuringNotification = value.Value;

        value.Value = 2;

        Assert.Equal(2, observedValueDuringNotification);
    }

    [Fact]
    public void ObserverCanUnsubscribeDuringNotification()
    {
        ObservableValue<int> value = new(1);
        int firstObserverNotifications = 0;
        int secondObserverNotifications = 0;

        EventHandler<ObservableValueChangedEventArgs<int>>? firstObserver = null;
        firstObserver = (_, _) =>
        {
            firstObserverNotifications++;
            value.ValueChanged -= firstObserver;
        };

        value.ValueChanged += firstObserver;
        value.ValueChanged += (_, _) => secondObserverNotifications++;

        value.Value = 2;
        value.Value = 3;

        Assert.Equal(1, firstObserverNotifications);
        Assert.Equal(2, secondObserverNotifications);
    }
}
