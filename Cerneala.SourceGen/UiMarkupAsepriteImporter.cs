using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Cerneala.SourceGen;

public sealed partial class UiMarkupGenerator
{
    // A non-markup AdditionalFiles item that markup can import, such as an Aseprite
    // JSON export. It compares by value, so editing one file regenerates the factories
    // and an unchanged file does not.
    private sealed class AnimationAsset : IEquatable<AnimationAsset>
    {
        public AnimationAsset(string path, string? text)
        {
            FilePath = path;
            Text = text;
        }

        public string FilePath { get; }

        public string? Text { get; }

        public bool Equals(AnimationAsset? other) =>
            other is not null &&
            string.Equals(FilePath, other.FilePath, StringComparison.Ordinal) &&
            string.Equals(Text, other.Text, StringComparison.Ordinal);

        public override bool Equals(object? obj) => Equals(obj as AnimationAsset);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(FilePath);
    }

    private static bool IsAnimationAssetPath(string path) =>
        !string.IsNullOrEmpty(path) && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);

    private sealed class AnimationAssetIndex
    {
        public static readonly AnimationAssetIndex Empty = new(ImmutableArray<AnimationAsset>.Empty);

        private readonly ImmutableArray<AnimationAsset> assets;

        public AnimationAssetIndex(ImmutableArray<AnimationAsset> assets)
        {
            this.assets = assets.IsDefault ? ImmutableArray<AnimationAsset>.Empty : assets;
        }

        // A Source matches an AdditionalFiles item whose full path ends with the Source
        // at a path-segment boundary, ignoring case and the slash style.
        public AnimationAsset[] Find(string source)
        {
            string wanted = NormalizeSlashes(source);
            return assets
                .Where(asset =>
                {
                    string path = NormalizeSlashes(asset.FilePath);
                    return string.Equals(path, wanted, StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith("/" + wanted, StringComparison.OrdinalIgnoreCase);
                })
                .OrderBy(asset => asset.FilePath, StringComparer.Ordinal)
                .ToArray();
        }

        private static string NormalizeSlashes(string path) => path.Trim().Replace('\\', '/');
    }

    private sealed class AsepriteFrame
    {
        public double X { get; set; }

        public double Y { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public double DurationMilliseconds { get; set; }

        public bool IsTrimmed { get; set; }

        public double OffsetX { get; set; }

        public double OffsetY { get; set; }

        public double FrameWidth { get; set; }

        public double FrameHeight { get; set; }
    }

    private sealed class AsepriteClip
    {
        public AsepriteClip(string name, bool isLooping, IReadOnlyList<AsepriteFrame> frames)
        {
            Name = name;
            IsLooping = isLooping;
            Frames = frames;
        }

        public string Name { get; }

        public bool IsLooping { get; }

        public IReadOnlyList<AsepriteFrame> Frames { get; }
    }

    // Reads the frame tags of an Aseprite JSON export (json-hash or json-array) into clips.
    private static class AsepriteJson
    {
        public static bool TryRead(string fileName, string? text, out List<AsepriteClip> clips, out string error)
        {
            clips = new List<AsepriteClip>();
            if (text is null)
            {
                error = "Aseprite JSON '" + fileName + "' could not be read.";
                return false;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(text);
            }
            catch (JsonException exception)
            {
                error = "Aseprite JSON '" + fileName + "' is not valid JSON: " + exception.Message;
                return false;
            }

            using (document)
            {
                return TryReadDocument(fileName, document.RootElement, clips, out error);
            }
        }

        private static bool TryReadDocument(string fileName, JsonElement root, List<AsepriteClip> clips, out string error)
        {
            error = string.Empty;
            if (!TryGetProperty(root, "frames", out JsonElement framesElement) ||
                framesElement.ValueKind is not (JsonValueKind.Array or JsonValueKind.Object))
            {
                error = "Aseprite JSON '" + fileName + "' has no 'frames' array or object.";
                return false;
            }

            // json-hash keeps the frame order of the export in the object, json-array in the array.
            List<JsonElement> frameElements = framesElement.ValueKind == JsonValueKind.Array
                ? framesElement.EnumerateArray().ToList()
                : framesElement.EnumerateObject().Select(property => property.Value).ToList();

            if (!TryGetProperty(root, "meta", out JsonElement meta) ||
                !TryGetProperty(meta, "frameTags", out JsonElement tagsElement) ||
                tagsElement.ValueKind != JsonValueKind.Array)
            {
                error = "Aseprite JSON '" + fileName + "' has no 'meta.frameTags' array. " +
                    "Export the sprite sheet with its tags (--list-tags).";
                return false;
            }

            JsonElement[] tags = tagsElement.EnumerateArray().ToArray();
            if (tags.Length == 0)
            {
                error = "Aseprite JSON '" + fileName + "' has no frame tags. " +
                    "A SpriteAnimationSet needs at least one tag; export the sprite sheet with its tags (--list-tags).";
                return false;
            }

            HashSet<string> names = new(StringComparer.Ordinal);
            Dictionary<int, AsepriteFrame> parsedFrames = new();
            for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
            {
                JsonElement tag = tags[tagIndex];
                if (tag.ValueKind != JsonValueKind.Object ||
                    !TryGetProperty(tag, "name", out JsonElement nameElement) ||
                    nameElement.ValueKind != JsonValueKind.String ||
                    string.IsNullOrWhiteSpace(nameElement.GetString()))
                {
                    error = "Tag " + tagIndex.ToString(CultureInfo.InvariantCulture) + " in '" + fileName + "' has no name.";
                    return false;
                }

                string name = nameElement.GetString()!;
                if (!names.Add(name))
                {
                    error = "Tag name '" + name + "' is duplicated in '" + fileName + "'.";
                    return false;
                }

                if (!TryGetInt(tag, "from", out int from) || !TryGetInt(tag, "to", out int to))
                {
                    error = "Tag '" + name + "' in '" + fileName + "' needs integer 'from' and 'to' values.";
                    return false;
                }

                if (from > to)
                {
                    error = "Tag '" + name + "' in '" + fileName + "': from " + from.ToString(CultureInfo.InvariantCulture) +
                        " is greater than to " + to.ToString(CultureInfo.InvariantCulture) + ".";
                    return false;
                }

                if (from < 0 || to >= frameElements.Count)
                {
                    error = "Tag '" + name + "' in '" + fileName + "' covers frames " +
                        from.ToString(CultureInfo.InvariantCulture) + "-" + to.ToString(CultureInfo.InvariantCulture) +
                        ", but the export has " + frameElements.Count.ToString(CultureInfo.InvariantCulture) + " frames.";
                    return false;
                }

                string? direction = TryGetProperty(tag, "direction", out JsonElement directionElement) &&
                    directionElement.ValueKind == JsonValueKind.String
                    ? directionElement.GetString()
                    : null;
                if (direction is not ("forward" or "reverse" or "pingpong" or "pingpong_reverse"))
                {
                    error = "Tag '" + name + "' in '" + fileName + "' has unknown direction '" + (direction ?? string.Empty) +
                        "'; expected forward, reverse, pingpong, or pingpong_reverse.";
                    return false;
                }

                // No repeat loops forever. Repeat "1" plays one pass, which for the ping-pong
                // directions is the first half: the pass in the tag's first direction.
                bool isLooping = true;
                if (TryGetProperty(tag, "repeat", out JsonElement repeatElement))
                {
                    string repeat = repeatElement.ValueKind == JsonValueKind.String
                        ? repeatElement.GetString() ?? string.Empty
                        : repeatElement.GetRawText();
                    if (repeat != "1")
                    {
                        error = "Tag '" + name + "' in '" + fileName + "' has repeat '" + repeat +
                            "'; only a missing repeat (loop) or repeat \"1\" (play once) is supported.";
                        return false;
                    }

                    isLooping = false;
                }

                List<AsepriteFrame> clipFrames = new();
                foreach (int frameIndex in ExpandFrameIndices(from, to, direction!, singlePass: !isLooping))
                {
                    if (!parsedFrames.TryGetValue(frameIndex, out AsepriteFrame? frame))
                    {
                        if (!TryReadFrame(fileName, frameIndex, frameElements[frameIndex], out frame, out error))
                        {
                            return false;
                        }

                        parsedFrames.Add(frameIndex, frame!);
                    }

                    clipFrames.Add(frame!);
                }

                clips.Add(new AsepriteClip(name, isLooping, clipFrames));
            }

            return true;
        }

        private static List<int> ExpandFrameIndices(int from, int to, string direction, bool singlePass)
        {
            if (singlePass)
            {
                direction = direction == "pingpong" ? "forward" : direction == "pingpong_reverse" ? "reverse" : direction;
            }

            List<int> indices = new();
            if (direction is "forward" or "pingpong")
            {
                for (int index = from; index <= to; index++)
                {
                    indices.Add(index);
                }

                if (direction == "pingpong")
                {
                    for (int index = to - 1; index > from; index--)
                    {
                        indices.Add(index);
                    }
                }
            }
            else
            {
                for (int index = to; index >= from; index--)
                {
                    indices.Add(index);
                }

                if (direction == "pingpong_reverse")
                {
                    for (int index = from + 1; index < to; index++)
                    {
                        indices.Add(index);
                    }
                }
            }

            return indices;
        }

        private static bool TryReadFrame(
            string fileName,
            int index,
            JsonElement element,
            out AsepriteFrame? frame,
            out string error)
        {
            frame = null;
            error = string.Empty;
            string label = "Frame " + index.ToString(CultureInfo.InvariantCulture) + " in '" + fileName + "'";
            if (element.ValueKind != JsonValueKind.Object)
            {
                error = label + " is not an object.";
                return false;
            }

            if (TryGetProperty(element, "rotated", out JsonElement rotated) && rotated.ValueKind == JsonValueKind.True)
            {
                error = label + " is rotated; rotated frames are not supported.";
                return false;
            }

            if (!TryGetProperty(element, "frame", out JsonElement rect) ||
                !TryGetNumber(rect, "x", out double x) ||
                !TryGetNumber(rect, "y", out double y) ||
                !TryGetNumber(rect, "w", out double width) ||
                !TryGetNumber(rect, "h", out double height) ||
                !TryGetNumber(element, "duration", out double duration))
            {
                error = label + " needs numeric frame.x, frame.y, frame.w, frame.h, and duration.";
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                error = label + " needs positive frame.w and frame.h.";
                return false;
            }

            // The emitter writes TimeSpan.FromMilliseconds(duration), so the frame needs the markup Duration rule.
            if (!IsTimeSpanDurationInRange(duration, TimeSpan.TicksPerMillisecond, allowZero: false))
            {
                error = label + " needs a positive duration (milliseconds) of at least one 100 ns tick, " +
                    "no longer than TimeSpan.MaxValue.";
                return false;
            }

            AsepriteFrame result = new()
            {
                X = x,
                Y = y,
                Width = width,
                Height = height,
                DurationMilliseconds = duration
            };

            if (TryGetProperty(element, "trimmed", out JsonElement trimmed) && trimmed.ValueKind == JsonValueKind.True)
            {
                if (!TryGetProperty(element, "spriteSourceSize", out JsonElement offset) ||
                    !TryGetNumber(offset, "x", out double offsetX) ||
                    !TryGetNumber(offset, "y", out double offsetY) ||
                    !TryGetProperty(element, "sourceSize", out JsonElement size) ||
                    !TryGetNumber(size, "w", out double frameWidth) ||
                    !TryGetNumber(size, "h", out double frameHeight))
                {
                    error = label + " is trimmed and needs numeric spriteSourceSize.x, spriteSourceSize.y, sourceSize.w, and sourceSize.h.";
                    return false;
                }

                if (offsetX < 0 || offsetY < 0 || frameWidth <= 0 || frameHeight <= 0 ||
                    offsetX + width > frameWidth || offsetY + height > frameHeight)
                {
                    error = label + " is trimmed but the frame does not fit inside sourceSize: " +
                        "spriteSourceSize.x + frame.w must not exceed sourceSize.w, " +
                        "and spriteSourceSize.y + frame.h must not exceed sourceSize.h.";
                    return false;
                }

                result.IsTrimmed = true;
                result.OffsetX = offsetX;
                result.OffsetY = offsetY;
                result.FrameWidth = frameWidth;
                result.FrameHeight = frameHeight;
            }

            frame = result;
            return true;
        }

        private static bool TryGetProperty(JsonElement parent, string name, out JsonElement value)
        {
            if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(name, out value))
            {
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryGetNumber(JsonElement parent, string name, out double value)
        {
            value = 0;
            return TryGetProperty(parent, name, out JsonElement element) &&
                element.ValueKind == JsonValueKind.Number &&
                element.TryGetDouble(out value) &&
                !double.IsNaN(value) &&
                !double.IsInfinity(value);
        }

        private static bool TryGetInt(JsonElement parent, string name, out int value)
        {
            value = 0;
            return TryGetProperty(parent, name, out JsonElement element) &&
                element.ValueKind == JsonValueKind.Number &&
                element.TryGetInt32(out value);
        }
    }
}
