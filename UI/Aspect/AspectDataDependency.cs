namespace Cerneala.UI.Aspect;

public sealed record AspectDataDependency(string Name, Type? OwnerType = null, string? PropertyName = null)
{
    public static AspectDataDependency Property<TData, TValue>(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            throw new ArgumentException("Data dependency property name cannot be empty.", nameof(propertyName));
        }

        return new AspectDataDependency($"{typeof(TData).Name}.{propertyName}", typeof(TData), propertyName);
    }

    public static AspectDataDependency Named(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Data dependency name cannot be empty.", nameof(name));
        }

        return new AspectDataDependency(name);
    }
}
