using Cerneala.UI.Accessibility;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.UI.Servo;

internal sealed class ServoActionEngine
{
    private readonly ServoContext context;
    private readonly ServoQueryEngine queryEngine;

    internal ServoActionEngine(ServoContext context, ServoQueryEngine queryEngine)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.queryEngine = queryEngine ?? throw new ArgumentNullException(nameof(queryEngine));
    }

    // Runs one dispatch attempt now and another after each rendered frame until an attempt finds the
    // target actionable. Every attempt resolves the target inside its own input sequence, when the
    // input owner materializes it, so no frame runs between the actionability check and the input it
    // guards. An attempt that fails with not-found or not-actionable has dispatched no input. Any
    // other failure, including an ambiguous target, ends the action at once.
    // Example: a "Save" button that becomes enabled two frames after ClickAsync gets one click, in the
    // frame after it became enabled. A button that stays disabled ends with the operation's
    // cancellation as a ServoTimeoutDiagnosticException ("Last failure: The Servo target Id="save" is
    // disabled.", inner: the last ServoTargetNotActionableException), which ServoOperation turns into
    // ServoTimeoutException when the timeout caused the cancellation.
    // checkBeforeInput runs in every attempt right before the actionability check, inside the same
    // materialization. Its not-found failure is retried like the others; any other failure ends the
    // action before input. Example: ReplaceTextAsync passes ReadTextValue, so a TextBox that appears
    // two frames later is replaced, and a Button fails at once because it has no text value.
    internal async Task DispatchWhenActionableAsync(
        ServoTarget target,
        Func<Func<UIRoot, ServoActionTarget>, Task> dispatch,
        CancellationToken cancellationToken,
        Action<UIRoot>? checkBeforeInput = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(dispatch);
        Func<UIRoot, ServoActionTarget> resolve = root =>
        {
            checkBeforeInput?.Invoke(root);
            return ResolveActionable(root, target);
        };
        using ServoFrameSignal signal = new(context);
        ServoException? lastFailure = null;
        while (true)
        {
            try
            {
                signal.ThrowIfInvalidated();
                Task attempt = await context.InvokeAsync(_ => dispatch(resolve), cancellationToken)
                    .ConfigureAwait(false);
                await attempt.ConfigureAwait(false);
                return;
            }
            catch (ServoException failure) when (
                failure is ServoTargetNotFoundException or ServoTargetNotActionableException)
            {
                lastFailure = failure;
            }
            catch (OperationCanceledException canceled) when (lastFailure is not null)
            {
                throw WithLastFailure(canceled, lastFailure);
            }

            try
            {
                await signal.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException canceled)
            {
                throw WithLastFailure(canceled, lastFailure);
            }
        }
    }

    private static ServoTimeoutDiagnosticException WithLastFailure(
        OperationCanceledException canceled,
        ServoException lastFailure) =>
        new($"Last failure: {lastFailure.Message}", lastFailure, canceled.CancellationToken);

    // Call it only inside a DispatchWhenActionableAsync attempt, so that its not-found and
    // not-actionable failures are retried. Example: DragAsync(source, destination) resolves the
    // destination here, inside the source's attempt, before the pointer moves.
    internal ServoActionTarget ResolveActionable(UIRoot root, ServoTarget target)
    {
        ServoResolvedElement match = queryEngine.Resolve(root, target);
        UIElement element = match.Element;
        if (!ReferenceEquals(element.Root, root))
        {
            throw new ServoTargetNotActionableException(
                $"The Servo target {target} detached before input could be dispatched.");
        }

        if (!UIElementVisibility.IsEffectivelyVisible(element))
        {
            throw new ServoTargetNotActionableException($"The Servo target {target} is not effectively visible.");
        }

        if (!element.IsEnabled)
        {
            throw new ServoTargetNotActionableException($"The Servo target {target} is disabled.");
        }

        LayoutRect bounds = InputCoordinateConverter.GetRootBounds(element);
        if (!HasUsableBounds(bounds))
        {
            throw new ServoTargetNotActionableException($"The Servo target {target} has no usable arranged bounds.");
        }

        float x = bounds.X + (bounds.Width / 2);
        float y = bounds.Y + (bounds.Height / 2);
        HitTestResult? hit = root.InputCache.HitTest(root, x, y);
        if (hit is null || !root.InputCache.EnsureCurrent(root).GetVisualRouteToRoot(hit.Element).Contains(element))
        {
            throw new ServoTargetNotActionableException(
                $"The Servo target {target} center is not hit-testable in the current tree.");
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
                $"The Servo target {target} does not expose a text value, so its text cannot be replaced.");
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
            ? $"The text replacement did not leave the requested password in the Servo target {target}; the target rejected or changed the typed input."
            : $"The text replacement did not leave the requested value in the Servo target {target}. Requested: \"{text}\". Actual: \"{actual.Text}\".");
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
