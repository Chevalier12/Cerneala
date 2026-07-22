float2 NeighborhoodUnclampedUv(VertexShaderOutput input)
{
    return
        (input.TextureCoordinates * UvScale) +
        UvOffset;
}

float2 MirrorNeighborhoodUv(float2 uv)
{
    float2 phase = frac(uv * 0.5) * 2.0;
    return 1.0 - abs(phase - 1.0);
}

float4 SampleNeighborhood(
    float2 uv,
    int profile,
    int edgeMode)
{
    float inside =
        step(0.0, uv.x) *
        step(uv.x, 1.0) *
        step(0.0, uv.y) *
        step(uv.y, 1.0);
    if (edgeMode == 2)
    {
        uv = frac(uv);
        inside = 1.0;
    }
    else if (edgeMode == 3)
    {
        uv = MirrorNeighborhoodUv(uv);
        inside = 1.0;
    }

    uv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    float4 sample = tex2D(
        SpriteTextureSampler,
        uv);
    if (edgeMode == 1)
    {
        sample *= inside;
    }
    return WorkingAssociatedToLinearSrgb(
        sample,
        profile);
}

#include "OptimizedBilinearGaussian.fx"

float4 SampleNeighborhoodResource(float2 uv)
{
    uv = clamp(
        uv,
        0.5 / max(FilterTextureSize, 1.0),
        1.0 - (0.5 / max(FilterTextureSize, 1.0)));
    return tex2D(
        SecondaryTextureSampler,
        uv);
}

int NeighborhoodEdgeMode(int operation)
{
    if (operation == 1 ||
        operation == 2 ||
        operation == 3 ||
        operation == 4 ||
        operation == 21)
    {
        return (int)(FilterOptions0.z + 0.5);
    }
    if (operation == 6)
    {
        return (int)(FilterOptions0.w + 0.5);
    }
    if (operation == 8)
    {
        return (int)(FilterOptions0.y + 0.5);
    }
    return 0;
}

float4 SampleNeighborhoodLine(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    bool gaussian)
{
    float4 total = 0.0;
    float totalWeight = 0.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float position = count <= 1
                ? 0.0
                : ((index / (count - 1.0)) * 2.0) - 1.0;
            float weight = gaussian
                ? exp(-3.125 * position * position)
                : 1.0;
            total += SampleNeighborhood(
                uv + (radius * PixelSize * position),
                profile,
                edgeMode) * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodDisk(
    float2 uv,
    float2 radius,
    int sampleCount,
    int edgeMode,
    int profile,
    float threshold,
    bool edgeAware)
{
    float4 center = SampleNeighborhood(
        uv,
        profile,
        edgeMode);
    float3 centerStraight = Unpremultiply(center);
    float4 total = center;
    float totalWeight = 1.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 1; index < 17; index++)
    {
        if (index < count)
        {
            float fraction = index / max(count - 1.0, 1.0);
            float angle = index * 2.39996323;
            float2 offset = float2(
                cos(angle),
                sin(angle)) *
                sqrt(fraction) *
                radius *
                PixelSize;
            float4 sample = SampleNeighborhood(
                uv + offset,
                profile,
                edgeMode);
            float difference = abs(
                dot(
                    Unpremultiply(sample) - centerStraight,
                    float3(0.2126, 0.7152, 0.0722)));
            float weight = !edgeAware ||
                difference <= threshold
                ? 1.0
                : 0.0;
            total += sample * weight;
            totalWeight += weight;
        }
    }
    return total / max(totalWeight, 0.000001);
}

float4 SampleNeighborhoodWhole(
    int profile,
    int sampleCount)
{
    float4 total = 0.0;
    int count = max(1, min(sampleCount, 17));
    for (int index = 0; index < 17; index++)
    {
        if (index < count)
        {
            float2 uv = float2(
                frac((index + 0.5) * 0.618033989),
                frac((index + 0.5) * 0.414213562));
            total += SampleNeighborhood(
                uv,
                profile,
                0);
        }
    }
    return total / count;
}

float NeighborhoodHash(float2 position, float seed)
{
    float value = dot(
        floor(position),
        float2(127.1, 311.7));
    return frac(
        sin(value + (seed * 0.00006103515625)) *
        43758.5453123);
}

