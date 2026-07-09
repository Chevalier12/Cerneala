# TextEditingController Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextEditingController.cs`

Adapts text input and editing keys to operations on a `TextEditor`.

```csharp
public sealed class TextEditingController
```

Inheritance:
`object` -> `TextEditingController`

## Examples

Insert text and handle navigation or deletion keys:

```csharp
using Cerneala.UI.Input;
using Cerneala.UI.Text;

TextEditor editor = new();
TextEditingController controller = new(editor);

bool inserted = controller.InsertText("hello");
bool moved = controller.HandleKey(InputKey.Left);
bool deleted = controller.HandleKey(InputKey.Back);
```

## Remarks

`TextEditingController` owns no document state itself. It delegates to the `TextEditor` supplied to the constructor, which must be non-null.

`InsertText` inserts the supplied text, treating `null` as an empty string, and returns whether the editor document text or selection changed.

`HandleKey` supports Backspace, Delete, Left, Right, Home, and End. It returns `false` for unsupported keys and otherwise returns whether the editor document, selection, or caret changed.

## Constructors

| Signature | Description |
| --- | --- |
| `TextEditingController(TextEditor editor)` | Initializes a controller for the specified editor. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Editor` | `TextEditor` | Gets the editor controlled by this instance. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `InsertText(string text)` | `bool` | Inserts text into the editor and returns whether state changed. |
| `HandleKey(InputKey key, bool extendSelection = false)` | `bool` | Applies a supported editing key and returns whether state changed. |

## Applies To

Cerneala UI text editing input handling.

## See Also

- `Cerneala.UI.Text.TextEditor`
- `Cerneala.UI.Input.InputKey`
- `Cerneala.UI.Text.TextSelection`
