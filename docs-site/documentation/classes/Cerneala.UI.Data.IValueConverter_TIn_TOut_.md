# IValueConverter<TIn, TOut> Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IValueConverter{TIn,TOut}.cs`

Converts a value from a source type to a target type, and back.

```csharp
public interface IValueConverter<TIn, TOut>
```

## Examples

A one-way binding that shows a count as text:

```csharp
using Cerneala.UI.Data;

ObservableValue<int> items = new(3);
string shown = "";
using Binding<int> binding = Binding<int>.OneWayConverted(
    items,
    (string text) => shown = text,
    new CountToText());

Console.WriteLine(shown);
// 3 items

items.Value = 1;
Console.WriteLine(shown);
// 1 item

sealed class CountToText : IValueConverter<int, string>
{
    public string Convert(int value) => value == 1 ? "1 item" : value + " items";

    public int ConvertBack(string value) => int.Parse(value.Split(' ')[0]);
}
```

## Remarks

Cerneala has no built-in implementation; you write the converter.

`Binding<T>.OneWayConverted` is the only Cerneala API that takes a converter. It calls `Convert` with the source value when the binding is created (unless `updateTargetImmediately` is `false`) and on every source change, and passes the result to the target setter. No Cerneala code calls `ConvertBack`.

An exception thrown by `Convert` propagates to the code that changed the source, or to the `OneWayConverted` call for the first update.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `Convert(TIn value)` | `TOut` | Converts a source value to the target type. |
| `ConvertBack(TOut value)` | `TIn` | Converts a target value back to the source type. |

## Applies to

`Binding<T>.OneWayConverted`.

## See also

- `Cerneala.UI.Data.Binding<T>`
- `Cerneala.UI.Data.ObservableValue<T>`
