using System.Security.Cryptography;
using System.Text.Json;
using Cerneala.Timbre.Corpus;
using Concentus.Enums;
using Concentus.Structs;
using NAudio.Lame;
using NAudio.Wave;
using OggVorbisEncoder;

// Regenerates the committed Timbre decoding corpus:
//   dotnet run --project tests/Fixtures/TimbreCorpusGenerator -c Release -- tests/Cerneala.Tests.Timbre/Corpus
// The output is deterministic for the pinned encoder versions; the manifest
// records each file's hash, encoder, parameters and oracle signal.
string outputDirectory = Path.GetFullPath(args.Length > 0 ? args[0] : "Corpus");
Directory.CreateDirectory(outputDirectory);
List<CorpusEntry> entries = [];
const double ShortSeconds = 2.0;
const double LongSeconds = 300.0;
const double LongPeriod = 10.0;

// ---- MP3 (LAME 3.100) ----
Mp3("mp3-mpeg1-44100-stereo-cbr128.mp3", 44100, 2, ShortSeconds, ShortSeconds, cbrKbps: 128);
Mp3("mp3-mpeg1-48000-mono-cbr64.mp3", 48000, 1, ShortSeconds, ShortSeconds, cbrKbps: 64);
Mp3("mp3-mpeg1-32000-stereo-vbr.mp3", 32000, 2, ShortSeconds, ShortSeconds, cbrKbps: null);
Mp3("mp3-mpeg2-22050-mono-vbr.mp3", 22050, 1, ShortSeconds, ShortSeconds, cbrKbps: null);
Mp3("mp3-mpeg2-16000-stereo-cbr48.mp3", 16000, 2, ShortSeconds, ShortSeconds, cbrKbps: 48);
Mp3("mp3-mpeg25-8000-mono-cbr16.mp3", 8000, 1, ShortSeconds, ShortSeconds, cbrKbps: 16);
Mp3("mp3-long-22050-mono-cbr64.mp3", 22050, 1, LongSeconds, LongPeriod, cbrKbps: 64);
// 192-byte frames carry 156 reservoir bytes each, so the bytes fed to
// NLayer reach a multiple of its 8192-byte ring exactly at frame 2048.
Mp3("mp3-reservoir-wrap-48000-stereo-cbr64.mp3", 48000, 2, 55.0, LongPeriod, cbrKbps: 64);
byte[] tagged = File.ReadAllBytes(Path.Combine(outputDirectory, "mp3-mpeg1-44100-stereo-cbr128.mp3"));
Derived("mp3-no-info-tag-44100-stereo.mp3", "mp3-mpeg1-44100-stereo-cbr128.mp3", "first frame (LAME Info tag) removed: no gapless metadata, length unknown", StripFirstFrame(tagged));
Derived("mp3-id3v2-id3v1-44100-stereo.mp3", "mp3-mpeg1-44100-stereo-cbr128.mp3", "1000-byte ID3v2.3 tag prepended and 128-byte ID3v1 tag appended", [.. Id3v2(1000), .. tagged, .. Id3v1()]);
Derived("mp3-truncated.mp3", "mp3-mpeg1-44100-stereo-cbr128.mp3", "cut at 60% of the file, inside a frame", tagged[..(tagged.Length * 6 / 10)]);
byte[] corruptSync = (byte[])tagged.Clone();
Array.Clear(corruptSync, corruptSync.Length / 2, 600);
Derived("mp3-corrupt-sync.mp3", "mp3-mpeg1-44100-stereo-cbr128.mp3", "600 zero bytes written at the middle: frame sync lost mid-stream", corruptSync);
Synthetic("mpeg-layer2-44100-stereo.mp2", "MPEG-1 Layer II frames with zero payload (excluded variant: not MP3)", LayerTwoFrames(40));

