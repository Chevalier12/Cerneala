using Cerneala.Language.Text;
using LanguageSourceText = Cerneala.Language.Text.SourceText;

namespace Cerneala.LanguageServer.Workspace;

internal sealed class DocumentOverlayStore : IDisposable
{
    private readonly object gate = new();
    private readonly Dictionary<string, Entry> entries = new(PathComparer.Instance);
    private bool disposed;

    public int Count
    {
        get
        {
            lock (gate)
            {
                return disposed ? 0 : entries.Count;
            }
        }
    }

    public bool Open(string path, string text, long version)
    {
        lock (gate)
        {
            ThrowIfDisposed();
            if (entries.TryGetValue(path, out Entry? current) && version <= current.Text.Version)
            {
                return false;
            }

            Replace(path, LanguageSourceText.From(text, version));
            return true;
        }
    }

    public bool ApplyChanges(string path, long version, IReadOnlyList<Protocol.TextDocumentContentChangeEvent> changes)
    {
        lock (gate)
        {
            ThrowIfDisposed();
            if (!entries.TryGetValue(path, out Entry? current) || version <= current.Text.Version)
            {
                return false;
            }

            LanguageSourceText text = current.Text;
            foreach (Protocol.TextDocumentContentChangeEvent change in changes)
            {
                if (change.Range is null)
                {
                    text = LanguageSourceText.From(change.Text, text.Version + 1);
                    continue;
                }

                int start = text.GetOffset(new LinePosition(
                    change.Range.Start.Line,
                    change.Range.Start.Character));
                int end = text.GetOffset(new LinePosition(
                    change.Range.End.Line,
                    change.Range.End.Character));
                text = text.WithChange(new TextChange(new TextSpan(start, end - start), change.Text));
            }

            Replace(path, LanguageSourceText.From(text.ToString(), version));
            return true;
        }
    }

    public bool TryGet(string path, out LanguageSourceText? text, out CancellationToken versionToken)
    {
        lock (gate)
        {
            ThrowIfDisposed();
            if (entries.TryGetValue(path, out Entry? entry))
            {
                text = entry.Text;
                versionToken = entry.VersionCancellation.Token;
                return true;
            }

            text = null;
            versionToken = default;
            return false;
        }
    }

    public bool IsCurrent(string path, long version)
    {
        lock (gate)
        {
            return !disposed &&
                entries.TryGetValue(path, out Entry? entry) &&
                entry.Text.Version == version;
        }
    }

    public IReadOnlyList<DocumentOverlaySnapshot> GetSnapshots()
    {
        lock (gate)
        {
            ThrowIfDisposed();
            return entries.Select(pair => new DocumentOverlaySnapshot(
                    pair.Key,
                    pair.Value.Text,
                    pair.Value.VersionCancellation.Token))
                .ToArray();
        }
    }

    public void Close(string path)
    {
        lock (gate)
        {
            if (entries.Remove(path, out Entry? entry))
            {
                entry.CancelAndDispose();
            }
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            foreach (Entry entry in entries.Values)
            {
                entry.CancelAndDispose();
            }

            entries.Clear();
        }
    }

    private void Replace(string path, LanguageSourceText text)
    {
        if (entries.Remove(path, out Entry? previous))
        {
            previous.CancelAndDispose();
        }

        entries[path] = new Entry(text);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
    }

    private sealed class Entry(LanguageSourceText text)
    {
        public LanguageSourceText Text { get; } = text;

        public CancellationTokenSource VersionCancellation { get; } = new();

        public void CancelAndDispose()
        {
            VersionCancellation.Cancel();
            VersionCancellation.Dispose();
        }
    }
}

internal sealed record DocumentOverlaySnapshot(
    string Path,
    LanguageSourceText Text,
    CancellationToken VersionToken);
