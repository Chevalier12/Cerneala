using System.Globalization;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Prism.Definitions;

namespace Cerneala.UI.Prism.Runtime;

internal struct PrismValueCounts
{
    public int Booleans;
    public int Integers;
    public int Numbers;
    public int Colors;
    public int Vectors;
    public int Resources;

    public void Add(ReadOnlySpan<PrismCatalogPropertyDescriptor> properties)
    {
        foreach (PrismCatalogPropertyDescriptor property in properties)
        {
            switch (property.ValueType)
            {
                case PrismCatalogValueType.Boolean:
                    Booleans++;
                    break;
                case PrismCatalogValueType.Integer:
                case PrismCatalogValueType.Symbol:
                    Integers++;
                    break;
                case PrismCatalogValueType.Number:
                    Numbers++;
                    break;
                case PrismCatalogValueType.Color:
                    Colors++;
                    break;
                case PrismCatalogValueType.Vector:
                    Vectors++;
                    break;
                case PrismCatalogValueType.Resource:
                    Resources++;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown Prism value type '{property.ValueType}'.");
            }
        }
    }
}

internal readonly record struct PrismValueSlice(
    int BooleanOffset,
    int IntegerOffset,
    int NumberOffset,
    int ColorOffset,
    int VectorOffset,
    int ResourceOffset);

internal sealed class PrismParameterStore
{
    private readonly bool[] booleans;
    private readonly int[] integers;
    private readonly float[] numbers;
    private readonly Color[] colors;
    private readonly Vector4[] vectors;
    private readonly PrismResourceId[] resources;

    public PrismParameterStore(PrismValueCounts counts)
    {
        booleans = new bool[counts.Booleans];
        integers = new int[counts.Integers];
        numbers = new float[counts.Numbers];
        colors = new Color[counts.Colors];
        vectors = new Vector4[counts.Vectors];
        resources = new PrismResourceId[counts.Resources];
    }

    public bool Get(PrismValueSlice slice, PrismParameterKey<bool> key) =>
        booleans[slice.BooleanOffset + key.Slot];

    public int Get(PrismValueSlice slice, PrismParameterKey<int> key) =>
        integers[slice.IntegerOffset + key.Slot];

    public float Get(PrismValueSlice slice, PrismParameterKey<float> key) =>
        numbers[slice.NumberOffset + key.Slot];

    public Color Get(PrismValueSlice slice, PrismParameterKey<Color> key) =>
        colors[slice.ColorOffset + key.Slot];

    public Vector4 Get(PrismValueSlice slice, PrismParameterKey<Vector4> key) =>
        vectors[slice.VectorOffset + key.Slot];

    public PrismResourceId Get(PrismValueSlice slice, PrismParameterKey<PrismResourceId> key) =>
        resources[slice.ResourceOffset + key.Slot];

    public bool Set(PrismValueSlice slice, PrismParameterKey<bool> key, bool value)
    {
        int index = slice.BooleanOffset + key.Slot;
        if (booleans[index] == value)
        {
            return false;
        }

        booleans[index] = value;
        return true;
    }

    public bool Set(PrismValueSlice slice, PrismParameterKey<int> key, int value)
    {
        int index = slice.IntegerOffset + key.Slot;
        if (integers[index] == value)
        {
            return false;
        }

        integers[index] = value;
        return true;
    }

    public bool Set(PrismValueSlice slice, PrismParameterKey<float> key, float value)
    {
        int index = slice.NumberOffset + key.Slot;
        if (numbers[index].Equals(value))
        {
            return false;
        }

        numbers[index] = value;
        return true;
    }

    public bool Set(PrismValueSlice slice, PrismParameterKey<Color> key, Color value)
    {
        int index = slice.ColorOffset + key.Slot;
        if (colors[index] == value)
        {
            return false;
        }

        colors[index] = value;
        return true;
    }

    public bool Set(PrismValueSlice slice, PrismParameterKey<Vector4> key, Vector4 value)
    {
        int index = slice.VectorOffset + key.Slot;
        if (vectors[index] == value)
        {
            return false;
        }

        vectors[index] = value;
        return true;
    }

    public bool Set(
        PrismValueSlice slice,
        PrismParameterKey<PrismResourceId> key,
        PrismResourceId value)
    {
        int index = slice.ResourceOffset + key.Slot;
        if (resources[index] == value)
        {
            return false;
        }

        resources[index] = value;
        return true;
    }