// ---- Ogg Vorbis (OggVorbisEncoder) ----
Vorbis("vorbis-44100-stereo-q4.ogg", 44100, 2, ShortSeconds, ShortSeconds, 0.4f, serial: 0x1001);
Vorbis("vorbis-48000-stereo-q2.ogg", 48000, 2, ShortSeconds, ShortSeconds, 0.2f, serial: 0x1002);
Vorbis("vorbis-22050-mono-q2.ogg", 22050, 1, ShortSeconds, ShortSeconds, 0.2f, serial: 0x1003);
Vorbis("vorbis-8000-mono-q2.ogg", 8000, 1, ShortSeconds, ShortSeconds, 0.2f, serial: 0x1004);
Vorbis("vorbis-96000-stereo-q4.ogg", 96000, 2, ShortSeconds, ShortSeconds, 0.4f, serial: 0x1005);
Vorbis("vorbis-192000-stereo-q2.ogg", 192000, 2, ShortSeconds, ShortSeconds, 0.2f, serial: 0x1007);
Vorbis("vorbis-16000-mono-q2.ogg", 16000, 1, ShortSeconds, ShortSeconds, 0.2f, serial: 0x1008);
Vorbis("vorbis-long-22050-mono-q2.ogg", 22050, 1, LongSeconds, LongPeriod, 0.2f, serial: 0x1006);
byte[] vorbis = File.ReadAllBytes(Path.Combine(outputDirectory, "vorbis-44100-stereo-q4.ogg"));
Derived("vorbis-truncated.ogg", "vorbis-44100-stereo-q4.ogg", "cut at 60% of the file, inside a page", vorbis[..(vorbis.Length * 6 / 10)]);
Derived("vorbis-corrupt-crc.ogg", "vorbis-44100-stereo-q4.ogg", "one body byte of the middle audio page inverted: CRC mismatch", CorruptMiddlePage(vorbis));

// ---- Ogg Opus (Concentus encoder) ----
Opus("opus-stereo-48000-96k.opus", 2, 48000, ShortSeconds, ShortSeconds, 96000, serial: 0x2001, extraPreSkip: 0);
Opus("opus-mono-16000-24k.opus", 1, 16000, ShortSeconds, ShortSeconds, 24000, serial: 0x2002, extraPreSkip: 0);
Opus("opus-stereo-preskip-trim.opus", 2, 48000, 1.2345, ShortSeconds, 64000, serial: 0x2003, extraPreSkip: 3840);
Opus("opus-long-mono-32k.opus", 1, 48000, LongSeconds, LongPeriod, 32000, serial: 0x2004, extraPreSkip: 0);
byte[] opus = File.ReadAllBytes(Path.Combine(outputDirectory, "opus-stereo-48000-96k.opus"));
Derived("opus-truncated.opus", "opus-stereo-48000-96k.opus", "cut at 60% of the file, inside a page", opus[..(opus.Length * 6 / 10)]);

// ---- Excluded Ogg variants ----
byte[] vorbisMono = File.ReadAllBytes(Path.Combine(outputDirectory, "vorbis-48000-stereo-q2.ogg"));
Derived("ogg-chained-vorbis.ogg", "vorbis-44100-stereo-q4.ogg + vorbis-48000-stereo-q2.ogg", "two logical streams one after another (chained)", [.. vorbis, .. vorbisMono]);
Derived("ogg-multiplexed-vorbis-opus.ogg", "vorbis-44100-stereo-q4.ogg + opus-stereo-48000-96k.opus", "two logical streams with interleaved pages (multiplexed)", Multiplex(vorbis, opus));
Synthetic("ogg-flac.ogg", "Ogg BOS page carrying a FLAC mapping header (codec not supported)", OggHeaderOnly(0x3001, [0x7F, .. "FLAC"u8, 1, 0, 0, 1, .. "fLaC"u8]));
Synthetic("opus-surround-6ch.opus", "OpusHead with 6 channels, mapping family 1 (surround excluded)", OggHeaderOnly(0x3002, OpusHead(6, 312, 48000, mappingFamily: 1)));

