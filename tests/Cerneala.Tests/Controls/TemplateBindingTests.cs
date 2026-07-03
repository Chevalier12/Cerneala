using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Styling;

namespace Cerneala.Tests.Controls;

public sealed class TemplateBindingTests
{
    [Fact]
    public void BindingCopiesOwnerValueToGeneratedChild()
    {
        Button button = new() { Background = DrawColor.White };
        Border? border = null;
        button.Template = new ControlTemplate<Button>(context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });

        Assert.Equal(DrawColor.White, border!.Background);
        Assert.Equal(UiPropertyValueSource.TemplateBinding, border.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void BindingFollowsOwnerPropertyChanges()
    {
        Button button = new() { Background = DrawColor.White };
        Border? border = null;
        button.Template = new ControlTemplate<Button>(context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });

        button.Background = DrawColor.Black;

        Assert.Equal(DrawColor.Black, border!.Background);
    }

    [Fact]
    public void BindingValueSurvivesStyleValueBeingCleared()
    {
        Button button = new() { Background = DrawColor.White };
        Border? border = null;
        button.Template = new ControlTemplate<Button>(context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });
        StyleApplicator applicator = new();
        StyleSheet sheet = new StyleSheet().Add(new StyleRule(StyleSelector.ForType<Border>())
            .Add(new Setter<DrawColor>(Control.BackgroundProperty, DrawColor.Black)));

        applicator.Apply(border!, sheet);
        applicator.Apply(border!, new StyleSheet());

        Assert.Equal(DrawColor.White, border!.Background);
    }

    [Fact]
    public void BindingDetachesWhenTemplateInstanceIsDetached()
    {
        Button button = new() { Background = DrawColor.White };
        Border? oldBorder = null;
        button.Template = new ControlTemplate<Button>(context =>
        {
            oldBorder = new Border();
            context.Bind(Control.BackgroundProperty, oldBorder, Control.BackgroundProperty);
            return oldBorder;
        });

        button.Template = new ControlTemplate<Button>(_ => new Border());
        button.Background = DrawColor.Black;

        Assert.Equal(DrawColor.White, oldBorder!.Background);
        Assert.Null(oldBorder.LogicalParent);
        Assert.Null(oldBorder.VisualParent);
    }

    [Fact]
    public void BindingRejectsMismatchedPropertyTypes()
    {
        Button button = new();
        Border border = new();
        TemplateContext<Button> context = new(button);

        Assert.Throws<ArgumentException>(() => context.Bind(Control.BackgroundProperty, border, Control.FontSizeProperty));
    }

    [Fact]
    public void BindingRejectsReadOnlyTargetBeforeTemplateRootIsAttached()
    {
        UiProperty<int> sourceProperty = UiProperty<int>.Register(
            UniqueName(),
            typeof(Button),
            new UiPropertyMetadata<int>(0));
        UiPropertyKey<int> targetKey = UiProperty<int>.RegisterReadOnly(
            UniqueName(),
            typeof(Border),
            new UiPropertyMetadata<int>(0));
        Button button = new();
        Border? border = null;
        button.SetValue(sourceProperty, 42);
        ControlTemplate<Button> template = new(context =>
        {
            border = new Border();
            context.Bind(sourceProperty, border, targetKey.Property);
            return border;
        });

        Exception exception = Assert.ThrowsAny<Exception>(() => button.Template = template);

        Assert.NotNull(border);
        Assert.Null(border.LogicalParent);
        Assert.Null(border.VisualParent);
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void BindingAttachFailureDetachesTemplateRoot()
    {
        UiProperty<float> sourceProperty = UiProperty<float>.Register(
            UniqueName(),
            typeof(Button),
            new UiPropertyMetadata<float>(0));
        Button button = new();
        Border? border = null;
        button.SetValue(sourceProperty, -1);
        ControlTemplate<Button> template = new(context =>
        {
            border = new Border();
            context.Bind(sourceProperty, border, Control.FontSizeProperty);
            return border;
        });

        Assert.Throws<ArgumentException>(() => button.Template = template);

        Assert.NotNull(border);
        Assert.Null(border.LogicalParent);
        Assert.Null(border.VisualParent);
    }

    [Fact]
    public void BindingAttachFailureDoesNotLeaveOwnerSubscription()
    {
        UiProperty<float> sourceProperty = UiProperty<float>.Register(
            UniqueName(),
            typeof(Button),
            new UiPropertyMetadata<float>(0));
        Button button = new();
        Border border = new();
        TemplateBinding<float> binding = new(sourceProperty, border, Control.FontSizeProperty);
        button.SetValue(sourceProperty, -1);

        Assert.Throws<ArgumentException>(() => binding.Attach(button));
        button.SetValue(sourceProperty, 20);

        Assert.Equal(16, border.FontSize);
    }

    private static string UniqueName()
    {
        return $"{nameof(TemplateBindingTests)}_{Guid.NewGuid():N}";
    }
}
