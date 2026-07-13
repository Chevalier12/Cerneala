using System.Reflection;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Markup;

namespace Cerneala.Tests.Controls.Primitives;

public sealed class RepeatButtonTests
{
    [Fact]
    public void DefaultsMatchRepeatContract()
    {
        RepeatButton button = new();

        Assert.IsAssignableFrom<Button>(button);
        Assert.Equal(500, button.Delay);
        Assert.Equal(100, button.Interval);
    }

    [Fact]
    public void DelayAndIntervalAcceptValidValues()
    {
        RepeatButton button = new()
        {
            Delay = 0,
            Interval = 1
        };

        Assert.Equal(0, button.Delay);
        Assert.Equal(1, button.Interval);
    }

    [Fact]
    public void DelayRejectsNegativeValues()
    {
        RepeatButton button = new();

        Assert.Throws<ArgumentException>(() => button.Delay = -1);
        Assert.Equal(500, button.Delay);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void IntervalRejectsNonPositiveValues(int value)
    {
        RepeatButton button = new();

        Assert.Throws<ArgumentException>(() => button.Interval = value);
        Assert.Equal(100, button.Interval);
    }

    [Fact]
    public void DelayAndIntervalDeclareMarkupConstraints()
    {
        PropertyInfo delay = typeof(RepeatButton).GetProperty(nameof(RepeatButton.Delay))!;
        PropertyInfo interval = typeof(RepeatButton).GetProperty(nameof(RepeatButton.Interval))!;

        Assert.Equal(
            MarkupValueConstraint.NonNegative,
            delay.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
        Assert.Equal(
            MarkupValueConstraint.Positive,
            interval.GetCustomAttribute<MarkupValueConstraintAttribute>()?.Constraint);
    }

    [Fact]
    public void MarkupParsesIntegerDelayAndInterval()
    {
        MarkupResult<UiMarkupDocument> document = new UiMarkupReader().Read("<RepeatButton Delay=\"250\" Interval=\"40\" />");

        MarkupResult<UIElement> result = new UiFactory(UiMarkupSchema.CreateDefault()).Create(document.Value!);

        RepeatButton button = Assert.IsType<RepeatButton>(result.Value);
        Assert.False(result.HasErrors);
        Assert.Equal(250, button.Delay);
        Assert.Equal(40, button.Interval);
    }

    [Fact]
    public void ProgrammaticActivationRaisesOneClick()
    {
        RepeatButton button = new();
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;

        ((IInputActivatable)button).Activate();

        Assert.Equal(1, clickCount);
    }

    [Fact]
    public void PointerReleaseDoesNotAddAnotherClick()
    {
        UIRoot root = new(100, 100);
        RepeatButton button = new();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        ((IInputActivatable)button).Activate();
        bridge.Dispatch(root, PointerFrame(currentDown: true));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true));

        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void LeftPressActivatesAndExecutesCommandImmediately()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);

        Assert.Equal(1, clickCount);
        Assert.Equal(1, executionCount);
        Assert.True(button.IsPressed);

