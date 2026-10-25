namespace Cerneala.UI.Controls;

/// <summary>Describes the other collider in a <see cref="Collider2D.Entered"/> or <see cref="Collider2D.Exited"/> notification.</summary>
public sealed class ColliderContactEventArgs : EventArgs
{
    /// <summary>Creates arguments that name <paramref name="other"/> as the collider on the other side of the contact.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public ColliderContactEventArgs(Collider2D other)
    {
        Other = other ?? throw new ArgumentNullException(nameof(other));
    }

    /// <summary>The collider that started or stopped touching the collider that raised the event.</summary>
    public Collider2D Other { get; }
}
