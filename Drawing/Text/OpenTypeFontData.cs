using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HarfBuzzSharp;
using SkiaSharp;

namespace Cerneala.Drawing.Text;

internal readonly record struct OpenTypeFontData(byte[] Bytes, uint FaceIndex)
{
    private static readonly ConditionalWeakTable<SKTypeface, Lazy<OpenTypeFontData>> Cache = new();

    public static OpenTypeFontData Read(SkiaFont font)
    {
        return Cache.GetValue(
            font.Typeface,
            static typeface => new Lazy<OpenTypeFontData>(
                () => ReadTypeface(typeface),
                LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    private static OpenTypeFontData ReadTypeface(SKTypeface typeface)
    {
        using SKStreamAsset stream = typeface.OpenStream(out int faceIndex);
        byte[] bytes = new byte[stream.Length];
        int read = stream.Read(bytes, bytes.Length);
        if (read != bytes.Length)
        {
            Array.Resize(ref bytes, read);
        }

        return new OpenTypeFontData(bytes, checked((uint)Math.Max(0, faceIndex)));
    }

    public Blob CreatePinnedBlob()
    {
        if (Bytes.Length == 0)
        {
            throw new InvalidOperationException("Cannot read metrics or shape text because the font data is empty.");
        }

        GCHandle handle = GCHandle.Alloc(Bytes, GCHandleType.Pinned);
        bool released = false;
        return new Blob(handle.AddrOfPinnedObject(), Bytes.Length, MemoryMode.ReadOnly, () =>
        {
            if (released)
            {
                return;
            }

            released = true;
            handle.Free();
        });
    }
}
