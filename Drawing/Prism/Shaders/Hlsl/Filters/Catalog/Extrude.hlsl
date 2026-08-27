



float ExtrudeHash(float2 cell)
{
    int2 integralCell = (int2)floor(cell);
    uint low = (uint)FilterOptions3.x;
    uint high = (uint)FilterOptions3.y;
    uint seed = (low & 0xffffu) | (high << 16);
    return CatalogIntegerHash(
        integralCell.x,
        integralCell.y,
        seed);
}

float ExtrudeCellDepth(float2 cell, float depth)
{
    if (depth <= 0.0)
    {
        return 0.0;
    }

    float level = FilterOptions1.x >= 0.5
        ? 1.0
        : 0.45 + (ExtrudeHash(cell) * 0.55);
    return depth * level;
}

bool ExtrudeCompleteCell(float2 cell, float size)
{
    float2 lowerRight = (cell + 1.0) * size;
    return cell.x >= 0.0 &&
        cell.y >= 0.0 &&
        lowerRight.x <= FilterTextureSize.x + 0.0001 &&
        lowerRight.y <= FilterTextureSize.y + 0.0001;
}

float4 ExtrudeCellSample(float2 cell, float size, int profile)
{
    return CatalogLinearSample(
        ((cell + 0.5) * size) * PixelSize,
        profile);
}

float4 ExtrudeShadeFace(float4 color, float shade)
{
    return float4(
        saturate(Unpremultiply(color) * shade) * color.a,
        color.a);
}

float ExtrudeCross(float2 left, float2 right)
{
    return (left.x * right.y) - (left.y * right.x);
}

bool ExtrudePointInTriangle(
    float2 position,
    float2 first,
    float2 second,
    float2 third)
{
    float firstCross = ExtrudeCross(second - first, position - first);
    float secondCross = ExtrudeCross(third - second, position - second);
    float thirdCross = ExtrudeCross(first - third, position - third);
    bool hasNegative =
        firstCross < -0.0001 ||
        secondCross < -0.0001 ||
        thirdCross < -0.0001;
    bool hasPositive =
        firstCross > 0.0001 ||
        secondCross > 0.0001 ||
        thirdCross > 0.0001;
    return !(hasNegative && hasPositive);
}

int ExtrudePyramidFace(
    float2 position,
    float2 topLeft,
    float2 bottomRight,
    float2 apex)
{
    float2 topRight = float2(bottomRight.x, topLeft.y);
    float2 bottomLeft = float2(topLeft.x, bottomRight.y);
    if (ExtrudePointInTriangle(position, topLeft, topRight, apex))
    {
        return 0;
    }
    if (ExtrudePointInTriangle(position, topRight, bottomRight, apex))
    {
        return 1;
    }
    if (ExtrudePointInTriangle(position, bottomRight, bottomLeft, apex))
    {
        return 2;
    }
    return ExtrudePointInTriangle(position, bottomLeft, topLeft, apex)
        ? 3
        : -1;
}

float4 CatalogExtrude(
    float2 uv,
    float4 source,
    int filterId,
    int profile)
{
    float2 pixel = floor((uv / PixelSize) + 0.0001) + 0.5;
    float size = max(FilterOptions4.x, 1.0);
    float depth = clamp(FilterOptions0.x, 0.0, size);
    float2 cell = floor(pixel / size);
    float2 cellOrigin = cell * size;
    float2 local = pixel - cellOrigin;
    bool maskIncompleteBlocks = FilterOptions2.x >= 0.5;
    bool solidFrontFaces = FilterOptions5.x >= 0.5;
    bool complete = ExtrudeCompleteCell(cell, size);
    int type = (int)(FilterOptions6.x + 0.5);
    float4 result = maskIncompleteBlocks && !complete
        ? 0.0
        : source;

    if (type == 0 &&
        solidFrontFaces &&
        (!maskIncompleteBlocks || complete))
    {
        result = ExtrudeCellSample(cell, size, profile);
    }
    else if (type == 1 &&
        (!maskIncompleteBlocks || complete))
    {
        float currentDepth = ExtrudeCellDepth(cell, depth);
        float2 apex =
            cellOrigin +
            (size * 0.5) +
            (currentDepth * 0.75);
        int face = ExtrudePyramidFace(
            pixel,
            cellOrigin,
            min(cellOrigin + size, FilterTextureSize),
            apex);
        if (face >= 0)
        {
            float shade = face == 0
                ? 0.84
                : face == 1
                    ? 0.68
                    : face == 2
                        ? 0.54
                        : 0.74;
            float4 faceColor = solidFrontFaces
                ? ExtrudeCellSample(cell, size, profile)
                : source;
            result = ExtrudeShadeFace(faceColor, shade);
        }
    }

    float bestSideScore = -1.0;
    float2 leftCell = cell + float2(-1.0, 0.0);
    if (!maskIncompleteBlocks ||
        ExtrudeCompleteCell(leftCell, size))
    {
        float leftDepth = ExtrudeCellDepth(leftCell, depth);
        float leftOffset = leftDepth * 0.75;
        if (leftOffset > 0.0 &&
            local.x <= leftOffset &&
            local.y >= local.x)
        {
            float sidePosition = local.x / leftOffset;
            bestSideScore = leftDepth + (sidePosition * 0.001);
            float shade = type == 1
                ? 0.68
                : 0.76 - (sidePosition * 0.16);
            result = ExtrudeShadeFace(
                ExtrudeCellSample(leftCell, size, profile),
                shade);
        }
    }

    float2 topCell = cell + float2(0.0, -1.0);
    if (!maskIncompleteBlocks ||
        ExtrudeCompleteCell(topCell, size))
    {
        float topDepth = ExtrudeCellDepth(topCell, depth);
        float topOffset = topDepth * 0.75;
        if (topOffset > 0.0 &&
            local.y <= topOffset &&
            local.x >= local.y)
        {
            float sidePosition = local.y / topOffset;
            float sideScore = topDepth + (sidePosition * 0.001);
            if (sideScore > bestSideScore)
            {
                float shade = type == 1
                    ? 0.54
                    : 0.58 - (sidePosition * 0.14);
                result = ExtrudeShadeFace(
                    ExtrudeCellSample(topCell, size, profile),
                    shade);
            }
        }
    }

    return result;
}
