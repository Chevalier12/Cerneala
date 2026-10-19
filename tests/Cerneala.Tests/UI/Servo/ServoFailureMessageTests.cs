using System.Text.RegularExpressions;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoFailureMessageTests
{
    [Fact]
    public void TargetToStringDescribesEveryCriterionAndTheScope()
    {
        Assert.Equal("Id=\"save\"", ServoTarget.ById("save").ToString());
        Assert.Equal("Name=\"Save\"", ServoTarget.ByName("Save").ToString());
        Assert.Equal("Role=Button", ServoTarget.ByRole(SemanticsRole.Button).ToString());
        Assert.Equal("Id=\"save\", Name=\"Save\"", ServoTarget.ById("save").WithName("Save").ToString());
        Assert.Equal(
            "Role=Button, Name=\"Save\" within (Id=\"toolbar\")",
            ServoTarget.ByRole(SemanticsRole.Button).WithName("Save").Within(ServoTarget.ById("toolbar")).ToString());
        Assert.Equal(
            "Id=\"ok\" within (Id=\"dialog\" within (Id=\"main\"))",
            ServoTarget.ById("ok").Within(ServoTarget.ById("dialog").Within(ServoTarget.ById("main"))).ToString());
        Assert.Equal("Name=\"Say \\\"hi\\\" C:\\\\x\"", ServoTarget.ByName("Say \"hi\" C:\\x").ToString());
    }

    [Fact]
    public void TargetToStringDescribesNameRefinementsPredicatesAndPositions()
    {
        ServoTarget button = ServoTarget.ByRole(SemanticsRole.Button);

        Assert.Equal("Role=Button, Name contains \"3 of\"", button.WithNameContaining("3 of").ToString());
        Assert.Equal("Role=Button, Name matches /^Item \\d+$/", button.WithNameMatching(new Regex(@"^Item \d+$")).ToString());
        Assert.Equal(
            "Role=Button, Name matches /item/ (IgnoreCase)",
            button.WithNameMatching(new Regex("item", RegexOptions.IgnoreCase)).ToString());
        Assert.Equal("Role=Button, Name=\"B\"", button.WithNameContaining("A").WithName("B").ToString());
        Assert.Equal(
            "Role=Button, where <predicate>, where <predicate>",
            button.Where(element => element.IsEnabled).Where(element => element.IsVisible).ToString());
        Assert.Equal("Role=Button, Nth(2)", button.Nth(2).ToString());
        Assert.Equal("Role=Button, First()", button.First().ToString());
        Assert.Equal("Role=Button, Last()", button.Last().ToString());
        // Nth(0) and First() select the same element and read the same.
        Assert.Equal(
            "Role=Button, Name=\"Delete\" within (Id=\"row\", Last()), First()",
            button.WithName("Delete").Within(ServoTarget.ById("row").Last()).Nth(0).ToString());
    }

    [Fact]
    public async Task NotFoundMessageCountsNearMissesOfPredicatesAndPositions()
    {
        StackPanel panel = new();
        panel.VisualChildren.Add(new Button { Content = "One" });
        panel.VisualChildren.Add(new Button { Content = "Two", IsEnabled = false });
        ServoApi servo = new(CreateHost(panel));

        ServoTargetNotFoundException outOfRange = await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).Nth(5)));
        ServoTargetNotFoundException predicate = await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button)
                .WithName("Two")
                .Where(element => element.IsEnabled)));

        Assert.Equal(
            "The Servo target Role=Button, Nth(5) did not match any element. " +
            "Near misses: 2 matched every criterion, but Nth(5) selects none of them.",
            outOfRange.Message);
        Assert.Equal(
            "The Servo target Role=Button, Name=\"Two\", where <predicate> did not match any element. " +
            "Near misses: 1 matched every criterion except Name=\"Two\"; " +
            "1 matched every criterion except where <predicate>.",
            predicate.Message);
    }

    [Fact]
    public async Task NotFoundMessageNamesASingleCriterionTargetWithoutNearMisses()
    {
        ServoApi servo = new(CreateHost(new Button { Content = "Save" }));

        ServoTargetNotFoundException failure = await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ById("missing")));

        Assert.Equal("The Servo target Id=\"missing\" did not match any element.", failure.Message);
    }

    [Fact]
    public async Task NotFoundMessageCountsElementsThatMissExactlyOneCriterion()
    {
        UIElement toolbar = new();
        ServoApi.SetId(toolbar, "toolbar");
        toolbar.VisualChildren.Add(new Button { Content = "Open" });
        StackPanel panel = new();
        panel.VisualChildren.Add(toolbar);
        panel.VisualChildren.Add(new Button { Content = "Save" });
        panel.VisualChildren.Add(new Button { Content = "Save" });
        ServoApi servo = new(CreateHost(panel));
        ServoTarget target = ServoTarget.ByRole(SemanticsRole.Button)
            .WithName("Save")
            .Within(ServoTarget.ById("toolbar"));

        ServoTargetNotFoundException failure = await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.ClickAsync(target));

        Assert.Equal(
            "The Servo target Role=Button, Name=\"Save\" within (Id=\"toolbar\") did not match any element. " +
            "Near misses: 2 matched every criterion except within (Id=\"toolbar\"); " +
            "1 matched every criterion except Name=\"Save\".",
            failure.Message);
    }

    [Fact]
    public async Task AmbiguousMessagesNameTheTarget()
    {
        StackPanel panel = new();
        Button first = new() { Content = "First" };
        Button second = new() { Content = "Second" };
        ServoApi.SetId(first, "duplicate");
        ServoApi.SetId(second, "duplicate");
        panel.VisualChildren.Add(first);
        panel.VisualChildren.Add(second);
        ServoApi servo = new(CreateHost(panel));
        ServoTarget target = ServoTarget.ById("duplicate");
        const string expected = "The Servo target Id=\"duplicate\" matched 2 elements; exactly one was required.";

        Assert.Equal(expected, (await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
            () => servo.FindAsync(target))).Message);
        Assert.Equal(expected, (await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
            () => servo.ClickAsync(target))).Message);
        // WaitForAsync treats ambiguity as not yet satisfied; its timeout names the target and the count.
        Assert.Equal(
            "The Servo wait for target Id=\"duplicate\" to be Visible exceeded its 00:00:00.1000000 timeout. " +
            "The Servo target still matched 2 elements; exactly one was required.",
            (await Assert.ThrowsAsync<ServoTimeoutException>(
                () => servo.WaitForAsync(target, ServoCondition.Visible, TimeSpan.FromMilliseconds(100)))).Message);
    }

    [Theory]
    [InlineData("hidden", "The Servo target Id=\"target\" is not effectively visible.")]
    [InlineData("disabled", "The Servo target Id=\"target\" is disabled.")]
    [InlineData("zero-bounds", "The Servo target Id=\"target\" has no usable arranged bounds.")]
    [InlineData("not-hit-testable", "The Servo target Id=\"target\" center is not hit-testable in the current tree.")]
    public async Task NotActionableMessagesNameTheTarget(string state, string expected)
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48 };
        ServoApi.SetId(button, "target");
        if (state == "hidden") button.Visibility = Visibility.Hidden;
        else if (state == "disabled") button.IsEnabled = false;
        else if (state == "not-hit-testable") button.IsHitTestVisible = false;
        UiHost host = CreateHost(button);
        if (state == "zero-bounds") button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 0, 48)));
        ServoApi servo = new(host);

        ServoTargetNotActionableException failure = await Assert.ThrowsAsync<ServoTargetNotActionableException>(
            () => servo.ClickAsync(ServoTarget.ById("target")));

        Assert.Equal(expected, failure.Message);
    }

    [Fact]
    public void ScreenshotRegionFailureNamesTheTarget()
    {
        Button button = new() { Content = "Target", Width = 160, Height = 48, Visibility = Visibility.Hidden };
        ServoApi.SetId(button, "target");
        UiHost host = CreateHost(button);

        ServoTargetNotActionableException failure = Assert.Throws<ServoTargetNotActionableException>(
            () => new ServoCaptureEngine(new ServoQueryEngine()).ResolveRegion(host.Root!, ServoTarget.ById("target")));

        Assert.Equal("The Servo target Id=\"target\" is not effectively visible for capture.", failure.Message);
    }

    [Fact]
    public void TimeoutMessagesOfTargetOperationsNameTheirTargets()
    {
        StackPanel panel = new();
        Thumb card = new() { Width = 60, Height = 40 };
        ServoApi.SetId(card, "card");
        Button bin = new() { Content = "Bin", Width = 80, Height = 40 };
        ServoApi.SetId(bin, "bin");
        panel.VisualChildren.Add(card);
        panel.VisualChildren.Add(bin);
        ServoApi servo = new(CreateHost(panel), new ServoOptions { DefaultTimeout = TimeSpan.FromMilliseconds(100) });

        Assert.Equal(
            "The Servo operation on target Id=\"card\" exceeded its 00:00:00.1000000 timeout.",
            QueuedTimeout(() => servo.ExistsAsync(ServoTarget.ById("card"))).Message);
        Assert.Equal(
            "The Servo operation on targets Id=\"card\" and Id=\"bin\" exceeded its 00:00:00.1000000 timeout.",
            QueuedTimeout(() => servo.DragAsync(ServoTarget.ById("card"), ServoTarget.ById("bin"))).Message);
        Assert.Equal(
            "The Servo operation exceeded its 00:00:00.1000000 timeout.",
            QueuedTimeout(() => servo.PressKeyAsync(InputKey.A)).Message);
        Assert.Equal(
            "The Servo wait for target Id=\"never\" to be Visible exceeded its 00:00:00.1000000 timeout.",
            Assert.Throws<ServoTimeoutException>(() => servo.WaitForAsync(
                ServoTarget.ById("never"), ServoCondition.Visible).GetAwaiter().GetResult()).Message);
        Assert.Equal(
            "The Servo wait for target Id=\"never\" to exist exceeded its 00:00:00.1000000 timeout.",
            Assert.Throws<ServoTimeoutException>(() => servo.WaitForAsync(
                ServoTarget.ById("never"), ServoCondition.Exists).GetAwaiter().GetResult()).Message);
    }

    // Started on a thread pool thread, the operation is queued to the host's Relay, which no frame pumps.
    private static ServoTimeoutException QueuedTimeout(Func<Task> operation) =>
        Assert.Throws<ServoTimeoutException>(() => Task.Run(operation).GetAwaiter().GetResult());

    private static UiHost CreateHost(UIElement content)
    {
        UIRoot root = new(320, 160);
        root.VisualChildren.Add(content);
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(320, 160) });
        host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }
}
