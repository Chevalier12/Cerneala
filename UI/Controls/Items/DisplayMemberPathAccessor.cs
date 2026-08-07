using System.Collections.Concurrent;
using System.Reflection;

namespace Cerneala.UI.Controls.Items;

internal static class DisplayMemberPathAccessor
{
    private static readonly ConcurrentDictionary<(Type Type, string Path), PropertyInfo[]> Accessors = new();

    public static object? Resolve(object? item, string path)
    {
        if (item is null || string.IsNullOrEmpty(path))
        {
            return item;
        }

        object? current = item;
        foreach (PropertyInfo property in Accessors.GetOrAdd((item.GetType(), path), CreateAccessor))
        {
            if (current is null)
            {
                return null;
            }

            current = property.GetValue(current);
        }

        return current;
    }

    private static PropertyInfo[] CreateAccessor((Type Type, string Path) key)
    {
        string[] segments = key.Path.Split('.');
        if (segments.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException(
                $"DisplayMemberPath '{key.Path}' contains an empty member segment.");
        }

        List<PropertyInfo> properties = [];
        Type currentType = key.Type;
        foreach (string segment in segments)
        {
            PropertyInfo? property = currentType.GetProperty(
                segment,
                BindingFlags.Instance | BindingFlags.Public);
            if (property?.GetMethod is null || !property.GetMethod.IsPublic || property.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    $"DisplayMemberPath '{key.Path}' cannot resolve readable public property '{segment}' " +
                    $"on type '{currentType.FullName}'.");
            }

            properties.Add(property);
            currentType = property.PropertyType;
        }

        return [.. properties];
    }
}
