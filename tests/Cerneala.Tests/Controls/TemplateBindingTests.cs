using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;

namespace Cerneala.Tests.Controls;

public sealed class TemplateBindingTests
{
    [Fact]
    public void BindingCopiesOwnerValueToGeneratedChild()
    {
        Button button = new() { Background = Color.White };
        Border? border = null;
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });

        Assert.Equal(Color.White, border!.Background);
        Assert.Equal(UiPropertyValueSource.TemplateBinding, border.GetValueSource(Control.BackgroundProperty));
    }

    [Fact]
    public void BindingFollowsOwnerPropertyChanges()
    {
        Button button = new() { Background = Color.White };
        Border? border = null;
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });

        button.Background = Color.Black;

        Assert.Equal(Color.Black, border!.Background);
    }

    [Fact]
    public void BindingValueSurvivesAspectValueBeingCleared()
    {
        Button button = new() { Background = Color.White };
        Border? border = null;
        button.ComponentTemplate = new ComponentTemplate<Button>("test", context =>
        {
            border = new Border();
            context.Bind(Control.BackgroundProperty, border, Control.BackgroundProperty);
            return border;
        });
        border!.SetValue(Control.BackgroundProperty, Color.Black, UiPropertyValueSource.AspectBase);
        border.ClearValue(Control.BackgroundProperty, UiPropertyValueSource.AspectBase);

        Assert.Equal(Color.White, border.Background);
    }

    [Fact]
    public void BindingDetachesWhenTemplateInstanceIsDetached()
    {
        Button button = new() { Background = Color.White };
        Border? oldBorder = null;
        button.ComponentTemplate = new ComponentTemplate<Button>("old", context =>
        {
            oldBorder = new Border();
            context.Bind(Control.BackgroundProperty, oldBorder, Control.BackgroundProperty);
            return oldBorder;
        });

        button.ComponentTemplate = new ComponentTemplate<Button>("new", _ => new Border());
        button.Background = Color.Black;

        Assert.Equal(Color.White, oldBorder!.Background);
        Assert.Null(oldBorder.LogicalParent);
        Assert.Null(oldBorder.VisualParent);
    }

    [Fact]
    public void BindingRejectsMismatchedPropertyTypes()
    {
        Button button = new();
        Border border = new();
        ComponentTemplateContext<Button> context = new(button, new AspectEnvironment("test"));

        Assert.Throws<ArgumentException>(() => context.Bind(
            Control.BackgroundProperty,
            border,
            Control.FontSizeProperty,
            UiPropertyValueSource.TemplateBinding));
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
        ComponentTemplate<Button> template = new("test", context =>
        {
            border = new Border();
            context.Bind(sourceProperty, border, targetKey.Property);
            return border;
        });

        Exception exception = Assert.ThrowsAny<Exception>(() => button.ComponentTemplate = template);

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
        ComponentTemplate<Button> template = new("test", context =>
        {
            border = new Border();
            context.Bind(sourceProperty, border, Control.FontSizeProperty);
            return border;
        });

        Assert.Throws<ArgumentException>(() => button.ComponentTemplate = template);

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
