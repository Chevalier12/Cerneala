namespace Cerneala.UI.Controls;

public readonly struct CollisionQuery2D
{
    private readonly bool initialized;
    private readonly uint collisionLayer;
    private readonly uint collisionMask;
    private readonly bool includeTriggers;

    public CollisionQuery2D(
        uint collisionLayer = uint.MaxValue,
        uint collisionMask = uint.MaxValue,
        bool includeTriggers = true,
        Collider2D? exclude = null)
    {
        initialized = true;
        this.collisionLayer = collisionLayer;
        this.collisionMask = collisionMask;
        this.includeTriggers = includeTriggers;
        Exclude = exclude;
    }

    public uint CollisionLayer => initialized ? collisionLayer : uint.MaxValue;

    public uint CollisionMask => initialized ? collisionMask : uint.MaxValue;

    public bool IncludeTriggers => !initialized || includeTriggers;

    public Collider2D? Exclude { get; }
}
