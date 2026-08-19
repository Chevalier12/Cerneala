using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Cerneala.Drawing;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Layout.Panels;
using Cerneala.UI.Media;

namespace Cerneala.PreviewHost;

internal enum PreviewMarkupUpdateResult
{
    Applied,
    Unchanged,
    DeferredInvalidEdit,
    RequiresCompilation
}

internal static class PreviewMarkupHotReload
{
    private const string RootPath = "$";
    private static readonly HashSet<string> CompileOnlyProperties = new(StringComparer.Ordinal)
    {
        "Aspect",
        "Name",
        "TargetType"
    };

    public static PreviewMarkupUpdateResult TryApply(
        UIElement runtimeRoot,
        string currentSource,
        string updatedSource)
    {
        ArgumentNullException.ThrowIfNull(runtimeRoot);
        ArgumentNullException.ThrowIfNull(currentSource);
        ArgumentNullException.ThrowIfNull(updatedSource);

        if (string.Equals(currentSource, updatedSource, StringComparison.Ordinal))
        {
            return PreviewMarkupUpdateResult.Unchanged;
        }

        if (!TryParse(currentSource, out XDocument? current) ||
            !TryParse(updatedSource, out XDocument? updated))
        {
            return PreviewMarkupUpdateResult.DeferredInvalidEdit;
        }

        XElement? currentRoot = current?.Root;
        XElement? updatedRoot = updated?.Root;
        if (currentRoot is null || updatedRoot is null)
        {
            return PreviewMarkupUpdateResult.DeferredInvalidEdit;
        }

        List<AttributeChange> changes = new();
        if (!TryCollectChanges(currentRoot, updatedRoot, RootPath, changes))
        {
            return PreviewMarkupUpdateResult.RequiresCompilation;
        }

        if (changes.Count == 0)
        {
            return PreviewMarkupUpdateResult.Unchanged;
        }

        Dictionary<string, RuntimeTarget> targets = new(StringComparer.Ordinal)
        {
            [RootPath] = new RuntimeTarget(runtimeRoot, runtimeRoot)
        };
        MapElement(currentRoot, runtimeRoot, runtimeRoot, RootPath, targets);

        List<PropertyMutation> mutations = new(changes.Count);
        foreach (AttributeChange change in changes)
        {
            if (CompileOnlyProperties.Contains(change.PropertyName))
            {
                return PreviewMarkupUpdateResult.RequiresCompilation;
            }

            if (!targets.TryGetValue(change.ElementPath, out RuntimeTarget? target) || target is null)
            {
                return PreviewMarkupUpdateResult.RequiresCompilation;
            }

            MutationPreparation preparation = TryPrepareMutation(target, change);
            switch (preparation.Result)
            {
                case PreviewMarkupUpdateResult.Applied:
                    mutations.Add(preparation.Mutation!);
                    break;
                case PreviewMarkupUpdateResult.DeferredInvalidEdit:
                    return PreviewMarkupUpdateResult.DeferredInvalidEdit;
                default:
                    return PreviewMarkupUpdateResult.RequiresCompilation;
            }
        }

        int applied = 0;
        try
        {
            for (; applied < mutations.Count; applied++)
            {
                mutations[applied].Apply();
            }
        }
        catch
        {
            for (int index = applied - 1; index >= 0; index--)
            {
                try
                {
                    mutations[index].Rollback();
                }
                catch
                {
                }
            }

            return PreviewMarkupUpdateResult.DeferredInvalidEdit;
        }

        return PreviewMarkupUpdateResult.Applied;
    }

    private static bool TryParse(string source, out XDocument? document)
    {
        try
        {
            document = XDocument.Parse(source, LoadOptions.PreserveWhitespace);
            return true;
        }
        catch (XmlException)
        {
            document = null;
            return false;
        }
    }