    public void Initialize(
        PrismValueSlice slice,
        ReadOnlySpan<PrismCatalogPropertyDescriptor> properties)
    {
        foreach (PrismCatalogPropertyDescriptor property in properties)
        {
            if (property.DefaultValue is null)
            {
                continue;
            }

            switch (property.ValueType)
            {
                case PrismCatalogValueType.Boolean:
                    booleans[slice.BooleanOffset + property.TypeSlot] =
                        bool.Parse(property.DefaultValue);
                    break;
                case PrismCatalogValueType.Integer:
                    integers[slice.IntegerOffset + property.TypeSlot] =
                        int.Parse(property.DefaultValue, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    break;
                case PrismCatalogValueType.Number:
                    numbers[slice.NumberOffset + property.TypeSlot] =
                        float.Parse(property.DefaultValue, NumberStyles.Float, CultureInfo.InvariantCulture);
                    break;
                case PrismCatalogValueType.Color:
                    if (!Color.TryParse(property.DefaultValue, out Color color))
                    {
                        throw InvalidDefault(property);
                    }
                    colors[slice.ColorOffset + property.TypeSlot] = color;
                    break;
                case PrismCatalogValueType.Vector:
                    vectors[slice.VectorOffset + property.TypeSlot] =
                        ParseVector(property.DefaultValue, property);
                    break;
                case PrismCatalogValueType.Symbol:
                    integers[slice.IntegerOffset + property.TypeSlot] =
                        PrismCatalogRuntime.ResolveSymbol(property.Name, property.DefaultValue);
                    break;
                case PrismCatalogValueType.Resource:
                    if (!string.Equals(property.DefaultValue, "null", StringComparison.Ordinal))
                    {
                        throw InvalidDefault(property);
                    }
                    break;
                default:
                    throw InvalidDefault(property);
            }
        }
    }

    public bool CopyFromIfDifferent(PrismParameterStore source)
    {
        if (ContentEquals(source))
        {
            return false;
        }

        source.booleans.CopyTo(booleans, 0);
        source.integers.CopyTo(integers, 0);
        source.numbers.CopyTo(numbers, 0);
        source.colors.CopyTo(colors, 0);
        source.vectors.CopyTo(vectors, 0);
        source.resources.CopyTo(resources, 0);
        return true;
    }

    public bool ContentEquals(PrismParameterStore other)
    {
        return booleans.AsSpan().SequenceEqual(other.booleans) &&
            integers.AsSpan().SequenceEqual(other.integers) &&
            numbers.AsSpan().SequenceEqual(other.numbers) &&
            colors.AsSpan().SequenceEqual(other.colors) &&
            vectors.AsSpan().SequenceEqual(other.vectors) &&
            resources.AsSpan().SequenceEqual(other.resources);
    }

    private static Vector4 ParseVector(
        string text,
        PrismCatalogPropertyDescriptor property)
    {
        Span<float> values = stackalloc float[4];
        string[] components = text.Split(',');
        if (components.Length is < 2 or > 4)
        {
            throw InvalidDefault(property);
        }

        for (int index = 0; index < components.Length; index++)
        {
            if (!float.TryParse(
                    components[index],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out values[index]))
            {
                throw InvalidDefault(property);
            }
        }

        return new Vector4(values[0], values[1], values[2], values[3]);
    }

    private static InvalidOperationException InvalidDefault(
        PrismCatalogPropertyDescriptor property) =>
        new($"Catalog default '{property.DefaultValue}' is invalid for '{property.Name}'.");
}

internal sealed class PrismValueAllocator
{
    private readonly PrismParameterStore store;
    private PrismValueCounts offsets;

    public PrismValueAllocator(PrismParameterStore store)
    {
        this.store = store;
    }

    public PrismValueSlice Allocate(ReadOnlySpan<PrismCatalogPropertyDescriptor> properties)
    {
        PrismValueSlice slice = new(
            offsets.Booleans,
            offsets.Integers,
            offsets.Numbers,
            offsets.Colors,
            offsets.Vectors,
            offsets.Resources);
        store.Initialize(slice, properties);
        offsets.Add(properties);
        return slice;
    }
}

internal static class PrismCatalogRuntime
{
    public static PrismCatalogEntryDescriptor GetEntry(int stableId)
    {
        int index = stableId - 1;
        if ((uint)index >= (uint)PrismCatalogGenerated.Entries.Length ||
            PrismCatalogGenerated.Entries[index].StableId != stableId)
        {
            throw new ArgumentOutOfRangeException(nameof(stableId), stableId, "Unknown Prism catalog entry.");
        }

        return PrismCatalogGenerated.Entries[index];
    }

    public static int ResolveSymbol(string propertyName, string symbol)
    {
        if (propertyName == "BlendMode" &&
            Enum.TryParse(symbol, ignoreCase: false, out PrismBlendMode blendMode))
        {
            return (int)blendMode;
        }
        if (propertyName == "WorkingColorProfile" &&
            Enum.TryParse(symbol, ignoreCase: false, out PrismColorProfile colorProfile))
        {
            return (int)colorProfile;
        }
        if (propertyName == "Channel" &&
            Enum.TryParse(symbol, ignoreCase: false, out PrismMaskChannel maskChannel))
        {
            return (int)maskChannel;
        }
        if (propertyName == "BlendChannels" &&
            string.Equals(symbol, "RGBA", StringComparison.Ordinal))
        {
            return (int)PrismBlendChannels.Rgba;
        }
        if (propertyName == "Knockout" &&
            Enum.TryParse(symbol, ignoreCase: false, out PrismKnockout knockout))
        {
            return (int)knockout;
        }
        if (propertyName == "BlendIfChannel" &&
            Enum.TryParse(symbol, ignoreCase: false, out PrismBlendIfChannel blendIfChannel))
        {
            return (int)blendIfChannel;
        }

        return StableSymbolId(symbol);
    }

    private static int StableSymbolId(string symbol)
    {
        const uint offset = 2166136261;
        const uint prime = 16777619;
        uint hash = offset;
        foreach (char character in symbol)
        {
            hash ^= character;
            hash *= prime;
        }

        return unchecked((int)hash);
    }
}
