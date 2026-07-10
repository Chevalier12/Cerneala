using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Data;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

namespace Cerneala.Tests.UI.Elements;

public sealed class RetainedLifecycleCleanupTests
{
    [Fact]
    public void DetachedItemsControlUnsubscribesObservableItemsSource()
    {
        UIRoot root = new();
        ObservableList<string> items = new(["one"]);
        ItemsControl control = new() { ItemsSource = items };
        root.VisualChildren.Add(control);
        root.ProcessFrame();

        root.VisualChildren.Remove(control);
        root.ProcessFrame();
        long dirtyVersion = control.DirtyState.Version;
        items.Add("two");

        Assert.Equal(dirtyVersion, control.DirtyState.Version);
    }

    [Fact]
    public void DetachedButtonUnsubscribesObservableCommandCanExecuteChanged()
    {
        UIRoot root = new();
        ButtonBase button = new();
        ActionCommand command = new(_ => { }, _ => false);
        button.Command = command;
        root.VisualChildren.Add(button);
        root.ProcessFrame();

        root.VisualChildren.Remove(button);
        root.ProcessFrame();
        long dirtyVersion = button.DirtyState.Version;
        command.RaiseCanExecuteChanged();

        Assert.Equal(dirtyVersion, button.DirtyState.Version);
    }

    [Fact]
    public void DetachedElementOwnedBindingStopsReceivingSourceChanges()
    {
        UIRoot root = new();
        TextBlock target = new();
        ObservableValue<string> source = new("one");
        root.VisualChildren.Add(target);
        target.Bindings.Add(BindingOperations.BindOneWay(target, TextBlock.TextProperty, source));

        root.VisualChildren.Remove(target);
        source.Value = "two";

        Assert.Equal("one", target.Text);
    }

    [Fact]
    public void TemplateReplacementDisposesTemplateBindingsExactlyOnce()
    {
        Button button = new();
        TextBlock firstChild = new();
        TextBlock secondChild = new();
        button.ComponentTemplate = new ComponentTemplate<Button>("first", _ => firstChild);
        button.ApplyTemplate();

        button.ComponentTemplate = new ComponentTemplate<Button>("second", _ => secondChild);
        button.ApplyTemplate();

        Assert.Null(firstChild.VisualParent);
        Assert.Same(button, secondChild.VisualParent);
    }

    [Fact]
    public void ContentPresenterContentReplacementDetachesGeneratedChildParents()
    {
        ContentPresenter presenter = new();
        TextBlock first = new();
        TextBlock second = new();

        presenter.Content = first;
        presenter.Content = second;

        Assert.Null(first.VisualParent);
        Assert.Same(presenter, second.VisualParent);
    }
}
