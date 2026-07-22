float ResamplingInside(float2 uv)
{
    return
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
}

float2 ResamplingMirror(float2 uv)
{
    return 1.0 - abs((frac(uv * 0.5) * 2.0) - 1.0);
}

float4 SampleResamplingSource(
    float2 uv,
    int profile,
    int edgeMode,
    float4 fillColor)
{
    float inside = ResamplingInside(uv);
    if (edgeMode == 1 && inside < 0.5)
    {
        return 0.0;
    }
    if (edgeMode == 4 && inside < 0.5)
    {
        float4 associatedFill = float4(
            fillColor.rgb * fillColor.a,
            fillColor.a);
        return WorkingAssociatedToLinearSrgb(
            associatedFill,
            profile);
    }
    if (edgeMode == 2)
    {
        uv = frac(uv);
    }
    else if (edgeMode == 3)
    {
        uv = ResamplingMirror(uv);
    }
    else
    {
        uv = clamp(
            uv,
            PixelSize * 0.5,
            1.0 - (PixelSize * 0.5));
    }

    return WorkingAssociatedToLinearSrgb(
        tex2D(SpriteTextureSampler, uv),
        profile);
}

float ResamplingChannel(float4 sample, int channel)
{
    if (channel == 0)
    {
        return sample.r;
    }
    if (channel == 1)
    {
        return sample.g;
    }
    if (channel == 2)
    {
        return sample.b;
    }
    if (channel == 3)
    {
        return sample.a;
    }
    return dot(
        sample.rgb,
        float3(0.2126, 0.7152, 0.0722));
}

float ResamplingWaveShape(float phase, int kind)
{
    if (kind == 1)
    {
        return (abs(frac(phase) - 0.5) * 4.0) - 1.0;
    }
    if (kind == 2)
    {
        return step(0.5, frac(phase)) * 2.0 - 1.0;
    }
    return sin(phase * 6.28318531);
}

