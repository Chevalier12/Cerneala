# InlineCollection Class

## Definition
Namespace: `Cerneala.UI.Documents`

Assembly/Project: `Cerneala`

Source: `UI/Documents/InlineCollection.cs`

Holds the inlines of a `TextBlock` or of a `Span`, and tells the `TextBlock` when they change.

```csharp
public sealed class InlineCollection : Collection<Inline>
```

Inheritance:
`object` -> `Collection<Inline>` -> `InlineCollection`

Implements:
`IList<Inline>`, `IReadOnlyList<Inline>`

## Examples

Every change of the collection updates `TextBlock.Text`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Documents;

TextBlock text = new();
Run first = new("a");

text.Inlines.Add(first);
text.Inlines.Add(new Bold(new Run("b")));
// text.Text is "ab"

text.Inlines.Remove(first);
// text.Text is "b"

text.Inlines[0] = new Run("z");
// text.Text is "z"

text.Inlines.Clear();
// text.Text is ""
```

The rules for what can be added:

```csharp
TextBlock a = new();
TextBlock b = new();
Run run = new("x");
a.Inlines.Add(run);

b.Inlines.Add(run);        // throws InvalidOperationException: run already has a parent
a.Inlines.Add(null!);      // throws ArgumentNullException

a.Inlines.Remove(run);
b.Inlines.Add(run);        // works: run has no parent now

Span outer = new();
Span inner = new();
outer.Inlines.Add(inner);
inner.Inlines.Add(outer);  // throws InvalidOperationException: outer is above inner
```

## Remarks

`TextBlock.Inlines` and `Span.Inlines` return an instance of this class. The class has no public constructor.

An inline has at most one parent. Adding an inline that already has a parent throws `InvalidOperationException`, as does adding a `Span` to its own collection or to a collection under it. Adding `null` throws `ArgumentNullException`. A removed or replaced inline has no parent any more and can be added somewhere else.

After each add, replace, remove or clear that changes the collection, and for a `Span` at any depth, the `TextBlock` that owns the tree:

- sets its `Text` to the text of its inlines, where `LineBreak` is `"\n"`;
- invalidates measure, render and semantics.

Clearing an empty collection changes nothing.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of inlines in this collection, not counting the ones in nested spans. Inherited from `Collection<Inline>`. |
| `Item[int]` | `Inline` | Gets or sets the inline at an index. Setting replaces the inline. Inherited from `Collection<Inline>`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Add(Inline item)` | `void` | Adds an inline at the end. Inherited. |
| `Insert(int index, Inline item)` | `void` | Inserts an inline. Inherited. |
| `Remove(Inline item)` | `bool` | Removes an inline. Inherited. |
| `RemoveAt(int index)` | `void` | Removes the inline at an index. Inherited. |
| `Clear()` | `void` | Removes all inlines. Inherited. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Add`, `Insert`, `Item[int]` setter | `ArgumentNullException` | The inline is `null`. |
| `Add`, `Insert`, `Item[int]` setter | `InvalidOperationException` | The inline already has a parent, or it is the owner `Span` or a `Span` above the owner. |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Documents.Span`
- `Cerneala.UI.Documents.Inline`
