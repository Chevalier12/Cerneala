# AspectState Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectState.cs`

Represents a named visual or interaction state that aspect conditions can match against an `AspectStateSet`.

```csharp
public sealed class AspectState : IEquatable<AspectState>
```

Inheritance:
`object` -> `AspectState`

Implements:
`IEquatable<AspectState>`

## Examples

Match an aspect condition against a built-in state:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCondition condition = AspectCondition.State(AspectState.Hover);
AspectMatchContext context = new(
    new Button(),
    states: AspectStateSet.Empty.Add(AspectState.Hover));

bool matches = condition.Evaluate(context).Matches;
```

Create and store a custom named state:

```csharp
using Cerneala.UI.Aspect;

AspectState danger = AspectState.Create("danger");
AspectStateSet states = AspectStateSet.Empty.Add(danger);

bool containsDanger = states.Contains(danger);
```

## Remarks

`AspectState` is a small immutable value object around a non-empty string `Name`. Equality, hash codes, and `ToString()` are based on the state name using ordinal string comparison.

`AspectStateSet.FromElement(UIElement)` derives the built-in hover, pressed, focus, focus-within, disabled, selected, checked, and expanded states from the corresponding element or control properties.

`Checked` is active when `ToggleButton.IsChecked` is `true` (including on `CheckBox` and other derived controls), or when `RadioButton.IsChecked` is `true`. `Expanded` is active when `MenuItem.IsSubmenuOpen` or `ComboBox.IsDropDownOpen` is `true`. False values do not activate either state. Changes to these source properties invalidate aspect resolution, so state-conditioned rules on attached controls are re-evaluated on the next processed frame.

Use `Create(string)` when a control or component needs a domain-specific state. The constructor is private, so callers cannot subclass or instantiate arbitrary states directly. State names must not be null, empty, or whitespace.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Checked` | `AspectState` | Gets the built-in `checked` state, derived from `ToggleButton.IsChecked` or `RadioButton.IsChecked`. |
| `Disabled` | `AspectState` | Gets the built-in `disabled` state. |
| `Expanded` | `AspectState` | Gets the built-in `expanded` state, derived from `MenuItem.IsSubmenuOpen` or `ComboBox.IsDropDownOpen`. |
| `Focus` | `AspectState` | Gets the built-in `focus` state. |
| `FocusWithin` | `AspectState` | Gets the built-in `focus-within` state. |
| `Hover` | `AspectState` | Gets the built-in `hover` state. |
| `Name` | `string` | Gets the state name used for equality, hashing, diagnostics, and string conversion. |
| `Pressed` | `AspectState` | Gets the built-in `pressed` state. |
| `Selected` | `AspectState` | Gets the built-in `selected` state. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create(string name)` | `AspectState` | Creates a custom state with the supplied non-empty name. |
| `Equals(AspectState? other)` | `bool` | Returns `true` when `other` has the same name using ordinal string comparison. |
| `Equals(object? obj)` | `bool` | Returns `true` when `obj` is an `AspectState` with the same name. |
| `GetHashCode()` | `int` | Returns an ordinal string hash code for `Name`. |
| `ToString()` | `string` | Returns `Name`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Create(string name)` | `ArgumentException` | `name` is null, empty, or whitespace. |

## Applies to

Cerneala UI aspect conditions, aspect state sets, and aspect rule matching.

## See also

- `Cerneala.UI.Aspect.AspectCondition`
- `Cerneala.UI.Aspect.AspectStateSet`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectTarget`
