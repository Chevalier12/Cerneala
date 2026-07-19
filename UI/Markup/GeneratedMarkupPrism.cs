using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;

namespace Cerneala.UI.Markup;

public static partial class GeneratedMarkup
{
    public static IDisposable AttachPrism(
        UIElement owner,
        Func<PrismInstance> instanceFactory)
    {
        return AttachPrism(
            owner,
            instanceFactory,
            Array.Empty<Func<PrismInstance, IDisposable>>());
    }

    public static IDisposable AttachPrism(
        UIElement owner,
        Func<PrismInstance> instanceFactory,
        IReadOnlyList<Func<PrismInstance, IDisposable>> bindingFactories)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(instanceFactory);
        ArgumentNullException.ThrowIfNull(bindingFactories);
        if (bindingFactories.Any(factory => factory is null))
        {
            throw new ArgumentException("Prism binding factories cannot contain null.", nameof(bindingFactories));
        }

        return PrismAttachment.Set(owner, instanceFactory, bindingFactories);
    }

    public static bool TryGetPrismInstance(UIElement owner, out PrismInstance? instance)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return PrismAttachment.TryGetInstance(owner, out instance);
    }

    public static PrismInstance GetPrismInstance(UIElement owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        return PrismAttachment.TryGetInstance(owner, out PrismInstance? instance)
            ? instance!
            : throw new InvalidOperationException("The element has no attached Prism instance.");
    }

    public static void SetPrismMotionProperty<T>(
        UIElement target,
        Func<PrismInstance, T> getValue,
        Action<PrismInstance, T> setValue,
        T value)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(getValue);
        ArgumentNullException.ThrowIfNull(setValue);
        PrismInstance instance = GetPrismInstance(target);
        _ = getValue(instance);
        setValue(instance, value);
    }

    public static bool GetPrismFilterBoolean(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<bool>(entryStableId, slot));

    public static int GetPrismFilterInteger(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<int>(entryStableId, slot));

    public static float GetPrismFilterNumber(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<float>(entryStableId, slot));

    public static Color GetPrismFilterColor(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Color>(entryStableId, slot));

    public static Vector4 GetPrismFilterVector(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Vector4>(entryStableId, slot));

    public static PrismResourceId GetPrismFilterResource(
        PrismFilterState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot));

    public static void SetPrismFilterBoolean(
        PrismFilterState state,
        int entryStableId,
        int slot,
        bool value) =>
        state.SetValue(new PrismParameterKey<bool>(entryStableId, slot), value);

    public static void SetPrismFilterInteger(
        PrismFilterState state,
        int entryStableId,
        int slot,
        int value) =>
        state.SetValue(new PrismParameterKey<int>(entryStableId, slot), value);

    public static void SetPrismFilterNumber(
        PrismFilterState state,
        int entryStableId,
        int slot,
        float value) =>
        state.SetValue(new PrismParameterKey<float>(entryStableId, slot), value);

    public static void SetPrismFilterColor(
        PrismFilterState state,
        int entryStableId,
        int slot,
        Color value) =>
        state.SetValue(new PrismParameterKey<Color>(entryStableId, slot), value);

    public static void SetPrismFilterVector(
        PrismFilterState state,
        int entryStableId,
        int slot,
        Vector4 value) =>
        state.SetValue(new PrismParameterKey<Vector4>(entryStableId, slot), value);

    public static void SetPrismFilterResource(
        PrismFilterState state,
        int entryStableId,
        int slot,
        PrismResourceId value) =>
        state.SetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot), value);

    public static bool GetPrismStyleBoolean(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<bool>(entryStableId, slot));

    public static int GetPrismStyleInteger(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<int>(entryStableId, slot));

    public static float GetPrismStyleNumber(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<float>(entryStableId, slot));

    public static Color GetPrismStyleColor(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Color>(entryStableId, slot));

    public static Vector4 GetPrismStyleVector(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<Vector4>(entryStableId, slot));

    public static PrismResourceId GetPrismStyleResource(
        PrismStyleState state,
        int entryStableId,
        int slot) =>
        state.GetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot));

    public static void SetPrismStyleBoolean(
        PrismStyleState state,
        int entryStableId,
        int slot,
        bool value) =>
        state.SetValue(new PrismParameterKey<bool>(entryStableId, slot), value);

    public static void SetPrismStyleInteger(
        PrismStyleState state,
        int entryStableId,
        int slot,
        int value) =>
        state.SetValue(new PrismParameterKey<int>(entryStableId, slot), value);

    public static void SetPrismStyleNumber(
        PrismStyleState state,
        int entryStableId,
        int slot,
        float value) =>
        state.SetValue(new PrismParameterKey<float>(entryStableId, slot), value);

    public static void SetPrismStyleColor(
        PrismStyleState state,
        int entryStableId,
        int slot,
        Color value) =>
        state.SetValue(new PrismParameterKey<Color>(entryStableId, slot), value);

    public static void SetPrismStyleVector(
        PrismStyleState state,
        int entryStableId,
        int slot,
        Vector4 value) =>
        state.SetValue(new PrismParameterKey<Vector4>(entryStableId, slot), value);

    public static void SetPrismStyleResource(
        PrismStyleState state,
        int entryStableId,
        int slot,
        PrismResourceId value) =>
        state.SetValue(new PrismParameterKey<PrismResourceId>(entryStableId, slot), value);
}
