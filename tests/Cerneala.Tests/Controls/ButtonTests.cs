using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.Controls;

public sealed class ButtonTests
{
    [Fact]
    public void ButtonContentElementParticipatesInTreeAndLayout()
    {
        Button button = new()
        {
            Padding = new Thickness(1),
            Content = new FixedElement(new LayoutSize(10, 5))
        };

        LayoutSize desired = button.Measure(new MeasureContext(new LayoutSize(100, 100)));
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 30, 20)));

        UIElement child = Assert.IsType<FixedElement>(button.Content);
        Assert.Same(button, child.LogicalParent);
        Assert.Same(button, child.VisualParent);
        Assert.Equal(new LayoutSize(12, 7), desired);
        Assert.Equal(new LayoutRect(1, 1, 28, 18), child.ArrangedBounds);
    }

    [Fact]
    public void ButtonReplacingEqualElementContentUpdatesRetainedOwnership()
    {
        Button button = new();
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);
        button.Content = oldChild;

        button.Content = newChild;

        Assert.Same(newChild, button.Content);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(button, newChild.LogicalParent);
        Assert.Same(button, newChild.VisualParent);
    }

    [Fact]
    public void TemplatedButtonPresentsElementContentAndUpdatesEqualElementReplacement()
    {
        Button button = new();
        ContentPresenter? presenter = null;
        button.Template = new ControlTemplate<Button>(context =>
        {
            presenter = new ContentPresenter();
            context.Bind(Button.ContentProperty, presenter, ContentPresenter.ContentProperty);
            return presenter;
        });
        EqualElement oldChild = new(1);
        EqualElement newChild = new(1);

        button.Content = oldChild;
        button.Content = newChild;

        Assert.Same(newChild, button.Content);
        Assert.Same(newChild, presenter!.PresentedChild);
        Assert.Null(oldChild.LogicalParent);
        Assert.Null(oldChild.VisualParent);
        Assert.Same(presenter, newChild.LogicalParent);
        Assert.Same(presenter, newChild.VisualParent);
        Assert.DoesNotContain(newChild, button.LogicalChildren);
        Assert.DoesNotContain(newChild, button.VisualChildren);
    }

    [Fact]
    public void ButtonRendersTextContentAndVisualState()
    {
        UIRoot root = new();
        Button button = new()
        {
            Content = "Go",
            Background = DrawColor.White,
            Foreground = DrawColor.Black
        };
        root.VisualChildren.Add(button);
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.Invalidate(InvalidationFlags.Render | InvalidationFlags.Subtree, "test");
        root.ProcessFrame();

        DrawCommandList commands = root.RetainedRenderer.Commit(root);

        Assert.Equal(2, commands.Count);
        Assert.Equal(DrawCommandKind.FillRectangle, commands[0].Kind);
        Assert.Equal(DrawCommandKind.DrawText, commands[1].Kind);
    }

    [Fact]
    public void ButtonCanBeHitHoveredPressedFocusedClickedAndCommandBound()
    {
        Executed = false;
        UIRoot root = new(100, 100);
        Button button = new()
        {
            Command = new ActionCommand(_ => Executed = true)
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.True(button.IsPointerOver);
        Assert.False(button.IsPressed);
        Assert.True(button.IsKeyboardFocused);
        Assert.True(Executed);
    }

    [Fact]
    public void ButtonClickOnElementContentExecutesButtonCommand()
    {
        Executed = false;
        UIRoot root = new(100, 100);
        Button button = new()
        {
            Content = new FixedElement(new LayoutSize(10, 10)),
            Command = new ActionCommand(_ => Executed = true)
        };
        button.Arrange(new ArrangeContext(new LayoutRect(0, 0, 40, 20)));
        root.VisualChildren.Add(button);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10, currentDown: true));
        bridge.Dispatch(root, PointerFrame(10, 10, previousDown: true));

        Assert.True(Executed);
    }

    private static bool Executed { get; set; }

    private static InputFrame PointerFrame(float x, float y, bool previousDown = false, bool currentDown = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
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

    private sealed class FixedElement(LayoutSize size) : UIElement
    {
        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            return size;
        }
    }

    private sealed class EqualElement(int id) : UIElement
    {
        private readonly int id = id;

        public override bool Equals(object? obj)
        {
            return obj is EqualElement other && other.id == id;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