    private static bool TryCollectChanges(
        XElement current,
        XElement updated,
        string path,
        List<AttributeChange> changes)
    {
        if (current.Name != updated.Name)
        {
            return false;
        }

        Dictionary<XName, XAttribute> currentAttributes = current.Attributes()
            .ToDictionary(attribute => attribute.Name);
        Dictionary<XName, XAttribute> updatedAttributes = updated.Attributes()
            .ToDictionary(attribute => attribute.Name);
        if (currentAttributes.Count != updatedAttributes.Count ||
            currentAttributes.Keys.Any(name => !updatedAttributes.ContainsKey(name)))
        {
            return false;
        }

        foreach ((XName name, XAttribute currentAttribute) in currentAttributes)
        {
            XAttribute updatedAttribute = updatedAttributes[name];
            if (string.Equals(currentAttribute.Value, updatedAttribute.Value, StringComparison.Ordinal))
            {
                continue;
            }

            if (currentAttribute.IsNamespaceDeclaration)
            {
                return false;
            }

            changes.Add(new AttributeChange(path, name.LocalName, updatedAttribute.Value));
        }

        if (!string.Equals(DirectText(current), DirectText(updated), StringComparison.Ordinal))
        {
            return false;
        }

        XElement[] currentChildren = current.Elements().ToArray();
        XElement[] updatedChildren = updated.Elements().ToArray();
        if (currentChildren.Length != updatedChildren.Length)
        {
            return false;
        }

        for (int index = 0; index < currentChildren.Length; index++)
        {
            if (!TryCollectChanges(
                    currentChildren[index],
                    updatedChildren[index],
                    ChildPath(path, index),
                    changes))
            {
                return false;
            }
        }

        return true;
    }

    private static string DirectText(XElement element)
    {
        string text = string.Concat(element.Nodes().OfType<XText>().Select(node => node.Value));
        return string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
    }

    private static void MapElement(
        XElement source,
        UIElement runtime,
        UIElement resourceScope,
        string path,
        Dictionary<string, RuntimeTarget> targets)
    {
        UIElement[] runtimeChildren = runtime.LogicalChildren.ToArray();
        int runtimeIndex = 0;
        XElement[] sourceChildren = source.Elements().ToArray();
        for (int sourceIndex = 0; sourceIndex < sourceChildren.Length; sourceIndex++)
        {
            XElement child = sourceChildren[sourceIndex];
            string childPath = ChildPath(path, sourceIndex);
            string childName = child.Name.LocalName;

            if (childName.EndsWith(".Resources", StringComparison.Ordinal))
            {
                MapResources(child, runtime, childPath, targets);
                continue;
            }

            if (childName.Contains('.', StringComparison.Ordinal))
            {
                if (!TryMapObjectCollection(child, runtime, childPath, targets))
                {
                    MapPromotedChildren(
                        child,
                        runtimeChildren,
                        ref runtimeIndex,
                        resourceScope,
                        childPath,
                        targets);
                }

                continue;
            }

            int match = FindRuntimeChild(runtimeChildren, runtimeIndex, childName);
            if (match < 0)
            {
                continue;
            }

            UIElement runtimeChild = runtimeChildren[match];
            runtimeIndex = match + 1;
            targets[childPath] = new RuntimeTarget(runtimeChild, runtimeChild);
            MapElement(child, runtimeChild, runtimeChild, childPath, targets);
        }
    }

    private static void MapResources(
        XElement resources,
        UIElement owner,
        string path,
        Dictionary<string, RuntimeTarget> targets)
    {
        XElement[] children = resources.Elements().ToArray();
        for (int index = 0; index < children.Length; index++)
        {
            XElement child = children[index];
            string? name = child.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name) || !owner.TryFindResource(name, out object? resource) || resource is null)
            {
                continue;
            }

