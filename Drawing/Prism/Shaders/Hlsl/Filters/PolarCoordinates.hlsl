float4 ApplyPolarCoordinates(VertexShaderOutput input, float4 source, int profile)
{
    float2 uv = ResolveUv(input);
    return SamplePolarEwa(uv, MapPolarCoordinate(uv), profile);
}
