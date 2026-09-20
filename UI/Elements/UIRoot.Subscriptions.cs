using Cerneala.UI.Relay;
using Cerneala.UI.Resources;
using Cerneala.UI.Theming;

namespace Cerneala.UI.Elements;

public sealed partial class UIRoot
{
    private sealed class ThemeChangedSubscription : IDisposable
    {
        private readonly WeakReference<UIRoot> rootReference;
        private readonly ThemeProvider provider;
        private readonly UiRelayRefreshDispatcher refreshDispatcher;
        private Func<bool>? callbackGuard;
        private bool disposed;

        public ThemeChangedSubscription(UIRoot root, ThemeProvider provider)
        {
            rootReference = new WeakReference<UIRoot>(root);
            this.provider = provider;
            refreshDispatcher = new UiRelayRefreshDispatcher(
                ResolveRelay,
                ApplyChange,
                "theme change");
            callbackGuard = refreshDispatcher.Activate();
            provider.ThemeChanged += OnThemeChanged;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            provider.ThemeChanged -= OnThemeChanged;
            refreshDispatcher.Deactivate();
            callbackGuard = null;
            disposed = true;
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs args)
        {
            if (rootReference.TryGetTarget(out UIRoot? root))
            {
                if (callbackGuard?.Invoke() == true)
                {
                    root.InvalidateThemeChange();
                }

                return;
            }

            Dispose();
        }

        private UiRelay? ResolveRelay()
        {
            return rootReference.TryGetTarget(out UIRoot? root) ? root.Relay : null;
        }

        private void ApplyChange()
        {
            if (!disposed && rootReference.TryGetTarget(out UIRoot? root))
            {
                root.InvalidateThemeChange();
            }
        }
    }

    private sealed class ResourceChangedSubscription : IDisposable
    {
        private readonly WeakReference<UIRoot> rootReference;
        private readonly WeakReference<ResourceChangedSubscription> selfReference;
        private readonly IObservableResourceProvider provider;
        private int disposed;

        public ResourceChangedSubscription(UIRoot root, IObservableResourceProvider provider)
        {
            rootReference = new WeakReference<UIRoot>(root);
            selfReference = new WeakReference<ResourceChangedSubscription>(this);
            this.provider = provider;
            provider.ResourceChanged += OnResourceChanged;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) != 0)
            {
                return;
            }

            provider.ResourceChanged -= OnResourceChanged;
        }

        private void OnResourceChanged(object? sender, ResourceChangedEventArgs args)
        {
            if (Volatile.Read(ref disposed) != 0)
            {
                return;
            }

            if (!rootReference.TryGetTarget(out UIRoot? root))
            {
                Dispose();
                return;
            }

            if (root.Relay.CheckAccess())
            {
                ApplyChange(args);
                return;
            }

            WeakReference<ResourceChangedSubscription> weak = selfReference;
            root.Relay.Post(() =>
            {
                if (weak.TryGetTarget(out ResourceChangedSubscription? subscription))
                {
                    subscription.ApplyChange(args);
                }
            });
        }

        private void ApplyChange(ResourceChangedEventArgs args)
        {
            if (Volatile.Read(ref disposed) == 0 && rootReference.TryGetTarget(out UIRoot? root))
            {
                root.ApplyResourceChange(args);
            }
        }
    }
}
