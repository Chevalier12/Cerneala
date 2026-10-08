# TimbreClipSound Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreClipSound.cs`

One named sound of a [TimbreClipDefinition](Cerneala.Timbre.TimbreClipDefinition.md): the `@sound Name { … }` node of a markup `<TimbreClip>`.

```csharp
public sealed class TimbreClipSound
```

Inheritance:
`object` -> `TimbreClipSound`

## Examples

```csharp
using Cerneala.Timbre;

var click = new TimbreClipSound("Click", new TimbreSound("audio/click.wav"));
var music = new TimbreClipSound("Music", new TimbreSound("audio/music.ogg", loop: true), autoPlay: true);
```

## Remarks

The name addresses the sound from markup, for example `@play $self.timbre.Click;` or the Motion target `$self.timbre.Music.Volume`. It must be an identifier: letters, digits, or underscores, not starting with a digit (`ArgumentException`); `null` throws `ArgumentNullException`, as does a `null` sound.

`AutoPlay` belongs to the sound inside a clip, not to [TimbreSound](Cerneala.Timbre.TimbreSound.md): when an Aspect attaches the clip, every sound with `AutoPlay = true` starts, on each application of the Aspect. A `TimbreSound` played directly from C# has no such notion.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreClipSound(string name, TimbreSound sound, bool autoPlay = false)` | Creates a named sound node. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Sound name, unique within its clip. |
| `Sound` | `TimbreSound` | The immutable sound definition that is played. |
| `AutoPlay` | `bool` | Whether the sound starts when an Aspect attaches the clip. |

## Applies to

`Cerneala` core (`net8.0`).

## See also

- [TimbreClipDefinition](Cerneala.Timbre.TimbreClipDefinition.md)
- [TimbreSound](Cerneala.Timbre.TimbreSound.md)
