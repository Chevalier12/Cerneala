using Cerneala.UI.Prism.Runtime;

namespace Cerneala.Drawing.Prism.Graph;

public readonly record struct PrismDependencyStamp(
    PrismCacheOwnerToken CacheOwnerToken,
    PrismStructuralVersion StructuralVersion,
    PrismValueVersion ValueVersion,
    long VisualContentVersion,
    long DescendantVersion);