string manifestPath = Path.Combine(outputDirectory, "corpus-manifest.json");
File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
{
    generator = "tests/Fixtures/TimbreCorpusGenerator",
    command = "dotnet run --project tests/Fixtures/TimbreCorpusGenerator -c Release -- tests/Cerneala.Tests.Timbre/Corpus",
    encoders = new
    {
        mp3 = "LAME 3.100 (libmp3lame.64.dll from NAudio.Lame 2.1.0, SHA-256 71dba147b6c16d8d0f012b8e657d52e12cb8796a659e1151200d0a6c8b04bce3)",
        vorbis = "OggVorbisEncoder 1.2.2 (MIT)",
        opus = "Concentus 2.2.2 managed OpusEncoder (BSD-3-Clause)",
        ogg = "TimbreCorpusGenerator/OggWriter.cs",
    },
    signal = "CorpusSignal.Sample: channel 0 chirp 200→1200 Hz amplitude 0.5, channel 1 chirp 1500→400 Hz amplitude 0.3, repeating every `period` seconds; mono files carry channel 0",
    files = entries,
}, new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
Console.WriteLine($"{entries.Count} files written to {outputDirectory}");

void Mp3(string name, int sampleRate, int channels, double seconds, double period, int? cbrKbps)
{
    long frames = (long)Math.Round(seconds * sampleRate);
    float[] samples = CorpusSignal.Render(sampleRate, channels, frames, period);
    byte[] pcm = new byte[samples.Length * 2];
    for (int index = 0; index < samples.Length; index++)
    {
        BitConverter.TryWriteBytes(pcm.AsSpan(index * 2), (short)Math.Round(samples[index] * 32767.0));
    }

    using MemoryStream output = new();
    LameConfig config = cbrKbps is int kbps
        ? new LameConfig { BitRate = kbps, OutputSampleRate = sampleRate, Mode = channels == 1 ? MPEGMode.Mono : MPEGMode.JointStereo }
        : new LameConfig { Preset = LAMEPreset.V2, OutputSampleRate = sampleRate, Mode = channels == 1 ? MPEGMode.Mono : MPEGMode.JointStereo };
    using (LameMP3FileWriter writer = new(output, new WaveFormat(sampleRate, 16, channels), config))
    {
        writer.Write(pcm, 0, pcm.Length);
    }

    Write(name, output.ToArray(), new
    {
        codec = "mp3",
        sampleRate,
        channels,
        frames,
        period,
        mode = cbrKbps is int value ? $"CBR {value} kbps" : "VBR preset V2",
        // LAME omits its Info tag when the first frame is too small to hold it.
        gapless = HasInfoTag(output.ToArray()) ? "LAME Info/Xing tag with encoder delay and padding" : "none: no Info tag (decoded output keeps LAME's 576-frame encoder delay plus the 529-frame decoder delay)",
    });
}

void Vorbis(string name, int sampleRate, int channels, double seconds, double period, float quality, int serial)
{
    long frames = (long)Math.Round(seconds * sampleRate);
    float[] samples = CorpusSignal.Render(sampleRate, channels, frames, period);
    VorbisInfo info = VorbisInfo.InitVariableBitRate(channels, sampleRate, quality);
    using MemoryStream output = new();
    OggWriter writer = new(output, serial);
    writer.WritePacket(HeaderPacketBuilder.BuildInfoPacket(info).PacketData, 0, flushAfter: true);
    writer.WritePacket(HeaderPacketBuilder.BuildCommentsPacket(new Comments()).PacketData, 0);
    writer.WritePacket(HeaderPacketBuilder.BuildBooksPacket(info).PacketData, 0, flushAfter: true);
    ProcessingState state = ProcessingState.Create(info);
    long finalGranule = 0;
    const int Block = 1024;
    float[][] planar = new float[channels][];
    for (int channel = 0; channel < channels; channel++)
    {
        planar[channel] = new float[Block];
    }

    for (long start = 0; start < frames; start += Block)
    {
        int count = (int)Math.Min(Block, frames - start);
        for (int frame = 0; frame < count; frame++)
        {
            for (int channel = 0; channel < channels; channel++)
            {
                planar[channel][frame] = samples[((start + frame) * channels) + channel];
            }
        }

        state.WriteData(planar, count);
        finalGranule = Drain(state, writer, finalGranule);
    }

    state.WriteEndOfStream();
    finalGranule = Drain(state, writer, finalGranule);

    // OggVorbisEncoder 1.2.2 starts its granule count after the first half
    // block: the stream it writes begins `leadingFramesDropped` frames into
    // the signal (both NVorbis container paths agree). The oracle shifts by
    // this container-level amount; it is not derived from decoded PCM.
    long leadingFramesDropped = frames - finalGranule;
    Write(name, output.ToArray(), new { codec = "vorbis", sampleRate, channels, frames, period, quality, serial, finalGranule, leadingFramesDropped });

    static long Drain(ProcessingState state, OggWriter writer, long granule)
    {
        while (state.PacketOut(out OggPacket packet))
        {
            writer.WritePacket(packet.PacketData, packet.GranulePosition, endOfStream: packet.EndOfStream);
            granule = packet.GranulePosition;
        }

        return granule;
    }
}

