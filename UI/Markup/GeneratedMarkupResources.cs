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
        owner.AddLifecycleBehavior(controller);
        try
        {
            // The owner's attach path has already run for an attached owner, so the controller subscribes now.
            // A detaching owner is past its behaviors' Detach, so it only gets the current value.
            if (owner.IsAttached && !owner.IsDetachingFromRoot)
            {
                controller.Attach();
            }
            else
            {
                controller.ApplyAvailableValue();
            }
        }
        catch
        {
            // The caller never receives the lifetime, so nothing may stay registered or subscribed.
            controller.Dispose();
            throw;
        }

        return controller;
    }

    private sealed class MarkupResourceController<T> : IElementLifecycleBehavior, IDisposable
    {
        private readonly UIElement owner;
        private readonly UiObject target;
        private readonly UiProperty<T> targetProperty;
        private readonly string key;
        private readonly UiPropertyValueSource valueSource;
        private UIRoot? root;
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

            root = owner.Root;
            if (root is not null)
            {
                root.ResourceProviderChanged += OnRootResourceProviderChanged;
            }

            SubscribeToRootProvider();
            ApplyAvailableValue();
        }

        public void Detach()
        {
            if (root is not null)
            {
                root.ResourceProviderChanged -= OnRootResourceProviderChanged;
                root = null;
            }

            UnsubscribeFromProvider();
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
            if (disposed)
            {
                return;
            }

            if (owner.TryFindResource(key, out T resource) ||
                Application.Current?.Resources.TryGetResource(key, out resource) == true)
            {
                target.SetValue(targetProperty, resource, valueSource);
            }
        }

        private void SubscribeToRootProvider()
        {
            provider = root?.ResourceProvider as IObservableResourceProvider;
            if (provider is not null)
            {
                provider.ResourceChanged += OnResourceChanged;
            }
        }

        private void UnsubscribeFromProvider()
        {
            if (provider is not null)
            {
                provider.ResourceChanged -= OnResourceChanged;
                provider = null;
            }
        }

        // The root replaced its provider: follow the new one only, and re-resolve the nearest value from it.
        private void OnRootResourceProviderChanged()
        {
            UnsubscribeFromProvider();
            SubscribeToRootProvider();
            ApplyAvailableValue();
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
