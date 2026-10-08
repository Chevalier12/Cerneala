using Cerneala.Timbre;

namespace Cerneala.Tests.Timbre.Definitions;

// docs/plans/2026-10-08-timbre-prism-in-aspect.md Etapa 3: a TimbreClip is a
// set of named sounds sharing clip-level parameters, like PrismClip and its layers.
public sealed class TimbreClipDefinitionTests
{
    [Fact]
    public void ClipKeepsNamedSoundsInDeclarationOrderAndItsParameters()
    {
        TimbreParameter<float> brightness = new("Brightness", 1200f);
        TimbreSound click = new(TimbreSource.FromFile("audio/click.wav"));
        TimbreSound hover = new(
            TimbreSource.FromFile("audio/hover.wav"),
            volume: 0.3f,
            parameters: [brightness],
            modifiers: [new LowPass(cutoff: brightness)]);
        TimbreSound music = new(TimbreSource.FromFile("audio/music.ogg"), loop: true);

        TimbreClipDefinition clip = new(
            "UiSounds",
            [new TimbreClipSound("Music", music, autoPlay: true), new TimbreClipSound("Click", click), new TimbreClipSound("Hover", hover)],
            [brightness]);

        Assert.Equal("UiSounds", clip.Name);
        Assert.Equal(["Music", "Click", "Hover"], clip.Sounds.Keys);
        Assert.Equal(["Music", "Click", "Hover"], clip.Sounds.Values.Select(sound => sound.Name));
        Assert.Same(hover, clip.Sounds["Hover"].Sound);
        Assert.True(clip.Sounds["Music"].AutoPlay);
        Assert.False(clip.Sounds["Click"].AutoPlay);
        Assert.True(clip.Sounds.ContainsKey("Click"));
        Assert.False(clip.Sounds.TryGetValue("Lipsa", out _));
        Assert.Throws<KeyNotFoundException>(() => clip.Sounds["Lipsa"]);
        Assert.Equal(3, clip.Sounds.Count);
        Assert.Equal([brightness], clip.Parameters);
    }

    [Fact]
    public void ClipCopiesItsInputs()
    {
        List<TimbreClipSound> sounds = [new TimbreClipSound("Click", new TimbreSound(TimbreSource.FromFile("audio/click.wav")))];
        List<TimbreParameter> parameters = [new TimbreParameter<float>("Unused", 1f)];

        TimbreClipDefinition clip = new("Sounds", sounds, parameters);
        sounds.Clear();
        parameters.Clear();

        Assert.Single(clip.Sounds);
        Assert.Single(clip.Parameters);
    }

    [Fact]
    public void SoundRequiresAnIdentifierNameAndASound()
    {
        TimbreSound sound = new(TimbreSource.FromFile("audio/click.wav"));

        Assert.Equal("_click2", new TimbreClipSound("_click2", sound).Name);
        Assert.Throws<ArgumentNullException>(() => new TimbreClipSound(null!, sound));
        Assert.Throws<ArgumentException>(() => new TimbreClipSound("", sound));
        Assert.Throws<ArgumentException>(() => new TimbreClipSound("2nd", sound));
        Assert.Throws<ArgumentException>(() => new TimbreClipSound("click sound", sound));
        Assert.Throws<ArgumentNullException>(() => new TimbreClipSound("Click", null!));
    }

    [Fact]
    public void ClipRejectsMissingDuplicateOrForeignDeclarations()
    {
        TimbreParameter<float> brightness = new("Brightness", 1200f);
        TimbreParameter<float> other = new("Brightness", 1200f);
        TimbreSound plain = new(TimbreSource.FromFile("audio/click.wav"));
        TimbreSound bright = new(TimbreSource.FromFile("audio/hover.wav"), parameters: [brightness], modifiers: [new LowPass(cutoff: brightness)]);

        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition(" ", [new TimbreClipSound("Click", plain)]));
        Assert.Throws<ArgumentNullException>(() => new TimbreClipDefinition(null!, [new TimbreClipSound("Click", plain)]));
        Assert.Throws<ArgumentNullException>(() => new TimbreClipDefinition("Sounds", null!));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", []));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [null!]));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition(
            "Sounds",
            [new TimbreClipSound("Click", plain), new TimbreClipSound("Click", plain)]));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [new TimbreClipSound("Click", plain)], [null!]));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [new TimbreClipSound("Click", plain)], [brightness, other]));
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [new TimbreClipSound("Click", plain)], [brightness, brightness]));

        // A sound may use only the clip's own parameters.
        ArgumentException foreign = Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [new TimbreClipSound("Hover", bright)]));
        Assert.Contains("Hover", foreign.Message, StringComparison.Ordinal);
        Assert.Contains("Brightness", foreign.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => new TimbreClipDefinition("Sounds", [new TimbreClipSound("Hover", bright)], [other]));
    }

    [Fact]
    public void OneParameterMayFeedSeveralSounds()
    {
        TimbreParameter<float> brightness = new("Brightness", 1200f);
        TimbreSound first = new(TimbreSource.FromFile("audio/a.wav"), parameters: [brightness], modifiers: [new LowPass(cutoff: brightness)]);
        TimbreSound second = new(TimbreSource.FromFile("audio/b.wav"), parameters: [brightness], modifiers: [new LowPass(cutoff: brightness)]);

        TimbreClipDefinition clip = new("Sounds", [new TimbreClipSound("A", first), new TimbreClipSound("B", second)], [brightness]);

        Assert.Same(clip.Sounds["A"].Sound.Parameters[0], clip.Sounds["B"].Sound.Parameters[0]);
    }
}