void Opus(string name, int channels, int inputRate, double seconds, double period, int bitrate, int serial, int extraPreSkip)
{
    const int Rate = 48000;
    const int FrameSize = 960;
    long frames = (long)Math.Round(seconds * Rate);
#pragma warning disable CS0618 // The managed encoder is wanted: the factory may load a native libopus.
    OpusEncoder encoder = new(Rate, channels, OpusApplication.OPUS_APPLICATION_AUDIO) { Bitrate = bitrate };
#pragma warning restore CS0618
    int preSkip = encoder.Lookahead + extraPreSkip;

    // `extraPreSkip` frames of leading silence are encoded and then skipped,
    // so pre-skip handling is visible in the decoded output.
    float[] signal = CorpusSignal.Render(Rate, channels, frames, period);
    long encodedFrames = extraPreSkip + frames + encoder.Lookahead;
    long packets = (encodedFrames + FrameSize - 1) / FrameSize;
    float[] input = new float[packets * FrameSize * channels];
    signal.CopyTo(input, extraPreSkip * channels);
    short[] pcm = new short[FrameSize * channels];
    byte[] packet = new byte[4000];
    using MemoryStream output = new();
    OggWriter writer = new(output, serial);
    writer.WritePacket(OpusHead(channels, preSkip, inputRate, mappingFamily: 0), 0, flushAfter: true);
    writer.WritePacket(OpusTags(), 0, flushAfter: true);
    long finalGranule = preSkip + frames;
    for (long index = 0; index < packets; index++)
    {
        for (int sample = 0; sample < pcm.Length; sample++)
        {
            pcm[sample] = (short)Math.Clamp(Math.Round(input[(index * FrameSize * channels) + sample] * 32767.0), short.MinValue, short.MaxValue);
        }

        int length = encoder.Encode(pcm, FrameSize, packet, packet.Length);
        bool last = index == packets - 1;
        long granule = last ? finalGranule : (index + 1) * FrameSize;
        writer.WritePacket(packet.AsSpan(0, length), granule, flushAfter: (index + 1) % 50 == 0, endOfStream: last);
    }

    Write(name, output.ToArray(), new { codec = "opus", sampleRate = Rate, inputRate, channels, frames, period, bitrate, preSkip, finalGranule, serial });
}

void Write(string name, byte[] data, object parameters)
{
    File.WriteAllBytes(Path.Combine(outputDirectory, name), data);
    entries.Add(new CorpusEntry(name, Convert.ToHexString(SHA256.HashData(data)).ToLowerInvariant(), data.Length, parameters));
}

void Derived(string name, string from, string transformation, byte[] data) =>
    Write(name, data, new { derivedFrom = from, transformation });

void Synthetic(string name, string description, byte[] data) =>
    Write(name, data, new { synthetic = description });

static byte[] OpusHead(int channels, int preSkip, int inputRate, int mappingFamily)
{
    List<byte> head = [.. "OpusHead"u8, 1, (byte)channels, (byte)preSkip, (byte)(preSkip >> 8)];
    head.AddRange(BitConverter.GetBytes(inputRate));
    head.AddRange([0, 0, (byte)mappingFamily]);
    if (mappingFamily != 0)
    {
        head.AddRange([(byte)channels, 0]);
        for (int channel = 0; channel < channels; channel++)
        {
            head.Add((byte)channel);
        }
    }

    return [.. head];
}

static byte[] OpusTags()
{
    byte[] vendor = "Cerneala corpus"u8.ToArray();
    return [.. "OpusTags"u8, .. BitConverter.GetBytes(vendor.Length), .. vendor, 0, 0, 0, 0];
}

