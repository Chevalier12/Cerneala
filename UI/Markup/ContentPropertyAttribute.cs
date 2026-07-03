namespace Cerneala.UI.Markup;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class ContentPropertyAttribute(string propertyName) : Attribute
{
    public string PropertyName { get; } = string.IsNullOrWhiteSpace(propertyName)
        ? throw new ArgumentException("Content property name cannot be empty.", nameof(propertyName))
        : propertyName;
}
