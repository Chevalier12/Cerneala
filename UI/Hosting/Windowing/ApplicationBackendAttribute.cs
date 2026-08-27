namespace Cerneala.UI.Hosting.Windowing;

/// <summary>
/// Selects the windowing backend used by source-generated application startup.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ApplicationBackendAttribute : Attribute
{
    /// <summary>
    /// Initializes a backend selection for the current application assembly.
    /// </summary>
    /// <param name="backendType">
    /// A public, non-generic static or concrete class that exposes
    /// <c>public static void EnsureRegistered()</c>.
    /// </param>
    public ApplicationBackendAttribute(Type backendType)
    {
        ArgumentNullException.ThrowIfNull(backendType);
        BackendType = backendType;
    }

    /// <summary>
    /// Gets the selected backend composition type.
    /// </summary>
    public Type BackendType { get; }
}