static byte[] OggHeaderOnly(int serial, byte[] packet)
{
    using MemoryStream output = new();
    OggWriter writer = new(output, serial);
    writer.WritePacket(packet, 0, flushAfter: true);
    writer.WritePacket(new byte[64], 960, endOfStream: true);
    return output.ToArray();
}

static List<(int Offset, int Length)> Pages(byte[] data)
{
    List<(int, int)> pages = [];
    int offset = 0;
    while (offset + 27 <= data.Length)
    {
        int segments = data[offset + 26];
        int length = 27 + segments;
        for (int index = 0; index < segments; index++)
        {
            length += data[offset + 27 + index];
        }

        pages.Add((offset, length));
        offset += length;
    }

    return pages;
}

static byte[] CorruptMiddlePage(byte[] data)
{
    byte[] copy = (byte[])data.Clone();
    List<(int Offset, int Length)> pages = Pages(copy);
    (int offset, int length) = pages[pages.Count / 2];
    copy[offset + length - 5] ^= 0xFF;
    return copy;
}

static byte[] Multiplex(byte[] first, byte[] second)
{
    List<(int Offset, int Length)> a = Pages(first);
    List<(int Offset, int Length)> b = Pages(second);
    List<byte> result = [];
    for (int index = 0; index < Math.Max(a.Count, b.Count); index++)
    {
        if (index < a.Count)
        {
            result.AddRange(first.AsSpan(a[index].Offset, a[index].Length).ToArray());
        }

        if (index < b.Count)
        {
            result.AddRange(second.AsSpan(b[index].Offset, b[index].Length).ToArray());
        }
    }

    return [.. result];
}

static bool HasInfoTag(byte[] mp3) =>
    mp3.AsSpan(0, Math.Min(64, mp3.Length)).IndexOf("Info"u8) >= 0 || mp3.AsSpan(0, Math.Min(64, mp3.Length)).IndexOf("Xing"u8) >= 0;

static byte[] StripFirstFrame(byte[] mp3)
{
    // LAME writes its Info/Xing tag as the first frame; drop exactly that frame.
    int length = Mp3FrameLength(mp3, 0);
    return mp3[length..];
}

static int Mp3FrameLength(byte[] data, int offset)
{
    int[] bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320];
    int[] rates = [44100, 48000, 32000];
    int bitrate = bitrates[data[offset + 2] >> 4] * 1000;
    int rate = rates[(data[offset + 2] >> 2) & 3];
    int padding = (data[offset + 2] >> 1) & 1;
    return (144 * bitrate / rate) + padding;
}

static byte[] Id3v2(int totalLength)
{
    int size = totalLength - 10;
    byte[] tag = new byte[totalLength];
    "ID3"u8.CopyTo(tag);
    tag[3] = 3;
    tag[6] = (byte)((size >> 21) & 0x7F);
    tag[7] = (byte)((size >> 14) & 0x7F);
    tag[8] = (byte)((size >> 7) & 0x7F);
    tag[9] = (byte)(size & 0x7F);
    byte[] title = "TIT2"u8.ToArray();
    title.CopyTo(tag, 10);
    tag[17] = 9;
    "\0Timbre"u8.CopyTo(tag.AsSpan(20));
    return tag;
}

static byte[] Id3v1()
{
    byte[] tag = new byte[128];
    "TAG"u8.CopyTo(tag);
    "Timbre corpus"u8.CopyTo(tag.AsSpan(3));
    return tag;
}

static byte[] LayerTwoFrames(int count)
{
    // MPEG-1 Layer II, 192 kbps, 44.1 kHz, stereo, no CRC: 626-byte frames.
    const int Length = 144 * 192000 / 44100;
    byte[] data = new byte[Length * count];
    for (int index = 0; index < count; index++)
    {
        data[index * Length] = 0xFF;
        data[(index * Length) + 1] = 0xFD;
        data[(index * Length) + 2] = 0xA0;
        data[(index * Length) + 3] = 0x00;
    }

    return data;
}

internal sealed record CorpusEntry(string Name, string Sha256, int Bytes, object Parameters);
