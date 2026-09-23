struct VertexInput
{
    float3 Start : TEXCOORD0;
    float3 End : TEXCOORD1;
    float4 Color : TEXCOORD2;
    float2 Corner : TEXCOORD3;
    float2 Parameters : TEXCOORD4; // diameter/thickness in physical pixels, marker flag
};

struct VertexOutput
{
    float4 Position : SV_Position;
    noperspective float2 ShapePosition : TEXCOORD0;
    nointerpolation float2 ShapeSize : TEXCOORD1;
    nointerpolation float4 Color : TEXCOORD2;
    nointerpolation float Marker : TEXCOORD3;
    nointerpolation float SampleCount : TEXCOORD4;
};

cbuffer Surface3DUniforms : register(b0, space1)
{
    row_major float4x4 Model;
    row_major float4x4 View;
    row_major float4x4 Projection;
    float2 ViewportSize;
    float SampleCount;
    float Padding;
};

bool ClipPlane(float first, float last, inout float begin, inout float end)
{
    if (first < 0 && last < 0) return false;
    float difference = last - first;
    if (first < 0) begin = max(begin, -first / difference);
    else if (last < 0) end = min(end, -first / difference);
    return begin <= end;
}

bool ClipSegment(float4 a, float4 b, float2 sideMargin, out float4 start, out float4 finish)
{
    float begin = 0;
    float end = 1;
    if (!ClipPlane(a.x + a.w * (1 + sideMargin.x), b.x + b.w * (1 + sideMargin.x), begin, end) ||
        !ClipPlane(a.w * (1 + sideMargin.x) - a.x, b.w * (1 + sideMargin.x) - b.x, begin, end) ||
        !ClipPlane(a.y + a.w * (1 + sideMargin.y), b.y + b.w * (1 + sideMargin.y), begin, end) ||
        !ClipPlane(a.w * (1 + sideMargin.y) - a.y, b.w * (1 + sideMargin.y) - b.y, begin, end) ||
        !ClipPlane(a.z, b.z, begin, end) ||
        !ClipPlane(a.w - a.z, b.w - b.z, begin, end))
    {
        start = finish = float4(2, 2, 2, 1);
        return false;
    }
    start = lerp(a, b, begin);
    finish = lerp(a, b, end);
    return start.w > 0 && finish.w > 0;
}

VertexOutput main(VertexInput input)
{
    VertexOutput output;
    float4 a = mul(mul(mul(float4(input.Start, 1), Model), View), Projection);
    float4 b = mul(mul(mul(float4(input.End, 1), Model), View), Projection);
    output.Color = input.Color;
    output.Marker = input.Parameters.y;
    output.SampleCount = SampleCount;
    output.ShapeSize = float2(0, input.Parameters.x * 0.5);
    output.ShapePosition = 0;
    if (input.Parameters.y > 0.5)
    {
        if (a.w <= 0 || a.z < 0 || a.z > a.w)
        {
            output.Position = float4(2, 2, 2, 1);
            return output;
        }
        float radius = input.Parameters.x * 0.5;
        float2 pixelOffset = input.Corner * (radius + 0.5);
        output.Position = a;
        output.Position.xy += pixelOffset * (2.0 / ViewportSize) * a.w;
        output.ShapePosition = pixelOffset;
        return output;
    }

    float4 clippedA;
    float4 clippedB;
    float2 sideMargin = (input.Parameters.x * 0.5 + 1) * (2.0 / ViewportSize);
    if (!ClipSegment(a, b, sideMargin, clippedA, clippedB))
    {
        output.Position = float4(2, 2, 2, 1);
        return output;
    }
    float2 firstPixel = clippedA.xy / clippedA.w * ViewportSize * 0.5;
    float2 lastPixel = clippedB.xy / clippedB.w * ViewportSize * 0.5;
    float2 direction = lastPixel - firstPixel;
    float lengthInPixels = length(direction);
    if (lengthInPixels < 0.0001)
    {
        output.Position = float4(2, 2, 2, 1);
        return output;
    }
    float2 tangent = direction / lengthInPixels;
    float2 normal = float2(-tangent.y, tangent.x);
    float halfWidth = input.Parameters.x * 0.5;
    float longitudinal = input.Corner.x > 0.5 ? lengthInPixels + 0.5 : -0.5;
    float lateral = input.Corner.y * (halfWidth + 0.5);
    float4 endpoint = input.Corner.x > 0.5 ? clippedB : clippedA;
    float2 pixelOffset = tangent * (input.Corner.x > 0.5 ? 0.5 : -0.5) + normal * lateral;
    output.Position = endpoint;
    output.Position.xy += pixelOffset * (2.0 / ViewportSize) * endpoint.w;
    output.ShapePosition = float2(longitudinal, lateral);
    output.ShapeSize = float2(lengthInPixels, halfWidth);
    return output;
}
