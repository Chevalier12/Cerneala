using Cerneala.UI.Elements;

namespace Cerneala.UI.Controls.Templates;

public sealed class TemplateInstance : IDisposable
{
    private readonly List<TemplateBinding> bindings;
    private Control? owner;
    private bool disposed;

    public TemplateInstance(UIElement? root, IEnumerable<TemplateBinding>? bindings = null)
    {
        Root = root;
        this.bindings = bindings?.ToList() ?? [];
    }

    public UIElement? Root { get; }

    public IReadOnlyList<TemplateBinding> Bindings => bindings;

    public bool IsAttached => owner is not null;

    public void Attach(Control templateOwner)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentNullException.ThrowIfNull(templateOwner);
        if (owner is not null)
        {
            throw new InvalidOperationException("Template instance is already attached.");
        }

        owner = templateOwner;
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
