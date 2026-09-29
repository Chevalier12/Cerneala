using System.Collections.ObjectModel;
using System.Numerics;

namespace Cerneala.Drawing;

public enum DrawBlendMode
{
    Normal,
    Opaque,
    Additive,
    Multiply,
    Screen
}

public sealed record DrawLayerOptions
{
    public DrawLayerOptions(
        float opacity = 1,
        DrawBlendMode blendMode = DrawBlendMode.Normal)
    {
        if (!float.IsFinite(opacity) || opacity < 0 || opacity > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(opacity));
        }
        if (!Enum.IsDefined(blendMode))
        {
            throw new ArgumentOutOfRangeException(nameof(blendMode));
        }

        Opacity = opacity;
        BlendMode = blendMode;
    }

    public float Opacity { get; }

    public DrawBlendMode BlendMode { get; }
}

internal enum DrawStateScopeKind
{
    Transform,
    Clip,
    Opacity,
    Blend,
    Layer
}

public ref struct DrawTransformScope
{
    private DrawingContext? context;
    private readonly long token;

    internal DrawTransformScope(DrawingContext context, long token)
    {
        this.context = context;
        this.token = token;
    }

    public void Dispose()
    {
        DrawingContext owner = context ??
            throw new ObjectDisposedException(nameof(DrawTransformScope));
        owner.PopScoped(DrawStateScopeKind.Transform, token);
        context = null;
    }
}

public ref struct DrawClipScope
{
    private DrawingContext? context;
    private readonly long token;

    internal DrawClipScope(DrawingContext context, long token)
    {
        this.context = context;
        this.token = token;
    }

    public void Dispose()
    {
        DrawingContext owner = context ??
            throw new ObjectDisposedException(nameof(DrawClipScope));
        owner.PopScoped(DrawStateScopeKind.Clip, token);
        context = null;
    }
}

public ref struct DrawOpacityScope
{
    private DrawingContext? context;
    private readonly long token;

    internal DrawOpacityScope(DrawingContext context, long token)
    {
        this.context = context;
        this.token = token;
    }

    public void Dispose()
    {
        DrawingContext owner = context ??
            throw new ObjectDisposedException(nameof(DrawOpacityScope));
        owner.PopScoped(DrawStateScopeKind.Opacity, token);
        context = null;
    }
}

public ref struct DrawBlendScope
{
    private DrawingContext? context;
    private readonly long token;

    internal DrawBlendScope(DrawingContext context, long token)
    {
        this.context = context;
        this.token = token;
    }

    public void Dispose()
    {
        DrawingContext owner = context ??
            throw new ObjectDisposedException(nameof(DrawBlendScope));
        owner.PopScoped(DrawStateScopeKind.Blend, token);
        context = null;
    }
}

public ref struct DrawLayerScope
{
    private DrawingContext? context;
    private readonly long token;

    internal DrawLayerScope(DrawingContext context, long token)
    {
        this.context = context;
        this.token = token;
    }

    public void Dispose()
    {
        DrawingContext owner = context ??
            throw new ObjectDisposedException(nameof(DrawLayerScope));
        owner.PopScoped(DrawStateScopeKind.Layer, token);
        context = null;
    }
}

public readonly record struct DrawCommandStateEntry(
    DrawRect? Bounds,
    Matrix3x2 Transform,
    DrawRect? ClipBounds,
    float Opacity,
    DrawBlendMode BlendMode,
    bool IsContextSensitive,
    int MatchingCommandIndex)
{
    internal DrawCommandMetadata? Metadata { get; init; }

    // Copies of the metadata's reuse key, so the next analysis can match an
    // unchanged retained command without reading its metadata object.
    internal RetainedCommandKey RetainedKey { get; init; }

    internal int ImageWidth { get; init; }

    internal int ImageHeight { get; init; }
}

// Read-only entries over the analyzer's array; later analyses read the
// array directly rather than through the collection interfaces.
internal sealed class DrawCommandStateEntries(DrawCommandStateEntry[] array, int count)
    : ReadOnlyCollection<DrawCommandStateEntry>(new ArraySegment<DrawCommandStateEntry>(array, 0, count))
{
    internal DrawCommandStateEntry[] Array { get; } = array;
}

public sealed class DrawCommandStateAnalysis
{
    internal DrawCommandStateAnalysis(
        DrawCommandList commands,
        long commandListVersion,
        DrawCommandStateEntry[] ownedEntries,
        int count)
        : this(commands, commandListVersion, new DrawCommandStateEntries(ownedEntries, count))
    {
        // The analyzer transfers its filled array after completing every entry.
        Buffer = ownedEntries;
    }

    // The array backing freshly analyzed entries, which the owner of this
    // analysis may recycle once nothing retains the entries.
    internal DrawCommandStateEntry[]? Buffer { get; }

