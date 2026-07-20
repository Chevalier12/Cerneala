using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.UI.Prism.Runtime;

internal static class PrismCatalogParameterValidation
{
    public static void Validate(
        PrismParameterKey<bool> key,
        bool value) =>
        Property(key.EntryStableId, key.Slot, PrismCatalogValueType.Boolean);

    public static void Validate(
        PrismParameterKey<int> key,
        int value)
    {
        PrismCatalogPropertyDescriptor property =
            Property(
                key.EntryStableId,
                key.Slot,
                PrismCatalogValueType.Integer,
                PrismCatalogValueType.Symbol);
        ValidateRange(property, value);
    }

    public static void Validate(
        PrismParameterKey<float> key,
        float value)
    {
        PrismCatalogPropertyDescriptor property =
            Property(
                key.EntryStableId,
                key.Slot,
                PrismCatalogValueType.Number);
        if (!float.IsFinite(value))
        {
            throw Invalid(property, value, "must be finite");
        }
        ValidateRange(property, value);
    }

    public static void Validate(
        PrismParameterKey<Color> key,
        Color value) =>
        Property(key.EntryStableId, key.Slot, PrismCatalogValueType.Color);

    public static void Validate(
        PrismParameterKey<Vector4> key,
        Vector4 value)
    {
        PrismCatalogPropertyDescriptor property =
            Property(
                key.EntryStableId,
                key.Slot,
                PrismCatalogValueType.Vector);
        if (!IsFinite(value))
        {
            throw Invalid(
                property,
                value,
                "must contain only finite components");
        }
    }

    public static void Validate(
        PrismParameterKey<PrismResourceId> key,
        PrismResourceId value)
    {
        PrismCatalogPropertyDescriptor property =
            Property(
                key.EntryStableId,
                key.Slot,
                PrismCatalogValueType.Resource);
        if (property.Required && value.Value <= 0)
        {
            throw Invalid(
                property,
                value,
                "requires a non-default resource");
        }
    }

    private static PrismCatalogPropertyDescriptor Property(
        int stableId,
        int typeSlot,
        params PrismCatalogValueType[] acceptedTypes)
    {
        PrismCatalogEntryDescriptor entry =
            PrismCatalogRuntime.GetEntry(stableId);
        foreach (PrismCatalogPropertyDescriptor property in
            entry.Properties)
        {
            if (property.TypeSlot == typeSlot &&
                acceptedTypes.Contains(property.ValueType))
            {
                return property;
            }
        }

        throw new ArgumentException(
            $"Catalog entry '{entry.Id}' has no compatible generated parameter at type slot {typeSlot}.");
    }

    private static void ValidateRange(
        PrismCatalogPropertyDescriptor property,
        double value)
    {
        string[] domain = property.Domain.Split(':');
        if (domain.Length != 3 ||
            domain[0] != "range")
        {
            return;
        }

        if (domain[1].Length > 0 &&
            value < double.Parse(
                domain[1],
                NumberStyles.Float,
                CultureInfo.InvariantCulture))
        {
            throw Invalid(
                property,
                value,
                $"must be at least {domain[1]}");
        }
        if (domain[2].Length > 0 &&
            value > double.Parse(
                domain[2],
                NumberStyles.Float,
                CultureInfo.InvariantCulture))
        {
            throw Invalid(
                property,
                value,
                $"must be at most {domain[2]}");
        }
    }

    private static ArgumentOutOfRangeException Invalid(
        PrismCatalogPropertyDescriptor property,
        object value,
        string requirement) =>
        new(
            property.Name,
            value,
            $"Prism property '{property.Name}' {requirement}.");

    private static bool IsFinite(Vector4 value) =>
        float.IsFinite(value.X) &&
        float.IsFinite(value.Y) &&
        float.IsFinite(value.Z) &&
        float.IsFinite(value.W);
}
