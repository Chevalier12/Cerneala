using System.Diagnostics;
using System.Numerics;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;
using Cerneala.UI.Input;
using Cerneala.UI.Resources;

namespace Cerneala.SceneVillage;

public sealed class VillageGameSurface : RenderSurface2D
{
    private readonly VillageInput input = new();
    private readonly Sprite2D player;
    private readonly Scene2D world;
    private readonly SceneItems2D stressItems;
    private readonly ImageReference townImage;
    private readonly ImageReference villagerImage;
    private long lastAdvanceTimestamp;
    private string facing = "Down";
    private float zoom = 1f;
    private float? contentScale;

    public VillageGameSurface()
    {
        var townId = new ResourceId<ImageResource>("VillageTown");
        var villagerId = new ResourceId<ImageResource>("VillageVillager");
        Resources.SetResource(townId, new ImageResource(Path.Combine(AppContext.BaseDirectory, "Assets", "tiny-town.png")));
        Resources.SetResource(villagerId, new ImageResource(Path.Combine(AppContext.BaseDirectory, "Assets", "ch003.png")));
        townImage = new ImageReference(townId);
        villagerImage = new ImageReference(villagerId);

        ClearColor = new Color(130, 190, 101);
        RedrawMode = RenderSurface2DRedrawMode.Continuous;
        Stretch = DrawBrushStretch.Fill;

        world = new Scene2D();
        AddVillageGround();
        AddVillageBuildingsAndDecor();

        stressItems = new SceneItems2D();
        stressItems.Templates.Add(new ContentTemplate<StressItem>(
            "StressActor", null, 0,
            context => VillageArt.StressActor(villagerImage, context.Data)));
        world.Children.Add(stressItems);

        float playerHalfSize = VillageLayout.CharacterSize / 2f;
        player = VillageArt.Player(villagerImage, VillageLayout.PlayerSpawn.X - playerHalfSize, VillageLayout.PlayerSpawn.Y - playerHalfSize);
        world.Children.Add(player);
        Scene = world;
        SetStress(StressPreset.Static, 0);
        UpdateCamera();
        Draw += OnDraw;
    }

