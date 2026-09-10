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
            1,
            ref lowerUiVersion);
        renderCache.MarkRootBuilt();
    }

    private static void AppendElement(
        UIElement element,
        RetainedRenderCache renderCache,
        RenderCounters counters,
        DrawCommandList rootCommands,
        float ancestorOpacity,
        ref long lowerUiVersion)
    {
        if (!UIElementVisibility.ParticipatesInRendering(element))
        {
            return;
        }

        float elementOpacity = ancestorOpacity * element.Opacity * element.PresenceOpacity;
        if (elementOpacity <= 0)
        {
            return;
        }

        ElementRenderCache localCache = renderCache.GetElementCache(element);
        Matrix3x2 elementTransform = localCache.GetElementTransform(element);

        counters.CountComposedElement();
        bool hasTransform = elementTransform != Matrix3x2.Identity;
        if (hasTransform)
        {
            rootCommands.Add(DrawCommand.PushTransform(ToNumerics(elementTransform)));
            counters.CountEmittedCommands(1);
        }
        bool hasClip = TryGetClip(element, out LayoutRect clipBounds);
        if (hasClip)
        {
            rootCommands.Add(DrawCommand.PushClip(ToDrawRect(clipBounds)));
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
                ToDrawRect(element.ArrangedBounds),
                Matrix3x2.Identity,
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
        DrawCommandList localCommands = GetLocalCommands(element, localCache, out float offsetX, out float offsetY);
        for (int index = 0; index < localCommands.Count; index++)
        {
            DrawCommand command = localCommands[index];
            // Layout relocation changes drawing coordinates before backend pixel snapping.
            // Local affine scopes are conjugated into that relocated coordinate space.
            DrawCommand composed = ApplyOpacity(Translate(command, offsetX, offsetY), elementOpacity);
            rootCommands.Add(composed);
            counters.CountEmittedCommands(1);
        }

        if (hasPrism)
        {
            PrismDrawScope scope = CreatePrismScope(
                element,
                prismInstance!,
                cacheOwnerToken,
                ToDrawRect(element.ArrangedBounds),
                Matrix3x2.Identity,
                element.PrismVisualVersion,
                prismLowerUiVersion,
                ResolvePrismResources(
                    element,
                    prismInstance!));
            rootCommands.ReplaceAt(prismBeginIndex, DrawCommand.BeginPrism(scope));
        }

        UIElementCollection visualChildren = element.VisualChildren;
        for (int index = 0; index < visualChildren.Count; index++)
        {
            AppendElement(
                visualChildren[index],
                renderCache,
                counters,
                rootCommands,
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
                    elementOpacity,
                    ref lowerUiVersion);
            }
        }

        if (hasPrism)
        {
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
        if (hasTransform)
        {
            rootCommands.Add(DrawCommand.PopTransform());
            counters.CountEmittedCommands(1);
        }
    }

    internal static PrismDrawScope CreatePrismScope(
        UIElement element,
        PrismInstance instance,
        PrismCacheOwnerToken cacheOwnerToken,
        DrawRect controlBounds,
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
            controlBounds,
            ToNumerics(effectiveTransform),
            pixelScale,
            visualContentVersion,
            resources,
            lowerUiVersion);
    }

    internal static PrismDrawResources ResolvePrismResources(
        UIElement element,
        PrismInstance instance)
    {
        List<PrismDrawImageResource> imageResources = [];
        List<PrismDrawCurvesResource> curveResources = [];
        List<PrismDrawLensProfileResource> lensProfileResources = [];
        List<PrismDrawLightingResource> lightingResources = [];
        List<PrismDrawColorMatrixResource> colorMatrixResources = [];
        HashSet<PrismResourceId> resolvedImageIds = [];
        HashSet<PrismResourceId> resolvedCurveIds = [];
        HashSet<PrismResourceId> resolvedLensProfileIds = [];
        HashSet<PrismResourceId> resolvedLightingIds = [];
        HashSet<PrismResourceId> resolvedColorMatrixIds = [];
        foreach (PrismNodeDefinition definition in
            instance.Definition.Nodes)
        {
            ResolveNodeState(
                instance.GetNodeState(definition.Id));
        }

        return PrismDrawResources.Create(
            imageResources,
            curveResources,
            [],
            lensProfileResources,
            lightingResources,
            colorMatrixResources);

        void ResolveNodeState(PrismNodeState state)
        {
            switch (state)
            {
                case PrismLayerState layer
                    when layer.Visible && layer.Opacity > 0:
                    ResolveMask(layer.Mask);
                    ResolveFilters(layer.Filters);
                    ResolveStyles(layer.Styles);
                    break;
                case PrismGroupState group
                    when group.Visible && group.Opacity > 0:
                    ResolveMask(group.Mask);
                    ResolveFilters(group.Filters);
                    ResolveStyles(group.Styles);
                    foreach (PrismNodeState child in group.Children)
                    {
                        ResolveNodeState(child);
                    }
                    break;
            }
        }

        void ResolveFilters(
            IReadOnlyList<PrismFilterState> filters)
        {
            foreach (PrismFilterState filter in filters)
            {
                if (!filter.Visible)
                {
                    continue;
                }

                int stableId = (int)filter.Filter;
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

                    PrismResourceId id = filter.GetValue(
                        new PrismParameterKey<PrismResourceId>(
                            stableId,
                            property.TypeSlot));
                    if (id.Value <= 0)
                    {
                        continue;
                    }

                    if (filter.Filter == PrismFilterId.Curves &&
                        property.Name == "Curves")
                    {
                        ResolveCurves(id);
                    }
                    else if (filter.Filter == PrismFilterId.LensFlare &&
                        property.Name == "Lens")
                    {
                        ResolveLensProfile(id);
                    }
                    else if (filter.Filter == PrismFilterId.LightingEffects &&
                        property.Name == "Lights")
                    {
                        ResolveLighting(id);
                    }
                    else if (filter.Filter == PrismFilterId.ColorMatrix &&
                        property.Name == "Matrix")
                    {
                        ResolveColorMatrix(id);
                    }
                    else
                    {
                        ResolveImage(id);
                    }
                }
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
                !resolvedImageIds.Add(id))
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
                    imageResources.Add(
                        new PrismDrawImageResource(
                            id,
                            image,
                            version,
                            identity));
                }
            }

        }

        void ResolveCurves(PrismResourceId id)
        {
            if (id.Key is not string key ||
                !resolvedCurveIds.Add(id) ||
                !element.TryFindResource<PrismCurvesResource>(
                    key,
                    out PrismCurvesResource? resource))
            {
                return;
            }

            curveResources.Add(
                new PrismDrawCurvesResource(
                    id,
                    resource,
                    ResourceVersion<PrismCurvesResource>(key),
                    unchecked((uint)System.Runtime.CompilerServices
                        .RuntimeHelpers.GetHashCode(resource)) + 1L));
        }

        void ResolveLensProfile(PrismResourceId id)
        {
            if (id.Key is not string key ||
                !resolvedLensProfileIds.Add(id) ||
                !element.TryFindResource<PrismLensProfileResource>(
                    key,
                    out PrismLensProfileResource? resource))
            {
                return;
            }

            lensProfileResources.Add(
                new PrismDrawLensProfileResource(
                    id,
                    resource,
                    ResourceVersion<PrismLensProfileResource>(key),
                    unchecked((uint)System.Runtime.CompilerServices
                        .RuntimeHelpers.GetHashCode(resource)) + 1L));
        }

        void ResolveLighting(PrismResourceId id)
        {
            if (id.Key is not string key ||
                !resolvedLightingIds.Add(id) ||
                !element.TryFindResource<PrismLightingResource>(
                    key,
                    out PrismLightingResource? resource))
            {
                return;
            }

            lightingResources.Add(
                new PrismDrawLightingResource(
                    id,
                    resource,
                    ResourceVersion<PrismLightingResource>(key),
                    unchecked((uint)System.Runtime.CompilerServices
                        .RuntimeHelpers.GetHashCode(resource)) + 1L));
        }

        void ResolveColorMatrix(PrismResourceId id)
        {
            if (id.Key is not string key ||
                !resolvedColorMatrixIds.Add(id) ||
                !element.TryFindResource<PrismColorMatrixResource>(
                    key,
                    out PrismColorMatrixResource? resource))
            {
                return;
            }

            colorMatrixResources.Add(
                new PrismDrawColorMatrixResource(
                    id,
                    resource,
                    ResourceVersion<PrismColorMatrixResource>(key),
                    unchecked((uint)System.Runtime.CompilerServices
                        .RuntimeHelpers.GetHashCode(resource)) + 1L));
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
            DrawCommandKind.DrawRectangle when command.Pen is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Pen, command.BrushOpacity),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.FillRoundedRectangle when command.Brush is not null => DrawCommand.FillRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Brush, command.BrushOpacity),
            DrawCommandKind.FillRoundedRectangle => DrawCommand.FillRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Color),
            DrawCommandKind.DrawRoundedRectangle when command.Pen is not null => DrawCommand.DrawRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Pen, command.BrushOpacity),
            DrawCommandKind.DrawRoundedRectangle => DrawCommand.DrawRoundedRectangle(Translate(command.Rect, offsetX, offsetY), command.CornerRadius, command.Color, command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.BrushOpacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawEllipse when command.Pen is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Pen),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Brush, command.Thickness, command.BrushOpacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(Translate(command.Rect, offsetX, offsetY), command.Color, command.Thickness),
            DrawCommandKind.DrawLine when command.Pen is not null => DrawCommand.DrawLine(
                Translate(command.Position, offsetX, offsetY),
                Translate(command.EndPoint, offsetX, offsetY),
                command.Pen,
                command.BrushOpacity),
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
            DrawCommandKind.FillPath when command.Brush is not null => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Brush,
                command.FillRule,
                command.BrushOpacity),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Color,
                command.FillRule),
            DrawCommandKind.DrawPath => DrawCommand.DrawPath(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.Pen!,
                command.BrushOpacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                Translate(command.Position, offsetX, offsetY),
                command.Brush,
                command.BrushOpacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, Translate(command.Position, offsetX, offsetY), command.Color),
            DrawCommandKind.DrawTextLayout => DrawCommand.DrawTextLayout(
                command.TextLayout!,
                Translate(command.Position, offsetX, offsetY),
                command.BrushOpacity),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(
                command.Image!,
                Translate(command.Rect, offsetX, offsetY),
                command.ImageOptions!),
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch => TransformMeshCommand(
                command,
                point => Translate(point, offsetX, offsetY),
                opacity: 1),
            DrawCommandKind.RenderSurface2D => DrawCommand.RenderSurface2D(command.RenderSurface!, Translate(command.Rect, offsetX, offsetY), command.Color),
            DrawCommandKind.PushTransform => DrawCommand.PushTransform(
                NumericsMatrix3x2.CreateTranslation(-offsetX, -offsetY) *
                command.Transform *
                NumericsMatrix3x2.CreateTranslation(offsetX, offsetY)),
            DrawCommandKind.PushPathClip => DrawCommand.PushClip(
                command.Path!,
                command.SourceRect,
                Translate(command.Rect, offsetX, offsetY),
                command.FillRule),
            DrawCommandKind.PushClip => DrawCommand.PushClip(Translate(command.Rect, offsetX, offsetY)),
            DrawCommandKind.PopClip => command,
            DrawCommandKind.BeginPrism
                when command.PrismScope is PrismDrawScope scope &&
                    scope.IsLocalDrawingScope =>
                DrawCommand.BeginPrism(scope.TranslateLocal(offsetX, offsetY)),
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

    private static DrawCommand ApplyOpacity(DrawCommand command, float opacity)
    {
        if (opacity >= 1)
        {
            return command;
        }

        return command.Kind switch
        {
            DrawCommandKind.FillRectangle when command.Brush is not null => DrawCommand.FillRectangle(command.Rect, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRectangle => DrawCommand.FillRectangle(command.Rect, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRectangle when command.Pen is not null => DrawCommand.DrawRectangle(command.Rect, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle when command.Brush is not null => DrawCommand.DrawRectangle(command.Rect, command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRectangle => DrawCommand.DrawRectangle(command.Rect, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillRoundedRectangle when command.Brush is not null => DrawCommand.FillRoundedRectangle(command.Rect, command.CornerRadius, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillRoundedRectangle => DrawCommand.FillRoundedRectangle(command.Rect, command.CornerRadius, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawRoundedRectangle when command.Pen is not null => DrawCommand.DrawRoundedRectangle(command.Rect, command.CornerRadius, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawRoundedRectangle => DrawCommand.DrawRoundedRectangle(command.Rect, command.CornerRadius, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.FillEllipse when command.Brush is not null => DrawCommand.FillEllipse(command.Rect, command.Brush, command.BrushOpacity * opacity),
            DrawCommandKind.FillEllipse => DrawCommand.FillEllipse(command.Rect, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawEllipse when command.Pen is not null => DrawCommand.DrawEllipse(command.Rect, command.Pen, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse when command.Brush is not null => DrawCommand.DrawEllipse(command.Rect, command.Brush, command.Thickness, command.BrushOpacity * opacity),
            DrawCommandKind.DrawEllipse => DrawCommand.DrawEllipse(command.Rect, ApplyOpacity(command.Color, opacity), command.Thickness),
            DrawCommandKind.DrawLine when command.Pen is not null => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                command.Pen,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine when command.Brush is not null => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                command.Brush,
                command.Thickness,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawLine => DrawCommand.DrawLine(
                command.Position,
                command.EndPoint,
                ApplyOpacity(command.Color, opacity),
                command.Thickness),
            DrawCommandKind.FillPath when command.Brush is not null => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                command.Brush,
                command.FillRule,
                command.BrushOpacity * opacity),
            DrawCommandKind.FillPath => DrawCommand.FillPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                ApplyOpacity(command.Color, opacity),
                command.FillRule),
            DrawCommandKind.DrawPath => DrawCommand.DrawPath(
                command.Path!,
                command.SourceRect,
                command.Rect,
                command.Pen!,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText when command.Brush is not null => DrawCommand.DrawText(
                command.TextRun!,
                command.Position,
                command.Brush,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawText => DrawCommand.DrawText(command.TextRun!, command.Position, ApplyOpacity(command.Color, opacity)),
            DrawCommandKind.DrawTextLayout => DrawCommand.DrawTextLayout(
                command.TextLayout!,
                command.Position,
                command.BrushOpacity * opacity),
            DrawCommandKind.DrawImage => DrawCommand.DrawImage(
                command.Image!,
                command.Rect,
                ApplyOpacity(command.ImageOptions!, opacity)),
            DrawCommandKind.DrawImageQuad or
            DrawCommandKind.DrawNineSlice or
            DrawCommandKind.DrawMesh or
            DrawCommandKind.DrawPointBatch or
            DrawCommandKind.DrawLineBatch or
            DrawCommandKind.DrawSpriteBatch => TransformMeshCommand(
                command,
                static point => point,
                opacity),
            DrawCommandKind.RenderSurface2D => DrawCommand.RenderSurface2D(command.RenderSurface!, command.Rect, ApplyOpacity(command.Color, opacity)),
            _ => command
        };
    }

    private static Color ApplyOpacity(Color color, float opacity)
    {
        if (opacity >= 1)
        {
            return color;
        }

        return new Color(color.R, color.G, color.B, (byte)Math.Clamp((int)MathF.Round(color.A * opacity), 0, 255));
    }

    private static DrawImageOptions ApplyOpacity(
        DrawImageOptions options,
        float opacity) =>
        new(
            options.Source,
            options.Tint,
            options.Opacity * opacity,
            options.Rotation,
            options.Origin,
            options.Flip,
            options.LayerDepth,
            options.Sampling,
            options.AddressMode);

    private static DrawCommand TransformMeshCommand(
        DrawCommand command,
        Func<DrawPoint, DrawPoint> transform,
        float opacity)
    {
        if (command.PointBatch is DrawPointBatch pointBatch)
        {
            DrawPointBatch transformed = pointBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                pointBatch: transformed);
        }
        if (command.LineBatch is DrawLineBatch lineBatch)
        {
            DrawLineBatch transformed = lineBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                lineBatch: transformed);
        }
        if (command.SpriteBatch is DrawSpriteBatch spriteBatch)
        {
            DrawSpriteBatch transformed = spriteBatch.Transform(
                transform,
                opacity);
            return DrawCommand.WithMesh(
                command,
                transformed.Mesh,
                spriteBatch: transformed);
        }

        return DrawCommand.WithMesh(
            command,
            command.Mesh!.Transform(transform, opacity));
    }

}