float4 ApplyResampling(
    VertexShaderOutput input,
    float4 source,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    int passKind = (int)(FilterHeader.z + 0.5);
    float2 uv = ResolveUv(input);
    float2 mapped = float2(uv.x, uv.y);
    int edgeMode = 0;
    float4 fillColor =
        float4(0.0, 0.0, 0.0, 0.0);

    if (operation == 3 && passKind == 1)
    {
        float2 stepSize =
            PixelSize *
            max(FilterOptions0.y, 0.5);
        float4 diffuse = (
            SampleResamplingSource(
                uv,
                profile,
                0,
                fillColor) * 4.0 +
            SampleResamplingSource(
                uv + float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv - float2(stepSize.x, 0.0),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv + float2(0.0, stepSize.y),
                profile,
                0,
                fillColor) +
            SampleResamplingSource(
                uv - float2(0.0, stepSize.y),
                profile,
                0,
                fillColor)) / 8.0;
        return lerp(
            source,
            diffuse,
            saturate(FilterOptions0.y));
    }
    else if (operation == 0)
    {
        float2 origin = FilterOptions2.xy;
        float2 size = max(FilterOptions3.xy, 1.0);
        float2 position =
            uv - origin - (FilterOptions0.xy / size);
        float cosine = cos(-FilterOptions1.x);
        float sine = sin(-FilterOptions1.x);
        float2 unrotated = float2(
            (position.x * cosine) -
                (position.y * sine),
            (position.x * sine) +
                (position.y * cosine));
        float determinant = max(
            1.0 -
                (FilterOptions1.y * FilterOptions1.z),
            0.000001);
        position = float2(
            unrotated.x -
                (FilterOptions1.y * unrotated.y),
            unrotated.y -
                (FilterOptions1.z * unrotated.x)) /
            determinant;
        float2 scale = FilterOptions0.zw;
        float2 safeScale = float2(
            scale.x < 0.0
                ? min(scale.x, -0.000001)
                : max(scale.x, 0.000001),
            scale.y < 0.0
                ? min(scale.y, -0.000001)
                : max(scale.y, 0.000001));
        position /= safeScale;
        mapped = origin + position;
        edgeMode = (int)(FilterOptions2.z + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 1)
    {
        float2 constraint = tex2D(
            SecondaryTextureSampler,
            uv).rg * 2.0 - 1.0;
        float2 centered = uv - 0.5;
        float radius = dot(centered, centered);
        float projection =
            1.0 + (FilterOptions0.x * 0.12);
        float focal = FilterOptions0.y > 0.5
            ? 0.75
            : 1.0;
        float radialScale =
            1.0 +
            (radius * projection * focal /
                max(FilterOptions0.z, 0.0001));
        centered = centered * radialScale;
        float angle = -FilterOptions1.x;
        float cosine = cos(angle);
        float sine = sin(angle);
        float2 rotated = float2(
            (centered.x * cosine) -
                (centered.y * sine),
            (centered.x * sine) +
                (centered.y * cosine));
        mapped =
            0.5 +
            (rotated /
                max(FilterOptions0.w, 0.0001)) -
            (FilterOptions1.yz * PixelSize) +
            (constraint * PixelSize);
        edgeMode = 1;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 2)
    {
        float2 centered = uv - 0.5;
        float angle = -FilterOptions1.w;
        float cosine = cos(angle);
        float sine = sin(angle);
        centered = float2(
            (centered.x * cosine) -
                (centered.y * sine),
            (centered.x * sine) +
                (centered.y * cosine));
        centered.x -= FilterOptions1.z * centered.y;
        centered.y -= FilterOptions1.y * centered.x;
        float radius = dot(centered, centered);
        centered *=
            (1.0 +
                (FilterOptions0.x * radius)) /
            max(FilterOptions2.x, 0.0001);
        mapped = centered + 0.5;
        edgeMode = (int)(FilterOptions2.y + 0.5);
        float4 baseSample = SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
        float2 chromaDirection =
            centered * radius * 0.01;
        float4 redSample = SampleResamplingSource(
            mapped +
                (chromaDirection * FilterOptions0.y),
            profile,
            edgeMode,
            fillColor);
        float4 blueSample = SampleResamplingSource(
            mapped +
                (chromaDirection * FilterOptions0.z),
            profile,
            edgeMode,
            fillColor);
        float vignette = 1.0 -
            (saturate(
                (length(centered) -
                    FilterOptions1.x) /
                max(1.0 - FilterOptions1.x, 0.0001)) *
                FilterOptions0.w);
        return float4(
            float3(
                redSample.r,
                baseSample.g,
                blueSample.b) * vignette,
            baseSample.a);
    }
    else if (operation == 3)
    {
        float noise = NeighborhoodHash(
            input.Position.xy,
            9173.0);
        float3 straight = saturate(
            Unpremultiply(source) +
            ((noise - 0.5) *
                FilterOptions0.x) +
            (FilterOptions1.rgb *
                FilterOptions0.z));
        return float4(
            straight * source.a,
            source.a);
    }
    else if (operation == 4)
    {
        float2 mapUv = FilterOptions0.z < 0.5
            ? uv
            : frac(uv);
        float4 map = tex2D(
            SecondaryTextureSampler,
            mapUv);
        float2 displacement = float2(
            ResamplingChannel(
                map,
                (int)(FilterOptions1.x + 0.5)),
            ResamplingChannel(
                map,
                (int)(FilterOptions1.y + 0.5)));
        displacement = (displacement * 2.0) - 1.0;
        mapped =
            uv -
            (displacement *
                FilterOptions0.xy *
                PixelSize);
        edgeMode = (int)(FilterOptions0.w + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 5)
    {
        float4 textureSample = FilterHeader.w > 0.5
            ? tex2D(SecondaryTextureSampler, uv)
            : float4(
                NeighborhoodHash(
                    input.Position.xy,
                    FilterOptions0.z + 31.0),
                NeighborhoodHash(
                    input.Position.xy + 43.0,
                    FilterOptions0.z + 59.0),
                0.5,
                1.0);
        float2 displacement =
            (textureSample.rg * 2.0) - 1.0;
        displacement = lerp(
            displacement,
            -displacement,
            step(0.5, FilterOptions1.x));
        mapped =
            uv -
            (displacement *
                FilterOptions0.x *
                max(FilterOptions0.w, 0.0001) *
                PixelSize);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 6)
    {
        float seed =
            FilterOptions0.z +
            (FilterOptions0.w * 65536.0);
        float2 position = uv /
            max(FilterOptions0.x * PixelSize, 0.000001);
        float noise = NeighborhoodHash(
            floor(position),
            seed);
        mapped = uv + float2(
            sin((position.y + noise) * 6.28318531),
            cos((position.x - noise) * 6.28318531)) *
            FilterOptions0.y *
            PixelSize;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 7)
    {
        float2 center = FilterOptions0.yz;
        float2 delta = uv - center;
        float radius = length(delta) / 0.70710678;
        float power =
            1.0 + FilterOptions0.x;
        mapped = center +
            (delta *
                pow(
                    max(radius, 0.000001),
                    power - 1.0));
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 8)
    {
        float2 center = FilterOptions0.yz;
        if (FilterOptions0.x < 0.5)
        {
            float angle =
                (uv.x - center.x) * 6.28318531;
            float radius =
                (uv.y - center.y + 0.5) *
                0.70710678;
            mapped = center + float2(
                cos(angle),
                sin(angle)) * radius;
        }
        else
        {
            float2 delta = uv - center;
            mapped = float2(
                center.x +
                    (atan2(delta.y, delta.x) /
                        6.28318531),
                center.y - 0.5 +
                    (length(delta) /
                        0.70710678));
        }
        edgeMode = 2;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 9)
    {
        float seed =
            FilterOptions0.z +
            (FilterOptions0.w * 65536.0);
        float size =
            6.0 + (FilterOptions0.y * 8.0);
        float phase =
            (uv.y * size) +
            (NeighborhoodHash(
                floor(input.Position.xy / size),
                seed) * 6.28318531);
        mapped.x +=
            sin(phase) *
            FilterOptions0.x *
            PixelSize.x;
        edgeMode = (int)(FilterOptions1.x + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 10)
    {
        float y = saturate(uv.y);
        float curve = FilterOptions0.x;
        float amount = curve < 0.5
            ? 0.0
            : curve < 1.5
                ? y * y
                : curve < 2.5
                    ? 1.0 -
                        ((1.0 - y) * (1.0 - y))
                    : curve < 3.5
                        ? smoothstep(0.0, 1.0, y)
                        : sin((y - 0.5) * 3.14159265);
        mapped.x -= (amount - 0.5) * 0.5;
        edgeMode = (int)(FilterOptions0.y + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 11)
    {
        float2 center = FilterOptions0.zw;
        float2 delta = uv - center;
        float radius = saturate(
            length(delta) / 0.70710678);
        float amount =
            FilterOptions0.x *
            (1.0 - radius * radius);
        float2 warped = delta /
            max(1.0 + amount, 0.0001);
        if (FilterOptions0.y > 0.5 &&
            FilterOptions0.y < 1.5)
        {
            warped.y = delta.y;
        }
        else if (FilterOptions0.y > 1.5)
        {
            warped.x = delta.x;
        }
        mapped = center + warped;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 12)
    {
        float2 center = FilterOptions0.yz;
        float2 delta = uv - center;
        float radius = length(delta) / 0.70710678;
        float angle =
            -FilterOptions0.x *
            saturate(1.0 - radius);
        float cosine = cos(angle);
        float sine = sin(angle);
        mapped = center + float2(
            (delta.x * cosine) -
                (delta.y * sine),
            (delta.x * sine) +
                (delta.y * cosine));
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 13)
    {
        float seed =
            FilterOptions2.y +
            (FilterOptions2.z * 65536.0);
        int kind =
            (int)(FilterOptions0.w + 0.5);
        float generators =
            max(FilterOptions0.x, 1.0);
        float phase =
            NeighborhoodHash(
                floor(input.Position.xy),
                seed);
        float waveX = ResamplingWaveShape(
            (uv.y * generators /
                max(FilterOptions0.y * PixelSize.y, 0.000001)) +
                phase,
            kind);
        float waveY = ResamplingWaveShape(
            (uv.x * generators /
                max(FilterOptions0.z * PixelSize.x, 0.000001)) -
                phase,
            kind);
        mapped += float2(
            waveX * FilterOptions1.x *
                FilterOptions1.z,
            waveY * FilterOptions1.y *
                FilterOptions1.w) * PixelSize;
        edgeMode = (int)(FilterOptions2.x + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 14)
    {
        float2 center = FilterOptions1.xy;
        float2 delta = uv - center;
        float radius = length(delta);
        float angle = atan2(delta.y, delta.x);
        float ridges = max(FilterOptions0.y, 1.0);
        float oscillation =
            sin((radius * ridges * 40.0) +
                (FilterOptions0.z > 1.5
                    ? angle * ridges
                    : 0.0));
        float amount =
            oscillation *
            FilterOptions0.x *
            PixelSize.x;
        if (FilterOptions0.z < 0.5)
        {
            mapped = center +
                normalize(delta + 0.000001) *
                (radius + amount);
        }
        else
        {
            angle += amount * 10.0;
            mapped = center + float2(
                cos(angle),
                sin(angle)) * radius;
        }
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else if (operation == 15)
    {
        float4 mesh = tex2D(
            SecondaryTextureSampler,
            uv);
        float2 displacement =
            (mesh.rg * 2.0) - 1.0;
        float mask = FilterOptions6.x > 0.5
            ? tex2D(
                FilterAuxiliaryTextureSampler,
                uv).a
            : 1.0;
        mask = lerp(
            mask,
            1.0 - mask,
            step(0.5, FilterOptions0.y));
        mapped =
            uv -
            (displacement *
                (1.0 - saturate(FilterOptions0.x)) *
                mask);
        edgeMode = (int)(FilterOptions0.z + 0.5);
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }
    else
    {
        mapped =
            uv -
            (FilterOptions0.xy * PixelSize);
        edgeMode = (int)(FilterOptions0.z + 0.5);
        fillColor = FilterOptions1;
        return SampleResamplingSource(
            mapped,
            profile,
            edgeMode,
            fillColor);
    }

}

float4 ResamplingFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = ApplyResampling(
        input,
        source,
        profile);
    float3 sourceStraight =
        saturate(Unpremultiply(source));
    float3 filteredStraight =
        saturate(Unpremultiply(filtered));
    float3 blendedStraight = EvaluateBlendMode(
        blendMode,
        sourceStraight,
        filteredStraight);
    float4 blended = float4(
        blendedStraight * filtered.a,
        filtered.a);
    float4 result = lerp(
        source,
        blended,
        saturate(Opacity));
    return LinearSrgbAssociatedToWorking(
        result,
        profile) * input.Color;
}

