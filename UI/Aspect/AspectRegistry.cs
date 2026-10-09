using System.Collections.ObjectModel;
using Cerneala.UI.Relay;

namespace Cerneala.UI.Aspect;

public sealed class AspectRegistry
{
    private readonly List<AspectPackage> packages = [];
    private readonly HashSet<AspectPackage> frameworkPackages = new(ReferenceEqualityComparer.Instance);
    private readonly ReadOnlyCollection<AspectPackage> packagesView;
    private readonly Action? changed;
    private readonly IUiThreadAccess threadAccess;
    private AspectCatalog? cachedCatalog;

    public AspectRegistry(Action? changed = null)
        : this(new CapturedUiThreadAccess(), changed)
    {
    }

    internal AspectRegistry(IUiThreadAccess threadAccess, Action? changed = null)
    {
        this.threadAccess = threadAccess ?? throw new ArgumentNullException(nameof(threadAccess));
        this.changed = changed;
        packagesView = packages.AsReadOnly();
    }

    public int Version { get; private set; }

    public IReadOnlyList<AspectPackage> Packages => packagesView;

    public AspectRegistry Register(AspectPackage package, bool notify = true)
    {
        return RegisterCore(package, notify, isFrameworkDefault: false);
    }

    internal AspectRegistry RegisterFrameworkDefault(AspectPackage package, bool notify = true)
    {
        return RegisterCore(package, notify, isFrameworkDefault: true);
    }

    private AspectRegistry RegisterCore(AspectPackage package, bool notify, bool isFrameworkDefault)
    {
        threadAccess.VerifyAccess();
        ArgumentNullException.ThrowIfNull(package);
        if (packages.Any(existing => string.Equals(existing.Name, package.Name, StringComparison.Ordinal)))
        {
            throw new InvalidOperationException($"Aspect package '{package.Name}' is already registered.");
        }

        packages.Add(package);
        if (isFrameworkDefault)
        {
            frameworkPackages.Add(package);
        }

        OnPackagesChanged(notify);
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

        frameworkPackages.Remove(packages[index]);
        packages.RemoveAt(index);
        OnPackagesChanged(notify: true);
        return true;
    }

    public AspectCatalog BuildCatalog()
    {
        threadAccess.VerifyAccess();
        return cachedCatalog ??= AspectCatalog.FromPackages(packages, Version, frameworkPackages);
    }

    private void OnPackagesChanged(bool notify)
    {
        Version++;
        cachedCatalog = null;
        if (notify)
        {
            changed?.Invoke();
        }
    }
}
