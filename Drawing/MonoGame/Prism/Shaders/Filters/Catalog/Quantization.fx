uint CatalogQuantizationSeed()
{
    uint low = (uint)FilterOptions1.x;
    uint high = (uint)FilterOptions1.y;
    return (low & 0xffffu) | (high << 16);
}

float CatalogCrystallizeHash(int2 cellIndex, uint seed)
{
    uint value =
        ((uint)cellIndex.x * 0x9e3779b9u) ^
        ((uint)cellIndex.y * 0x85ebca6bu) ^
        seed;
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return (value & 0x00ffffffu) / 16777215.0;
}

float4 CatalogFacetLinearSample(float2 uv, int profile)
{
    float2 clampedUv = clamp(
        uv,
        PixelSize * 0.5,
        1.0 - (PixelSize * 0.5));
    return WorkingAssociatedToLinearSrgb(
        tex2Dlod(
            SpriteTextureSampler,
            float4(clampedUv, 0.0, 0.0)),
        profile);
}

float3 CatalogFacetStraight(float2 uv, int profile)
{
    float4 sample = CatalogFacetLinearSample(uv, profile);
    return sample.a <= 0.000001
        ? 0.0
        : saturate(Unpremultiply(sample));
}

float3 CatalogFacetStructureTensor(float2 uv, int profile)
{
    float3 topLeft = CatalogFacetStraight(
        uv + float2(-PixelSize.x, -PixelSize.y),
        profile);
    float3 top = CatalogFacetStraight(
        uv + float2(0.0, -PixelSize.y),
        profile);
    float3 topRight = CatalogFacetStraight(
        uv + float2(PixelSize.x, -PixelSize.y),
        profile);
    float3 left = CatalogFacetStraight(
        uv + float2(-PixelSize.x, 0.0),
        profile);
    float3 right = CatalogFacetStraight(
        uv + float2(PixelSize.x, 0.0),
        profile);
    float3 bottomLeft = CatalogFacetStraight(
        uv + float2(-PixelSize.x, PixelSize.y),
        profile);
    float3 bottom = CatalogFacetStraight(
        uv + float2(0.0, PixelSize.y),
        profile);
    float3 bottomRight = CatalogFacetStraight(
        uv + PixelSize,
        profile);
    float3 horizontal =
        -topLeft + topRight -
        (2.0 * left) + (2.0 * right) -
        bottomLeft + bottomRight;
    float3 vertical =
        -topLeft - (2.0 * top) - topRight +
        bottomLeft + (2.0 * bottom) + bottomRight;
    return float3(
        dot(horizontal, horizontal),
        dot(horizontal, vertical),
        dot(vertical, vertical));
}

static const uint CatalogMezzotintDispersedRanks[64] =
{
    0x4d6ce734u, 0xd1551fe1u, 0x79f56927u, 0xc8108cceu,
    0x17c50286u, 0x75fbab8bu, 0x19afdb45u, 0x6341e456u,
    0x3ff69bb3u, 0xc30630d7u, 0xbe600d9au, 0xda74aa33u,
    0x667a512du, 0x7d5c96bbu, 0xfe8938edu, 0x18f00794u,
    0x0c29eac0u, 0x1644f282u, 0x2854e0b0u, 0x834ecd6fu,
    0x4bdfa859u, 0xd02ba1cau, 0xd59f1b6au, 0x9725ac40u,
    0xb48f6d05u, 0x8ee5681cu, 0x0b81c74au, 0xfcc662ebu,
    0xf9143ed6u, 0xad007b3au, 0xb75d35fau, 0x31761a8du,
    0x9d5fbc7eu, 0x26c25acfu, 0xf1a31372u, 0xa64fd439u,
    0x20de47f4u, 0x9346e884u, 0x774cd2e2u, 0x0eba9503u,
    0x36927024u, 0x2ea40fb5u, 0xb22a8761u, 0x88e65eddu,
    0xef04b1ccu, 0xcbf76b53u, 0x42fdbd08u, 0x32521e71u,
    0xa05be367u, 0x3c7c1dc1u, 0x129957aeu, 0x98f3a9c9u,
    0x4380153bu, 0xdc4990d3u, 0xe97f226eu, 0xbf09852fu,
    0x2cff91d9u, 0x119cec0au, 0xd850c4eeu, 0x7348b965u,
    0xa7b62158u, 0x37b86478u, 0x3d01a58au, 0xa2f8239eu
};

