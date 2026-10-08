# TimbreClipDefinition Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreClipDefinition.cs`

Immutable set of named sounds that share clip-level parameters. It is the C# form of a markup `<TimbreClip>` and the audio counterpart of [PrismClipDefinition](Cerneala.UI.Prism.Definitions.PrismClipDefinition.md): a clip holds named [TimbreClipSound](Cerneala.Timbre.TimbreClipSound.md) nodes the way a Prism clip holds named layers.

```csharp
public sealed class TimbreClipDefinition
```

Inheritance:
`object` -> `TimbreClipDefinition`

## Examples

```csharp
using Cerneala.Timbre;

var brightness = new TimbreParameter<float>("Brightness", 1200f);
var ui = new TimbreClipDefinition(
    "UiSounds",
    sounds:
    [
        new TimbreClipSound("Click", new TimbreSound("audio/click.wav")),
        new TimbreClipSound(
            "Hover",
            new TimbreSound("audio/hover.wav", volume: 0.3f, parameters: [brightness], modifiers: [new LowPass(cutoff: brightness)])),
        new TimbreClipSound("Music", new TimbreSound("audio/music.ogg", loop: true), autoPlay: true)
    ],
    parameters: [brightness]);

// Plain C#: play one sound of the clip on an element.
button.Timbre.Play(ui.Sounds["Hover"].Sound);
```

The same clip in markup:

```xml
<TimbreClip Name="UiSounds">
    @parameter Brightness: float = 1200;
    @sound Click { Source = "audio/click.wav"; }
    @sound Hover { Source = "audio/hover.wav"; Volume = 0.3; @modifier LowPass { Cutoff = Brightness; } }
    @sound Music { Source = "audio/music.ogg"; Loop = true; AutoPlay = true; }
</TimbreClip>
```

## Remarks

Declaring a clip does not open its sources, an output device, or perform I/O. A markup Aspect attaches a clip with `@timbre $UiSounds;` (or an inline `@timbre { … }`): the application gets one playback slot per sound, starts the sounds whose `AutoPlay` is `true`, and stops everything when the Aspect is replaced or its element detaches.

`Sounds` is keyed by sound name and enumerates in declaration order. `Parameters` are the clip-level parameters; each sound declares, in its own [TimbreSound.Parameters](Cerneala.Timbre.TimbreSound.md), the subset its modifiers use, and the same parameter instance may feed several sounds.

Validation happens in the constructor:

- `name` must not be `null` (`ArgumentNullException`) or whitespace (`ArgumentException`).
- `sounds` must not be `null`, must contain at least one sound, no `null` entries and no two sounds with the same name (`ArgumentException`).
- `parameters` cannot contain `null`, the same instance twice, or two parameters with the same name.
- Every parameter declared by a sound must be one of the clip's `parameters`, by reference.

The `sounds` and `parameters` sequences are copied at construction.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreClipDefinition(string name, IEnumerable<TimbreClipSound> sounds, IEnumerable<TimbreParameter>? parameters = null)` | Creates a validated, immutable clip. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Clip name, used in diagnostics. |
| `Sounds` | `IReadOnlyDictionary<string, TimbreClipSound>` | Named sounds, enumerated in declaration order. |
| `Parameters` | `IReadOnlyList<TimbreParameter>` | Clip-level parameters shared by the sounds. |

## Applies to

`Cerneala` core (`net8.0`). The type does not depend on UI, markup, Aspect, or SDL.

## See also

- [TimbreClipSound](Cerneala.Timbre.TimbreClipSound.md)
- [TimbreSound](Cerneala.Timbre.TimbreSound.md)
- [TimbreParameter&lt;T&gt;](Cerneala.Timbre.TimbreParameter_T_.md)
- [PrismClipDefinition](Cerneala.UI.Prism.Definitions.PrismClipDefinition.md)