    internal DrawCommandStateAnalysis(
        DrawCommandList commands,
        long commandListVersion,
        IReadOnlyList<DrawCommandStateEntry> immutableEntries)
    {
        Commands = commands;
        CommandListVersion = commandListVersion;
        // Rebinding a fully revalidated snapshot changes only its list/version
        // association. No consumer can modify the shared entries.
        Entries = immutableEntries;
    }

    public IReadOnlyList<DrawCommandStateEntry> Entries { get; }

    public long CommandListVersion { get; }

    internal DrawCommandList Commands { get; }

    internal void EnsureCurrent(DrawCommandList commands)
    {
        if (!ReferenceEquals(Commands, commands) ||
            commands.Version != CommandListVersion ||
            commands.Count != Entries.Count)
        {
            throw new InvalidOperationException(
                "The draw command list changed after its state analysis was built.");
        }
    }
}

public sealed class DrawCommandStateAnalyzer
{
    public DrawCommandStateAnalysis Analyze(DrawCommandList commands) => Analyze(commands, previousEntries: null);

    // Scenes re-record their visible content every frame, so commands enter
    // and leave the list as the camera moves. Pair each command with the next
    // identical earlier command rather than only the one at the same position,
    // so one insertion or removal does not re-derive every later snapshot.
    private struct PreviousCommandCursor(DrawCommandStateEntry[]? entries, int count, int start)
    {
        private const int Window = 64;
        private int next = start;

        internal DrawCommandMetadata Skip(DrawCommandMetadata metadata)
        {
            next++;
            return metadata;
        }

        // The previous entry to derive this command's snapshot from, or -1.
        // Locate a retained owner's prior command even when its version changed.
        // Resolve still checks the full key before reusing metadata; finding a
        // predecessor is not proof that its content is unchanged.
        internal int Find(in DrawCommand command, RetainedCommandKey key, int index)
        {
            if (entries is null)
            {
                return -1;
            }

            // A run entering visibility can precede a removed command. Keep
            // looking within the bounded window after misses, otherwise the
            // unchanged, shifted tail never gets a chance to resynchronize.
            int limit = Math.Min(count, next + Window);
            for (int candidate = next; candidate < limit; candidate++)
            {
                ref readonly DrawCommandStateEntry entry = ref entries[candidate];
                if (entry.Metadata is DrawCommandMetadata metadata &&
                    (key.IsSet
                        ? ReferenceEquals(key.Owner, entry.RetainedKey.Owner)
                        : metadata.MatchesBits(command)))
                {
                    next = candidate + 1;
                    return candidate;
                }
            }

            if (index >= count || entries[index].Metadata is null)
            {
                return -1;
            }
            // An owner match at the same position resynchronizes the cursor
            // after a run of changed commands.
            if (key.IsSet && ReferenceEquals(key.Owner, entries[index].RetainedKey.Owner))
            {
                next = index + 1;
            }
            return index;
        }
    }

    // A previous entry's snapshot when a retained key vouches for it (exactly
    // Create's keyed reuse), otherwise a snapshot derived from it.
    private static DrawCommandMetadata Resolve(
        in DrawCommand command,
        RetainedCommandKey key,
        DrawCommandStateEntry[]? previousEntries,
        int previousIndex)
    {
        if (previousIndex < 0)
        {
            return DrawCommandMetadata.Create(command, null, key);
        }

        ref readonly DrawCommandStateEntry previous = ref previousEntries![previousIndex];
        return DrawCommandMetadata.MatchesRetained(command, key, previous.RetainedKey, previous.ImageWidth, previous.ImageHeight)
            ? previous.Metadata!
            : DrawCommandMetadata.Create(command, previous.Metadata, key);
    }

