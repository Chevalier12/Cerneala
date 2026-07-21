using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Prism.Definitions;
using Cerneala.UI.Prism.Runtime;
using Cerneala.UI.Resources;
using NumericsMatrix3x2 = System.Numerics.Matrix3x2;

namespace Cerneala.UI.Rendering;

public sealed class DrawCommandListBuilder
{
    public void Build(UIElement root, RetainedRenderCache renderCache, RenderCounters counters)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(renderCache);
        ArgumentNullException.ThrowIfNull(counters);

        DrawCommandList rootCommands = renderCache.RootCommands;
        rootCommands.Clear();
        long lowerUiVersion = 17;
        AppendElement(
            root,
            renderCache,
            counters,
            rootCommands,
            Matrix3x2.Identity,
            1,
            ref lowerUiVersion);
        renderCache.MarkRootBuilt();
    }

    private static void AppendElement(
        UIElement element,
        RetainedRenderCache renderCache,
        RenderCounters counters,
        DrawCommandList rootCommands,
        Matrix3x2 ancestorTransform,
        float ancestorOpacity,
        ref long lowerUiVersion)
    {
        if (!UIElementVisibility.ParticipatesInRendering(element))
        {
            return;
        }

        Matrix3x2 elementTransform = Matrix3x2.Multiply(
            ElementVisualTransform.GetElementTransform(element),
            ancestorTransform);
        float elementOpacity = ancestorOpacity * element.Opacity * element.PresenceOpacity;
        if (elementOpacity <= 0)
        {
            return;
        }

        counters.CountComposedElement();
        bool hasClip = TryGetClip(element, out LayoutRect clipBounds);
        if (hasClip)
        {
            rootCommands.Add(ApplyRenderScope(DrawCommand.PushClip(ToDrawRect(clipBounds)), elementTransform, 1));
            counters.CountEmittedCommands(1);
        }

        bool hasPrism = PrismAttachment.TryGetRenderState(
            element,
            out PrismInstance? prismInstance,
            out PrismCacheOwnerToken cacheOwnerToken);
        long prismLowerUiVersion = lowerUiVersion;
        int prismBeginIndex = -1;
        if (hasPrism)
        {
            prismBeginIndex = rootCommands.Count;
            rootCommands.Add(DrawCommand.BeginPrism(CreatePrismScope(
                element,
                prismInstance!,
                cacheOwnerToken,
                elementTransform,
                visualContentVersion: 0,
                prismLowerUiVersion,
                PrismDrawResources.Empty)));
            counters.CountEmittedCommands(1);
        }

        lowerUiVersion = MixVisualVersion(
            lowerUiVersion,
            element.PrismVisualNodeId);
        lowerUiVersion = MixVisualVersion(
            lowerUiVersion,
            element.PrismLocalVisualVersion);
        ElementRenderCache localCache = renderCache.GetElementCache(element);
        DrawCommandList localCommands = GetLocalCommands(element, localCache, out float offsetX, out float offsetY);
        for (int index = 0; index < localCommands.Count; index++)
        {
            DrawCommand command = localCommands[index];
            DrawCommand composed = ApplyRenderScope(
                Translate(command, offsetX, offsetY),
                elementTransform,
                elementOpacity);
            rootCommands.Add(composed);
            counters.CountEmittedCommands(1);
        }

        UIElementCollection visualChildren = element.VisualChildren;
        for (int index = 0; index < visualChildren.Count; index++)
        {
            AppendElement(
                visualChildren[index],
                renderCache,
                counters,
                rootCommands,
                elementTransform,
                elementOpacity,
                ref lowerUiVersion);
        }

        if (element.Root is UIRoot root)
        {
            IReadOnlyList<UIElement> exitingChildren =
                root.Motion.Presence.GetExitingVisualChildren(element);
            for (int index = 0; index < exitingChildren.Count; index++)
            {
                AppendElement(
                    exitingChildren[index],
                    renderCache,
                    counters,
                    rootCommands,
                    elementTransform,
                    elementOpacity,
                    ref lowerUiVersion);
            }
        }

        long visualContentVersion = element.PrismVisualVersion;
        if (hasPrism)
        {
            PrismDrawScope scope = CreatePrismScope(
                element,
                prismInstance!,
                cacheOwnerToken,
                elementTransform,
                visualContentVersion,
                prismLowerUiVersion,
                ResolvePrismResources(
                    element,
                    prismInstance!));
            rootCommands.ReplaceAt(prismBeginIndex, DrawCommand.BeginPrism(scope));
            rootCommands.Add(DrawCommand.EndPrism());
            counters.CountEmittedCommands(1);
            lowerUiVersion = MixVisualVersion(
                lowerUiVersion,
                cacheOwnerToken.Value);
            lowerUiVersion = MixVisualVersion(
                lowerUiVersion,
                prismInstance!.StructuralVersion.Value);
            lowerUiVersion = MixVisualVersion(
                lowerUiVersion,
                prismInstance.ValueVersion.Value);
        }

        if (hasClip)
        {
            rootCommands.Add(DrawCommand.PopClip());
            counters.CountEmittedCommands(1);
        }
    }

    private static PrismDrawScope CreatePrismScope(
        UIElement element,
        PrismInstance instance,
        PrismCacheOwnerToken cacheOwnerToken,
        Matrix3x2 effectiveTransform,
        long visualContentVersion,
        long lowerUiVersion,
        PrismDrawResources resources)
    {
        float pixelScale = element is UIRoot root
            ? root.Scale
            : element.Root?.Scale ?? 1;
        return new PrismDrawScope(
            instance,
            cacheOwnerToken,
            ToDrawRect(element.ArrangedBounds),
            ToNumerics(effectiveTransform),
            pixelScale,
            visualContentVersion,
            resources,
            lowerUiVersion);
    }

    private static PrismDrawResources ResolvePrismResources(
        UIElement element,
        PrismInstance instance)
    {
        List<PrismDrawImageResource> resources = [];
        HashSet<PrismResourceId> resolvedIds = [];
        foreach (PrismNodeDefinition definition in
            instance.Definition.Nodes)
        {
            ResolveNodeState(
                instance.GetNodeState(definition.Id));
        }

        return PrismDrawResources.Create(resources);

        void ResolveNodeState(PrismNodeState state)
        {
            switch (state)
            {
                case PrismLayerState layer
                    when layer.Visible && layer.Opacity > 0:
                    ResolveMask(layer.Mask);
                    ResolveStyles(layer.Styles);
                    break;
                case PrismGroupState group
                    when group.Visible && group.Opacity > 0:
                    ResolveMask(group.Mask);
                    ResolveStyles(group.Styles);
                    foreach (PrismNodeState child in group.Children)
                    {
                        ResolveNodeState(child);
                    }
                    break;
                case PrismBackdropState backdrop
                    when backdrop.Visible && backdrop.Opacity > 0:
                    ResolveMask(backdrop.Mask);
                    ResolveStyles(backdrop.Styles);
                    break;
            }
        }

        void ResolveMask(PrismMaskState? mask)
        {
            if (mask is null ||
                mask.Density <= 0 ||
                mask.Image.Value <= 0)
            {
                return;
            }

            ResolveImage(mask.Image);
        }

        void ResolveStyles(
            IReadOnlyList<PrismStyleState> styles)
        {
            foreach (PrismStyleState style in styles)
            {
                if (!style.Visible)
                {
                    continue;
                }

                int stableId = (int)style.Style;
                PrismCatalogEntryDescriptor entry =
                    PrismCatalogRuntime.GetEntry(stableId);
                foreach (PrismCatalogPropertyDescriptor property in
                    entry.Properties)
                {
                    if (property.ValueType !=
                        PrismCatalogValueType.Resource)
                    {
                        continue;
                    }

                    PrismResourceId id = style.GetValue(
                        new PrismParameterKey<PrismResourceId>(
                            stableId,
                            property.TypeSlot));
                    if (id.Value > 0)
                    {
                        ResolveImage(id);
                    }
                }
            }
        }

        void ResolveImage(PrismResourceId id)
        {
            if (id.Key is not string key ||
                !resolvedIds.Add(id))
            {
                return;
            }

            if (element.TryFindResource<ImageResource>(
                    key,
                    out ImageResource? resource))
            {
                IDrawImage? image = resource.HasEmbeddedImage
                    ? resource.Resolve()
                    : element.Root?.ImageResourceCache?.Resolve(resource);
                AddResolvedImage(
                    image,
                    ResourceVersion<ImageResource>(key),
                    resource.RetainedIdentity);
                return;
            }

            if (!element.TryFindResource<ImageBrush>(
                    key,
                    out ImageBrush? brush))
            {
                return;
            }

            IDrawImage? brushImage =
                brush.Source?.ResolveDrawImage() ?? brush.Image;
            string? sourceIdentity =
                brush.Source?.Identity ?? brush.SourceIdentity;
            if (brushImage is null &&
                !string.IsNullOrWhiteSpace(sourceIdentity))
            {
                ImageResource pathResource = new(sourceIdentity);
                brushImage = element.Root?.ImageResourceCache?
                    .Resolve(pathResource);
            }

            long identity = !string.IsNullOrWhiteSpace(sourceIdentity)
                ? unchecked((uint)StringComparer.Ordinal.GetHashCode(sourceIdentity)) + 1L
                : brushImage is null
                    ? 0
                    : unchecked((uint)System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(brushImage)) + 1L;
            AddResolvedImage(
                brushImage,
                ResourceVersion<ImageBrush>(key),
                identity);

            void AddResolvedImage(
                IDrawImage? image,
                long version,
                long identity)
            {
                if (image is not null)
                {
                    resources.Add(
                        new PrismDrawImageResource(
                            id,
                            image,
                            version,
                            identity));
                }
            }

            long ResourceVersion<T>(string resourceKey)
            {
                UIRoot? root = element.Root;
                if (root is null)
                {
                    return 1;
                }

                ResourceId<T> resourceId = new(resourceKey);
                root.ResourceDependencyTracker.RecordDependency(
                    element,
                    resourceId,
                    InvalidationFlags.Render,
                    affectsIntrinsicSize: false);
                return Math.Max(
                    1,
                    root.ResourceDependencyTracker
                        .GetResourceVersion(resourceId));
            }
        }
    }

    private static NumericsMatrix3x2 ToNumerics(Matrix3x2 matrix)
    {
        return new NumericsMatrix3x2(
            matrix.M11,
            matrix.M12,
            matrix.M21,
            matrix.M22,
            matrix.M31,
            matrix.M32);
    }

    private static long MixVisualVersion(long current, long value)
    {
        unchecked
        {
            ulong hash = (ulong)current;
            hash ^= (ulong)value + 0x9E3779B97F4A7C15UL + (hash << 6) + (hash >> 2);
            return (long)(hash & long.MaxValue);
        }
    }

    private static bool TryGetClip(UIElement element, out LayoutRect bounds)
    {
        if (ClipNode.TryGetClip(element, out ClipNode clip))
        {
            bounds = clip.Bounds;
            return true;
        }

        if (element.ClipToBounds)
        {
            bounds = element.ArrangedBounds;
            return true;
        }

        bounds = default;
        return false;
    }

    private static DrawRect ToDrawRect(LayoutRect rect)
    {
        return new DrawRect(rect.X, rect.Y, rect.Width, rect.Height);
    }

    private static DrawCommandList GetLocalCommands(
        UIElement element,
        ElementRenderCache localCache,
        out float offsetX,
        out float offsetY)
    {
        if (CanReuseTranslatedCommands(element, localCache, out offsetX, out offsetY))
        {
            return localCache.Commands;
        }

        offsetX = 0;
        offsetY = 0;
        return localCache.GetValidCommands(element);
    }

    private static bool CanReuseTranslatedCommands(
        UIElement element,
        ElementRenderCache localCache,
        out float offsetX,
        out float offsetY)
    {
        offsetX = 0;
        offsetY = 0;
        if (!localCache.IsValid ||
            element.DirtyState.Has(InvalidationFlags.Render) ||
            localCache.Dependencies != element.RenderDependencies ||
            localCache.ContentBounds.Width != element.ArrangedBounds.Width ||
            localCache.ContentBounds.Height != element.ArrangedBounds.Height)
        {
            return false;
        }

        offsetX = element.ArrangedBounds.X - localCache.ContentBounds.X;
        offsetY = element.ArrangedBounds.Y - localCache.ContentBounds.Y;
        return offsetX != 0 || offsetY != 0;
    }

    private static DrawCommand Translate(DrawCommand command, float offsetX, float offsetY)
    {
        if (offsetX == 0 && offsetY == 0)
        {
            return command;
        }

        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Brush,
                command.Thickness,
                command.BrushOpacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Color,
                command.Thickness),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.PathData!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Brush!,
                command.BrushOpacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                Translate(command.Position, offsetX, offsetY),
                command.Brush,
                command.BrushOpacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, Translate(command.Position, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(command.Image!, Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Translate(command.Rect, offsetX, offsetY)),
            DrawCommandKind.PopClip => command,
            DrawCommandKind.BeginPrism => command,
            DrawCommandKind.EndPrism => command,
            _ => command
        };
    }

    private static DrawRect Translate(DrawRect rect, float offsetX, float offsetY)
    {
        return new DrawRect(rect.X + offsetX, rect.Y + offsetY, rect.Width, rect.Height);
    }

    private static DrawPoint Translate(DrawPoint point, float offsetX, float offsetY)
    {
        return new DrawPoint(point.X + offsetX, point.Y + offsetY);
    }

    private static DrawCommand ApplyRenderScope(DrawCommand command, Matrix3x2 transform, float opacity)
    {
        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(Transform(command.Rect, transform), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Transform(command.Rect, transform), command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Transform(command.Rect, transform), command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Transform(command.Rect, transform), command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                transform.Transform(command.Position),
                transform.Transform(command.EndPoint),
                command.Brush,
                command.Thickness,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                transform.Transform(command.Position),
                transform.Transform(command.EndPoint),
                ApplyOpacity(command.Color, opacity),
                command.Thickness),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.PathData!,
                command.SourceRect,
                Transform(command.Rect, transform),
                command.Brush!,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                transform.Transform(command.Position),
                command.Brush,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, transform.Transform(command.Position), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(command.Image!, Transform(command.Rect, transform), ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Transform(command.Rect, transform)),
            DrawCommandKind.PopClip => command,
            DrawCommandKind.BeginPrism => command,
            DrawCommandKind.EndPrism => command,
            _ => command
        };
    }

    private static DrawRect Transform(DrawRect rect, Matrix3x2 transform)
    {
        DrawPoint topLeft = transform.Transform(new DrawPoint(rect.X, rect.Y));
        DrawPoint topRight = transform.Transform(new DrawPoint(rect.Right, rect.Y));
        DrawPoint bottomLeft = transform.Transform(new DrawPoint(rect.X, rect.Bottom));
        DrawPoint bottomRight = transform.Transform(new DrawPoint(rect.Right, rect.Bottom));

        float minX = MathF.Min(MathF.Min(topLeft.X, topRight.X), MathF.Min(bottomLeft.X, bottomRight.X));
        float minY = MathF.Min(MathF.Min(topLeft.Y, topRight.Y), MathF.Min(bottomLeft.Y, bottomRight.Y));
        float maxX = MathF.Max(MathF.Max(topLeft.X, topRight.X), MathF.Max(bottomLeft.X, bottomRight.X));
        float maxY = MathF.Max(MathF.Max(topLeft.Y, topRight.Y), MathF.Max(bottomLeft.Y, bottomRight.Y));

        return new DrawRect(minX, minY, maxX - minX, maxY - minY);
    }

    private static Color ApplyOpacity(Color color, float opacity)
    {
        if (opacity >= 1)
        {
            return color;
        }

        return new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255));
    }
}
