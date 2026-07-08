namespace Cerneala.UI.Aspect;

public readonly record struct AspectSpecificity(
    int Component = 0,
    int Slot = 0,
    int Variant = 0,
    int State = 0,
    int Property = 0,
    int Data = 0,
    int Compound = 0,
    int Predicate = 0) : IComparable<AspectSpecificity>
{
    public int CompareTo(AspectSpecificity other)
    {
        int result = Component.CompareTo(other.Component);
        if (result != 0) return result;
        result = Slot.CompareTo(other.Slot);
        if (result != 0) return result;
        result = Variant.CompareTo(other.Variant);
        if (result != 0) return result;
        result = State.CompareTo(other.State);
        if (result != 0) return result;
        result = Property.CompareTo(other.Property);
        if (result != 0) return result;
        result = Data.CompareTo(other.Data);
        if (result != 0) return result;
        result = Compound.CompareTo(other.Compound);
        if (result != 0) return result;
        return Predicate.CompareTo(other.Predicate);
    }

    public static AspectSpecificity operator +(AspectSpecificity left, AspectSpecificity right)
    {
        return new AspectSpecificity(
            left.Component + right.Component,
            left.Slot + right.Slot,
            left.Variant + right.Variant,
            left.State + right.State,
            left.Property + right.Property,
            left.Data + right.Data,
            left.Compound + right.Compound,
            left.Predicate + right.Predicate);
    }
}