    public float Zoom
    {
        get => zoom;
        set
        {
            if (!float.IsFinite(value) || value <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (zoom == value)
            {
                return;
            }

            zoom = value;
            UpdateCamera();
        }
    }

    // Null follows the window's coordinate scale. The Village window opts into
    // one physical pixel per world unit independently of the surrounding UI.
    public float? ContentScale
    {
        get => contentScale;
        set
        {
            if (value.HasValue && (!float.IsFinite(value.Value) || value.Value <= 0f))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (contentScale == value)
            {
                return;
            }

            contentScale = value;
            UpdateCamera();
        }
    }

    internal Vector2 PlayerPosition => new(player.X, player.Y);

    internal Vector2 PlayerCenter => new(player.X + player.Width / 2f, player.Y + player.Height / 2f);

    internal StressPreset Preset { get; private set; }

    internal int RequestedStressCount { get; private set; }

    internal int RealizedStressCount => stressItems.RealizedItemCount;

    internal bool SetMovementKey(InputKey key, bool isDown) => input.Set(key, isDown);

    internal void ClearMovementKeys() => input.Clear();

    internal void SetStress(StressPreset preset, int count)
    {
        if (!Enum.IsDefined(preset))
        {
            throw new ArgumentOutOfRangeException(nameof(preset));
        }

        stressItems.ItemsSource = VillageStress.CreateItems(count, preset);
        Preset = preset;
        RequestedStressCount = count;
    }

    internal void ResetPlayer()
    {
        MovePlayerTo(VillageLayout.PlayerSpawn);
    }

    internal void GoToStressField()
    {
        MovePlayerTo(VillageLayout.StressFieldWaypoint);
    }

    private void MovePlayerTo(Vector2 center)
    {
        input.Clear();
        player.X = center.X - player.Width / 2f;
        player.Y = center.Y - player.Height / 2f;
        facing = "Down";
        player.AnimationState = "IdleDown";
        UpdateCamera();
    }

    internal void Advance(TimeSpan elapsed)
    {
        float seconds = (float)elapsed.TotalSeconds;
        if (!float.IsFinite(seconds) || seconds <= 0f)
        {
            UpdateCamera();
            return;
        }

        Vector2 direction = input.Direction;
        if (direction == Vector2.Zero)
        {
            player.AnimationState = "Idle" + facing;
            UpdateCamera();
            return;
        }

        facing = Math.Abs(direction.X) >= Math.Abs(direction.Y)
            ? (direction.X < 0f ? "Left" : "Right")
            : (direction.Y < 0f ? "Up" : "Down");

        Vector2 requested = direction * VillageLayout.PlayerSpeed * seconds;
        float oldX = player.X;
        float oldY = player.Y;
        if (requested.X != 0f)
        {
            float travelX = world.CollisionWorld.MoveAndCollide(player.Collider!, new Vector2(requested.X, 0f)).Travel.X;
            player.X = Math.Clamp(player.X + travelX, 0f, VillageLayout.WorldSize - player.Width);
        }

        if (requested.Y != 0f)
        {
            float travelY = world.CollisionWorld.MoveAndCollide(player.Collider!, new Vector2(0f, requested.Y)).Travel.Y;
            player.Y = Math.Clamp(player.Y + travelY, 0f, VillageLayout.WorldSize - player.Height);
        }

        bool moved = player.X != oldX || player.Y != oldY;
        player.AnimationState = (moved ? "Walk" : "Idle") + facing;
        UpdateCamera();
    }

    private void OnDraw(RenderSurface2D sender, RenderSurface2DFrame frame)
    {
        long now = Stopwatch.GetTimestamp();
        TimeSpan elapsed = lastAdvanceTimestamp == 0
            ? TimeSpan.Zero
            : Stopwatch.GetElapsedTime(lastAdvanceTimestamp, now);
        lastAdvanceTimestamp = now;
        Advance(elapsed);
    }

    private void UpdateCamera()
    {
        float width = ArrangedBounds.Width > 0f ? ArrangedBounds.Width : 1200f;
        float height = ArrangedBounds.Height > 0f ? ArrangedBounds.Height : 720f;
        DrawRect next = VillageCamera.Follow(PlayerCenter, width, height, Root?.Scale ?? 1f, contentScale, zoom);
        if (!ViewBox.Equals(next))
        {
            ViewBox = next;
        }
    }

    private void AddVillageGround()
    {
        for (int row = 0; row < 50; row++)
        {
            for (int column = 0; column < 50; column++)
            {
                int tile = (row * 19 + column * 7) % 13 == 0 ? 1 : 0;
                world.Children.Add(VillageArt.TownTile(
                    townImage,
                    tile,
                    1664f + column * VillageLayout.TileSize,
                    1664f + row * VillageLayout.TileSize));
            }
        }

        for (int i = 0; i < 46; i++)
        {
            float offset = 1696f + i * VillageLayout.TileSize;
            world.Children.Add(VillageArt.TownTile(townImage, 43, offset, 2032f));
            world.Children.Add(VillageArt.TownTile(townImage, 43, 2032f, offset));
        }
    }

    private void AddVillageBuildingsAndDecor()
    {
        foreach (HouseSite house in VillageLayout.Houses)
        {
            foreach (Sprite2D tile in VillageArt.House(townImage, house))
            {
                world.Children.Add(tile);
            }
        }

        foreach (DecorationSite decoration in VillageLayout.Decorations)
        {
            Sprite2D sprite = decoration.TileIndex == 16
                ? VillageArt.GreenTree(townImage, decoration.X, decoration.Y)
                : VillageArt.TownTile(townImage, decoration.TileIndex, decoration.X, decoration.Y);
            if (decoration.TileIndex is 5 or 16 or 27 or 28)
            {
                sprite.Collider = new BoxCollider2D
                {
                    Width = 7f,
                    Height = 5f,
                    OffsetX = 4.5f,
                    // Obstacle bounds follow the source-cell proportions.
                    // The tall tree starts one cell above its lower site.
                    OffsetY = 10f + (decoration.TileIndex == 16 ? VillageLayout.TileSize : 0f),
                    CollisionLayer = 1u,
                    CollisionMask = 1u
                };
            }

            world.Children.Add(sprite);
        }
    }
}
