using Cerneala.UI.Core;
using Cerneala.UI.Elements;

namespace Cerneala.UI.Automation;

public static class AutomationProperties
{
    public static readonly UiProperty<string?> AutomationIdProperty = UiProperty<string?>.Register(
        "AutomationId",
        typeof(AutomationProperties),
        new UiPropertyMetadata<string?>(
            null,
            UiPropertyOptions.AffectsSemantics,
            coerceValue: (_, value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()));

    public static string? GetAutomationId(UIElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetValue(AutomationIdProperty);
    }

    public static void SetAutomationId(UIElement element, string? automationId)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.SetValue(AutomationIdProperty, automationId);
    }
}