static const uint CatalogMezzotintGrainyRanks[64] =
{
    0x9d23bb02u, 0x6a2557c7u, 0x17dd4dc6u, 0xb3d54f73u,
    0x62fb4c70u, 0xdc84e637u, 0x9264b42eu, 0x912baae8u,
    0xc0118ddeu, 0x18b50590u, 0x34f2407au, 0x42f60e5eu,
    0x467db71eu, 0xfe496eebu, 0xa501bf86u, 0xb18778d6u,
    0xcd35f36du, 0x3dc48e22u, 0x6cce4e9bu, 0x3acb5124u,
    0x5aac0485u, 0xda0ab079u, 0x8c28ed20u, 0xe316a1fau,
    0xe243d49fu, 0x5c7ef519u, 0xb85b81a9u, 0x44bd630cu,
    0xa76f215fu, 0x26be3288u, 0x3fe407c8u, 0xea3683cau,
    0x0ffd80c5u, 0xe74771ccu, 0x72a34b89u, 0x09aef02du,
    0x9641ad4au, 0x1da0ee3bu, 0xd915fc67u, 0x99651b8fu,
    0x56cf12efu, 0xd75300bcu, 0x50b93c9au, 0x33df7bafu,
    0xe52a9375u, 0x60c18b68u, 0x2c74d010u, 0xba0654f4u,
    0x9e61c239u, 0x7c27f11cu, 0xc39848ecu, 0x5dd2951au,
    0x52f713dbu, 0xa2c945b2u, 0x5903b629u, 0xa43069e0u,
    0x0d976b8au, 0x580877e1u, 0x82f866d8u, 0x1fffab38u,
    0x76d33ee9u, 0x94f9a62fu, 0xa8319c14u, 0x557f0bd1u
};

static const uint CatalogPointillizeRanks[64] =
{
    0x8a208000u, 0xc42e830cu, 0xb32d8f03u, 0xea3aee0au,
    0x79b55096u, 0x51f655e4u, 0x6b856efeu, 0x7fb941fdu,
    0xa3138c37u, 0xf91ca633u, 0x9410902fu, 0xd118863du,
    0x66b04f9cu, 0x68e0578bu, 0x60b667fbu, 0x5bef4aadu,
    0xa026840fu, 0xa125c605u, 0xac31aa08u, 0xa536bd06u,
    0x70e548edu, 0x569a7da2u, 0x71a959d7u, 0x4b8745c0u,
    0xb11ef539u, 0x981dca38u, 0xf21bb729u, 0xe617e732u,
    0x5faf7af7u, 0x78fa76fcu, 0x43cc53bfu, 0x479558e2u,
    0xd83c9e02u, 0xd5308d09u, 0xda2ab201u, 0x933b920bu,
    0x5ccb52cfu, 0x77ab4697u, 0x4dbb6ff4u, 0x619f63bcu,
    0xd916c13eu, 0xc2158924u, 0xd411de2bu, 0xd61af021u,
    0x75be4cc5u, 0x62d344cdu, 0x64d25ef1u, 0x72db7bd0u,
    0xeb22dc0eu, 0xba279b07u, 0xa42c820du, 0xff288e04u,
    0x4e885499u, 0x73df40ecu, 0x65e86cddu, 0x49f86a81u,
    0xc914b434u, 0xc319a723u, 0xe312b835u, 0xe11fce3fu,
    0x69a874c7u, 0x5ac842e9u, 0x7e915daeu, 0x6d9d7cf3u
};

