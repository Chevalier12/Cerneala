struct FragmentInput
{
    float4 Position : SV_Position;
    noperspective float2 ShapePosition : TEXCOORD0;
    nointerpolation float2 ShapeSize : TEXCOORD1;
    nointerpolation float4 Color : TEXCOORD2;
    nointerpolation float Marker : TEXCOORD3;
    nointerpolation float SampleCount : TEXCOORD4;
};

struct FragmentOutput
{
    float4 Color : SV_Target0;
    uint CoverageMask : SV_Coverage;
};

FragmentOutput main(FragmentInput input)
{
    float distanceToShape;
    if (input.Marker > 0.5)
    {
        distanceToShape = length(input.ShapePosition) - input.ShapeSize.y;
    }
    else
    {
        float2 center = float2(input.ShapeSize.x * 0.5, 0);
        float2 q = abs(input.ShapePosition - center) - float2(input.ShapeSize.x * 0.5, input.ShapeSize.y);
        distanceToShape = length(max(q, 0)) + min(max(q.x, q.y), 0);
    }
    float coverage = saturate(0.5 - distanceToShape);
    if (coverage <= 0) discard;
    FragmentOutput output;
    output.Color = float4(input.Color.rgb, 1);
    uint coveredSamples = (uint)round(coverage * input.SampleCount);
    output.CoverageMask = (1u << coveredSamples) - 1u;
    return output;
}
