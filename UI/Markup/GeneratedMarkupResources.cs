using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static IDisposable AttachResource<T>(
        UIElement owner,
        UiObject target,
        UiProperty<T> targetProperty,
        string key,
        UiPropertyValueSource valueSource)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(targetProperty);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        MarkupResourceController<T> controller = new(
            owner,
            target,
            targetProperty,
            key,
            valueSource);
        controller.ApplyAvailableValue();
        owner.AddLifecycleBehavior(controller);
        return controller;
    }

    private sealed class MarkupResourceController<T> : IElementLifecycleBehavior, IDisposable
    {
        private readonly UIElement owner;
        private readonly UiObject target;
        private readonly UiProperty<T> targetProperty;
        private readonly string key;
        private readonly UiPropertyValueSource valueSource;
        private IObservableResourceProvider? provider;
        private bool disposed;

        public MarkupResourceController(
            UIElement owner,
            UiObject target,
            UiProperty<T> targetProperty,
            string key,
            UiPropertyValueSource valueSource)
        {
            this.owner = owner;
            this.target = target;
            this.targetProperty = targetProperty;
            this.key = key;
            this.valueSource = valueSource;
        }

        public void Attach()
        {
            if (disposed)
            {
                return;
            }

            provider = owner.Root?.ResourceProvider as IObservableResourceProvider;
            if (provider is not null)
            {
                provider.ResourceChanged += OnResourceChanged;
            }

            ApplyAvailableValue();
        }

        public void Detach()
        {
            if (provider is not null)
            {
                provider.ResourceChanged -= OnResourceChanged;
                provider = null;
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            Detach();
            owner.RemoveLifecycleBehavior(this);
        }

        public void ApplyAvailableValue()
        {
            if (owner.TryFindResource(key, out T resource) ||
                Application.Current?.Resources.TryGetResource(key, out resource) == true)
            {
                target.SetValue(targetProperty, resource, valueSource);
            }
        }

        private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
        {
            if (!string.Equals(args.Key, key, StringComparison.Ordinal))
            {
                return;
            }

            UIRoot? root = owner.Root;
            if (root is null || root.Relay.CheckAccess())
            {
                ApplyAvailableValue();
                return;
            }

            root.Relay.Post(ApplyAvailableValue);
        }
    }
}
