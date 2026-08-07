# IValueConverter<TIn, TOut> Interface

## Definition
Namespace: `Cerneala.UI.Data`

Assembly/Project: `Cerneala`

Source: `UI/Data/IValueConverter{TIn,TOut}.cs`

Provides the `Cerneala.UI.Data.IValueConverter<TIn, TOut>` API surface.

```csharp
public interface IValueConverter<TIn, TOut>
```

## Type Parameters

| Name | Description |
| --- | --- |
| `TIn` | The input value type. |
| `TOut` | The converted value type. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Convert(TIn value)` | `TOut` | Converts an input value. |
| `ConvertBack(TOut value)` | `TIn` | Converts an output value back to the input type. |

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
