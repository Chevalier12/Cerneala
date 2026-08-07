# Dissolve FastNoise rank map

`dissolve-fastnoise-ranks.bin` is a 256 by 256 scalar threshold map generated
with EA FastNoise commit `2cf53e4bb510d07511fe63a312556d2a2e108c70`.

Generator command:

```text
FastNoise.exe real uniform gauss 1.0 box 1 separate 1.0 256 256 1 cerneala-dissolve-fastnoise -numsteps 10000 -output csv -seed 146
```

The first scalar channel was sorted globally and replaced by its 8-bit rank.
Every rank from 0 through 255 therefore occurs exactly 256 times. The spatial
arrangement produced by FastNoise is unchanged.

SHA-256:
`6A874D43B56A4869A9579B7A89007C3CB414E3D9070E70C85956539679DF10F5`

Sources:

- https://github.com/electronicarts/fastnoise/tree/2cf53e4bb510d07511fe63a312556d2a2e108c70
- https://github.com/electronicarts/fastnoise/blob/2cf53e4bb510d07511fe63a312556d2a2e108c70/fastnoise/shaders/loss.hlsl
- https://github.com/electronicarts/fastnoise/blob/2cf53e4bb510d07511fe63a312556d2a2e108c70/fastnoise/shaders/swap.hlsl
