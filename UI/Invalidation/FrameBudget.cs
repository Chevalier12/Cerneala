namespace Cerneala.UI.Invalidation;

public readonly record struct FrameBudget(int? MaxWorkItems)
{
    public static FrameBudget ProcessAll { get; } = new(null);

    public bool DefersWork => false;
}
