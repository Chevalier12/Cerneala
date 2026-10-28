using System.Text.RegularExpressions;
using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

public sealed class ServoTargetSelectorTests
{
    private static readonly ServoTarget Delete = ServoTarget.ByRole(SemanticsRole.Button).WithName("Delete");

    [Fact]
    public async Task NthSelectsTheMatchAtTheZeroBasedDocumentOrderIndex()
    {
        ServoApi servo = new(CreateRowsHost(out _));

        ServoElement third = await servo.FindAsync(Delete.Nth(2));
        ServoElement first = await servo.FindAsync(Delete.First());
        ServoElement last = await servo.FindAsync(Delete.Last());

        Assert.Equal("delete-3", third.Id);
        Assert.Equal("delete-1", first.Id);
        Assert.Equal("delete-3", last.Id);
        Assert.Equal("delete-2", (await servo.FindAsync(Delete.Nth(1))).Id);
    }

    [Fact]
    public async Task NthBeyondTheMatchCountIsNotFoundForEveryQuery()
    {
        ServoApi servo = new(CreateRowsHost(out _));

        await Assert.ThrowsAsync<ServoTargetNotFoundException>(() => servo.FindAsync(Delete.Nth(3)));
        Assert.Empty(await servo.FindAllAsync(Delete.Nth(3)));
        Assert.False(await servo.ExistsAsync(Delete.Nth(3)));
        await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ById("missing").First()));
        await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(ServoTarget.ById("missing").Last()));
    }

    [Fact]
    public async Task PositionalTargetsResolveToOneElementForFindAllExistsAndWaitFor()
    {
        ServoApi servo = new(CreateRowsHost(out _));

        IReadOnlyList<ServoElement> selected = await servo.FindAllAsync(Delete.Nth(1));

        Assert.Equal(["delete-2"], selected.Select(element => element.Id));
        Assert.True(await servo.ExistsAsync(Delete.Last()));
        await servo.WaitForAsync(Delete.Last(), ServoCondition.Visible);
    }

    [Fact]
    public async Task TargetsWithoutAPositionStayStrict()
    {
        ServoApi servo = new(CreateRowsHost(out _));

        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(() => servo.FindAsync(Delete));
        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
            () => servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameContaining("Del")));
        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(
            () => servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).Where(element => element.IsEnabled)));
        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(() => servo.ClickAsync(Delete));
    }

    [Fact]
    public async Task ASecondPositionReplacesTheFirst()
    {
        ServoApi servo = new(CreateRowsHost(out _));

        Assert.Equal("delete-1", (await servo.FindAsync(Delete.Last().First())).Id);
        Assert.Equal("delete-2", (await servo.FindAsync(Delete.Nth(0).Nth(1))).Id);
    }

    [Fact]
    public async Task ClickOnAPositionalTargetClicksThatElementThroughRealInput()
    {
        UiHost host = CreateRowsHost(out Button[] buttons);
        ServoApi servo = new(host);
        int[] clicks = new int[buttons.Length];
        for (int index = 0; index < buttons.Length; index++)
        {
            int captured = index;
            buttons[index].Click += (_, _) => clicks[captured]++;
        }

        await servo.ClickAsync(Delete.Nth(1));

        Assert.Equal([0, 1, 0], clicks);
    }

    [Fact]
    public async Task PositionIsAppliedAfterWithinRegardlessOfCallOrder()
    {
        ServoApi servo = new(CreateRowsHost(out _));
        ServoTarget secondRow = ServoTarget.ById("row").Nth(1);

        Assert.Equal("delete-2", (await servo.FindAsync(Delete.Within(secondRow).First())).Id);
        Assert.Equal("delete-2", (await servo.FindAsync(Delete.First().Within(secondRow))).Id);
        await Assert.ThrowsAsync<ServoTargetNotFoundException>(
            () => servo.FindAsync(Delete.Nth(1).Within(secondRow)));
    }

    [Fact]
    public async Task PositionalScopesSelectOneAncestor()
    {
        ServoApi servo = new(CreateRowsHost(out _));
        ServoTarget row = ServoTarget.ById("row");

        Assert.Equal("delete-1", (await servo.FindAsync(Delete.Within(row.First()))).Id);
        Assert.Equal("delete-3", (await servo.FindAsync(Delete.Within(row.Nth(2)))).Id);
        Assert.Equal("delete-3", (await servo.FindAsync(Delete.Within(row.Last()))).Id);
        Assert.Equal("delete-2", (await servo.FindAsync(Delete.Within(row.Where(element => element.Bounds.Y >= 40).First()))).Id);
        Assert.False(await servo.ExistsAsync(Delete.Within(row.Nth(3))));
        Assert.Equal(3, (await servo.FindAllAsync(Delete.Within(row))).Count);
        await Assert.ThrowsAsync<ServoTargetAmbiguousException>(() => servo.FindAsync(Delete.Within(row)));
    }

    [Fact]
    public async Task NameSubstringAndRegexUseOrdinalComparison()
    {
        UIRoot root = new(320, 240);
        Button item = new() { Content = "Item 3 of 10" };
        Button other = new() { Content = "Other" };
        root.VisualChildren.Add(item);
        root.VisualChildren.Add(other);
        ServoApi servo = new(CreateHost(root));

        Assert.Equal("Item 3 of 10", (await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameContaining("3 of"))).Name);
        Assert.False(await servo.ExistsAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameContaining("item 3")));
        Assert.Equal(
            "Item 3 of 10",
            (await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex(@"^Item \d+ of \d+$")))).Name);
        Assert.False(await servo.ExistsAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex("^item"))));
        Assert.True(await servo.ExistsAsync(
            ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex("^item", RegexOptions.IgnoreCase))));
        Assert.Equal("Item 3 of 10", (await servo.FindAsync(ServoTarget.ByRole(SemanticsRole.Button).WithNameMatching(new Regex(@"\d")))).Name);
    }

    [Fact]
    public async Task TheThreeNameMethodsShareOneSlotAndTheLastCallWins()
    {
        UIRoot root = new(320, 240);
        root.VisualChildren.Add(new Button { Content = "Item 3 of 10" });
        root.VisualChildren.Add(new Button { Content = "3 apples" });
        ServoApi servo = new(CreateHost(root));
        ServoTarget button = ServoTarget.ByRole(SemanticsRole.Button);

        Assert.Equal("Item 3 of 10", (await servo.FindAsync(button.WithName("3 apples").WithNameContaining("Item"))).Name);
        Assert.Equal("3 apples", (await servo.FindAsync(button.WithNameContaining("Item").WithName("3 apples"))).Name);
        Assert.Equal("3 apples", (await servo.FindAsync(button.WithNameMatching(new Regex("Item")).WithNameContaining("apples"))).Name);
        Assert.Equal("3 apples", (await servo.FindAsync(ServoTarget.ByName("Item 3 of 10").WithNameMatching(new Regex("^3")))).Name);
        Assert.Equal(2, (await servo.FindAllAsync(button.WithNameContaining("Item").WithNameContaining("3"))).Count);
    }

    [Fact]
    public async Task WherePredicatesFilterSnapshotsAndStack()
    {
        UIRoot root = new(320, 240);
        Button enabled = new() { Content = "Delete" };
        Button disabled = new() { Content = "Delete", IsEnabled = false };
        Button disabledOther = new() { Content = "Other", IsEnabled = false };
        ServoApi.SetId(enabled, "enabled");
        ServoApi.SetId(disabled, "disabled");
        ServoApi.SetId(disabledOther, "disabled-other");
        root.VisualChildren.Add(enabled);
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(disabledOther);
        ServoApi servo = new(CreateHost(root));
        ServoTarget button = ServoTarget.ByRole(SemanticsRole.Button);

        Assert.Equal("enabled", (await servo.FindAsync(button.Where(element => element.IsEnabled))).Id);
        Assert.Equal(2, (await servo.FindAllAsync(button.Where(element => !element.IsEnabled))).Count);
        Assert.Equal(
            "disabled",
            (await servo.FindAsync(button.Where(element => !element.IsEnabled).Where(element => element.Name == "Delete"))).Id);
        Assert.Equal("disabled-other", (await servo.FindAsync(button.Where(element => !element.IsEnabled).Last())).Id);
        Assert.Equal("Button", (await servo.FindAsync(ServoTarget.ById("enabled").Where(element => element.TypeName == "Button"))).TypeName);
    }

    [Fact]
    public async Task WherePredicateIsNotRunOnElementsThatAreNotCandidatesWhenTheTargetIsMissing()
    {
        UIRoot root = new(320, 240);
        root.VisualChildren.Add(new Button { Content = "Only a button" });
        ServoApi servo = new(CreateHost(root));
        int predicateCalls = 0;
        ServoTarget editable = ServoTarget.ByRole(SemanticsRole.EditableText);

        await Assert.ThrowsAsync<ServoTargetNotFoundException>(() => servo.FindAsync(editable));
        Exception? failure = await Record.ExceptionAsync(() => servo.FindAsync(
            editable.Where(element =>
            {
                predicateCalls++;
                return element.Value!.Length > 0;
            })));

        Assert.True(
            failure is ServoTargetNotFoundException,
            $"FindAsync threw {failure?.GetType().Name ?? "nothing"} ({failure?.Message}); predicate calls: {predicateCalls}.");
        Assert.Equal(0, predicateCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ClickOnAMissingTargetWithAWherePredicateTimesOutInsteadOfThrowingFromThePredicate(bool withWhere)
    {
        // Row `false` (no Where) is the control: a missing target times out with the not-found failure as inner exception.
        UIRoot root = new(320, 240);
        root.VisualChildren.Add(new Button { Content = "Only a button" });
        ServoApi servo = new(CreateHost(root));
        ServoTarget editable = ServoTarget.ByRole(SemanticsRole.EditableText);
        if (withWhere)
        {
            editable = editable.Where(element => element.Value!.Length > 0);
        }

        Exception? failure = await Record.ExceptionAsync(() =>
            servo.ClickAsync(editable, TimeSpan.FromMilliseconds(300)));

        Assert.True(
            failure is ServoTimeoutException { InnerException: ServoTargetNotFoundException },
            $"withWhere={withWhere}: ClickAsync threw {failure?.GetType().Name ?? "nothing"}" +
            $"{(failure is null ? string.Empty : " (" + failure.Message + ")")}" +
            $"{(failure?.InnerException is null ? string.Empty : ", inner " + failure.InnerException.GetType().Name)}.");
    }

    [Fact]
    public void NewRefinementsValidateTheirInputs()
    {
        ServoTarget target = ServoTarget.ByRole(SemanticsRole.Button);

        Assert.Throws<ArgumentOutOfRangeException>(() => target.Nth(-1));
        Assert.Throws<ArgumentException>(() => target.WithNameContaining(""));
        Assert.Throws<ArgumentNullException>(() => target.WithNameContaining(null!));
        Assert.Throws<ArgumentNullException>(() => target.WithNameMatching(null!));
        Assert.Throws<ArgumentNullException>(() => target.Where(null!));
    }

    private static UiHost CreateRowsHost(out Button[] buttons)
    {
        UIRoot root = new(320, 240);
        StackPanel list = new();
        buttons = new Button[3];
        for (int index = 0; index < buttons.Length; index++)
        {
            StackPanel row = new() { Orientation = Orientation.Horizontal, Height = 40 };
            ServoApi.SetId(row, "row");
            Button delete = new() { Content = "Delete", Width = 100, Height = 32 };
            ServoApi.SetId(delete, $"delete-{index + 1}");
            row.VisualChildren.Add(delete);
            list.VisualChildren.Add(row);
            buttons[index] = delete;
        }

        root.VisualChildren.Add(list);
        return CreateHost(root);
    }

    private static UiHost CreateHost(UIRoot root, float width = 320, float height = 240)
    {
        UiHost host = new(new UiHostOptions
        {
            Root = root,
            Viewport = new UiViewport(width, height)
        });
        host.Update(
            new InputFrame(
                PointerSnapshot.Empty,
                PointerSnapshot.Empty,
                KeyboardSnapshot.Empty,
                KeyboardSnapshot.Empty,
                []),
            host.Viewport,
            TimeSpan.Zero);
        return host;
    }
}