            targets[ChildPath(path, index)] = new RuntimeTarget(resource, owner);
        }
    }

    private static bool TryMapObjectCollection(
        XElement propertyElement,
        object runtimeOwner,
        string path,
        Dictionary<string, RuntimeTarget> targets)
    {
        string localName = propertyElement.Name.LocalName;
        int separator = localName.LastIndexOf('.');
        if (separator < 0 || separator == localName.Length - 1)
        {
            return false;
        }

        string propertyName = localName[(separator + 1)..];
        PropertyInfo? property = runtimeOwner.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (property?.GetValue(runtimeOwner) is not IEnumerable collection)
        {
            return false;
        }

        object[] runtimeItems = collection.Cast<object>().ToArray();
        XElement[] sourceItems = propertyElement.Elements().ToArray();
        int count = Math.Min(runtimeItems.Length, sourceItems.Length);
        for (int index = 0; index < count; index++)
        {
            object runtimeItem = runtimeItems[index];
            if (!TypeMatches(runtimeItem.GetType(), sourceItems[index].Name.LocalName))
            {
                return false;
            }

            UIElement? scope = runtimeOwner as UIElement;
            if (scope is not null)
            {
                targets[ChildPath(path, index)] = new RuntimeTarget(runtimeItem, scope);
            }
        }

        return sourceItems.Length == 0 || count == sourceItems.Length;
    }

    private static void MapPromotedChildren(
        XElement wrapper,
        UIElement[] runtimeChildren,
        ref int runtimeIndex,
        UIElement resourceScope,
        string path,
        Dictionary<string, RuntimeTarget> targets)
    {
        XElement[] children = wrapper.Elements().ToArray();
        for (int index = 0; index < children.Length; index++)
        {
            XElement child = children[index];
            int match = FindRuntimeChild(runtimeChildren, runtimeIndex, child.Name.LocalName);
            if (match < 0)
            {
                continue;
            }

            UIElement runtimeChild = runtimeChildren[match];
            runtimeIndex = match + 1;
            string childPath = ChildPath(path, index);
            targets[childPath] = new RuntimeTarget(runtimeChild, runtimeChild);
            MapElement(child, runtimeChild, runtimeChild, childPath, targets);
        }
    }

    private static int FindRuntimeChild(UIElement[] runtimeChildren, int start, string sourceTypeName)
    {
        for (int index = start; index < runtimeChildren.Length; index++)
        {
            if (TypeMatches(runtimeChildren[index].GetType(), sourceTypeName))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool TypeMatches(Type runtimeType, string sourceTypeName)
    {
        string localName = sourceTypeName.Contains(':', StringComparison.Ordinal)
            ? sourceTypeName[(sourceTypeName.LastIndexOf(':') + 1)..]
            : sourceTypeName;
        return string.Equals(runtimeType.Name, localName, StringComparison.Ordinal) ||
            EnumerateBaseTypes(runtimeType).Any(type => string.Equals(type.Name, localName, StringComparison.Ordinal));
    }

    private static IEnumerable<Type> EnumerateBaseTypes(Type type)
    {
        for (Type? current = type.BaseType; current is not null; current = current.BaseType)
        {
            yield return current;
        }
    }

    private static MutationPreparation TryPrepareMutation(RuntimeTarget target, AttributeChange change)
    {
        if (target.Value is UiObject uiObject && TryFindUiProperty(uiObject, change.PropertyName, out UiProperty? uiProperty))
        {
            UiProperty property = uiProperty!;
            ConversionResult conversion = TryConvert(target.ResourceScope, change.Value, property.ValueType);
            if (!conversion.Success)
            {
                return MutationPreparation.Deferred();
            }

            object? oldLocalValue = uiObject.GetSourceValue(property, UiPropertyValueSource.Local);
            bool hadLocalValue = uiObject.GetValueSource(property) == UiPropertyValueSource.Local || oldLocalValue is not null;
            return MutationPreparation.Applied(new PropertyMutation(
                () => uiObject.SetValueUntyped(property, conversion.Value, UiPropertyValueSource.Local),
                () =>
                {
                    if (hadLocalValue)
                    {
                        uiObject.SetValueUntyped(property, oldLocalValue, UiPropertyValueSource.Local);
                    }
                    else
                    {
                        uiObject.ClearValueUntyped(property, UiPropertyValueSource.Local);
                    }
                }));
        }

        PropertyInfo? clrProperty = target.Value.GetType().GetProperty(
            change.PropertyName,
            BindingFlags.Instance | BindingFlags.Public);
        if (clrProperty is null || !clrProperty.CanRead || !clrProperty.CanWrite || clrProperty.GetIndexParameters().Length != 0)
        {
            return MutationPreparation.RequiresCompilation();
        }

        ConversionResult clrConversion = TryConvert(target.ResourceScope, change.Value, clrProperty.PropertyType);
        if (!clrConversion.Success)
        {
            return MutationPreparation.Deferred();
        }

        object? oldValue = clrProperty.GetValue(target.Value);
        return MutationPreparation.Applied(new PropertyMutation(
            () => clrProperty.SetValue(target.Value, clrConversion.Value),
            () => clrProperty.SetValue(target.Value, oldValue)));
    }

    private static bool TryFindUiProperty(UiObject target, string attributeName, out UiProperty? result)
    {
        string ownerName = string.Empty;
        string propertyName = attributeName;
        int separator = attributeName.LastIndexOf('.');
        if (separator >= 0)
        {
            ownerName = attributeName[..separator];
            propertyName = attributeName[(separator + 1)..];
        }

        Type targetType = target.GetType();
        IEnumerable<UiProperty> candidates = UiPropertyRegistry.GetRegisteredProperties()
            .Where(property => string.Equals(property.Name, propertyName, StringComparison.Ordinal) && !property.IsReadOnly);
        candidates = separator >= 0
            ? candidates.Where(property => string.Equals(property.OwnerType.Name, ownerName, StringComparison.Ordinal))
            : candidates.Where(property => property.OwnerType.IsAssignableFrom(targetType));

        result = candidates
            .OrderByDescending(property => InheritanceDepth(property.OwnerType))
            .FirstOrDefault();
        return result is not null;
    }

    private static int InheritanceDepth(Type type)
    {
        int depth = 0;
        for (Type? current = type; current is not null; current = current.BaseType)
        {
            depth++;
        }

        return depth;
    }

    private static ConversionResult TryConvert(UIElement resourceScope, string rawValue, Type requestedType)
    {
        Type targetType = Nullable.GetUnderlyingType(requestedType) ?? requestedType;
        if (rawValue.StartsWith('$') && rawValue.Length > 1)
        {
            int suffix = rawValue.IndexOf(':');
            string resourceName = suffix < 0 ? rawValue[1..] : rawValue[1..suffix];
            if (resourceScope.TryFindResource(resourceName, out object? resource) &&
                resource is not null &&
                targetType.IsInstanceOfType(resource))
            {
                return ConversionResult.Converted(resource);
            }

            return ConversionResult.Failed();
        }

        if (targetType == typeof(string) || targetType == typeof(object))
        {
            return ConversionResult.Converted(rawValue);
        }

        if (targetType == typeof(bool) && bool.TryParse(rawValue, out bool boolean))
        {
            return ConversionResult.Converted(boolean);
        }

        if (targetType == typeof(float) && TryParseFloat(rawValue, out float single))
        {
            return ConversionResult.Converted(single);
        }

        if (targetType == typeof(double) && double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double @double))
        {
            return ConversionResult.Converted(@double);
        }

        if (targetType == typeof(decimal) && decimal.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal @decimal))
        {
            return ConversionResult.Converted(@decimal);
        }

        if (targetType == typeof(int) && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integer))
        {
            return ConversionResult.Converted(integer);
        }

        if (targetType == typeof(long) && long.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long @long))
        {
            return ConversionResult.Converted(@long);
        }

        if (targetType == typeof(Thickness) && TryParseThickness(rawValue, out Thickness thickness))
        {
            return ConversionResult.Converted(thickness);
        }

        if (targetType == typeof(LayoutPoint) && TryParseFloatParts(rawValue, 2, out float[]? point))
        {
            return ConversionResult.Converted(new LayoutPoint(point[0], point[1]));
        }

        if (targetType == typeof(GridLength) && TryParseGridLength(rawValue, out GridLength gridLength))
        {
            return ConversionResult.Converted(gridLength);
        }

        if (targetType == typeof(Color) && Color.TryParse(rawValue, out Color color))
        {
            return ConversionResult.Converted(color);
        }

        if ((typeof(Brush).IsAssignableFrom(targetType) || targetType == typeof(IDrawBrush)) &&
            Color.TryParse(rawValue, out Color brushColor))
        {
            return ConversionResult.Converted(new SolidColorBrush(brushColor));
        }

        if (targetType.IsEnum && Enum.TryParse(targetType, rawValue, ignoreCase: false, out object? enumeration))
        {
            return ConversionResult.Converted(enumeration);
        }

        try
        {
            TypeConverter converter = TypeDescriptor.GetConverter(targetType);
            if (converter.CanConvertFrom(typeof(string)))
            {
                return ConversionResult.Converted(converter.ConvertFromInvariantString(rawValue));
            }
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException or NotSupportedException)
        {
        }

        return ConversionResult.Failed();
    }

    private static bool TryParseFloat(string value, out float parsed)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed);
    }

    private static bool TryParseFloatParts(string value, int expectedCount, out float[] parts)
    {
        string[] rawParts = value.Split(',', StringSplitOptions.TrimEntries);
        parts = new float[rawParts.Length];
        if (rawParts.Length != expectedCount)
        {
            return false;
        }

        for (int index = 0; index < rawParts.Length; index++)
        {
            if (!TryParseFloat(rawParts[index], out parts[index]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryParseThickness(string value, out Thickness thickness)
    {
        string[] rawParts = value.Split(',', StringSplitOptions.TrimEntries);
        if (rawParts.Length == 1 && TryParseFloat(rawParts[0], out float uniform))
        {
            thickness = new Thickness(uniform);
            return true;
        }

        if (rawParts.Length == 4 && TryParseFloatParts(value, 4, out float[] parts))
        {
            thickness = new Thickness(parts[0], parts[1], parts[2], parts[3]);
            return true;
        }

        thickness = default;
        return false;
    }

    private static bool TryParseGridLength(string value, out GridLength length)
    {
        if (string.Equals(value, "Auto", StringComparison.OrdinalIgnoreCase))
        {
            length = GridLength.Auto;
            return true;
        }

        if (value.EndsWith('*'))
        {
            string factor = value[..^1];
            if (factor.Length == 0)
            {
                length = GridLength.Star;
                return true;
            }

            if (TryParseFloat(factor, out float stars))
            {
                length = GridLength.Stars(stars);
                return true;
            }
        }
        else if (TryParseFloat(value, out float pixels))
        {
            length = GridLength.Pixels(pixels);
            return true;
        }

        length = default;
        return false;
    }

    private static string ChildPath(string parent, int index) => parent + "/" + index.ToString(CultureInfo.InvariantCulture);

    private sealed record AttributeChange(string ElementPath, string PropertyName, string Value);

    private sealed record RuntimeTarget(object Value, UIElement ResourceScope);

    private sealed record PropertyMutation(Action Apply, Action Rollback);

    private sealed record MutationPreparation(PreviewMarkupUpdateResult Result, PropertyMutation? Mutation)
    {
        public static MutationPreparation Applied(PropertyMutation mutation) =>
            new(PreviewMarkupUpdateResult.Applied, mutation);

        public static MutationPreparation Deferred() =>
            new(PreviewMarkupUpdateResult.DeferredInvalidEdit, null);

        public static MutationPreparation RequiresCompilation() =>
            new(PreviewMarkupUpdateResult.RequiresCompilation, null);
    }

    private readonly record struct ConversionResult(bool Success, object? Value)
    {
        public static ConversionResult Converted(object? value) => new(true, value);

        public static ConversionResult Failed() => new(false, null);
    }
}
