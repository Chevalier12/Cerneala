using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class ComponentTemplateInstance : IDisposable
{
    private readonly List<TemplateBinding> bindings;
    private readonly List<TemplateTokenBinding> tokenBindings;
    private Control? owner;
    private bool disposed;

    public ComponentTemplateInstance(
        UIElement? root,
        IEnumerable<TemplateBinding>? bindings,
        IEnumerable<TemplateTokenBinding>? tokenBindings,
        TemplateSlotMap slots,
        TemplatePartMap parts)
    {
        Root = root;
        this.bindings = bindings?.ToList() ?? [];
        this.tokenBindings = tokenBindings?.ToList() ?? [];
        Slots = slots;
        Parts = parts;
    }

    public UIElement? Root { get; }

    public TemplateSlotMap Slots { get; }

    public TemplatePartMap Parts { get; }

    public void Attach(Control templateOwner)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (owner is not null)
        {
            throw new InvalidOperationException("Component template instance is already attached.");
        }

        owner = templateOwner ?? throw new ArgumentNullException(nameof(templateOwner));
        try
        {
            if (Root is not null)
            {
                TemplateChildOwner.Attach(templateOwner, Root);
            }

            foreach (TemplateBinding binding in bindings)
            {
                binding.Attach(templateOwner);
            }

            foreach (TemplateTokenBinding binding in tokenBindings)
            {
                binding.Attach();
            }
        }
        catch
        {
            Detach();
            throw;
        }
    }

    public void Detach()
    {
        if (owner is null)
        {
            return;
        }

        foreach (TemplateTokenBinding binding in tokenBindings)
        {
            binding.Detach();
        }

        foreach (TemplateBinding binding in bindings)
        {
            binding.Detach();
        }

        if (Root is not null)
        {
            TemplateChildOwner.Detach(owner, Root);
        }

        owner = null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        Detach();
        disposed = true;
    }
}

internal static class TemplateChildOwner
{
    public static void Attach(Control owner, UIElement child)
    {
        ContentControl.ValidateCanOwnChild(owner, child);
        owner.LogicalChildren.Add(child);
        try
        {
            owner.VisualChildren.Add(child);
        }
        catch
        {
            owner.LogicalChildren.Remove(child);
            throw;
        }
    }

    public static void Detach(Control owner, UIElement child)
    {
        owner.VisualChildren.Remove(child);
        owner.LogicalChildren.Remove(child);
    }
}