        bridge.Dispatch(root, PointerFrame(previousDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.Equal(1, executionCount);
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void RepeatsAfterDelayAndThenAtIntervalAtMostOncePerFrame()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(499));
        Assert.Equal(1, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(99));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(1));
        Assert.Equal(3, clickCount);
    }

    [Fact]
    public void LateFrameProducesOneRepeatAndSkipsMissedIntervals()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromSeconds(2));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.Zero);
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Interval));
        Assert.Equal(3, clickCount);
    }

    [Fact]
    public void PointerLeavingHitTargetCancelsRepeatSession()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(
            root,
            PointerFrame(previousDown: true, currentDown: true, previousX: 10, currentX: 70),
            TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);
    }

    [Fact]
    public void RoutedCommandUsesExistingCommandRouterForEveryActivation()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        RoutedCommand command = new("Repeat", typeof(RepeatButtonTests));
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = command;
        button.CommandBindings.Add(new CommandBinding(
            command,
            (_, args) =>
            {
                executionCount++;
                args.Handled = true;
            },
            (_, args) =>
            {
                args.CanExecute = true;
                args.Handled = true;
            }));
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(2, clickCount);
        Assert.Equal(2, executionCount);
    }

    [Fact]
    public void ZeroDelayDoesNotDuplicateInitialActivationInPressFrame()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        button.Delay = 0;
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.FromSeconds(1));
        Assert.Equal(1, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.Zero);

        Assert.Equal(2, clickCount);
    }

    [Fact]
    public void DelayChangeAffectsOnlyNextSession()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        button.Delay = 100;
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        button.Delay = 0;
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(99));
        Assert.Equal(1, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(1));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.Zero);

        Assert.Equal(4, clickCount);
    }

    [Fact]
    public void IntervalChangeAppliesToNextCalculatedDeadline()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        button.Delay = 10;
        button.Interval = 100;
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        button.Interval = 20;
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(10));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(19));
        Assert.Equal(2, clickCount);

        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(1));

        Assert.Equal(3, clickCount);
    }

    [Theory]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void NonVisibleButtonStopsRepeating(Visibility visibility)
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        button.Visibility = visibility;
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void DisabledButtonStopsRepeating()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        button.IsEnabled = false;
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
    }

    [Fact]
    public void DetachedButtonStopsRepeatingAndIsReleased()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        root.VisualChildren.Remove(button);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);
    }

    [Fact]
    public void ChangingRootCancelsOldRepeatSession()
    {
        UIRoot firstRoot = RootWithButton(out RepeatButton button);
        UIRoot secondRoot = new(100, 100);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(firstRoot, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(secondRoot, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(1, clickCount);
        Assert.False(button.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);
    }

    [Fact]
    public void CommandBecomingUnavailableSkipsExecutionButKeepsClickSequence()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        bool canExecute = true;
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = new ActionCommand(_ => executionCount++, _ => canExecute);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        canExecute = false;
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromMilliseconds(button.Delay));

        Assert.Equal(2, clickCount);
        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void ClickHandlerCanDetachButtonWithoutLeavingActiveSession()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) =>
        {
            clickCount++;
            root.VisualChildren.Remove(button);
        };
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, currentDown: true), TimeSpan.FromSeconds(1));

        Assert.Equal(1, clickCount);
        Assert.Equal(1, executionCount);
        Assert.False(button.IsPressed);
        Assert.Null(bridge.PressedStateTracker.PressedElement);
    }

    [Fact]
    public void PressingSecondRepeatButtonReplacesOldSession()
    {
        UIRoot root = new(100, 100);
        RepeatButton first = new();
        RepeatButton second = new();
        first.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        second.Arrange(new ArrangeContext(new LayoutRect(50, 0, 40, 40)));
        root.VisualChildren.Add(first);
        root.VisualChildren.Add(second);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(previousDown: true, previousX: 10, currentX: 60), TimeSpan.Zero);
        bridge.Dispatch(root, PointerFrame(currentDown: true, previousX: 60, currentX: 60), TimeSpan.Zero);
        bridge.Dispatch(
            root,
            PointerFrame(previousDown: true, currentDown: true, previousX: 60, currentX: 60),
            TimeSpan.FromMilliseconds(second.Delay));

        Assert.Equal(1, firstClicks);
        Assert.Equal(2, secondClicks);
    }

    [Fact]
    public void RightButtonAndWheelDoNotStartRepeating()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        int clickCount = 0;
        button.Click += (_, _) => clickCount++;
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, MouseButtonFrame(InputMouseButton.Right, currentDown: true), TimeSpan.Zero);
        bridge.Dispatch(
            root,
            MouseButtonFrame(InputMouseButton.Right, previousDown: true, currentDown: true),
            TimeSpan.FromSeconds(1));
        bridge.Dispatch(root, WheelFrame(), TimeSpan.FromSeconds(1));

        Assert.Equal(0, clickCount);
    }

    [Fact]
    public void KeyboardSpaceActivatesOnceOnReleaseWithoutRepeating()
    {
        UIRoot root = RootWithButton(out RepeatButton button);
        button.Delay = 0;
        int clickCount = 0;
        int executionCount = 0;
        button.Click += (_, _) => clickCount++;
        button.Command = new ActionCommand(_ => executionCount++);
        ElementInputBridge bridge = new();
        Assert.True(bridge.FocusManager.Focus(button, root.InputCache.EnsureCurrent(root)));

        bridge.Dispatch(root, KeyboardFrame(previousKeys: [], currentKeys: [InputKey.Space]), TimeSpan.FromSeconds(1));
        bridge.Dispatch(root, KeyboardFrame(previousKeys: [InputKey.Space], currentKeys: [InputKey.Space]), TimeSpan.FromSeconds(1));
        bridge.Dispatch(root, KeyboardFrame(previousKeys: [InputKey.Space], currentKeys: []), TimeSpan.FromSeconds(1));

        Assert.Equal(1, clickCount);
        Assert.Equal(1, executionCount);
        Assert.False(button.IsPressed);
    }

    private static UIRoot RootWithButton(out RepeatButton button)
    {
        UIRoot root = new(100, 100);
        button = new RepeatButton();
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 40)));
        root.VisualChildren.Add(button);
        return root;
    }

    private static InputFrame PointerFrame(
        bool previousDown = false,
        bool currentDown = false,
        float previousX = 10,
        float currentX = 10)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(previousX, 10);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(currentX, 10);
        if (previousDown)
        {
            previous = previous.WithButton(InputMouseButton.Left, true);
        }

        if (currentDown)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame MouseButtonFrame(
        InputMouseButton button,
        bool previousDown = false,
        bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(10, 10);
        if (previousDown)
        {
            previous = previous.WithButton(button, true);
        }

        if (currentDown)
        {
            current = current.WithButton(button, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame WheelFrame()
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(10, 10);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(10, 10).WithWheelValue(120);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }

    private static InputFrame KeyboardFrame(IEnumerable<InputKey> previousKeys, IEnumerable<InputKey> currentKeys)
    {
        return new InputFrame(
            PointerSnapshot.Empty,
            PointerSnapshot.Empty,
            KeyboardSnapshot.FromDownKeys(previousKeys),
            KeyboardSnapshot.FromDownKeys(currentKeys),
            []);
    }
}