uint CatalogMezzotintMix(uint value)
{
    value ^= value >> 16;
    value *= 0x7feb352du;
    value ^= value >> 15;
    value *= 0x846ca68bu;
    value ^= value >> 16;
    return value;
}

float CatalogMezzotintThreshold(float2 pixel)
{
    uint extent =
        (uint)max(floor(FilterOptions2.x + 0.5), 1.0);
    uint thickness =
        (uint)max(floor(FilterOptions2.y + 0.5), 1.0);
    uint patternKind =
        (uint)max(floor(FilterOptions2.z + 0.5), 0.0);
    uint phase = CatalogMezzotintMix(
        CatalogQuantizationSeed() ^
        (patternKind * 0x9e3779b9u));
    bool vertical =
        patternKind >= 2u &&
        (phase & 0x100u) != 0u;
    float primary = vertical ? pixel.y : pixel.x;
    float secondary = vertical ? pixel.x : pixel.y;
    uint matrixX =
        ((uint)floor(primary / extent) +
            (phase & 15u)) &
        15u;
    uint matrixY =
        ((uint)floor(secondary / thickness) +
            ((phase >> 4) & 15u)) &
        15u;
    uint index = (matrixY * 16u) + matrixX;
    uint packedIndex = index >> 2;
    uint packed =
        patternKind == 1u
            ? CatalogMezzotintGrainyRanks[packedIndex]
            : CatalogMezzotintDispersedRanks[packedIndex];
    uint rank =
        (packed >> ((index & 3u) * 8u)) &
        0xffu;
    return (rank + 0.5) / 256.0;
}

uint CatalogPointillizeSeed()
{
    uint low = (uint)FilterOptions2.x;
    uint high = (uint)FilterOptions2.y;
    return (low & 0xffffu) | (high << 16);
}

uint CatalogPointillizeRank(int2 cellIndex, uint seed)
{
    int x =
        (cellIndex.x + (int)(seed & 15u)) & 15;
    int y =
        (cellIndex.y + (int)((seed >> 4) & 15u)) & 15;
    uint transform = (seed >> 8) & 7u;
    if ((transform & 4u) != 0u)
    {
        int exchanged = x;
        x = y;
        y = exchanged;
    }
    if ((transform & 1u) != 0u)
    {
        x = 15 - x;
    }
    if ((transform & 2u) != 0u)
    {
        y = 15 - y;
    }

    uint index = (uint)((y * 16) + x);
    uint packed = CatalogPointillizeRanks[index >> 2];
    return
        (packed >> ((index & 3u) * 8u)) &
        0xffu;
}

float2 CatalogPointillizeCenter(
    int2 cellIndex,
    uint seed,
    float cellSize)
{
    return (
        (float2)cellIndex +
        0.15 +
        (0.7 *
            float2(
                CatalogCrystallizeHash(
                    cellIndex,
                    seed ^ 0x13579bdfu),
                CatalogCrystallizeHash(
                    cellIndex,
                    seed ^ 0x2468ace0u)))) *
        cellSize;
}

float4 CatalogPointillizeAverage(
    float2 center,
    float radius,
    int profile)
{
    float offset = min(radius * 0.35, 1.5);
    float4 result =
        CatalogLinearSample(
            (center + 0.5) * PixelSize,
            profile) +
        CatalogLinearSample(
            (center + float2(-offset, 0.0) + 0.5) *
                PixelSize,
            profile) +
        CatalogLinearSample(
            (center + float2(offset, 0.0) + 0.5) *
                PixelSize,
            profile) +
        CatalogLinearSample(
            (center + float2(0.0, -offset) + 0.5) *
                PixelSize,
            profile) +
        CatalogLinearSample(
            (center + float2(0.0, offset) + 0.5) *
                PixelSize,
            profile);
    result /= 5.0;
    result.a = saturate(result.a);
    result.rgb = clamp(result.rgb, 0.0, result.a);
    return result;
}
