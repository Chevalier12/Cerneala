using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;
using Cerneala.UI.Servo;
using ServoApi = Cerneala.UI.Servo.Servo;

namespace Cerneala.Tests.UI.Servo;

using Scene2D = global::Cerneala.UI.Controls.Scene2D;

public sealed class ServoSceneTests
{
    [Theory]
    [InlineData(false, 1f)]
    [InlineData(true, 1f)]
    [InlineData(false, 1.25f)]
    [InlineData(true, 1.25f)]
    public async Task SurfaceLayoutOriginIsIncludedWithAndWithoutViewBox(bool useViewBox, float dpi)
    {
        Scene2D world = new();
        BoxCollider2D targetNode = new() { Width = 16, Height = 12, TranslateX = 30, TranslateY = 15 };
        world.Children.Add(targetNode);
        ServoApi.SetId(targetNode, "target");
        UiHost host = CreateHost(world, useViewBox, new Thickness(20, 32, 0, 0), dpi);
        RenderSurface2D surface = world.Surface!;
        Assert.Equal(20, surface.ArrangedBounds.X);
        Assert.Equal(32, surface.ArrangedBounds.Y);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("target");
        float scale = useViewBox ? 2 : 1 / dpi;
        LayoutRect expected = new(20 + 30 * scale, 32 + 15 * scale, 16 * scale, 12 * scale);
        LayoutRect actual = (await servo.FindAsync(target)).Bounds;
        Assert.InRange(Math.Abs(expected.X - actual.X), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Y - actual.Y), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Width - actual.Width), 0, 0.0001f);
        Assert.InRange(Math.Abs(expected.Height - actual.Height), 0, 0.0001f);
        Assert.Equal(new System.Numerics.Vector2(expected.X, expected.Y),
            surface.SceneToRoot(new System.Numerics.Vector2(30, 15)));
        await servo.ClickAsync(target);
        WindowScreenshotRegion region = new ServoCaptureEngine(new ServoQueryEngine()).ResolveRegion(host.Root!, target);
        float pixelScale = useViewBox ? 2 * dpi : 1;
        float pixelX = 20 * dpi + 30 * pixelScale;
        float pixelY = 32 * dpi + 15 * pixelScale;
        WindowScreenshotRegion expectedRegion = new(
            (int)MathF.Floor(pixelX), (int)MathF.Floor(pixelY),
            (int)(MathF.Ceiling(pixelX + 16 * pixelScale) - MathF.Floor(pixelX)),
            (int)(MathF.Ceiling(pixelY + 12 * pixelScale) - MathF.Floor(pixelY)));
        Assert.Equal(expectedRegion, region);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SceneTargetsUseCurrentGeometryAndRealInputWithoutLayout(bool colliderOnly)
    {
        Scene2D world = new() { TranslateX = 10, TranslateY = 5 };
        Scene2D entity = new() { TranslateX = 20, TranslateY = 10, Focusable = true };
        entity.Children.Add(colliderOnly
            ? new BoxCollider2D { Width = 16, Height = 12 }
            : new Sprite2D { Destination = new DrawRect(0, 0, 16, 12) });
        world.Children.Add(entity);
        ServoApi.SetId(world, "world");
        ServoApi.SetId(entity, "player");
        UiHost host = CreateHost(world);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("player").Within(ServoTarget.ById("world"));
        int clicks = 0;
        int keys = 0;
        entity.MouseDown += (_, _) => clicks++;
        entity.KeyDown += (_, _) => keys++;

        ServoElement snapshot = await servo.FindAsync(target);
        Assert.Equal(new LayoutRect(60, 30, 32, 24), snapshot.Bounds);
        Assert.Equal(default, entity.ArrangedBounds);
        await servo.ClickAsync(target);
        await servo.PressKeyAsync(InputKey.Enter);
        Assert.Equal(1, clicks);
        Assert.Equal(1, keys);
        Assert.True(entity.IsKeyboardFocused);

        entity.TranslateX = 30;
        Assert.Equal(new LayoutRect(80, 30, 32, 24), (await servo.FindAsync(target)).Bounds);
        Assert.Equal(new LayoutRect(60, 30, 32, 24), snapshot.Bounds);
    }

    [Fact]
    public async Task ColliderIsQueryableButRealHitTestStillRejectsDisabledAndSingularTargets()
    {
        Scene2D world = new();
        BoxCollider2D collider = new() { Width = 20, Height = 10, TranslateX = 10, OffsetX = 2 };
        world.Children.Add(collider);
        ServoApi.SetId(collider, "collider");
        UiHost host = CreateHost(world);
        ServoApi servo = new(host);
        ServoTarget target = ServoTarget.ById("collider");
        Assert.Equal(new LayoutRect(24, 0, 40, 20), (await servo.FindAsync(target)).Bounds);
        await servo.ClickAsync(target);
        collider.Enabled = false;
        await Assert.ThrowsAsync<ServoTargetNotActionableException>(() => servo.ClickAsync(target));
        collider.Enabled = true;
        world.ScaleX = 0;
        await Assert.ThrowsAsync<ServoTargetNotActionableException>(() => servo.ClickAsync(target));
        world.Visibility = Visibility.Hidden;
        Assert.False((await servo.FindAsync(target)).IsVisible);
        await Assert.ThrowsAsync<ServoTargetNotActionableException>(() => servo.ClickAsync(target));
        world.Children.Remove(collider);
        Assert.False(await servo.ExistsAsync(target));
    }

    private static UiHost CreateHost(Scene2D world, bool useViewBox = true, Thickness margin = default, float dpi = 1)
    {
        float width = 200 + margin.Left + margin.Right;
        float height = 200 + margin.Top + margin.Bottom;
        UIRoot root = new(width, height);
        root.VisualChildren.Add(new RenderSurface2D
        {
            Width = 200, Height = 200, Scene = world,
            ViewBox = useViewBox ? new DrawRect(0, 0, 100, 100) : null,
            Stretch = DrawBrushStretch.Fill, Margin = margin
        });
        UiHost host = new(new UiHostOptions { Root = root, Viewport = new UiViewport(width, height, dpi) });
        host.Update(new InputFrame(PointerSnapshot.Empty, PointerSnapshot.Empty,
            KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []), host.Viewport, TimeSpan.Zero);
        return host;
    }
}
