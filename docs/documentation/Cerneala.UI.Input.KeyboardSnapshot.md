# KeyboardSnapshot Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyboardSnapshot.cs`

Represents an immutable snapshot of the keyboard keys that are currently down.

```csharp
public sealed class KeyboardSnapshot
```

Inheritance:
`Object` -> `KeyboardSnapshot`

## Examples

Create a snapshot from down keys and query it directly:

```csharp
using Cerneala.UI.Input;

KeyboardSnapshot snapshot = KeyboardSnapshot.FromDownKeys(
    new[] { InputKey.Space, InputKey.LeftCtrl, InputKey.None, InputKey.Unknown });

bool spaceIsDown = snapshot.IsDown(InputKey.Space);
bool noneIsDown = snapshot.IsDown(InputKey.None);
```

`spaceIsDown` is `true`. `noneIsDown` is `false` because `FromDownKeys` ignores `InputKey.None` and `InputKey.Unknown`.

## Remarks

`KeyboardSnapshot` stores the set of keys that are down at one input sampling point. It is used by `InputFrame` to compare previous and current keyboard state for operations such as `IsDown`, `IsPressed`, and `IsReleased`.

Instances are created through `Empty` or `FromDownKeys`. `FromDownKeys` enumerates the supplied keys immediately, removes duplicates through a set, and filters out `InputKey.None` and `InputKey.Unknown`.

The type has no public constructor. Passing `null` to `FromDownKeys` throws `ArgumentNullException`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Empty` | `KeyboardSnapshot` | Gets a snapshot with no keys down. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `FromDownKeys(IEnumerable<InputKey> keys)` | `KeyboardSnapshot` | Creates a snapshot from the supplied down keys, excluding `InputKey.None` and `InputKey.Unknown`. |
| `IsDown(InputKey key)` | `bool` | Returns `true` when `key` is present in this snapshot. |

## Applies to

Project: `Cerneala`

Input namespace: `Cerneala.UI.Input`

## See also

- `InputFrame`
- `InputKey`
- `UI/Input/KeyboardSnapshot.cs`
