# ReferenceContentEqualityComparer Class

## Definition
Namespace: `Cerneala.UI.Controls`
Assembly/Project: `Cerneala`
Source: `UI/Controls/ContentPresenter.cs`

Provides reference-only equality for `ContentPresenter.Content` change detection.

```csharp
private sealed class ReferenceContentEqualityComparer : IEqualityComparer<object?>
```

Containing type:
`ContentPresenter`

Implements:
`IEqualityComparer<object?>`

## Examples

The comparer is private to `ContentPresenter`; the observable behavior is that two distinct values that are equal by value still count as different content.

```csharp
private sealed record EqualContent(string Value);

ContentPresenter presenter = new()
{
    Content = new EqualContent("same"),
    ContentTemplate = new ContentTemplate<EqualContent>(
        "EqualContent",
        key: null,
        priority: 0,
        context => new TextBlock { Text = context.Data!.Value })
};

presenter.Measure(new MeasureContext(new LayoutSize(100, 100)));
UIElement firstChild = presenter.PresentedChild!;

presenter.Content = new EqualContent("same");

bool rematerialized = !ReferenceEquals(firstChild, presenter.PresentedChild);
```

## Remarks

`ReferenceContentEqualityComparer` is a private nested implementation detail used by `ContentPresenter.ContentProperty` metadata. It compares content with `ReferenceEquals` instead of `object.Equals`, so replacing `Content` with a different object instance is treated as a change even when the two objects are value-equal.

This matters for templated content. When content changes by reference, `ContentPresenter.OnPropertyChanged` marks presentation as dirty and refreshes the presented child. For example, two separate record instances with the same data can produce a new templated child instead of reusing the child created for the old instance.

`GetHashCode` returns `0` for `null`; otherwise it uses `RuntimeHelpers.GetHashCode`, which is based on object identity rather than an overridden value hash code.

## Methods

| Name | Description |
| --- | --- |
| `Equals(object?, object?)` | Returns `true` only when both arguments are the same object reference, including when both are `null`. |
| `GetHashCode(object?)` | Returns `0` for `null`; otherwise returns the runtime identity hash code for the object instance. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Controls/ContentPresenter.cs`
- `ContentPresenter`
- `ContentPresenter.ContentProperty`
