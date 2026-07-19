using System.Runtime.CompilerServices;
using Cerneala.Drawing.Prism;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Prism.Runtime;

internal sealed class PrismAttachment : IElementLifecycleBehavior, IDisposable
{
    private static readonly ConditionalWeakTable<UIElement, PrismAttachment> Attachments = new();
    private static long nextCacheOwnerToken;

    private UIElement? owner;
    private Func<PrismInstance>? instanceFactory;
    private IReadOnlyList<Func<PrismInstance, IDisposable>>? bindingFactories;
    private IDisposable[]? bindings;
    private PrismInstance? instance;
    private PrismCacheOwnerToken cacheOwnerToken;
    private bool attached;
    private bool renderable;
    private bool disposed;

    private PrismAttachment(
        UIElement owner,
        Func<PrismInstance> instanceFactory,
        IReadOnlyList<Func<PrismInstance, IDisposable>> bindingFactories)
    {
        this.owner = owner;
        this.instanceFactory = instanceFactory;
        this.bindingFactories = bindingFactories;
    }

    public static IDisposable Set(
        UIElement owner,
        Func<PrismInstance> instanceFactory,
        IReadOnlyList<Func<PrismInstance, IDisposable>> bindingFactories)
    {
        if (Attachments.TryGetValue(owner, out PrismAttachment? previous))
        {
            previous.Dispose();
        }

        PrismAttachment attachment = new(owner, instanceFactory, bindingFactories);
        Attachments.Add(owner, attachment);
        owner.AddLifecycleBehavior(attachment);
        try
        {
            if (owner.IsAttached)
            {
                attachment.Attach();
            }
        }
        catch
        {
            attachment.Dispose();
            throw;
        }

        return attachment;
    }

    public static bool TryGetInstance(UIElement owner, out PrismInstance? instance)
    {
        if (Attachments.TryGetValue(owner, out PrismAttachment? attachment))
        {
            instance = attachment.instance;
            return instance is not null;
        }

        instance = null;
        return false;
    }

    public static bool TryGetRenderState(
        UIElement owner,
        out PrismInstance? instance,
        out PrismCacheOwnerToken cacheOwnerToken)
    {
        if (Attachments.TryGetValue(owner, out PrismAttachment? attachment) &&
            attachment.instance is PrismInstance current)
        {
            instance = current;
            cacheOwnerToken = attachment.cacheOwnerToken;
            return true;
        }

        instance = null;
        cacheOwnerToken = default;
        return false;
    }

    public void Attach()
    {
        if (attached || disposed)
        {
            return;
        }

        PrismInstance created = instanceFactory!()
            ?? throw new InvalidOperationException("A generated Prism factory returned null.");
        instance = created;
        cacheOwnerToken = NextCacheOwnerToken();
        attached = true;
        renderable = UIElementVisibility.IsEffectivelyVisible(owner!);

        if (!renderable)
        {
            return;
        }

        try
        {
            ConnectBindings(created);
        }
        catch
        {
            Detach();
            throw;
        }
    }

    public void OnRenderabilityChanged(bool isRenderable)
    {
        if (!attached || disposed || renderable == isRenderable)
        {
            return;
        }

        if (!isRenderable)
        {
            renderable = false;
            DisconnectBindings();
            return;
        }

        PrismInstance previous = instance!;
        PrismCacheOwnerToken previousCacheOwnerToken = cacheOwnerToken;
        PrismInstance created = instanceFactory!()
            ?? throw new InvalidOperationException("A generated Prism factory returned null.");
        instance = created;
        cacheOwnerToken = NextCacheOwnerToken();
        renderable = true;
        try
        {
            ConnectBindings(created);
        }
        catch
        {
            renderable = false;
            instance = previous;
            cacheOwnerToken = previousCacheOwnerToken;
            throw;
        }
    }

    public void Detach()
    {
        if (!attached)
        {
            return;
        }

        attached = false;
        renderable = false;
        try
        {
            DisconnectBindings();
        }
        finally
        {
            instance = null;
            cacheOwnerToken = default;
        }
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        UIElement? previousOwner = owner;
        Exception? failure = null;
        try
        {
            Detach();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            owner = null;
            instanceFactory = null;
            bindingFactories = null;
            bindings = null;
            instance = null;
            cacheOwnerToken = default;

            if (previousOwner is not null)
            {
                previousOwner.RemoveLifecycleBehavior(this);
                if (Attachments.TryGetValue(previousOwner, out PrismAttachment? current) &&
                    ReferenceEquals(current, this))
                {
                    Attachments.Remove(previousOwner);
                }
            }
        }

        if (failure is not null)
        {
            throw new AggregateException("The Prism attachment failed to dispose cleanly.", failure);
        }
    }

    private void ConnectBindings(PrismInstance target)
    {
        IReadOnlyList<Func<PrismInstance, IDisposable>> factories = bindingFactories!;
        if (factories.Count == 0)
        {
            return;
        }

        List<IDisposable> created = new(factories.Count);
        try
        {
            foreach (Func<PrismInstance, IDisposable> factory in factories)
            {
                created.Add(factory(target)
                    ?? throw new InvalidOperationException(
                        "A generated Prism binding factory returned null."));
            }

            bindings = created.ToArray();
        }
        catch
        {
            bindings = created.ToArray();
            DisconnectBindings();
            throw;
        }
    }

    private void DisconnectBindings()
    {
        IDisposable[] subscriptions = bindings ?? Array.Empty<IDisposable>();
        bindings = null;

        Exception? failure = null;
        for (int index = subscriptions.Length - 1; index >= 0; index--)
        {
            try
            {
                subscriptions[index].Dispose();
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
        }

        if (failure is not null)
        {
            throw new AggregateException(
                "One or more Prism bindings failed to detach.",
                failure);
        }
    }

    private static PrismCacheOwnerToken NextCacheOwnerToken()
    {
        long value = Interlocked.Increment(ref nextCacheOwnerToken);
        if (value <= 0)
        {
            throw new InvalidOperationException("Prism cache owner token space was exhausted.");
        }

        return new PrismCacheOwnerToken(value);
    }
}
