# Cursor Record

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/Cursor.cs`

Represents a named cursor requested by the retained UI input layer.

```csharp
public readonly record struct Cursor(string Name)
```

## Examples

```csharp
using Cerneala.UI.Input;

Cursor pointer = Cursor.Arrow;
Cursor link = Cursor.Hand;
Cursor text = Cursor.IBeam;
```

## Remarks

`Cursor` is a small value type that carries a cursor name. Platform integrations can map the name to an actual platform cursor.

The built-in cursor presets are `Arrow`, `Hand`, `IBeam`, and `Crosshair`. Custom names can still be represented by constructing a new `Cursor`.

## Constructors

| Name | Description |
| --- | --- |
| `Cursor(string)` | Initializes a cursor value from a cursor name. |

## Properties

| Name | Description |
| --- | --- |
| `Name` | Gets the cursor name. |
| `Arrow` | Gets the default arrow cursor. |
| `Hand` | Gets the hand cursor. |
| `IBeam` | Gets the text editing cursor. |
| `Crosshair` | Gets the crosshair cursor. |

## Applies to

Cerneala retained UI input and platform cursor services.

## See also

- `Cerneala.UI.Input.CursorService`
- `Cerneala.UI.Platform.ICursorService`
