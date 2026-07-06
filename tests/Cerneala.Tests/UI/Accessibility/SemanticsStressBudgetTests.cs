using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout.Panels;
using StackPanel = Cerneala.UI.Controls.StackPanel;

namespace Cerneala.Tests.UI.Accessibility;

public sealed class SemanticsStressBudgetTests
{
    private const int ButtonCount = 40;
    private const int UnrelatedElementCount = 160;

    [Fact]
    public void ManyButtonsSingleCommandCanExecuteChangedRefreshesOnlyRegisteredCommandSources()
    {
        UIRoot root = new(800, 600);
        StackPanel panel = new()
        {
            Orientation = Orientation.Vertical
        };
        bool canExecute = true;
        ActionCommand command = new(_ => { }, _ => canExecute);

        for (int i = 0; i < ButtonCount; i++)
        {
            panel.VisualChildren.Add(new Button
            {
                Content = $"Command {i}",
                Command = command
            });
        }

        for (int i = 0; i < UnrelatedElementCount; i++)
        {
            panel.VisualChildren.Add(new TextBlock { Text = $"Passive {i}" });
        }

        root.VisualChildren.Add(panel);
        root.ProcessFrame();

        canExecute = false;
        command.RaiseCanExecuteChanged();
        FrameStats stats = root.ProcessFrame();

        Assert.Equal(ButtonCount, stats.CommandStateElements);
        Assert.True(stats.StyledElements <= ButtonCount, $"Styled {stats.StyledElements} elements for {ButtonCount} command sources.");
        Assert.True(stats.RenderedElements <= ButtonCount, $"Rendered {stats.RenderedElements} elements for {ButtonCount} command sources.");
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(0, stats.MeasureCalls);
        Assert.Equal(0, stats.ArrangeCalls);
    }

    [Fact]
    public void SemanticsRepeatedQueriesReturnCachedTreeForLargeTree()
    {
        UIRoot root = LargeSemanticRoot(out _);
        root.ProcessFrame();

        SemanticsTree first = root.GetSemanticsTree();

        for (int i = 0; i < 50; i++)
        {
            Assert.Same(first, root.GetSemanticsTree());
        }
    }

    [Fact]
    public void SemanticNameChangeRebuildsSemanticsWithoutLayoutOrRenderBudget()
    {
        UIRoot root = LargeSemanticRoot(out Button target);
        root.ProcessFrame();
        SemanticsTree first = root.GetSemanticsTree();
        int treeVersion = root.TreeVersion;

        AccessibleName.SetName(target, "Updated target");
        FrameStats stats = root.ProcessFrame();
        SemanticsTree second = root.GetSemanticsTree();

        Assert.NotNull(target.ElementId);
        Assert.NotSame(first, second);
        Assert.Equal(treeVersion, root.TreeVersion);
        Assert.NotNull(FindNode(second.Root, target.ElementId));
        Assert.Equal("Updated target", FindNode(second.Root, target.ElementId)!.Name);
        Assert.Equal(0, stats.MeasuredElements);
        Assert.Equal(0, stats.ArrangedElements);
        Assert.Equal(0, stats.MeasureCalls);
        Assert.Equal(0, stats.ArrangeCalls);
        Assert.Equal(0, stats.RenderedElements);
    }

    private static UIRoot LargeSemanticRoot(out Button target)
    {
        UIRoot root = new(800, 600);
        StackPanel panel = new()
        {
            Orientation = Orientation.Vertical
        };
        target = new Button { Content = "Target" };
        panel.VisualChildren.Add(target);

        for (int i = 0; i < ButtonCount; i++)
        {
            panel.VisualChildren.Add(new Button { Content = $"Action {i}" });
        }

        for (int i = 0; i < UnrelatedElementCount; i++)
        {
            panel.VisualChildren.Add(new TextBlock { Text = $"Label {i}" });
        }

        root.VisualChildren.Add(panel);
        return root;
    }

    private static SemanticsNode? FindNode(SemanticsNode node, UiElementId? elementId)
    {
        if (node.ElementId == elementId)
        {
            return node;
        }

        foreach (SemanticsNode child in node.Children)
        {
            SemanticsNode? match = FindNode(child, elementId);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }
}
