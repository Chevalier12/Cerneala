# Prism catalog filters

The filter catalog is the source of truth for every remaining Stylize,
Pixelate, Render, Video, artistic, texture, morphology, and Cerneala-native
filter. Generated descriptors carry defaults, validation, execution metadata,
coverage ownership, and documentation data. Runtime code consumes those
descriptors instead of maintaining a second filter list.

The generated [filter reference](prism-filter-reference.generated.md) contains
the complete filter/property/default matrix. This handwritten document keeps
only conceptual behavior and implementation conventions.

## Shared execution primitives

Filters are planned through reusable morphology, quantization, procedural,
video, artistic, edge-detection, tiling, texture, convolution, and color
primitives. Public filter identities remain distinct even when they share the
same mathematical primitive. This keeps markup semantics and diagnostics
specific while avoiding duplicate sampling and alpha code.

Maximum and Minimum use prepared horizontal and vertical morphology passes.
Facet and Diffuse expand their catalog iteration count into explicit passes.
The planner computes sampling radii and conservative bounds once; the shader
receives prepared values and does not reinterpret markup.

## Determinism and resources

Procedural filters derive randomness only from the catalog seed property and
pixel coordinates. They do not read the clock or a global random generator, so
identical inputs and resource versions produce identical output.

Lighting Effects binds its required lights resource and optional texture as
separate prepared inputs. Texturizer accepts one optional texture, while Custom
Convolution requires its kernel resource. Resource identities participate in
the existing graph dependency and cache contracts.

## Color, alpha, and composition

CPU conformance math and GPU kernels work in the selected working profile with
premultiplied RGBA. Sampling results are clamped back to valid associated
alpha. Filter order is preserved because noncommutative catalog operations are
represented as separate graph nodes.

Catalog filters use the same layer and group graph boundaries as every other
Prism operation. Masks, clipping alpha, isolation, opacity, and blend modes are
applied outside the filter pass in their declared composition order.

## Conformance gallery

The conformance gallery is generated from the same catalog-derived filter set
used by the planner tests. Each entry contains one composition for one filter;
adding a supported catalog filter therefore adds a gallery case without a
hand-written view.
