namespace Cerneala.UI.Motion.Interpolation;

public interface IValueMixer
{
    Type ValueType { get; }

    bool SupportsVectorOperations { get; }

    object? MixUntyped(object? from, object? to, float progress);

    bool EqualsWithinToleranceUntyped(object? left, object? right, float tolerance);

    object? AddUntyped(object? left, object? right);

    object? SubtractUntyped(object? left, object? right);

    object? ScaleUntyped(object? value, float scalar);

    float MagnitudeUntyped(object? value);
}