    internal DrawCommandStateAnalysis Analyze(
        DrawCommandList commands,
        IReadOnlyList<DrawCommandStateEntry>? previousEntries,
        DrawCommandStateEntry[]? buffer = null)
    {
        ArgumentNullException.ThrowIfNull(commands);
        long version = commands.Version;
        int commandCount = commands.Count;
        int revalidatedCount = 0;
        DrawCommandMetadata? firstChangedMetadata = null;
        // Analyzer output exposes its array; read it without interface dispatch.
        int previousCount = previousEntries?.Count ?? 0;
        DrawCommandStateEntry[]? previousArray = previousEntries switch
        {
            null => null,
            DrawCommandStateEntries owned => owned.Array,
            _ => previousEntries.ToArray()
        };
        if (previousArray is not null && previousCount == commandCount)
        {
            for (; revalidatedCount < commandCount; revalidatedCount++)
            {
                DrawCommandMetadata? previous = previousArray[revalidatedCount].Metadata;
                DrawCommandMetadata current = previous is null
                    ? DrawCommandMetadata.Create(commands.ItemRef(revalidatedCount), null, commands.GetRetainedKey(revalidatedCount))
                    : Resolve(commands.ItemRef(revalidatedCount), commands.GetRetainedKey(revalidatedCount),
                        previousArray, revalidatedCount);
                EnsureUnchanged(commands, version, commandCount);
                if (!ReferenceEquals(current, previous))
                {
                    firstChangedMetadata = current;
                    break;
                }
            }
            if (revalidatedCount == commandCount)
            {
                return new DrawCommandStateAnalysis(commands, version, previousEntries!);
            }
        }

        // A recycled buffer avoids a large-object allocation on every frame; a
        // recycling caller's replacement leaves room for the list to grow.
        DrawCommandStateEntry[] entries = buffer is null
            ? new DrawCommandStateEntry[commandCount]
            : buffer.Length >= commandCount
                ? buffer
                : new DrawCommandStateEntry[commandCount + (commandCount / 4)];
        Array.Clear(entries, commandCount, entries.Length - commandCount);
        List<OpenState> stack = [];
        List<Matrix3x2> transforms = [Matrix3x2.Identity];
        List<DrawRect?> clips = [null];
        List<float> opacities = [1];
        List<DrawBlendMode> blends = [DrawBlendMode.Normal];

        PreviousCommandCursor cursor = new(previousArray, previousCount, revalidatedCount);
        for (int index = 0; index < commands.Count; index++)
        {
            ref readonly DrawCommand command = ref commands.ItemRef(index);
            DrawCommandKind commandKind = command.Kind;
            // The equivalent prefix and first changed command were already
            // visited above. Do not invoke mutable resource descriptors twice.
            RetainedCommandKey key = commands.GetRetainedKey(index);
            DrawCommandMetadata metadata = index < revalidatedCount
                ? previousArray![index].Metadata!
                : index == revalidatedCount && firstChangedMetadata is not null
                    ? cursor.Skip(firstChangedMetadata)
                    : Resolve(command, key, previousArray, cursor.Find(command, key, index));
            Matrix3x2 transform = transforms[^1];
            DrawRect? clip = clips[^1];
            DrawRect? bounds = metadata.Bounds is DrawRect localBounds
                ? TransformBounds(localBounds, transform)
                : null;
            if (bounds is DrawRect commandBounds && clip is DrawRect clipBounds)
            {
                bounds = Intersect(commandBounds, clipBounds);
            }

            entries[index] = new DrawCommandStateEntry(
                bounds,
                transform,
                clip,
                opacities[^1],
                blends[^1],
                metadata.IsContextSensitive,
                MatchingCommandIndex: -1)
            {
                Metadata = metadata,
                RetainedKey = metadata.RetainedKey,
                ImageWidth = metadata.ImageWidth,
                ImageHeight = metadata.ImageHeight
            };

            if (!metadata.IsContextSensitive && bounds is DrawRect drawnBounds)
            {
                for (int scopeIndex = 0; scopeIndex < stack.Count; scopeIndex++)
                {
                    stack[scopeIndex].Include(drawnBounds);
                }
            }

            switch (command.Kind)
            {
                case DrawCommandKind.PushTransform:
                    transforms.Add(Matrix3x2.Multiply(
                        command.Transform,
                        transforms[^1]));
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                case DrawCommandKind.PopTransform:
                    Close(command.Kind, DrawCommandKind.PushTransform, transforms);
                    break;
                case DrawCommandKind.PushClip:
                {
                    DrawRect worldClip = TransformBounds(
                        command.Rect,
                        transforms[^1]);
                    clips.Add(clips[^1] is DrawRect current
                        ? Intersect(current, worldClip)
                        : worldClip);
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                }
                case DrawCommandKind.PushPathClip:
                {
                    DrawRect worldClip = TransformBounds(
                        command.Rect,
                        transforms[^1]);
                    clips.Add(clips[^1] is DrawRect current
                        ? Intersect(current, worldClip)
                        : worldClip);
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                }
                case DrawCommandKind.PopClip:
                    CloseClip();
                    break;
                case DrawCommandKind.PushOpacity:
                    opacities.Add(opacities[^1] * command.Opacity);
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                case DrawCommandKind.PopOpacity:
                    Close(command.Kind, DrawCommandKind.PushOpacity, opacities);
                    break;
                case DrawCommandKind.PushBlend:
                    blends.Add(command.BlendMode);
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                case DrawCommandKind.PopBlend:
                    Close(command.Kind, DrawCommandKind.PushBlend, blends);
                    break;
                case DrawCommandKind.PushLayer:
                    opacities.Add(opacities[^1] * command.LayerOptions!.Opacity);
                    blends.Add(command.LayerOptions.BlendMode);
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                case DrawCommandKind.PopLayer:
                    CloseLayer();
                    break;
                case DrawCommandKind.BeginPrism:
                    stack.Add(new OpenState(command.Kind, index));
                    break;
                case DrawCommandKind.EndPrism:
                    CloseSimple(command.Kind, DrawCommandKind.BeginPrism);
                    break;
            }

            void Close<T>(
                DrawCommandKind popKind,
                DrawCommandKind pushKind,
                List<T> values)
            {
                CloseSimple(popKind, pushKind);
                values.RemoveAt(values.Count - 1);
            }

            void CloseClip()
            {
                if (stack.Count == 0 ||
                    stack[^1].Kind is not (
                        DrawCommandKind.PushClip or
                        DrawCommandKind.PushPathClip))
                {
                    throw Mismatch(commandKind, index, stack);
                }
                CompleteScope(index);
                clips.RemoveAt(clips.Count - 1);
            }

            void CloseLayer()
            {
                CloseSimple(commandKind, DrawCommandKind.PushLayer);
                opacities.RemoveAt(opacities.Count - 1);
                blends.RemoveAt(blends.Count - 1);
            }

            void CloseSimple(
                DrawCommandKind popKind,
                DrawCommandKind pushKind)
            {
                if (stack.Count == 0 || stack[^1].Kind != pushKind)
                {
                    throw Mismatch(popKind, index, stack);
                }
                CompleteScope(index);
            }

            void CompleteScope(int popIndex)
            {
                OpenState opened = stack[^1];
                stack.RemoveAt(stack.Count - 1);
                DrawRect? scopeBounds = opened.Bounds;
                entries[opened.CommandIndex] = entries[opened.CommandIndex] with
                {
                    Bounds = scopeBounds,
                    MatchingCommandIndex = popIndex
                };
                entries[popIndex] = entries[popIndex] with
                {
                    Bounds = scopeBounds,
                    MatchingCommandIndex = opened.CommandIndex
                };
            }
        }

        if (stack.Count > 0)
        {
            OpenState opened = stack[^1];
            throw new InvalidOperationException(
                $"{opened.Kind} at command index {opened.CommandIndex} has no matching pop command.");
        }
        EnsureUnchanged(commands, version, commandCount);

        return new DrawCommandStateAnalysis(commands, version, entries, commandCount);
    }

