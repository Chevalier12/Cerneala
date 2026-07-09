# TextCompositionManager Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextCompositionManager.cs`

Manages the active text composition state and commits composed text into a `TextEditor`.

```csharp
public sealed class TextCompositionManager
```

Inheritance:
`object` -> `TextCompositionManager`

## Examples

Begin, update, and commit a composition:

```csharp
using Cerneala.UI.Text;

TextCompositionManager manager = new();

manager.Begin(start: 3, text: "preedit");
manager.Update("committed");

string text = manager.Commit();
```

Commit directly into an editor:

```csharp
manager.Begin(0, "hello");
manager.CommitTo(editor);
```

## Remarks

`TextCompositionManager` tracks one `TextCompositionState`. The initial state is inactive.

`Begin` starts a composition at a non-negative position and treats a `null` text value as an empty string. `Update` replaces the composition text and throws `InvalidOperationException` when no composition is active. `Commit` returns the active composition text and resets the state, or returns an empty string when inactive. `Cancel` resets the state without returning text.

`CommitTo` commits into a `TextEditor` by moving the editor caret to the composition start and inserting the committed text when that text is non-empty.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `State` | `TextCompositionState` | Gets the current composition state. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Begin(int start, string text = "")` | `void` | Starts a composition at `start` with optional initial text. |
| `Update(string text)` | `void` | Updates the active composition text. |
| `Commit()` | `string` | Ends the active composition and returns its text, or returns an empty string when inactive. |
| `Cancel()` | `void` | Cancels the active composition and resets the state. |
| `CommitTo(TextEditor editor)` | `void` | Commits the active composition text into an editor. |

## Applies To

Cerneala UI text editing and composition APIs.

## See Also

- `Cerneala.UI.Text.TextCompositionState`
- `Cerneala.UI.Text.TextEditor`
