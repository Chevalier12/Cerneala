# Guide Prism

Prism only processes the presentation of a Cerneala element. It does not change the measurement,
arrangement, hitbox, focus or input routing.

## Photoshop model and default source

Prism only captures the local visual of the control once: the commands
produced by `OnRender`'s own, without the commands of visual descendants. That catch
is the default normal stack source; children are drawn normally after the result
Prism, and the layer and group names are addresses for Motion and
diagnostics, not image sources.

The declared order is like in the Photoshop panel: the first node is in front,
the last is at the back, and evaluation runs bottom-up. Every layer and group has
two distinct inputs: the captured control is its source, while the accumulated
result behind it is its compositing backdrop.

`@backdrop` is no longer part of the Prism language. A visible non-normal blend
on a layer, group, or style requests the physical host backdrop automatically.
The GPU capture stays lazy, so a source-only composition does not pay for it.

## Layer, group, mask and clipping

- `@layer` is leaf and contains filters and/or styles.
- `@group` contains nested layers or groups and can process their output.
- `@mask` applies to the prepared result after filters and styles, before
  opacity and blend.
- `ClipToBelow = true` uses the alpha of the closest normal sibling
  unclipped below, in the same scope.
- `Visible = false` removes its entire scope and work; `Opacity = 0` keeps
  evaluation, then make the contribution transparent.

Generic example:
```xml
<PrismComposition Name="FrostedPanelPrism">
    @layer Highlight
    {
        Opacity = 0.18;
        BlendMode = Screen;

        @style OuterGlow
        {
            Size = 7;
            Opacity = 0.72;
            Color = #8060D8FF;
        }

        @mask
        {
            Image = $PanelMask;
            Channel = Luminance;
            Feather = 1.5;
            Density = 0.42;
        }
    }

    @group CardTreatment
    {
        @layer CardClarity
        {
            @filter BrightnessContrast
            {
                Brightness = 0.02;
                Contrast = 0.08;
            }
        }
    }

    @layer SpaceGlass
    {
        Opacity = 0.76;
        BlendMode = Overlay;
        @filter Blur { Radius = 8; }
    }
</PrismComposition>

<Border Name="FrostedPanel">
    @prism $FrostedPanelPrism;
</Border>
```
`Blur` still processes the layer source. `BlendMode = Overlay` composites that
prepared layer result against everything accumulated behind it, including the
host image and lower UI. Style blend modes use the same destination without
requiring a special node. A non-normal style blend and a non-normal layer blend
are two distinct composite stages; set the style blend to `Normal` when only the
final layer blend should apply.

## Motion paths

Motion enters the Prism instance through the reserved segment `.prism.`:
```text
$self.prism.Highlight.Opacity
$owner.prism.CardTreatment.CardClarity.Visible
$FrostedPanel.prism.SpaceGlass.Opacity
```
`$self`, `$owner` and namescope names follow existing Motion rules.
Intermediate segments cross groups and named nodes. The numbers and
colors interpolate continuously; bool, int and enum change discretely at the end
the interval. Resources are not animable. The generator validates the path and type,
and the runtime does not do textual lookup per frame.

## Backdrop and backends

The frame analyzer requests at most one readonly host lease when any visible
destination-aware operation needs physical pixels. Layers and groups reuse the
cropped, color-normalized backdrop for that scope. If no such operation exists,
Prism does not acquire or allocate a backdrop surface. The lease is released in
the same draw, including exceptions.

Pass-through groups expose their inherited backdrop to children. Isolated groups
start a private transparent stack for their children and composite the completed
group against the inherited backdrop only at the group boundary.

A backend without Prism ignores `BeginPrism` and `EndPrism` but processes all
the commands between them. It doesn't need to implement backdrop and none appears
change of layout or input.

## Diagnostics and budgets

`MonoGameDrawingBackend.RendererDiagnostics` provides immutable snapshots for
hit/miss/promotion/eviction, bytes, entries, peaks, catches and passes saved.
Detailed operational diagnostics are internal, deterministic, redact IDs
Unstable GPUs and activates via `PrismRendererOptions` only when needed.

Measured defaults are 512 MiB hard for all Prism surfaces, 256 MiB
soft cache retained and 256 entries. At pressure the LRU is evacuated
don't forget If the hard limit still cannot accommodate a transient surface, the backend
report `PRISM7006`/`SurfaceAllocationFailed`, restore host and continue
the remaining gross internal orders, without partial output or quality downgrade
hidden

Markup errors use diagnostics `PRISM1xxx`-`PRISM6xxx`; failure paths of
runtime uses accurate, deduplicated diagnostics where the contract requires it.

## Catalogue

The machine-readable catalog remains the only source for operations, properties,
types, defaults and capabilities. Complete lists are not copied here:

- [generated filter reference](prism-filter-reference.generated.md)
- [adjustment filters](prism-adjustment-filters.md)
- [distortion filters](prism-distortion-filters.md)
- [neighborhood filters](prism-neighborhood-filters.md)
- [](prism-catalog-filters.md) filter catalog

The first implementation does not include the public SDK for third-party shader operations
source at runtime, adaptive quality or async compute.
