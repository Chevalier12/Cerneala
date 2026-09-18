using Cerneala.Drawing;

namespace Cerneala.Tests.Drawing;

public sealed class SpriteBatchGeometryParityTests
{
    [Theory]
    [InlineData(DrawSamplingMode.Point, DrawAddressMode.Clamp)]
    [InlineData(DrawSamplingMode.Point, DrawAddressMode.Wrap)]
    [InlineData(DrawSamplingMode.Linear, DrawAddressMode.Clamp)]
    [InlineData(DrawSamplingMode.Linear, DrawAddressMode.Wrap)]
    public void BatchMatchesIndependentGeometryForEverySprite(
        DrawSamplingMode sampling, DrawAddressMode addressMode)
    {
        TestImage image = new(64, 48);
        List<DrawSprite2D> sprites = [];
        List<DrawVertex2D> vertices = [];
        List<int> indices = [];
        DrawRect?[] sources = [null, new(3, 4, 5, 7), new(60, 40, 4, 8)];
        DrawRect[] destinations = [new(2.25f, -3.5f, 9.5f, 6.25f), new(-17, 23, 0, 8), new(9, -11, 3, 0)];
        foreach (DrawRect? requested in sources)
        foreach (DrawRect destination in destinations)
        foreach (DrawPoint origin in new DrawPoint[] { default, new(-1.25f, 2.5f) })
        foreach (float rotation in new[] { 0f, -0.7f, MathF.PI / 2, MathF.PI })
        for (int flip = 0; flip < 4; flip++)
        foreach (float opacity in new[] { 0f, 0.4f, 1f })
        {
            Color tint = new(101, 53, 211, 129);
            DrawImageOptions options = new(requested, tint, opacity, rotation, origin,
                (DrawImageFlip)flip, layerDepth: 0.75f, sampling: sampling, addressMode: addressMode);
            sprites.Add(new(destination, options));
            DrawRect source = requested ?? new DrawRect(0, 0, image.Width, image.Height);
            float cosine = MathF.Cos(rotation), sine = MathF.Sin(rotation);
            int first = vertices.Count;
            for (int corner = 0; corner < 4; corner++)
            {
                bool right = corner is 1 or 2, bottom = corner is 2 or 3;
                float x = (right ? destination.Width : 0) - origin.X * destination.Width / source.Width;
                float y = (bottom ? destination.Height : 0) - origin.Y * destination.Height / source.Height;
                DrawPoint position = new(destination.X + x * cosine - y * sine,
                    destination.Y + x * sine + y * cosine);
                DrawPoint uv = new(
                    (right ^ ((flip & 1) != 0) ? source.Right : source.X) / image.Width,
                    (bottom ^ ((flip & 2) != 0) ? source.Bottom : source.Y) / image.Height);
                vertices.Add(new(position, new Color(tint.R, tint.G, tint.B,
                    (byte)MathF.Round(tint.A * opacity)), uv));
            }
            foreach (int offset in new[] { 0, 1, 2, 0, 2, 3 }) { indices.Add(first + offset); }
        }

        DrawSprite2D[] input = sprites.ToArray();
        DrawSpriteBatch batch = new(image, input);
        DrawMesh2D expected = new(vertices, indices, image: image);
        input[0] = new(new DrawRect(100, 100, 1, 1));

        Assert.Equal(864, batch.Sprites.Count);
        Assert.Same(sprites[0], batch.Sprites[0]);
        Assert.Equal(expected.VertexArray, batch.Mesh.VertexArray);
        Assert.Equal(expected.IndexArray, batch.Mesh.IndexArray);
        Assert.Equal(expected.Bounds, batch.Bounds);
        Assert.Equal(sampling, batch.Sampling);
        Assert.Equal(addressMode, batch.AddressMode);
    }

    [Fact]
    public void BatchStillRejectsInvalidImageSourceAndMixedSamplers()
    {
        DrawSprite2D sprite = new(new DrawRect(0, 0, 8, 8));
        Assert.Equal("image", Assert.Throws<ArgumentNullException>(() => new DrawSpriteBatch(null!, [sprite])).ParamName);
        Assert.Equal("image", Assert.Throws<ArgumentException>(() => new DrawSpriteBatch(new TestImage(0, 8), [sprite])).ParamName);
        TestImage image = new(8, 8);
        Assert.Equal("options", Assert.Throws<ArgumentOutOfRangeException>(() => new DrawSpriteBatch(image,
            [new(new DrawRect(0, 0, 8, 8), new DrawImageOptions(source: new DrawRect(7, 0, 2, 8)))])).ParamName);
        Assert.Equal("sprites", Assert.Throws<ArgumentException>(() => new DrawSpriteBatch(image, [])).ParamName);
        Assert.Equal("sprites", Assert.Throws<ArgumentException>(() => new DrawSpriteBatch(image,
            [sprite, new(new DrawRect(0, 0, 8, 8), new DrawImageOptions(sampling: DrawSamplingMode.Point))])).ParamName);
        Assert.Equal("sprites", Assert.Throws<ArgumentException>(() => new DrawSpriteBatch(image,
            [sprite, new(new DrawRect(0, 0, 8, 8), new DrawImageOptions(addressMode: DrawAddressMode.Wrap))])).ParamName);
    }

    private sealed record TestImage(int Width, int Height) : IDrawImage;
}
