using System.Buffers.Binary;
using Cerneala.Timbre.Corpus;

namespace Cerneala.Tests.Timbre.Decoding;

// Test-side RIFF/WAVE writer (independent of the decoder) for the WAV matrix
// and its negative variants. Samples come from CorpusSignal, quantized here.
internal static class WavFixture
{
    public static string Write(
        string name,
        int sampleRate,
        int channels,
        int bits,
        bool isFloat = false,
        double seconds = 1.0,
        bool extensible = false,
        string riff = "RIFF",
        ushort? formatTag = null,
        long? declaredDataBytes = null,
        int truncateBytes = 0,
        bool omitData = false,
        string form = "WAVE")
    {
        long frames = (long)Math.Round(seconds * sampleRate);
        float[] samples = CorpusSignal.Render(sampleRate, channels, frames, period: seconds);
        int bytesPerSample = bits / 8;
        byte[] data = new byte[samples.Length * bytesPerSample];
        for (int index = 0; index < samples.Length; index++)
        {
            Span<byte> target = data.AsSpan(index * bytesPerSample, bytesPerSample);
            double value = samples[index];
            switch (bits)
            {
                case 8:
                    target[0] = (byte)Math.Clamp(Math.Round((value * 128.0) + 128.0), 0, 255);
                    break;
                case 16:
                    BinaryPrimitives.WriteInt16LittleEndian(target, (short)Math.Round(value * 32767.0));
                    break;
                case 24:
                    int packed = (int)Math.Round(value * 8388607.0);
                    target[0] = (byte)packed;
                    target[1] = (byte)(packed >> 8);
                    target[2] = (byte)(packed >> 16);
                    break;
                case 32 when isFloat:
                    BinaryPrimitives.WriteSingleLittleEndian(target, (float)value);
                    break;
                case 64:
                    BinaryPrimitives.WriteDoubleLittleEndian(target, value);
                    break;
                default:
                    BinaryPrimitives.WriteInt32LittleEndian(target, (int)Math.Round(value * 2147483647.0));
                    break;
            }
        }

        using MemoryStream stream = new();
        using (BinaryWriter writer = new(stream, System.Text.Encoding.ASCII, leaveOpen: true))
        {
            ushort tag = formatTag ?? (ushort)(extensible ? 0xFFFE : isFloat ? 3 : 1);
            int fmtSize = extensible ? 40 : 16;
            writer.Write(System.Text.Encoding.ASCII.GetBytes(riff));
            writer.Write(0u);
            writer.Write(System.Text.Encoding.ASCII.GetBytes(form));
            writer.Write("fmt "u8);
            writer.Write(fmtSize);
            writer.Write(tag);
            writer.Write((ushort)channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * channels * bytesPerSample);
            writer.Write((ushort)(channels * bytesPerSample));
            writer.Write((ushort)bits);
            if (extensible)
            {
                writer.Write((ushort)22);
                writer.Write((ushort)bits);
                writer.Write(channels == 1 ? 4u : 3u);
                writer.Write((ushort)(isFloat ? 3 : 1));
                writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x80, 0x00, 0x00, 0xAA, 0x00, 0x38, 0x9B, 0x71 });
            }

            writer.Write("LIST"u8);
            writer.Write(4);
            writer.Write("INFO"u8);
            if (!omitData)
            {
                writer.Write("data"u8);
                writer.Write((uint)(declaredDataBytes ?? data.Length));
                writer.Write(data);
            }
        }

        byte[] file = stream.ToArray();
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), (uint)(file.Length - 8));
        string path = Path.Combine(Path.GetTempPath(), "cerneala-timbre-wav", name);
        System.IO.Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, file[..(file.Length - truncateBytes)]);
        return path;
    }
}
