# SoundModifier Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundModifier.cs`

Base of the immutable modifier definitions in a clip's chain.

```csharp
public abstract class SoundModifier
```

Inheritance:
`object` -> `SoundModifier`

Derived:
[LowPass](Cerneala.Timbre.LowPass.md), [Delay](Cerneala.Timbre.Delay.md)

## Remarks

The set of modifiers is closed: the type cannot be derived from outside the core assembly. A modifier is a definition only; each playback creates its own processing state per modifier, which persists across processing blocks, is independent per channel, and is never shared with another playback. The same modifier instance may appear in several clips.

Processing runs on the runtime's mixer thread in 480-frame blocks. Parameter changes take effect at the next block boundary.
