using Cerneala.UI.Core;

namespace Cerneala.UI.Motion.Properties;

public sealed class MotionPropertyStore
{
    private readonly Dictionary<MotionPropertyKey, PendingWrite> writes = [];

    public bool HasPendingWrites => writes.Count > 0;

    internal void StageSet<T>(
        UiObject target,
        UiProperty<T> property,
        T value,
        MotionPropertyInvalidationCategory category)
    {
        MotionPropertyKey key = new(target, property);
        writes[key] = PendingWrite.Set(target, property, value, category);
    }

    internal void StageClear<T>(
        UiObject target,
        UiProperty<T> property,
        MotionPropertyInvalidationCategory category)
    {
        MotionPropertyKey key = new(target, property);
        writes[key] = PendingWrite.Clear(target, property, category);
    }

    internal MotionPropertyFlushResult Flush()
    {
        if (writes.Count == 0)
        {
            return default;
        }

        PendingWrite[] snapshot = writes.Values.ToArray();
        writes.Clear();

        int propertyWrites = 0;
        int renderInvalidations = 0;
        int layoutInvalidations = 0;

        foreach (PendingWrite write in snapshot)
        {
            object? oldValue = write.Target.GetValue(write.Property);
            UiPropertyValueSource oldSource = write.Target.GetValueSource(write.Property);

            if (write.Kind == PendingWriteKind.Set &&
                oldSource == UiPropertyValueSource.Animation &&
                write.Property.AreEqualUntyped(oldValue, write.Value))
            {
                continue;
            }

            if (write.Kind == PendingWriteKind.Set)
            {
                write.Target.SetValueUntyped(write.Property, write.Value, UiPropertyValueSource.Animation);
            }
            else
            {
                write.Target.ClearValueUntyped(write.Property, UiPropertyValueSource.Animation);
            }

            object? newValue = write.Target.GetValue(write.Property);
            if (write.Property.AreEqualUntyped(oldValue, newValue))
            {
                continue;
            }

            propertyWrites++;
            if (write.Category.HasFlag(MotionPropertyInvalidationCategory.Render))
            {
                renderInvalidations++;
            }

            if (write.Category.HasFlag(MotionPropertyInvalidationCategory.Layout))
            {
                layoutInvalidations++;
            }
        }

        return new MotionPropertyFlushResult(propertyWrites, renderInvalidations, layoutInvalidations);
    }

    private enum PendingWriteKind
    {
        Set,
        Clear
    }

    private readonly record struct PendingWrite(
        PendingWriteKind Kind,
        UiObject Target,
        UiProperty Property,
        object? Value,
        MotionPropertyInvalidationCategory Category)
    {
        public static PendingWrite Set<T>(
            UiObject target,
            UiProperty<T> property,
            T value,
            MotionPropertyInvalidationCategory category)
        {
            return new PendingWrite(PendingWriteKind.Set, target, property, value, category);
        }

        public static PendingWrite Clear<T>(
            UiObject target,
            UiProperty<T> property,
            MotionPropertyInvalidationCategory category)
        {
            return new PendingWrite(PendingWriteKind.Clear, target, property, null, category);
        }
    }
}

public readonly record struct MotionPropertyFlushResult(
    int PropertyWrites,
    int RenderInvalidations,
    int LayoutInvalidations);
