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
}

public sealed class DrawCommandStateAnalysis
{
    internal DrawCommandStateAnalysis(
        DrawCommandList commands,
        long commandListVersion,
        IReadOnlyList<DrawCommandStateEntry> entries)
    {
        Commands = commands;
        CommandListVersion = commandListVersion;
        Entries = new ReadOnlyCollection<DrawCommandStateEntry>(
            entries.ToArray());
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
    public DrawCommandStateAnalysis Analyze(DrawCommandList commands)
    {
        ArgumentNullException.ThrowIfNull(commands);
        long version = commands.Version;
        DrawCommandStateEntry[] entries = new DrawCommandStateEntry[commands.Count];
        List<OpenState> stack = [];
        List<Matrix3x2> transforms = [Matrix3x2.Identity];
        List<DrawRect?> clips = [null];
        List<float> opacities = [1];
        List<DrawBlendMode> blends = [DrawBlendMode.Normal];

        for (int index = 0; index < commands.Count; index++)
        {
            DrawCommand command = commands[index];
            DrawCommandMetadata metadata = DrawCommandMetadata.Create(command);
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
                Metadata = metadata
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
                    throw Mismatch(command.Kind, index, stack);
                }
                CompleteScope(index);
                clips.RemoveAt(clips.Count - 1);
            }

            void CloseLayer()
            {
                CloseSimple(command.Kind, DrawCommandKind.PushLayer);
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
        if (commands.Version != version || commands.Count != entries.Length)
        {
            throw new InvalidOperationException(
                "The draw command list changed while its state analysis was being built.");
        }

        return new DrawCommandStateAnalysis(commands, version, entries);
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
