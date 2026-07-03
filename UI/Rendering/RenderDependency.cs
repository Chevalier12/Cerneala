namespace Cerneala.UI.Rendering;

public readonly record struct RenderDependency(
    int TextVersion = 0,
    string TextLayoutIdentity = "",
    int ImageVersion = 0,
    int ResourceVersion = 0,
    int CustomVersion = 0)
{
    public static RenderDependency None { get; } = new();

    public RenderDependency WithTextVersion(int version)
    {
        return this with { TextVersion = version };
    }

    public RenderDependency WithTextLayoutIdentity(string identity)
    {
        return this with { TextLayoutIdentity = identity ?? string.Empty };
    }

    public RenderDependency WithImageVersion(int version)
    {
        return this with { ImageVersion = version };
    }

    public RenderDependency WithResourceVersion(int version)
    {
        return this with { ResourceVersion = version };
    }

    public RenderDependency WithCustomVersion(int version)
    {
        return this with { CustomVersion = version };
    }
}
