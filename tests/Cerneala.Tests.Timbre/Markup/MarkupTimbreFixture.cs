using System.Buffers.Binary;
using Cerneala.Tests.Timbre.Harness;
using Cerneala.Timbre;
using Cerneala.UI.Detective;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Input;
using Cerneala.UI.Motion.Core;
using ServoApi = Cerneala.UI.Servo.Servo;
using ServoTarget = Cerneala.UI.Servo.ServoTarget;

namespace Cerneala.Tests.Timbre.Markup;

internal sealed record TimbreAsset(string RelativePath, long Frames, Func<long, int, float> Signal);

// One executed generated consumer: deterministic float32 WAV assets, a real
// TimbreRuntime on the deterministic sink, a UIRoot hosted by UiHost and Servo
// for user-like input. Every playback the runtime accepts is recorded in order.
internal sealed class MarkupTimbreFixture : IDisposable
{
    public const string ToneSource = "audio/tone.wav";
    public const string OtherSource = "audio/other.wav";
    public const string RampSource = "audio/ramp.wav";
    public const string ShortSource = "audio/short.wav";

    public static readonly TimbreAsset Tone = new(ToneSource, 24000, (frame, channel) => channel == 0 ? 0.25f : -0.25f);
    public static readonly TimbreAsset Other = new(OtherSource, 24000, (frame, channel) => (frame % 480) / 960f);

    // Position-identifying signal: every source frame has a distinct value.
    public static readonly TimbreAsset Ramp = new(RampSource, 24000, (frame, channel) => (channel == 0 ? 0.5f : -0.5f) * frame / 24000f);
    public static readonly TimbreAsset Short = new(ShortSource, 480, (frame, channel) => 0.1f);

    private readonly string assets;

    public MarkupTimbreFixture(
        string markup,
        string fileName = "TimbreFixture.crn",
        IReadOnlyList<TimbreAsset>? sounds = null,
        bool hold = true,
        bool attach = true,
        Action<TimbreRuntimeOptions>? configure = null,
        InvalidationTrace? trace = null,
        Func<GeneratedTimbreConsumer, UIElement>? create = null,
        string? companionSource = null)
    {
        assets = Path.Combine(Path.GetTempPath(), "cerneala-markup-sound", Guid.NewGuid().ToString("N"));
        foreach (TimbreAsset asset in sounds ?? [Tone, Other, Ramp, Short])
        {
            WriteFloatWav(Path.Combine(assets, asset.RelativePath), asset.Frames, asset.Signal);
        }

        Rig = new TimbreRig(
            options =>
            {
                options.BaseDirectory = assets;
                configure?.Invoke(options);
            },
            hold);
        Rig.Runtime.PlaybackAccepted = playback => Started.Add(playback);
        Consumer = companionSource is null
            ? GeneratedTimbreConsumer.Compile([(fileName, markup)])
            : GeneratedTimbreConsumer.Compile([(fileName, markup)], companionSource, fileName + ".cs");
        Element = create?.Invoke(Consumer) ??
            Consumer.Create("Cerneala.GeneratedUi." + Path.GetFileNameWithoutExtension(fileName) + "Factory");
        Root = trace is null ? new UIRoot(motionClock: Clock) : new UIRoot(trace, motionClock: Clock);
        Root.SetTimbreRuntime(Rig.Runtime);
        Host = new UiHost(new UiHostOptions
        {
            Root = Root,
            Viewport = new UiViewport(400, 300),
            TimbreRuntime = Rig.Runtime
        });
        Servo = new ServoApi(Host);
        if (attach)
        {
            Attach();
        }
    }

    public TimbreRig Rig { get; }

    public GeneratedTimbreConsumer Consumer { get; }

    public UIElement Element { get; }

    public UIRoot Root { get; }

    public UiHost Host { get; }

    public ServoApi Servo { get; }

    public ManualClock Clock { get; } = new();

    public List<TimbrePlayback> Started { get; } = [];

    public string AssetsDirectory => assets;

    public void Attach()
    {
        Root.VisualChildren.Add(Element);
        Pump();
    }

    public void Detach()
    {
        Root.VisualChildren.Remove(Element);
        Pump();
    }

    // One host frame without input: drains the relay, layout and Motion.
    public void Pump(TimeSpan? advance = null)
    {
        if (advance is TimeSpan delta)
        {
            Clock.Advance(delta);
        }

        Host.Update(
            new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []),
            Host.Viewport,
            TimeSpan.Zero);
    }

    public Task ClickAsync(string name) => Servo.ClickAsync(ServoTarget.ByName(name));

    public Task HoverAsync(string name) => Servo.HoverAsync(ServoTarget.ByName(name));

    public IEnumerable<T> All<T>()
        where T : UIElement =>
        ElementTreeWalker.PreOrder(Root).OfType<T>();

    public void Dispose()
    {
        Rig.Runtime.PlaybackAccepted = null;
        Rig.Dispose();
        Consumer.Dispose();
        try
        {
            Directory.Delete(assets, recursive: true);
        }
        catch (IOException)
        {
            // A reader may still close its file; the temp root is reused per run.
        }
    }

    public static float[] Expected(TimbreAsset asset, long frames, float gain = 1f, long sourceStart = 0) =>
        TimbreRig.Expected(frames, (frame, channel) => frame < asset.Frames ? asset.Signal(frame, channel) : 0f, gain, sourceStart);

    public static void WriteFloatWav(string path, long frames, Func<long, int, float> signal)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        int dataBytes = checked((int)(frames * 2 * sizeof(float)));
        byte[] file = new byte[44 + dataBytes];
        Span<byte> span = file;
        "RIFF"u8.CopyTo(span);
        BinaryPrimitives.WriteUInt32LittleEndian(span[4..], (uint)(file.Length - 8));
        "WAVE"u8.CopyTo(span[8..]);
        "fmt "u8.CopyTo(span[12..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[16..], 16);
        BinaryPrimitives.WriteUInt16LittleEndian(span[20..], 3);
        BinaryPrimitives.WriteUInt16LittleEndian(span[22..], 2);
        BinaryPrimitives.WriteUInt32LittleEndian(span[24..], (uint)TimbreRuntime.SampleRate);
        BinaryPrimitives.WriteUInt32LittleEndian(span[28..], (uint)(TimbreRuntime.SampleRate * 8));
        BinaryPrimitives.WriteUInt16LittleEndian(span[32..], 8);
        BinaryPrimitives.WriteUInt16LittleEndian(span[34..], 32);
        "data"u8.CopyTo(span[36..]);
        BinaryPrimitives.WriteUInt32LittleEndian(span[40..], (uint)dataBytes);
        for (long frame = 0; frame < frames; frame++)
        {
            for (int channel = 0; channel < 2; channel++)
            {
                BinaryPrimitives.WriteSingleLittleEndian(span[(int)(44 + ((frame * 2) + channel) * 4)..], signal(frame, channel));
            }
        }

        File.WriteAllBytes(path, file);
    }

    internal sealed class ManualClock : IMotionClock
    {
        private TimeSpan now;

        public TimeSpan Now => now;

        public void Advance(TimeSpan delta) => now += delta;
    }
}
