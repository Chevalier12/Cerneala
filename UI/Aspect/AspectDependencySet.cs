using Cerneala.UI.Core;

namespace Cerneala.UI.Aspect;

public sealed class AspectDependencySet
{
    public AspectDependencySet(
        IReadOnlyList<AspectToken>? tokens = null,
        IReadOnlyList<AspectState>? states = null,
        IReadOnlyList<AspectVariantKey>? variants = null,
        IReadOnlyList<UiProperty>? properties = null,
        IReadOnlyList<AspectDataDependency>? data = null,
        AspectSlot? slot = null,
        int catalogVersion = 0,
        int environmentVersion = 0)
    {
        Tokens = tokens ?? [];
        States = states ?? [];
        Variants = variants ?? [];
        Properties = properties ?? [];
        Data = data ?? [];
        Slot = slot;
        CatalogVersion = catalogVersion;
        EnvironmentVersion = environmentVersion;
    }

    public IReadOnlyList<AspectToken> Tokens { get; }

    public IReadOnlyList<AspectState> States { get; }

    public IReadOnlyList<AspectVariantKey> Variants { get; }

    public IReadOnlyList<UiProperty> Properties { get; }

    public IReadOnlyList<AspectDataDependency> Data { get; }

    public AspectSlot? Slot { get; }

    public int CatalogVersion { get; }

    public int EnvironmentVersion { get; }
}
