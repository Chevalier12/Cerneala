# TextDocument Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: [`UI/Text/TextDocument.cs`](../../UI/Text/TextDocument.cs)

Represents a mutable text buffer used by the Cerneala text editing APIs.

```csharp
public sealed class TextDocument
```

Inheritance:
`object` -> `TextDocument`

## Examples

```csharp
using Cerneala.UI.Text;

TextDocument document = new("hello");

string previous = document.Replace(5, 0, " world");

Console.WriteLine(document.Text);    // hello world
Console.WriteLine(previous);         // hello
Console.WriteLine(document.Length);  // 11
Console.WriteLine(document.Version); // 1
```

## Remarks

`TextDocument` stores text as a single `string`. The constructor and mutation methods normalize `null` text values to `string.Empty`.

The document can be changed only through `Replace` and `SetText`; `Text` and `Version` have private setters. `Version` starts at `0` and increments only when a mutation changes the stored text. Calling `SetText` with the current text, or using `Replace` in a way that leaves the final text unchanged, returns the previous text without incrementing `Version`.

Ranges use .NET string indexes and lengths. `Length` is `Text.Length`, so indexes are counted in UTF-16 code units. `ValidateRange` rejects negative starts, starts after the end of the document, negative lengths, and lengths that extend past the document end.

## Constructors

| Name | Description |
| --- | --- |
| `TextDocument(string text = "")` | Initializes a document with the supplied text, or an empty string when `text` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the current document text. |
| `Length` | `int` | Gets the current text length, equivalent to `Text.Length`. |
| `Version` | `long` | Gets the document version, incremented when stored text changes. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Replace(int start, int length, string text)` | `string` | Replaces a valid text range with `text`, normalizing `null` to an empty string, and returns the previous document text. |
| `SetText(string text)` | `string` | Replaces the entire document text, normalizing `null` to an empty string, and returns the previous document text. |
| `ValidateRange(int start, int length)` | `void` | Throws `ArgumentOutOfRangeException` when the supplied range is outside the document. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Replace` | `ArgumentOutOfRangeException` | `start` or `length` fails `ValidateRange`. |
| `ValidateRange` | `ArgumentOutOfRangeException` | `start` is less than `0` or greater than `Length`. |
| `ValidateRange` | `ArgumentOutOfRangeException` | `length` is less than `0` or greater than `Length - start`. |

## Applies to

Cerneala retained UI text services.

## See also

- [`TextEditor`](../../UI/Text/TextEditor.cs)
- [`TextSelection`](../../UI/Text/TextSelection.cs)
