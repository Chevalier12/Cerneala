using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public abstract class TemplateBinding
{
    private protected TemplateBinding(
        UiProperty sourceProperty,
        UIElement target,
        UiProperty targetProperty,
        UiPropertyValueSource targetSource)
    {
        SourceProperty = sourceProperty ?? throw new ArgumentNullException(nameof(sourceProperty));
        Target = target ?? throw new ArgumentNullException(nameof(target));
        TargetProperty = targetProperty ?? throw new ArgumentNullException(nameof(targetProperty));
        TargetSource = targetSource;
        if (SourceProperty.ValueType != TargetProperty.ValueType)
        {
            throw new ArgumentException(
                $"Template binding source type '{SourceProperty.ValueType.FullName}' does not match target type '{TargetProperty.ValueType.FullName}'.",
                nameof(targetProperty));
        }

        if (TargetProperty.IsReadOnly)
        {
            throw new ArgumentException(
                $"Template binding target property '{TargetProperty.DiagnosticName}' is read-only.",
                nameof(targetProperty));
        }
    }

    public UiProperty SourceProperty { get; }

    public UIElement Target { get; }

    public UiProperty TargetProperty { get; }

    public UiPropertyValueSource TargetSource { get; }

    public abstract void Attach(Control owner);

    public abstract void Detach();

    public static TemplateBinding Create(
        UiProperty sourceProperty,
        UIElement target,
        UiProperty targetProperty,
        UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)
    {
        ArgumentNullException.ThrowIfNull(sourceProperty);
        ArgumentNullException.ThrowIfNull(targetProperty);
        if (sourceProperty.ValueType != targetProperty.ValueType)
        {
            throw new ArgumentException(
                $"Template binding source type '{sourceProperty.ValueType.FullName}' does not match target type '{targetProperty.ValueType.FullName}'.",
                nameof(targetProperty));
        }

        if (targetProperty.IsReadOnly)
        {
            throw new ArgumentException(
                $"Template binding target property '{targetProperty.DiagnosticName}' is read-only.",
                nameof(targetProperty));
        }

        return new UntypedTemplateBinding(sourceProperty, target, targetProperty, targetSource);
    }

    private sealed class UntypedTemplateBinding(
        UiProperty sourceProperty,
        UIElement target,
        UiProperty targetProperty,
        UiPropertyValueSource targetSource)
        : TemplateBinding(sourceProperty, target, targetProperty, targetSource)
    {
        private Control? owner;

        public override void Attach(Control owner)
        {
            ArgumentNullException.ThrowIfNull(owner);
            if (this.owner is not null)
            {
                throw new InvalidOperationException("Template binding is already attached.");
            }

            this.owner = owner;
            owner.PropertyChanged += OnOwnerPropertyChanged;
            try
            {
                UpdateTarget(owner);
            }
            catch
            {
                owner.PropertyChanged -= OnOwnerPropertyChanged;
                this.owner = null;
                throw;
            }
        }

        public override void Detach()
        {
            if (owner is null)
            {
                return;
            }

            if (TargetSource != UiPropertyValueSource.TemplateBinding)
            {
                Target.ClearValueUntyped(TargetProperty, TargetSource);
            }

            owner.PropertyChanged -= OnOwnerPropertyChanged;
            owner = null;
        }

        private void OnOwnerPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            if (sender is Control control && ReferenceEquals(args.Property, SourceProperty))
            {
                UpdateTarget(control);
            }
        }

        private void UpdateTarget(Control control)
        {
            Target.SetValueUntyped(TargetProperty, control.GetValue(SourceProperty), TargetSource);
        }
    }
}

public sealed class TemplateBinding<T> : TemplateBinding
{
    private Control? owner;

    public TemplateBinding(
        UiProperty<T> sourceProperty,
        UIElement target,
        UiProperty<T> targetProperty,
        UiPropertyValueSource targetSource = UiPropertyValueSource.TemplateBinding)
        : base(sourceProperty, target, targetProperty, targetSource)
    {
        SourceProperty = sourceProperty;
        TargetProperty = targetProperty;
    }

    public new UiProperty<T> SourceProperty { get; }

    public new UiProperty<T> TargetProperty { get; }

    public override void Attach(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        if (this.owner is not null)
        {
            throw new InvalidOperationException("Template binding is already attached.");
        }

        this.owner = owner;
        owner.PropertyChanged += OnOwnerPropertyChanged;
        try
        {
            UpdateTarget(owner);
        }
        catch
        {
            owner.PropertyChanged -= OnOwnerPropertyChanged;
            this.owner = null;
            throw;
        }
    }

    public override void Detach()
    {
        if (owner is null)
        {
            return;
        }

        if (TargetSource != UiPropertyValueSource.TemplateBinding)
        {
            Target.ClearValue(TargetProperty, TargetSource);
        }

        owner.PropertyChanged -= OnOwnerPropertyChanged;
        owner = null;
    }

    private void OnOwnerPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
    {
        if (sender is Control control && ReferenceEquals(args.Property, SourceProperty))
        {
            UpdateTarget(control);
        }
    }

    private void UpdateTarget(Control control)
    {
        Target.SetValue(TargetProperty, control.GetValue(SourceProperty), TargetSource);
    }
}
