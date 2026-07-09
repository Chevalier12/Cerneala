namespace Cerneala.UI.Motion.Interpolation;

/// <summary>
/// Component form used for transform interpolation. Compose honors both skew axes,
/// while Decompose returns an equivalent canonical form with SkewY set to zero.
/// </summary>
public readonly record struct TransformComponents(
    float TranslationX,
    float TranslationY,
    float ScaleX,
    float ScaleY,
    float RotationRadians,
    float SkewX,
    float SkewY);
