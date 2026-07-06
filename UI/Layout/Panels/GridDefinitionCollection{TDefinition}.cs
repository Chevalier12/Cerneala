using System.Collections;

namespace Cerneala.UI.Layout.Panels;

public sealed class GridDefinitionCollection<TDefinition> : IList<TDefinition>, IReadOnlyList<TDefinition>
    where TDefinition : class
{
    private readonly Grid owner;
    private readonly Action<TDefinition, Grid> attach;
    private readonly Action<TDefinition, Grid> detach;
    private readonly List<TDefinition> definitions = [];

    internal GridDefinitionCollection(
        Grid owner,
        Action<TDefinition, Grid> attach,
        Action<TDefinition, Grid> detach)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        this.attach = attach ?? throw new ArgumentNullException(nameof(attach));
        this.detach = detach ?? throw new ArgumentNullException(nameof(detach));
    }

    public int Count => definitions.Count;

    public bool IsReadOnly => false;

    public TDefinition this[int index]
    {
        get => definitions[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            TDefinition oldDefinition = definitions[index];
            if (ReferenceEquals(oldDefinition, value))
            {
                return;
            }

            ThrowIfAlreadyContained(value);
            attach(value, owner);
            detach(oldDefinition, owner);
            definitions[index] = value;
            owner.InvalidateDefinitions("Grid definition replaced");
        }
    }

    public void Add(TDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        ThrowIfAlreadyContained(item);
        attach(item, owner);
        definitions.Add(item);
        owner.InvalidateDefinitions("Grid definition added");
    }

    public void Clear()
    {
        if (definitions.Count == 0)
        {
            return;
        }

        foreach (TDefinition definition in definitions)
        {
            detach(definition, owner);
        }

        definitions.Clear();
        owner.InvalidateDefinitions("Grid definitions cleared");
    }

    public bool Contains(TDefinition item)
    {
        return definitions.Contains(item);
    }

    public void CopyTo(TDefinition[] array, int arrayIndex)
    {
        definitions.CopyTo(array, arrayIndex);
    }

    public IEnumerator<TDefinition> GetEnumerator()
    {
        return definitions.GetEnumerator();
    }

    public int IndexOf(TDefinition item)
    {
        return definitions.IndexOf(item);
    }

    public void Insert(int index, TDefinition item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (index < 0 || index > definitions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        ThrowIfAlreadyContained(item);
        attach(item, owner);
        definitions.Insert(index, item);
        owner.InvalidateDefinitions("Grid definition inserted");
    }

    public bool Remove(TDefinition item)
    {
        int index = definitions.IndexOf(item);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    public void RemoveAt(int index)
    {
        TDefinition oldDefinition = definitions[index];
        definitions.RemoveAt(index);
        detach(oldDefinition, owner);
        owner.InvalidateDefinitions("Grid definition removed");
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private void ThrowIfAlreadyContained(TDefinition item)
    {
        if (definitions.Any(definition => ReferenceEquals(definition, item)))
        {
            throw new InvalidOperationException("Grid definition is already in this collection.");
        }
    }
}
