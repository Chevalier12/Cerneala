using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class AspectDeclaration
{
    public AspectDeclaration(UiProperty property, AspectValue value, AspectMotion? motion = null, string? diagnosticName = null)
    {
        Property = property ?? throw new ArgumentNullException(nameof(property));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        if (property.ValueType != value.ValueType)
        {
            throw new ArgumentException("Aspect declaration value type must match the UI property value type.", nameof(value));
        }

        Motion = motion;
        DiagnosticName = diagnosticName;
    }

    public UiProperty Property { get; }

    public AspectValue Value { get; }

    public AspectMotion? Motion { get; }

    public string? DiagnosticName { get; }
}
