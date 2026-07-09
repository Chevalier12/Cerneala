# TextInputSnapshotEvent Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: [`UI/Input/TextInputSnapshotEvent.cs`](../../UI/Input/TextInputSnapshotEvent.cs)

Represents one non-empty text input payload captured during an input frame.

```csharp
public sealed record TextInputSnapshotEvent(string Text)
```

Inheritance:
`Object` -> `TextInputSnapshotEvent`

Implements:
`IEquatable<TextInputSnapshotEvent>`

## Examples

Create a text input event and pass it through an input frame:

```csharp
using Cerneala.UI.Input;

TextInputSnapshotEvent textInput = new("a");

InputFrame frame = new(
    PointerSnapshot.Empty,
    PointerSnapshot.Empty,
    KeyboardSnapshot.Empty,
    KeyboardSnapshot.Empty,
    [textInput]);

string text = frame.TextInputEvents[0].Text;
```

`text` is `"a"`.

## Remarks

`TextInputSnapshotEvent` stores the text value produced by platform text input. `MonoGameInputSource.QueueTextInput` wraps queued text in this record, and `InputFrame` exposes those records through `TextInputEvents`.

`TextInputBridge` dispatches each record to the currently focused element by raising `InputEvents.PreviewTextInputEvent` followed by `InputEvents.TextInputEvent`, both with `TextCompositionEventArgs.Text` set to this record's `Text` value.

The constructor rejects `null` text with `ArgumentNullException` and rejects an empty string with `ArgumentException`. Non-empty strings are stored unchanged.

Because this type is a `record`, equality is value-based and uses the `Text` value.

## Constructors

| Name | Description |
| --- | --- |
| `TextInputSnapshotEvent(string Text)` | Initializes a text input snapshot event with non-empty text. Throws `ArgumentNullException` when `Text` is `null`, and `ArgumentException` when `Text` is empty. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the non-empty text payload for this input event. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out string Text)` | `void` | Deconstructs the record into its `Text` component. |

## Applies To

Cerneala input snapshots in the `Cerneala.UI.Input` namespace.

## See Also

- [`InputFrame`](../../UI/Input/InputFrame.cs)
- [`TextInputBridge`](../../UI/Input/TextInputBridge.cs)
- [`TextCompositionEventArgs`](../../UI/Input/TextCompositionEventArgs.cs)
- [`MonoGameInputSource`](../../UI/Input/MonoGame/MonoGameInputSource.cs)