float4 NeighborhoodMedian3x3(
    float2 uv,
    int profile)
{
    float4 values[9];
    values[0] = SampleNeighborhood(
        uv + (float2(-1.0, -1.0) * PixelSize),
        profile,
        0);
    values[1] = SampleNeighborhood(
        uv + (float2(0.0, -1.0) * PixelSize),
        profile,
        0);
    values[2] = SampleNeighborhood(
        uv + (float2(1.0, -1.0) * PixelSize),
        profile,
        0);
    values[3] = SampleNeighborhood(
        uv + (float2(-1.0, 0.0) * PixelSize),
        profile,
        0);
    values[4] = SampleNeighborhood(uv, profile, 0);
    values[5] = SampleNeighborhood(
        uv + (float2(1.0, 0.0) * PixelSize),
        profile,
        0);
    values[6] = SampleNeighborhood(
        uv + (float2(-1.0, 1.0) * PixelSize),
        profile,
        0);
    values[7] = SampleNeighborhood(
        uv + (float2(0.0, 1.0) * PixelSize),
        profile,
        0);
    values[8] = SampleNeighborhood(
        uv + (float2(1.0, 1.0) * PixelSize),
        profile,
        0);

    for (int outer = 0; outer < 8; outer++)
    {
        for (int inner = outer + 1; inner < 9; inner++)
        {
            float outerValue = dot(
                values[outer].rgb,
                float3(0.2126, 0.7152, 0.0722));
            float innerValue = dot(
                values[inner].rgb,
                float3(0.2126, 0.7152, 0.0722));
            if (innerValue < outerValue)
            {
                float4 swap = values[outer];
                values[outer] = values[inner];
                values[inner] = swap;
            }
        }
    }
    return values[4];
}

float4 NeighborhoodSharpen(
    float4 center,
    float4 blurred,
    float amount,
    float threshold)
{
    float difference = abs(
        dot(
            Unpremultiply(center) -
                Unpremultiply(blurred),
            float3(0.2126, 0.7152, 0.0722)));
    return difference < threshold
        ? center
        : center + ((center - blurred) * amount);
}

