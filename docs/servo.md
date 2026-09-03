# Servo UI Automation

Servo is Cerneala's code-first UI automation API. It drives a Cerneala
`Window` or retained `UiHost` in the same process through the framework's
semantic tree, input pipeline, and frame lifecycle. It does not automate other
applications, expose the retained element tree, or provide a script runner.

The canonical API reference starts at the
[`Servo` class page](../docs-site/documentation/classes/Cerneala.UI.Servo.Servo.md).
This guide covers how the pieces fit together.

## Identify Elements

Assign stable IDs in markup with the attached `Servo.Id` property:

```xml
<TextBox Servo.Id="account-name" />
<Button Servo.Id="save-account" Content="Save" />
<TextBlock Servo.Id="save-status" Text="Ready" />
```

Code-first trees use the same property:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Servo;

var saveButton = new Button { Content = "Save" };
Servo.SetId(saveButton, "save-account");
```

IDs and semantic names use exact ordinal comparison. `ServoTarget` values are
immutable descriptors, so keep and reuse them rather than keeping query
results:

```csharp
using Cerneala.UI.Accessibility;
using Cerneala.UI.Servo;

ServoTarget editor = ServoTarget.ById("account-editor");
ServoTarget save = ServoTarget.ByRole(SemanticsRole.Button)
    .WithName("Save")
    .Within(editor);
```

Every query, action, and wait resolves its target again. A `ServoElement` is a
read-only snapshot of semantic and live layout state at query time; it does not
retain or expose a `UIElement`.

## Query Cardinality Is Explicit

Create one Servo instance for the window or host being automated:

```csharp
var servo = new Servo(window);

ServoElement saveButton = await servo.FindAsync(save);
IReadOnlyList<ServoElement> buttons = await servo.FindAllAsync(
    ServoTarget.ByRole(SemanticsRole.Button));
bool hasError = await servo.ExistsAsync(ServoTarget.ById("save-error"));
```

`FindAsync` requires exactly one result and throws
`ServoTargetNotFoundException` or `ServoTargetAmbiguousException` for the two
invalid cardinalities. `FindAllAsync` may return an empty list, and
`ExistsAsync` accepts any nonzero match count.

## Drive Real Input

Pointer and keyboard operations send Cerneala input frames. They do not assign
control properties to imitate user interaction:

```csharp
using Cerneala.UI.Input;
using Cerneala.UI.Servo;

ServoTarget accountName = ServoTarget.ById("account-name");

await servo.ReplaceTextAsync(accountName, "Ada Lovelace");
await servo.ClickAsync(save);
await servo.PressKeyAsync(InputKey.S, ServoModifiers.Control);
```

`TypeIntoAsync` composes a click and text input. `ReplaceTextAsync` composes a
click, `Control+A`, and text input. Target-based actions resolve current bounds,
effective visibility, enabled state, and hit testing before sending input.
Their tasks complete after the final input frame is committed by the retained
host, and after presentation when Servo owns a `Window` context.

Multiple Servo instances for one window or host share that context's serialized
input state. Separate windows keep separate state.

## Wait For Observable State

An action completing does not mean the application is globally idle. Wait for
the state the scenario actually needs:

```csharp
ServoTarget status = ServoTarget.ById("save-status");

await servo.WaitForAsync(status, ServoCondition.Visible);
await servo.WaitUntilAsync(async cancellationToken =>
{
    ServoElement current = await servo.FindAsync(status, cancellationToken);
    return current.Name == "Saved";
});
```

Use `WaitForIdleAsync` only when global quiescence is the contract. It considers
relay work, scheduled work, input state, and Motion; continuous Motion therefore
causes the wait to time out rather than reporting false idle.

The default timeout is five seconds. Configure it when constructing Servo:

```csharp
var servo = new Servo(window, new ServoOptions
{
    DefaultTimeout = TimeSpan.FromSeconds(10)
});
```

Timeouts throw `ServoTimeoutException`. Caller cancellation remains
`OperationCanceledException`, and exceptions from `WaitUntilAsync` predicates
propagate unchanged.

## Capture The Application-Owned Framebuffer

A window-backed Servo can save the full rendered window or the visible pixel
rectangle of a target:

```csharp
await servo.SaveScreenshotAsync("artifacts/account-window.png");
await servo.SaveScreenshotAsync(
    status,
    "artifacts/save-status.png");
```

Both operations use the application's `Window.SaveScreenshot` pipeline. A
target capture crops the fully rendered framebuffer, so it includes overlays
drawn over the target and clips effects outside its arranged bounds. The target
must be effectively visible and intersect the client framebuffer, but it need
not be enabled or hit-testable.

A Servo constructed directly from `UiHost` supports queries, input, and waits.
Its screenshot overloads throw `NotSupportedException` because a standalone
host has no application-owned encoder or capture owner.

## Scope And Failure Model

Servo is intentionally limited to in-process Cerneala UI. It has no process or
window discovery, remote transport, XPath/CSS selector layer, recording, or
JSON/YAML playback surface. Accessibility remains a separate subsystem; Servo
consumes the semantic projection without renaming or exposing accessibility
peers.

Servo-specific failures derive from `ServoException`:

- `ServoTargetNotFoundException` — an operation requiring one target found none;
- `ServoTargetAmbiguousException` — an operation requiring one target found several;
- `ServoTargetNotActionableException` — a resolved target cannot perform the requested input or capture;
- `ServoTimeoutException` — the configured operation deadline expired.

## See Also

- [`Servo` API reference](../docs-site/documentation/classes/Cerneala.UI.Servo.Servo.md)
- [`ServoTarget` API reference](../docs-site/documentation/classes/Cerneala.UI.Servo.ServoTarget.md)
- [`ServoElement` API reference](../docs-site/documentation/classes/Cerneala.UI.Servo.ServoElement.md)
- [Getting Started](getting-started.md)