    private static void EnsureUnchanged(DrawCommandList commands, long version, int count)
    {
        if (commands.Version != version || commands.Count != count)
        {
            throw new InvalidOperationException(
                "The draw command list changed while its state analysis was being built.");
        }
    }

    private static Exception Mismatch(
        DrawCommandKind popKind,
        int commandIndex,
        IReadOnlyList<OpenState> stack)
    {
        string open = stack.Count == 0
            ? "no state scope is open"
            : $"the current scope is {stack[^1].Kind} from command index {stack[^1].CommandIndex}";
        return new InvalidOperationException(
            $"{popKind} at command index {commandIndex} is not LIFO; {open}.");
    }

    internal static DrawRect TransformBounds(
        DrawRect bounds,
        Matrix3x2 transform)
    {
        Vector2 topLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Y), transform);
        Vector2 topRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Y), transform);
        Vector2 bottomLeft = Vector2.Transform(new Vector2(bounds.X, bounds.Bottom), transform);
        Vector2 bottomRight = Vector2.Transform(new Vector2(bounds.Right, bounds.Bottom), transform);
        float left = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float top = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float right = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float bottom = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));
        return new DrawRect(left, top, MathF.Max(0, right - left), MathF.Max(0, bottom - top));
    }

    internal static DrawRect Intersect(DrawRect left, DrawRect right)
    {
        float x = MathF.Max(left.X, right.X);
        float y = MathF.Max(left.Y, right.Y);
        float rightEdge = MathF.Min(left.Right, right.Right);
        float bottomEdge = MathF.Min(left.Bottom, right.Bottom);
        return new DrawRect(
            x,
            y,
            MathF.Max(0, rightEdge - x),
            MathF.Max(0, bottomEdge - y));
    }

    private sealed class OpenState
    {
        public OpenState(DrawCommandKind kind, int commandIndex)
        {
            Kind = kind;
            CommandIndex = commandIndex;
        }

        public DrawCommandKind Kind { get; }

        public int CommandIndex { get; }

        public DrawRect? Bounds { get; private set; }

        public void Include(DrawRect bounds)
        {
            Bounds = Bounds is DrawRect current
                ? Union(current, bounds)
                : bounds;
        }

        private static DrawRect Union(DrawRect first, DrawRect second)
        {
            float left = MathF.Min(first.X, second.X);
            float top = MathF.Min(first.Y, second.Y);
            float right = MathF.Max(first.Right, second.Right);
            float bottom = MathF.Max(first.Bottom, second.Bottom);
            return new DrawRect(left, top, right - left, bottom - top);
        }
    }
}
