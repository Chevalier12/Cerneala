# GeneratedUiFactory Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/GeneratedUiFactory.cs`

Wraps a generated or code-first UI creation delegate and returns a `MarkupResult<UIElement>` with markup diagnostics instead of letting factory failures escape.

```csharp
public sealed class GeneratedUiFactory
```

Inheritance:
`object` -> `GeneratedUiFactory`

## Examples

Create a retained UI tree from a generated or code-first factory delegate:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

GeneratedUiFactory factory = new((Func<UIElement>)(() => new Border
{
    Background = DrawColor.White
}));

MarkupResult<UIElement> result = factory.Create();

if (!result.HasErrors)
{
    UIElement root = result.Value!;
}
```

Return explicit diagnostics from a factory when creation is not successful:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Markup;

GeneratedUiFactory factory = new(() =>
    new MarkupResult<UIElement>(
        null,
        [MarkupDiagnostic.Error("MARKUP030", "Generated UI factory returned no root element.")]));

MarkupResult<UIElement> result = factory.Create();
```

## Remarks

`GeneratedUiFactory` is the runtime wrapper used by generated UI factory classes. The source generator emits an `AsGeneratedFactory()` method that returns `new GeneratedUiFactory(Create)`, where `Create` is the generated method that builds the root `UIElement`.

The `Func<UIElement>` constructor is a convenience overload for factories that only return a root element. It wraps the element in `MarkupResult<UIElement>` with no diagnostics. If that delegate returns `null`, `Create()` returns a failed result with diagnostic code `MARKUP030`.

The `Func<MarkupResult<UIElement>>` constructor is for factories that already produce diagnostics. `Create()` preserves a non-null result from that delegate, except when the result has a `null` `Value` and no errors; in that case it adds a `MARKUP030` error for the missing root element. If the delegate itself returns `null`, `Create()` returns a failed result with `MARKUP030`.

Exceptions thrown while invoking the factory delegate are caught and converted to a failed `MarkupResult<UIElement>`. The diagnostic message includes the exception message and uses code `MARKUP030`.

## Constructors

| Name | Description |
| --- | --- |
| `GeneratedUiFactory(Func<UIElement> create)` | Initializes a factory from a delegate that returns the root `UIElement`. Throws `ArgumentNullException` when `create` is `null`. |
| `GeneratedUiFactory(Func<MarkupResult<UIElement>> create)` | Initializes a factory from a delegate that returns a full markup result. Throws `ArgumentNullException` when `create` is `null`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create()` | `MarkupResult<UIElement>` | Invokes the stored delegate and returns the created root element plus diagnostics. Converts null results, null root elements without errors, and thrown exceptions into `MARKUP030` diagnostics. |

## Applies to

`Cerneala` projects targeting `net8.0`.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Markup.MarkupResult<T>`
- `Cerneala.UI.Markup.MarkupDiagnostic`
- `Cerneala.UI.Markup.UiFactory`
