# BindingMode Enum

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/BindingMode.cs`

Specifies whether a binding copies values only from the source to the target, or in both directions.

```csharp
public enum BindingMode
```

## Examples

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Data;

TextBox textBox = new();
ObservableValue<string> name = new("Ana");
using UiPropertyBinding<string> binding = BindingOperations.BindTwoWay(textBox, TextBox.TextProperty, name);

Console.WriteLine($"{binding.Mode} {textBox.Text}");
// TwoWay Ana

textBox.Text = "Maria";
Console.WriteLine(name.Value);
// Maria

name.Value = "Ion";
Console.WriteLine(textBox.Text);
// Ion
```

With `BindingOperations.BindOneWay` instead, `textBox.Text = "Maria"` changes only the text box; `name.Value` stays `"Ana"`.

## Remarks

`BindingOperations.BindOneWay` and `BindTwoWay` choose the mode for you. `BindingOperations.Bind`, the `UiPropertyBinding<T>` constructor, and the `Binding<T>` constructor take it as a parameter. The default is `OneWay`.

| Type | `OneWay` | `TwoWay` |
| --- | --- | --- |
| `UiPropertyBinding<T>` | Writes the source value to the target property when the binding activates and on every source change. | Also writes each change of the target property back to the source. |
| `Binding<T>` | Calls the target setter on every source change. | `CommitTargetValue` writes a value back to the source. When no source writer is passed, `ObservableValue<T>.SetValue` is used. |

`Binding<T>.CommitTargetValue` throws `InvalidOperationException` when the mode is not `TwoWay`. `UiPropertyBinding<T>` throws `ArgumentOutOfRangeException` for a value outside the enum, for example `(BindingMode)5`. `Binding<T>` does not check the value; any value other than `TwoWay` behaves like `OneWay`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `OneWay` | `0` | Copies values from the source to the target only. |
| `TwoWay` | `1` | Copies values from the source to the target, and from the target back to the source. |

## Applies to

`BindingOperations`, `UiPropertyBinding<T>`, and `Binding<T>`.

## See also

- `Cerneala.UI.Data.BindingOperations`
- `Cerneala.UI.Data.UiPropertyBinding<T>`
- `Cerneala.UI.Data.Binding<T>`
- `Cerneala.UI.Data.ObservableValue<T>`
