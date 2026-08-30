using System.Numerics;
using System.Runtime.InteropServices;

namespace Cerneala.Backends.SdlGpu;

internal sealed class SdlGpuPrismUniforms
{
    internal const int VectorCount = 59;
    internal const int ByteCount = VectorCount * 16;

    private readonly Vector4[] values = new Vector4[VectorCount];
    private readonly byte[] packedValues = new byte[ByteCount];

    public Vector4 this[int index]
    {
        get => values[index];
        set => values[index] = value;
    }

    public Span<Vector4> Values => values;

    public byte[] Pack()
    {
        MemoryMarshal.AsBytes(values.AsSpan()).CopyTo(packedValues);
        return packedValues;
    }

    public void Reset()
    {
        Array.Clear(values);
        values[2] = Vector4.One;
        values[4] = new Vector4(0, 0, 1, 1);
        values[5] = new Vector4(0, 0, 1, 1);
        values[6] = new Vector4(1, 0, 1, 0);
        values[7] = new Vector4(1, 0, 0, 0);
        values[8] = new Vector4(0, 1, 0, 0);
        values[10] = Vector4.One;
        values[11] = Vector4.One;
        values[20] = new Vector4(1, 0, 0, 0);
        values[21] = new Vector4(0, 1, 0, 0);
        values[34] = new Vector4(1, 1, 0, 0);
    }

    internal static int OffsetOfVector(int vectorIndex)
    {
        if ((uint)vectorIndex >= VectorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(vectorIndex));
        }
        return vectorIndex * 16;
    }
}
