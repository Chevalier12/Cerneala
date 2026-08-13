using System.Collections.Concurrent;
using Cerneala.Language.Diagnostics;
using Cerneala.Language.Semantics.Symbols;

namespace Cerneala.Language.Semantics;

internal sealed class CernealaCompilation : IDisposable
{
    private readonly IReadOnlyDictionary<string, CernealaDocument> documents;
    private readonly ConcurrentDictionary<string, CacheEntry> semanticModels;
    private bool disposed;

    public CernealaCompilation(
        ILanguageCompilationSymbols symbols,
        IEnumerable<CernealaDocument> documents,
        AnalysisMode mode = AnalysisMode.Editor)
        : this(
            symbols,
            documents.ToDictionary(document => document.Path, StringComparer.OrdinalIgnoreCase),
            mode,
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase))
    {
    }

    private CernealaCompilation(
        ILanguageCompilationSymbols symbols,
        IReadOnlyDictionary<string, CernealaDocument> documents,
        AnalysisMode mode,
        ConcurrentDictionary<string, CacheEntry> semanticModels)
    {
        Symbols = symbols ?? throw new ArgumentNullException(nameof(symbols));
        this.documents = documents;
        Mode = mode;
        this.semanticModels = semanticModels;
    }

    public ILanguageCompilationSymbols Symbols { get; }

    public AnalysisMode Mode { get; }

    public IReadOnlyCollection<CernealaDocument> Documents => documents.Values.ToArray();

    public CernealaSemanticModel GetSemanticModel(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        cancellationToken.ThrowIfCancellationRequested();
        if (!documents.TryGetValue(path, out CernealaDocument? document))
        {
            throw new KeyNotFoundException("Unknown Cerneala document '" + path + "'.");
        }

        CacheEntry entry = semanticModels.GetOrAdd(
            path,
            _ => new CacheEntry(
                Symbols.Version,
                document.Version,
                new CernealaSemanticModel(document, documents.Values.ToArray(), Symbols, Mode, cancellationToken)));
        if (entry.CompilationVersion != Symbols.Version || entry.DocumentVersion != document.Version)
        {
            entry.Release();
            entry = new CacheEntry(
                Symbols.Version,
                document.Version,
                new CernealaSemanticModel(document, documents.Values.ToArray(), Symbols, Mode, cancellationToken));
            semanticModels[path] = entry;
        }

        return entry.Model;
    }

    public CernealaCompilation WithDocument(CernealaDocument document)
    {
        ThrowIfDisposed();
        Dictionary<string, CernealaDocument> updated = documents.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase);
        updated[document.Path] = document;
        ConcurrentDictionary<string, CacheEntry> retained = new(StringComparer.OrdinalIgnoreCase);
        bool changesApplicationResources =
            documents.TryGetValue(document.Path, out CernealaDocument? previous) && IsApplicationDocument(previous) ||
            IsApplicationDocument(document);
        foreach (KeyValuePair<string, CacheEntry> pair in semanticModels)
        {
            if (!changesApplicationResources &&
                !string.Equals(pair.Key, document.Path, StringComparison.OrdinalIgnoreCase))
            {
                pair.Value.Retain();
                retained[pair.Key] = pair.Value;
            }
        }

        return new CernealaCompilation(Symbols, updated, Mode, retained);
    }

    public CernealaCompilation WithProjectSymbols(ILanguageCompilationSymbols symbols)
    {
        ThrowIfDisposed();
        return new CernealaCompilation(symbols, documents, Mode, new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (CacheEntry entry in semanticModels.Values.Distinct())
        {
            entry.Release();
        }

        semanticModels.Clear();
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(CernealaCompilation));
        }
    }

    private static bool IsApplicationDocument(CernealaDocument document) =>
        document.Syntax.Children.OfType<Syntax.ElementSyntax>().SingleOrDefault()?.Name == "Application";

    private sealed class CacheEntry
    {
        private int referenceCount = 1;

        public CacheEntry(long compilationVersion, long documentVersion, CernealaSemanticModel model)
        {
            CompilationVersion = compilationVersion;
            DocumentVersion = documentVersion;
            Model = model;
        }

        public long CompilationVersion { get; }

        public long DocumentVersion { get; }

        public CernealaSemanticModel Model { get; }

        public void Retain()
        {
            Interlocked.Increment(ref referenceCount);
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                Model.Dispose();
            }
        }
    }
}
