using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    private sealed partial class GenerationScope
    {

        private enum NamedSymbolKind
        {
            Element,
            Brush,
            ImageResource,
            SpriteAnimationSet,
            Aspect,
            MotionSpec,
            MotionClip,
            TimbreClip
        }

        private sealed class NamedSymbol
        {
            public NamedSymbol(string name, NamedSymbolKind kind, object source)
            {
                Name = name;
                Kind = kind;
                Source = source;
            }

            public string Name { get; }

            public NamedSymbolKind Kind { get; }

            public object Source { get; }
        }

        private sealed class BrushResource
        {
            public BrushResource(string name, string variable, string expression, MarkupElement source, string? colorExpression = null)
            {
                Name = name;
                Variable = variable;
                Expression = expression;
                ColorExpression = colorExpression;
                Source = source;
            }

            public string Name { get; }

            public string Variable { get; }

            public string Expression { get; }

            public string? ColorExpression { get; }

            public MarkupElement Source { get; }
        }

        private sealed class ImageResourceDeclaration
        {
            public ImageResourceDeclaration(
                string name,
                string variable,
                string source,
                MarkupElement element)
            {
                Name = name;
                Variable = variable;
                Source = source;
                Element = element;
            }

            public string Name { get; }

            public string Variable { get; }

            public string Source { get; }

            public MarkupElement Element { get; }
        }

        private sealed class SpriteAnimationSetResource
        {
            public SpriteAnimationSetResource(
                string name,
                string variable,
                string expression,
                IReadOnlyCollection<string> clipNames,
                MarkupElement element)
            {
                Name = name;
                Variable = variable;
                Expression = expression;
                ClipNames = clipNames;
                Element = element;
            }

            public string Name { get; }

            public string Variable { get; }

            public string Expression { get; }

            public IReadOnlyCollection<string> ClipNames { get; }

            public MarkupElement Element { get; }
        }

        private sealed class ResourceScope
        {
            public ResourceScope(MarkupElement owner)
            {
                Owner = owner;
            }

            public MarkupElement Owner { get; }

            public Dictionary<string, NamedSymbol> NamedResources { get; } = new(StringComparer.Ordinal);

            public Dictionary<string, AspectResource> DefaultAspectsByTarget { get; } = new(StringComparer.Ordinal);

            public List<PrismCompositionResourceSyntax> PrismCompositions { get; } = [];

            public List<object> RuntimeResources { get; } = [];
        }

        public sealed class ApplicationResourceCatalog
        {
            private readonly HashSet<object> symbols;

            public ApplicationResourceCatalog(
                IReadOnlyDictionary<string, object> namedResources,
                IReadOnlyDictionary<string, object> defaultAspects,
                IReadOnlyDictionary<string, object> prismCompositions)
            {
                NamedResources = namedResources;
                DefaultAspects = defaultAspects;
                PrismCompositions = prismCompositions;
                symbols = new HashSet<object>(namedResources.Values);
            }

            public IReadOnlyDictionary<string, object> NamedResources { get; }

            public IReadOnlyDictionary<string, object> DefaultAspects { get; }

            public IReadOnlyDictionary<string, object> PrismCompositions { get; }

            public bool Contains(object symbol) => symbols.Contains(symbol);
        }

        private void ReadResources()
        {
            MarkupElement[] owners = document.Root.DescendantsAndSelf().ToArray();
            foreach (MarkupElement owner in owners)
            {
                string expectedName = owner.Name.LocalName + ".Resources";
                MarkupElement[] resourceProperties = owner.Elements()
                    .Where(element => element.Name.LocalName.EndsWith(".Resources", StringComparison.Ordinal))
                    .ToArray();
                MarkupElement[] matching = resourceProperties
                    .Where(element => string.Equals(element.Name.LocalName, expectedName, StringComparison.Ordinal))
                    .ToArray();

                foreach (MarkupElement invalid in resourceProperties.Where(element => !matching.Contains(element)))
                {
                    Report(
                        InvalidDocumentShape,
                        invalid,
                        Path.GetFileName(file.Path),
                        "Resource property element '" + invalid.Name.LocalName + "' must match its owner tag '" + expectedName + "'.");
                    invalid.Remove();
                }

                if (matching.Length > 1)
                {
                    Report(
                        InvalidDocumentShape,
                        matching[1],
                        Path.GetFileName(file.Path),
                        "Element '" + owner.Name.LocalName + "' may declare only one Resources property element.");
                    foreach (MarkupElement duplicate in matching.Skip(1))
                    {
                        duplicate.Remove();
                    }
                }

                if (matching.Length == 0)
                {
                    continue;
                }

                MarkupElement resources = matching[0];
                if (resources.HasAttributes || resources.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                {
                    Report(
                        InvalidDocumentShape,
                        resources,
                        Path.GetFileName(file.Path),
                        "A Resources property element accepts only resource declarations.");
                }

                ResourceScope scope = new(owner);
                resourceScopes.Add(owner, scope);
                resourcePropertyScopes.Add(resources, scope);
                foreach (MarkupElement resource in resources.Elements())
                {
                    switch (resource.Name.LocalName)
                    {
                        case "SolidColorBrush":
                        case "LinearGradientBrush":
                        case "RadialGradientBrush":
                        case "ImageBrush":
                        case "DrawingBrush":
                            ReadBrush(scope, resource);
                            break;
                        case "ImageResource":
                            ReadImageResource(scope, resource);
                            break;
                        case "SpriteAnimationSet":
                            ReadSpriteAnimationSet(scope, resource);
                            break;
                        case "VisualBrush":
                            Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "VisualBrush is runtime-only because its source is a live element.");
                            break;
                        case "Aspect":
                            ReadAspect(scope, resource);
                            break;
                        case "ContentTemplate":
                            Report(
                                InvalidDocumentShape,
                                resource,
                                Path.GetFileName(file.Path),
                                "ContentTemplate cannot be declared in Resources. " +
                                "Declare it inline on a template property or inside ItemsControl.Templates.");
                            break;
                        case "Tween":
                        case "Spring":
                            ReadMotionSpecResource(scope, resource);
                            break;
                        case "MotionClip":
                            ReadMotionClip(scope, resource);
                            break;
                        case "PrismComposition":
                            ReadPrismComposition(scope, resource);
                            break;
                        case "TimbreClip":
                            ReadTimbreClip(scope, resource);
                            break;
                        default:
                            Report(UnsupportedElement, resource, resource.Name.LocalName);
                            break;
                    }
                }

                resources.Remove();
            }
        }

        private void ReadBrush(ResourceScope scope, MarkupElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }

            if (scope.NamedResources.ContainsKey(name))
            {
                Report(InvalidDocumentShape, resource, Path.GetFileName(file.Path), "Duplicate resource Name '" + name + "' in the same scope.");
                return;
            }

            string? expression = BuildBrushExpression(resource);
            if (expression is null)
            {
                return;
            }

            _ = BuildSolidColorBrushExpression(resource, out string? colorExpression);

            string variable = CreateIdentifier(name) + "Resource" + nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            BrushResource brush = new(name, variable, expression, resource, colorExpression);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.Brush, brush));
            scope.RuntimeResources.Add(brush);
            string brushType = "global::Cerneala.UI.Media." + resource.Name.LocalName;
            string initializer = brush.Expression.StartsWith("new " + brushType, StringComparison.Ordinal)
                ? "new" + brush.Expression.Substring(("new " + brushType).Length)
                : brush.Expression;
            currentLines.Add(brushType + " " + variable + " = " + initializer + ";");
        }

        private void ReadImageResource(ResourceScope scope, MarkupElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }

            if (scope.NamedResources.ContainsKey(name))
            {
                Report(
                    InvalidDocumentShape,
                    resource,
                    Path.GetFileName(file.Path),
                    "Duplicate resource Name '" + name + "' in the same scope.");
                return;
            }

            MarkupAttribute? unsupportedAttribute = resource.Attributes()
                .FirstOrDefault(attribute => !attribute.IsNamespaceDeclaration &&
                    attribute.Name.LocalName is not "Name" and not "Source");
            if (unsupportedAttribute is not null)
            {
                Report(
                    InvalidDocumentShape,
                    unsupportedAttribute,
                    Path.GetFileName(file.Path),
                    "ImageResource supports only Name and Source attributes.");
                return;
            }

            if (resource.Elements().Any() ||
                resource.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            {
                Report(
                    InvalidDocumentShape,
                    resource,
                    Path.GetFileName(file.Path),
                    "ImageResource accepts no child content.");
                return;
            }

            MarkupAttribute? sourceAttribute = resource.Attribute("Source");
            string source = sourceAttribute?.Value.Trim() ?? string.Empty;
            if (source.Length == 0)
            {
                Report(
                    InvalidPropertyValue,
                    (MarkupObject?)sourceAttribute ?? resource,
                    "ImageResource",
                    "Source",
                    sourceAttribute?.Value ?? string.Empty);
                return;
            }

            string variable = CreateIdentifier(name) + "ImageResource" +
                nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            ImageResourceDeclaration declaration = new(name, variable, source, resource);
            scope.NamedResources.Add(
                name,
                new NamedSymbol(name, NamedSymbolKind.ImageResource, declaration));
            scope.RuntimeResources.Add(declaration);
            currentLines.Add(
                "global::Cerneala.UI.Resources.ImageResource " + variable +
                " = new global::Cerneala.UI.Resources.ImageResource(" + Literal(source) + ");");
        }

        private void ReadSpriteAnimationSet(ResourceScope scope, MarkupElement resource)
        {
            string? name = RequiredName(resource);
            if (name is null)
            {
                return;
            }
            if (scope.NamedResources.ContainsKey(name))
            {
                ReportAnimation((object?)resource.Attribute("Name") ?? resource, "Resource name '" + name + "' is duplicated in the same scope.");
                return;
            }

            MarkupAttribute? unsupportedSetAttribute = resource.Attributes()
                .FirstOrDefault(attribute => !attribute.IsNamespaceDeclaration &&
                    attribute.Name.LocalName is not "Name" and not "Version");
            if (unsupportedSetAttribute is not null)
            {
                ReportAnimation(unsupportedSetAttribute, "SpriteAnimationSet supports only Name and Version attributes.");
                return;
            }
            if (resource.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            {
                ReportAnimation(resource, "SpriteAnimationSet accepts clip elements and no text content.");
                return;
            }

            string? setVersion = ParseAnimationVersion(resource, "SpriteAnimationSet");
            if (setVersion is null)
            {
                return;
            }

            List<string> clipExpressions = [];
            HashSet<string> clipNames = new(StringComparer.Ordinal);
            foreach (MarkupElement clip in resource.Elements())
            {
                if (clip.Name.LocalName != "SpriteAnimationClip")
                {
                    ReportAnimation(clip, "SpriteAnimationSet accepts only SpriteAnimationClip children.");
                    return;
                }

                MarkupAttribute? clipNameAttribute = clip.Attribute("Name");
                string clipName = clipNameAttribute?.Value.Trim() ?? string.Empty;
                if (clipName.Length == 0)
                {
                    ReportAnimation((object?)clipNameAttribute ?? clip, "SpriteAnimationClip Name cannot be empty.");
                    return;
                }
                if (!clipNames.Add(clipName))
                {
                    ReportAnimation(clipNameAttribute!, "Animation clip name '" + clipName + "' is duplicate.");
                    return;
                }

                MarkupAttribute? unsupportedClipAttribute = clip.Attributes()
                    .FirstOrDefault(attribute => !attribute.IsNamespaceDeclaration &&
                        attribute.Name.LocalName is not "Name" and not "IsLooping" and not "Version");
                if (unsupportedClipAttribute is not null)
                {
                    ReportAnimation(unsupportedClipAttribute, "SpriteAnimationClip supports only Name, IsLooping, and Version attributes.");
                    return;
                }
                if (clip.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
                {
                    ReportAnimation(clip, "SpriteAnimationClip accepts frame elements and no text content.");
                    return;
                }

                bool isLooping = true;
                MarkupAttribute? loopingAttribute = clip.Attribute("IsLooping");
                if (loopingAttribute is not null && !bool.TryParse(loopingAttribute.Value, out isLooping))
                {
                    ReportAnimation(loopingAttribute, "IsLooping must be true or false.");
                    return;
                }
                string? clipVersion = ParseAnimationVersion(clip, "SpriteAnimationClip");
                if (clipVersion is null)
                {
                    return;
                }

                List<string> frameExpressions = [];
                foreach (MarkupElement frame in clip.Elements())
                {
                    string? frameExpression = ParseSpriteAnimationFrame(frame);
                    if (frameExpression is null)
                    {
                        return;
                    }
                    frameExpressions.Add(frameExpression);
                }
                if (frameExpressions.Count == 0)
                {
                    ReportAnimation(clip, "SpriteAnimationClip '" + clipName + "' requires at least one frame.");
                    return;
                }

                clipExpressions.Add(
                    "new global::Cerneala.UI.Controls.SpriteAnimationClip(" + Literal(clipName) +
                    ", new global::Cerneala.UI.Controls.SpriteAnimationFrame[] { " +
                    string.Join(", ", frameExpressions) + " }, " +
                    (isLooping ? "true" : "false") + ", " + clipVersion + ")");
            }
            if (clipExpressions.Count == 0)
            {
                ReportAnimation(resource, "SpriteAnimationSet requires at least one clip.");
                return;
            }

            string expression =
                "new global::Cerneala.UI.Controls.SpriteAnimationSet(" +
                "new global::Cerneala.UI.Controls.SpriteAnimationClip[] { " +
                string.Join(", ", clipExpressions) + " }, " + setVersion + ")";
            string variable = CreateIdentifier(name) + "SpriteAnimationSet" +
                nextResourceId.ToString(CultureInfo.InvariantCulture);
            nextResourceId++;
            SpriteAnimationSetResource declaration = new(name, variable, expression, clipNames.ToArray(), resource);
            scope.NamedResources.Add(name, new NamedSymbol(name, NamedSymbolKind.SpriteAnimationSet, declaration));
            scope.RuntimeResources.Add(declaration);
            currentLines.Add("global::Cerneala.UI.Controls.SpriteAnimationSet " + variable + " = " + expression + ";");
        }

        private string? ParseAnimationVersion(MarkupElement element, string elementName)
        {
            MarkupAttribute? attribute = element.Attribute("Version");
            if (attribute is null)
            {
                return "1L";
            }
            if (!long.TryParse(attribute.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long version) || version <= 0)
            {
                ReportAnimation(attribute, elementName + " Version must be a positive 64-bit integer.");
                return null;
            }
            return version.ToString(CultureInfo.InvariantCulture) + "L";
        }

        private string? ParseSpriteAnimationFrame(MarkupElement frame)
        {
            if (frame.Name.LocalName != "SpriteAnimationFrame")
            {
                ReportAnimation(frame, "SpriteAnimationClip accepts only SpriteAnimationFrame children.");
                return null;
            }
            if (frame.Elements().Any() ||
                frame.Nodes().OfType<MarkupText>().Any(text => !string.IsNullOrWhiteSpace(text.Value)))
            {
                ReportAnimation(frame, "SpriteAnimationFrame accepts attributes only.");
                return null;
            }

            string[] required = ["SourceX", "SourceY", "SourceWidth", "SourceHeight", "Duration"];
            string? missingName = required.FirstOrDefault(attributeName => frame.Attribute(attributeName) is null);
            if (missingName is not null)
            {
                ReportAnimation(frame, "SpriteAnimationFrame requires " + missingName + ".");
                return null;
            }
            MarkupAttribute? unsupported = frame.Attributes()
                .FirstOrDefault(attribute => !attribute.IsNamespaceDeclaration &&
                    !required.Contains(attribute.Name.LocalName) && attribute.Name.LocalName != "Flip");
            if (unsupported is not null)
            {
                ReportAnimation(unsupported, "SpriteAnimationFrame attribute '" + unsupported.Name.LocalName + "' is not supported.");
                return null;
            }

            MarkupAttribute sourceXAttribute = frame.Attribute("SourceX")!;
            MarkupAttribute sourceYAttribute = frame.Attribute("SourceY")!;
            MarkupAttribute sourceWidthAttribute = frame.Attribute("SourceWidth")!;
            MarkupAttribute sourceHeightAttribute = frame.Attribute("SourceHeight")!;
            bool validX = TryParseFiniteFloat(sourceXAttribute.Value, out string? sourceX);
            bool validY = TryParseFiniteFloat(sourceYAttribute.Value, out string? sourceY);
            bool validWidth = TryParseFiniteFloat(sourceWidthAttribute.Value, out string? sourceWidth) &&
                float.TryParse(sourceWidthAttribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float width) && width > 0;
            bool validHeight = TryParseFiniteFloat(sourceHeightAttribute.Value, out string? sourceHeight) &&
                float.TryParse(sourceHeightAttribute.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float height) && height > 0;
            if (!validX || !validY || !validWidth || !validHeight)
            {
                MarkupAttribute location = !validWidth
                    ? sourceWidthAttribute
                    : !validHeight
                        ? sourceHeightAttribute
                        : !validX
                            ? sourceXAttribute
                            : sourceYAttribute;
                ReportAnimation(location, "Frame source rectangle must be finite with positive SourceWidth and SourceHeight.");
                return null;
            }

            MarkupAttribute durationAttribute = frame.Attribute("Duration")!;
            if (!TryBuildDurationExpression(durationAttribute.Value.Trim(), out string duration))
            {
                ReportAnimation(durationAttribute, "Frame duration must be positive and use the existing ms or s duration syntax.");
                return null;
            }

            string flip = "global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip.None";
            MarkupAttribute? flipAttribute = frame.Attribute("Flip");
            if (flipAttribute is not null)
            {
                string value = flipAttribute.Value.Trim();
                if (value is "None" or "Horizontal" or "Vertical")
                {
                    flip = "global::Cerneala.UI.Controls.RenderSurface2DSpriteFlip." + value;
                }
                else
                {
                    ReportAnimation(flipAttribute, "Frame Flip must be None, Horizontal, or Vertical.");
                    return null;
                }
            }

            return "new global::Cerneala.UI.Controls.SpriteAnimationFrame(" +
                "new global::Cerneala.Drawing.DrawRect(" + sourceX + ", " + sourceY + ", " + sourceWidth + ", " + sourceHeight + "), " +
                duration + ", " + flip + ")";
        }

        private void ReportAnimation(object locationSource, string message) =>
            Report(InvalidSpriteAnimation, locationSource, Path.GetFileName(file.Path), message);

        private void ValidateStaticAnimationState(MarkupElement element)
        {
            if (element.Name.LocalName != "Sprite2D")
            {
                return;
            }

            MarkupAttribute? animationsAttribute = element.Attribute("Animations");
            MarkupAttribute? stateAttribute = element.Attribute("AnimationState");
            if (animationsAttribute is null || stateAttribute is null ||
                LooksLikeBindingPath(animationsAttribute.Value) ||
                LooksLikeBindingPath(stateAttribute.Value) ||
                !animationsAttribute.Value.Trim().StartsWith("$", StringComparison.Ordinal))
            {
                return;
            }

            string resourceName = animationsAttribute.Value.Trim().Substring(1);
            string state = stateAttribute.Value.Trim();
            if (TryResolveResource(animationsAttribute, resourceName, out NamedSymbol symbol) &&
                symbol.Source is SpriteAnimationSetResource animation &&
                !animation.ClipNames.Contains(state))
            {
                ReportAnimation(
                    stateAttribute,
                    "Animation state '" + state + "' does not exist in SpriteAnimationSet '" + resourceName + "'.");
            }
        }

        private string? RequiredName(MarkupElement element)
        {
            string? name = element.Attribute("Name")?.Value;
            if (string.IsNullOrWhiteSpace(name))
            {
                Report(InvalidPropertyValue, element, element.Name.LocalName, "Name", name ?? string.Empty);
                return null;
            }

            name = name!.Trim();
            if (IsReservedTemplateReference(name))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)element.Attribute("Name") ?? element,
                    Path.GetFileName(file.Path),
                    "Resource Name '" + name + "' is reserved by component templates.");
                return null;
            }

            return name;
        }

        private bool AddSymbol(string name, NamedSymbolKind kind, object source, MarkupElement location)
        {
            if (IsReservedTemplateReference(name))
            {
                Report(
                    InvalidDocumentShape,
                    (MarkupObject?)location.Attribute("Name") ?? location,
                    Path.GetFileName(file.Path),
                    "Name '" + name + "' is reserved by component templates.");
                return false;
            }

            if (symbols.ContainsKey(name))
            {
                Report(InvalidDocumentShape, location, Path.GetFileName(file.Path), "Duplicate Name '" + name + "'.");
                return false;
            }

            symbols.Add(name, new NamedSymbol(name, kind, source));
            return true;
        }

        private static bool IsReservedTemplateReference(string name)
        {
            return string.Equals(name, "owner", StringComparison.Ordinal) ||
                string.Equals(name, "self", StringComparison.Ordinal) ||
                string.Equals(name, "root", StringComparison.Ordinal);
        }

        private void EmitRuntimeResources(MarkupElement owner, string ownerVariable)
        {
            if (!resourceScopes.TryGetValue(owner, out ResourceScope? scope))
            {
                return;
            }

            foreach (object resource in scope.RuntimeResources)
            {
                switch (resource)
                {
                    case BrushResource brush:
                        currentLines.Add(
                            ownerVariable + ".Resources.SetResource(new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Media.Brush>(" +
                            Literal(brush.Name) + "), " + brush.Variable + ");");
                        break;
                    case ImageResourceDeclaration image:
                        currentLines.Add(
                            ownerVariable + ".Resources.SetResource(new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Resources.ImageResource>(" +
                            Literal(image.Name) + "), " + image.Variable + ");");
                        break;
                    case SpriteAnimationSetResource animation:
                        currentLines.Add(
                            ownerVariable + ".Resources.SetResource(new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Controls.SpriteAnimationSet>(" +
                            Literal(animation.Name) + "), " + animation.Variable + ");");
                        break;
                    case TimbreClipResource sound:
                        currentLines.Add(
                            ownerVariable + ".Resources.SetResource(new global::Cerneala.UI.Resources.ResourceId<" + TimbreClipType + ">(" +
                            Literal(sound.Name) + "), " + sound.Variable + ");");
                        break;
                    case AspectResource aspect:
                        string targetType = ResolveAspectTargetType(aspect.TargetName, aspect.Source)!;
                        string key = aspect.Name is null ? "typeof(" + targetType + ")" : Literal(aspect.Name);
                        if (aspect.Name is null)
                        {
                            EmitAspectPackageResource(ownerVariable, key, targetType, aspect);
                        }
                        else
                        {
                            EmitNamedElementAspectResource(ownerVariable, key, targetType, aspect);
                        }
                        break;
                }
            }
        }

        public ApplicationResourceCatalog CreateApplicationResourceCatalog()
        {
            if (!resourceScopes.TryGetValue(document.Root, out ResourceScope? scope))
            {
                return new ApplicationResourceCatalog(
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new Dictionary<string, object>(StringComparer.Ordinal),
                    new Dictionary<string, object>(StringComparer.Ordinal));
            }

            IReadOnlyDictionary<string, object> prismCompositions =
                boundPrismResources.TryGetValue(
                    scope,
                    out Dictionary<string, BoundPrismComposition>? bound)
                    ? bound.ToDictionary(
                        pair => pair.Key,
                        pair => (object)pair.Value,
                        StringComparer.Ordinal)
                    : new Dictionary<string, object>(StringComparer.Ordinal);
            return new ApplicationResourceCatalog(
                scope.NamedResources.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal),
                scope.DefaultAspectsByTarget.ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal),
                prismCompositions);
        }

        public void EmitApplicationResources()
        {
            EmitRuntimeResources(document.Root, "this");
        }

        private string ReadReferenceName(string elementName, string propertyName, MarkupAttribute attribute)
        {
            string value = attribute.Value.Trim();
            if (!value.StartsWith("$", StringComparison.Ordinal) || value.Length == 1)
            {
                Report(InvalidPropertyValue, attribute, elementName, propertyName, attribute.Value);
                return string.Empty;
            }

            return value.Substring(1);
        }

        private GeneratedExpression? ResolveReferenceValue(string elementName, string propertyName, string referenceName, MarkupValueKind targetKind, MarkupObject source)
        {
            if (!TryResolveResource(source, referenceName, out NamedSymbol symbol))
            {
                Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
                return null;
            }

            if (targetKind == MarkupValueKind.Brush && symbol.Source is BrushResource brushResource)
            {
                if (applicationResources?.Contains(symbol) == true)
                {
                    string code =
                        "((global::Cerneala.UI.Resources.IResourceProvider)global::Cerneala.UI.Application.Current!.Resources).GetResource(" +
                        "new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Media.Brush>(" +
                        Literal(referenceName) + "))";
                    return new GeneratedExpression(code, MarkupValueKind.Brush, referenceName);
                }

                return new GeneratedExpression(brushResource.Variable, MarkupValueKind.Brush);
            }

            if (targetKind == MarkupValueKind.Color && symbol.Source is BrushResource brush && brush.ColorExpression is not null)
            {
                return new GeneratedExpression(brush.ColorExpression, MarkupValueKind.Color);
            }

            if (targetKind == MarkupValueKind.ImageReference &&
                symbol.Source is ImageResourceDeclaration)
            {
                return new GeneratedExpression(
                    "new global::Cerneala.UI.Resources.ImageReference(new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Resources.ImageResource>(" +
                    Literal(referenceName) + "))",
                    MarkupValueKind.ImageReference);
            }

            if (targetKind == MarkupValueKind.ImageResourceId &&
                symbol.Source is ImageResourceDeclaration)
            {
                return new GeneratedExpression(
                    "new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Resources.ImageResource>(" +
                    Literal(referenceName) + ")",
                    MarkupValueKind.ImageResourceId);
            }

            if (targetKind == MarkupValueKind.SpriteAnimationSet &&
                symbol.Source is SpriteAnimationSetResource animation)
            {
                if (applicationResources?.Contains(symbol) == true)
                {
                    return new GeneratedExpression(
                        "((global::Cerneala.UI.Resources.IResourceProvider)global::Cerneala.UI.Application.Current!.Resources).GetResource(" +
                        "new global::Cerneala.UI.Resources.ResourceId<global::Cerneala.UI.Controls.SpriteAnimationSet>(" +
                        Literal(referenceName) + "))",
                        MarkupValueKind.SpriteAnimationSet,
                        referenceName);
                }

                return new GeneratedExpression(animation.Variable, MarkupValueKind.SpriteAnimationSet);
            }

            Report(InvalidPropertyValue, source, elementName, propertyName, "$" + referenceName);
            return null;
        }

        private bool TryResolveResource(MarkupObject source, string name, out NamedSymbol symbol)
        {
            foreach (ResourceScope scope in EnumerateResourceScopes(source))
            {
                if (scope.NamedResources.TryGetValue(name, out symbol))
                {
                    return true;
                }
            }

            if (applicationResources is not null &&
                applicationResources.NamedResources.TryGetValue(name, out object? applicationSymbol) &&
                applicationSymbol is NamedSymbol typedSymbol)
            {
                symbol = typedSymbol;
                return true;
            }

            symbol = null!;
            return false;
        }

        private bool TryResolveObjectSymbol(MarkupObject source, string name, out NamedSymbol symbol)
        {
            if (symbols.TryGetValue(name, out symbol))
            {
                return true;
            }

            return TryResolveResource(source, name, out symbol);
        }

        private IEnumerable<ResourceScope> EnumerateResourceScopes(MarkupObject source)
        {
            MarkupElement? current = source switch
            {
                MarkupElement element => element,
                _ => source.Parent
            };

            while (current is not null)
            {
                if (resourcePropertyScopes.TryGetValue(current, out ResourceScope? declarationScope))
                {
                    yield return declarationScope;
                    current = declarationScope.Owner.Parent;
                    continue;
                }

                if (resourceScopes.TryGetValue(current, out ResourceScope? scope))
                {
                    yield return scope;
                }

                current = current.Parent;
            }
        }
    }
}
