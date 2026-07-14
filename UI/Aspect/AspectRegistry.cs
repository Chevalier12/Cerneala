using Cerneala.UI.Relay;

namespace Cerneala.UI.Aspect;

public sealed class AspectRegistry
{
    private readonly List<AspectPackage> packages = [];
    private readonly Action? changed;
    private readonly IUiThreadAccess threadAccess;

    public AspectRegistry(Action? changed = null)
        : this(new CapturedUiThreadAccess(), changed)
    {
    }

    internal AspectRegistry(IUiThreadAccess threadAccess, Action? changed = null)
    {
        this.threadAccess = threadAccess ?? throw new ArgumentNullException(nameof(threadAccess));
        this.changed = changed;
    }

    public int Version { get; private set; }

    public IReadOnlyList<AspectPackage> Packages => packages;

    public AspectRegistry Register(AspectPackage package, bool notify = true)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(package);
        if (packages.Any(existing => string.Equals(existing.Name, package.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Aspect package '{package.Name}' is already registered.");
        }

        packages.Add(package);
        Version++;
        if (notify)
        {
            changed?.Invoke();
        }

        return this;
    }

    public bool Unregister(string packageName)
    {
        threadAccess.VerifyAccess();
        if (string.IsNullOrWhiteSpace(packageName))
        {
            throw new ArgumentException("Aspect package name cannot be empty.", nameof(packageName));
        }

        int index = packages.FindIndex(package => string.Equals(package.Name, packageName, StringComparison.Ordinal));
        if (index < 0)
        {
            return false;
        }

        packages.RemoveAt(index);
        Version++;
        changed?.Invoke();
        return true;
    }

    public AspectCatalog BuildCatalog()
    {
        return AspectCatalog.FromPackages(packages, Version);
    }
}
