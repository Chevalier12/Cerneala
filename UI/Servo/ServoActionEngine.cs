using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Servo;

internal sealed class ServoActionEngine
{
    private readonly ServoQueryEngine queryEngine;

    internal ServoActionEngine(ServoQueryEngine queryEngine)
    {
        this.queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
    }

    internal ServoActionTarget ResolveActionable(UIRoot root, ServoTarget target)
    {
        ServoResolvedElement match = queryEngine.Resolve(root, target);
        UIElement element = match.Element;
        if (!ReferenceEquals(element.Root, root))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target detached before input could be dispatched.");
        }

        if (!UIElementVisibility.IsEffectivelyVisible(element))
        {
            throw new ServoTargetNotActionableException("The Servo target is not effectively visible.");
        }

        if (!element.IsEnabled)
        {
            throw new ServoTargetNotActionableException("The Servo target is disabled.");
        }

        LayoutRect bounds = InputCoordinateConverter.GetRootBounds(element);
        if (!HasUsableBounds(bounds))
        {
            throw new ServoTargetNotActionableException("The Servo target has no usable arranged bounds.");
        }

        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);
        HitTestResult? hit = root.InputCache.HitTest(root, x, y);
        if (hit is null || !root.InputCache.EnsureCurrent(root).GetVisualRouteToRoot(hit.Element).Contains(element))
        {
            throw new ServoTargetNotActionableException(
                "The Servo target center is not hit-testable in the current tree.");
        }

        return new ServoActionTarget(element, x, y);
    }

    // The text a text replacement must leave: the semantic Value of the target, or the password of a
    // PasswordBox, whose semantic Value is always null so that the projection never reveals it.
    // Example: a TextBox holding "abc" reads "abc"; a Button has no Value and throws ServoException.
    internal ServoTextValue ReadTextValue(UIRoot root, ServoTarget target)
    {
        ServoResolvedElement match = queryEngine.Resolve(root, target);
        if (match.Element is PasswordBox passwordBox)
        {
            return new ServoTextValue(passwordBox.Password, IsSecret: true);
        }

        if (!match.Node.Properties.TryGetValue(SemanticsProperty.Value, out object? value) ||
            value is not (string or null))
        {
            throw new ServoException(
                "The Servo target does not expose a text value, so its text cannot be replaced.");
        }

        return new ServoTextValue((string?)value ?? string.Empty, IsSecret: false);
    }

    internal void VerifyReplacedText(UIRoot root, ServoTarget target, string text)
    {
        ServoTextValue actual = ReadTextValue(root, target);
        if (string.Equals(actual.Text, text, StringComparison.Ordinal))
        {
            return;
        }

        throw new ServoException(actual.IsSecret
            ? "The text replacement did not leave the requested password in the Servo target; the target rejected or changed the typed input."
            : $"The text replacement did not leave the requested value in the Servo target. Requested: \"{text}\". Actual: \"{actual.Text}\".");
    }

    private static bool HasUsableBounds(LayoutRect bounds)
    {
        return float.IsFinite(bounds.X) &&
            float.IsFinite(bounds.Y) &&
            float.IsFinite(bounds.Width) &&
            float.IsFinite(bounds.Height) &&
            bounds.Width > 0 &&
            bounds.Height > 0;
    }

}

internal readonly record struct ServoActionTarget(UIElement Element, float X, float Y);

internal readonly record struct ServoTextValue(string Text, bool IsSecret);