float4 ApplyNeighborhood(
    VertexShaderOutput input,
    float4 center,
    int profile)
{
    int operation = (int)(FilterHeader.x + 0.5);
    int sampleCount =
        (int)(FilterOptions9.z + 0.5);
    int edgeMode =
        NeighborhoodEdgeMode(operation);
    float2 uv = NeighborhoodUnclampedUv(input);
    float2 radius = FilterOptions9.xy;

    if (operation == 0)
    {
        return center;
    }
    if (operation == 1)
    {
        return SampleOptimizedBilinearGaussian(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile);
    }
    if (operation == 2)
    {
        return SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            0.0,
            false);
    }
    if (operation == 3 || operation == 4)
    {
        return SampleNeighborhoodLine(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            operation == 4);
    }
    if (operation == 5)
    {
        float depth = FilterOptions1.w;
        if (FilterHeader.w > 0.5)
        {
            float4 depthSample =
                SampleNeighborhoodResource(uv);
            int depthChannel =
                (int)(FilterOptions1.z + 0.5);
            if (depthChannel == 0)
            {
                depth = dot(
                    depthSample.rgb,
                    float3(0.2126, 0.7152, 0.0722));
            }
            else if (depthChannel == 1)
            {
                depth = depthSample.r;
            }
            else if (depthChannel == 2)
            {
                depth = depthSample.g;
            }
            else if (depthChannel == 3)
            {
                depth = depthSample.b;
            }
            else
            {
                depth = depthSample.a;
            }
        }
        if (FilterOptions2.x > 0.5)
        {
            depth = 1.0 - depth;
        }
        float focus = saturate(
            abs(depth - FilterOptions1.w));
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius * max(focus, 0.05),
            sampleCount,
            0,
            profile,
            0.0,
            false);
        float highlight = step(
            FilterOptions1.y,
            dot(
                Unpremultiply(blurred),
                float3(0.2126, 0.7152, 0.0722)));
        blurred.rgb *=
            1.0 + (highlight * FilterOptions1.x);
        return blurred;
    }
    if (operation == 6)
    {
        float angle = FilterOptions0.y;
        float2 direction = float2(
            cos(angle),
            -sin(angle)) * FilterOptions0.x;
        return SampleNeighborhoodLine(
            uv,
            direction,
            sampleCount,
            edgeMode,
            profile,
            false);
    }
    if (operation == 7)
    {
        float2 centerUv = FilterOptions0.zw;
        float amount = FilterOptions0.y;
        float4 total = 0.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 0; index < 17; index++)
        {
            if (index < count)
            {
                float position = count <= 1
                    ? 0.0
                    : (index / (count - 1.0)) - 0.5;
                float2 sampleUv;
                if (FilterOptions0.x < 0.5)
                {
                    float angle = position * amount;
                    float2 delta = uv - centerUv;
                    float cosine = cos(angle);
                    float sine = sin(angle);
                    sampleUv = centerUv + float2(
                        (delta.x * cosine) -
                            (delta.y * sine),
                        (delta.x * sine) +
                            (delta.y * cosine));
                }
                else
                {
                    sampleUv = lerp(
                        uv,
                        centerUv,
                        position * amount);
                }
                total += SampleNeighborhood(
                    sampleUv,
                    profile,
                    0);
            }
        }
        return total / count;
    }
    if (operation == 8)
    {
        float4 total = center;
        float totalWeight = 1.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 1; index < 17; index++)
        {
            if (index < count)
            {
                float position =
                    (index / max(count - 1.0, 1.0));
                float angle = index * 2.39996323;
                float2 unit = float2(
                    cos(angle),
                    sin(angle));
                float weight = FilterHeader.w > 0.5
                    ? SampleNeighborhoodResource(
                        (unit * 0.5) + 0.5).a
                    : 1.0;
                total += SampleNeighborhood(
                    uv +
                        (unit * position *
                            FilterOptions0.x * PixelSize),
                    profile,
                    edgeMode) * weight;
                totalWeight += weight;
            }
        }
        return total / max(totalWeight, 0.000001);
    }
    if (operation == 9 || operation == 10)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            0,
            profile,
            FilterOptions0.y,
            true);
        if (operation == 9 &&
            FilterOptions0.w > 0.5)
        {
            float edge = saturate(
                length(center.rgb - blurred.rgb) * 4.0);
            float4 edgeColor = float4(
                edge,
                edge,
                edge,
                center.a);
            return FilterOptions0.w > 1.5
                ? lerp(center, edgeColor, edge)
                : edgeColor;
        }
        return blurred;
    }
    if (operation == 11)
    {
        float amount = FilterHeader.w > 0.5
            ? SampleNeighborhoodResource(uv).r
            : 0.0;
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            amount * 24.0,
            sampleCount,
            0,
            profile,
            0.0,
            false);
        return lerp(center, blurred, saturate(amount));
    }
    if (operation == 12)
    {
        float2 delta =
            (uv - FilterOptions0.xy) /
            max(FilterOptions0.zw, 0.000001);
        float angle = -FilterOptions1.y;
        float2 rotated = float2(
            (delta.x * cos(angle)) -
                (delta.y * sin(angle)),
            (delta.x * sin(angle)) +
                (delta.y * cos(angle)));
        float amount = smoothstep(
            1.0,
            1.0 + max(FilterOptions1.x, 0.000001),
            length(rotated));
        return lerp(
            center,
            SampleNeighborhoodDisk(
                uv,
                radius,
                sampleCount,
                0,
                profile,
                0.0,
                false),
            amount);
    }
    if (operation == 13)
    {
        float2 direction = float2(
            -sin(FilterOptions0.z),
            cos(FilterOptions0.z));
        float distance = abs(
            dot(
                uv - FilterOptions0.xy,
                direction));
        float amount = smoothstep(
            FilterOptions0.w,
            FilterOptions0.w +
                max(FilterOptions1.x, 0.000001),
            distance);
        return lerp(
            center,
            SampleNeighborhoodDisk(
                uv,
                radius,
                sampleCount,
                0,
                profile,
                0.0,
                false),
            amount);
    }
    if (operation == 14)
    {
        float4 path = FilterHeader.w > 0.5
            ? SampleNeighborhoodResource(uv)
            : float4(0.5, 0.5, 0.0, 1.0);
        float2 direction =
            (path.rg * 2.0) - 1.0;
        direction /= max(length(direction), 0.000001);
        float speed = lerp(
            FilterOptions0.x,
            FilterOptions0.w,
            saturate(path.b));
        return SampleNeighborhoodLine(
            uv,
            direction * speed,
            sampleCount,
            0,
            profile,
            false);
    }
    if (operation == 15)
    {
        float2 centerUv = FilterOptions0.xy;
        float2 delta = uv - centerUv;
        float2 normalized =
            delta / max(FilterOptions0.zw, 0.000001);
        float inside = step(
            length(normalized),
            1.0);
        float4 total = 0.0;
        int count = max(1, min(sampleCount, 17));
        for (int index = 0; index < 17; index++)
        {
            if (index < count)
            {
                float position = count <= 1
                    ? 0.0
                    : (index / (count - 1.0)) - 0.5;
                float angle =
                    position * FilterOptions1.x;
                float cosine = cos(angle);
                float sine = sin(angle);
                float2 sampleUv = centerUv + float2(
                    (delta.x * cosine) -
                        (delta.y * sine),
                    (delta.x * sine) +
                        (delta.y * cosine));
                total += SampleNeighborhood(
                    sampleUv,
                    profile,
                    0);
            }
        }
        return lerp(
            center,
            total / count,
            inside);
    }
    if (operation == 16 || operation == 17)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            1.0,
            9,
            0,
            profile,
            0.0,
            false);
        float amount = FilterOptions0.x *
            (operation == 17 ? 2.0 : 1.0);
        return NeighborhoodSharpen(
            center,
            blurred,
            amount,
            0.0);
    }
    if (operation == 18)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            1.0,
            9,
            0,
            profile,
            0.0,
            false);
        return NeighborhoodSharpen(
            center,
            blurred,
            FilterOptions0.x,
            FilterOptions0.y);
    }
    if (operation == 19 || operation == 20)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            max(FilterOptions0.y, 0.000001),
            9,
            0,
            profile,
            0.0,
            false);
        float amount = FilterOptions0.x;
        float threshold = operation == 19
            ? FilterOptions0.z
            : 0.0;
        float4 sharpened = NeighborhoodSharpen(
            center,
            blurred,
            amount,
            threshold);
        if (operation == 20)
        {
            sharpened = lerp(
                sharpened,
                blurred,
                saturate(FilterOptions0.z));
        }
        return sharpened;
    }
    if (operation == 21)
    {
        float4 blurred = SampleNeighborhoodDisk(
            uv,
            radius,
            sampleCount,
            edgeMode,
            profile,
            0.0,
            false);
        return float4(
            saturate(
                0.5 * center.a +
                center.rgb -
                blurred.rgb),
            center.a);
    }
    if (operation == 22)
    {
        float seed =
            FilterOptions0.w +
            (FilterOptions1.x * 65536.0);
        float redNoise =
            NeighborhoodHash(input.Position.xy, seed);
        float greenNoise = FilterOptions0.z > 0.5
            ? redNoise
            : NeighborhoodHash(
                input.Position.xy + 19.0,
                seed);
        float blueNoise = FilterOptions0.z > 0.5
            ? redNoise
            : NeighborhoodHash(
                input.Position.xy + 47.0,
                seed);
        float3 noise =
            float3(redNoise, greenNoise, blueNoise) *
            2.0 - 1.0;
        if (FilterOptions0.y > 0.5)
        {
            noise = (
                noise +
                float3(
                    NeighborhoodHash(
                        input.Position.xy + 71.0,
                        seed),
                    NeighborhoodHash(
                        input.Position.xy + 89.0,
                        seed),
                    NeighborhoodHash(
                        input.Position.xy + 107.0,
                        seed)) *
                    2.0 - 1.0) * 0.5;
        }
        float3 straight = saturate(
            Unpremultiply(center) +
            (noise * FilterOptions0.x));
        return float4(
            straight * center.a,
            center.a);
    }
    if (operation == 23 ||
        operation == 24 ||
        operation == 25)
    {
        float4 median = NeighborhoodMedian3x3(
            uv,
            profile);
        if (operation == 25)
        {
            return median;
        }
        float difference = abs(
            dot(
                Unpremultiply(center) -
                    Unpremultiply(median),
                float3(0.2126, 0.7152, 0.0722)));
        float threshold = operation == 23
            ? FilterOptions0.x
            : FilterOptions0.y;
        return difference > threshold
            ? median
            : center;
    }

    float4 denoised = SampleNeighborhoodDisk(
        uv,
        1.0,
        9,
        0,
        profile,
        0.0,
        false);
    float detail = saturate(FilterOptions0.y);
    float4 reduced = lerp(
        denoised,
        center,
        detail);
    reduced = NeighborhoodSharpen(
        reduced,
        denoised,
        FilterOptions0.w,
        0.0);
    return lerp(
        center,
        reduced,
        saturate(FilterOptions0.x));
}

float4 NeighborhoodFilterPixelShader(
    VertexShaderOutput input) : COLOR0
{
    int profile = (int)(FilterHeader.y + 0.5);
    int blendMode =
        (int)(FilterOptions9.w + 0.5);
    float4 source = WorkingAssociatedToLinearSrgb(
        SampleSource(input),
        profile);
    float4 filtered = source;
    if ((int)(FilterHeader.x + 0.5) == 0)
    {
        filtered = SampleNeighborhoodWhole(
            profile,
            (int)(FilterOptions9.z + 0.5));
    }
    else
    {
        filtered = ApplyNeighborhood(
            input,
            source,
            profile);
    }
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

