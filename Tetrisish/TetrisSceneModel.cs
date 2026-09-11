using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;
using Cerneala.UI.Resources;

namespace Cerneala.Tetris;

public sealed class TetrisSceneModel : INotifyPropertyChanged
{
    private IEnumerable lockedPieces = Array.Empty<TetrisSpriteModel>();
    private ImageReference? currentImage;
    private DrawRect? currentSource;
    private DrawRect currentDestination;
    private Color currentTint = Color.White;
    private bool currentVisible;
    private ImageReference? ghostImage;
    private DrawRect? ghostSource;
    private DrawRect ghostDestination;
    private Color ghostTint = Color.White;
    private bool ghostVisible;

    public event PropertyChangedEventHandler? PropertyChanged;

    public DrawRect? ViewBox { get; } = new DrawRect(0, 0, 10, 20);

    public IEnumerable LockedPieces
    {
        get => lockedPieces;
        private set => Set(ref lockedPieces, value);
    }

    public ImageReference? CurrentImage
    {
        get => currentImage;
        private set => Set(ref currentImage, value);
    }

    public DrawRect? CurrentSource
    {
        get => currentSource;
        private set
        {
            if (!Set(ref currentSource, value)) return;
            Notify(nameof(CurrentSourceX));
            Notify(nameof(CurrentSourceY));
            Notify(nameof(CurrentSourceWidth));
            Notify(nameof(CurrentSourceHeight));
        }
    }

    public float CurrentSourceX => currentSource?.X ?? 0;
    public float CurrentSourceY => currentSource?.Y ?? 0;
    public float CurrentSourceWidth => currentSource?.Width ?? float.NaN;
    public float CurrentSourceHeight => currentSource?.Height ?? float.NaN;

    public DrawRect CurrentDestination
    {
        get => currentDestination;
        private set
        {
            if (!Set(ref currentDestination, value)) return;
            Notify(nameof(CurrentX));
            Notify(nameof(CurrentY));
            Notify(nameof(CurrentWidth));
            Notify(nameof(CurrentHeight));
        }
    }

    public float CurrentX => currentDestination.X;
    public float CurrentY => currentDestination.Y;
    public float CurrentWidth => currentDestination.Width;
    public float CurrentHeight => currentDestination.Height;

    public Color CurrentTint
    {
        get => currentTint;
        private set => Set(ref currentTint, value);
    }

    public bool CurrentVisible
    {
        get => currentVisible;
        private set => Set(ref currentVisible, value);
    }

    public ImageReference? GhostImage
    {
        get => ghostImage;
        private set => Set(ref ghostImage, value);
    }

    public DrawRect? GhostSource
    {
        get => ghostSource;
        private set
        {
            if (!Set(ref ghostSource, value)) return;
            Notify(nameof(GhostSourceX));
            Notify(nameof(GhostSourceY));
            Notify(nameof(GhostSourceWidth));
            Notify(nameof(GhostSourceHeight));
        }
    }

    public float GhostSourceX => ghostSource?.X ?? 0;
    public float GhostSourceY => ghostSource?.Y ?? 0;
    public float GhostSourceWidth => ghostSource?.Width ?? float.NaN;
    public float GhostSourceHeight => ghostSource?.Height ?? float.NaN;

    public DrawRect GhostDestination
    {
        get => ghostDestination;
        private set
        {
            if (!Set(ref ghostDestination, value)) return;
            Notify(nameof(GhostX));
            Notify(nameof(GhostY));
            Notify(nameof(GhostWidth));
            Notify(nameof(GhostHeight));
        }
    }

    public float GhostX => ghostDestination.X;
    public float GhostY => ghostDestination.Y;
    public float GhostWidth => ghostDestination.Width;
    public float GhostHeight => ghostDestination.Height;

    public Color GhostTint
    {
        get => ghostTint;
        private set => Set(ref ghostTint, value);
    }

    public bool GhostVisible
    {
        get => ghostVisible;
        private set => Set(ref ghostVisible, value);
    }

    internal void UpdateLockedPieces(IEnumerable<TetrisSpriteModel> pieces)
    {
        LockedPieces = pieces.ToArray();
    }

    internal void UpdateActivePiece(
        IDrawImage? currentImageValue,
        IDrawImage? ghostImageValue,
        DrawRect? source,
        DrawRect currentDestinationValue,
        Color tint,
        DrawRect ghostDestinationValue,
        bool visible)
    {
        CurrentImage = ReferenceImage(currentImageValue, CurrentImage);
        CurrentSource = source;
        CurrentDestination = currentDestinationValue;
        CurrentTint = tint;
        CurrentVisible = visible && currentImageValue is not null;

        GhostImage = ReferenceImage(ghostImageValue, GhostImage);
        GhostSource = source;
        GhostDestination = ghostDestinationValue;
        GhostTint = new Color(tint.R, tint.G, tint.B, 55);
        GhostVisible = visible && ghostImageValue is not null;
    }

    internal void Reset()
    {
        LockedPieces = Array.Empty<TetrisSpriteModel>();
        CurrentImage = null;
        CurrentVisible = false;
        GhostImage = null;
        GhostVisible = false;
    }

    private static ImageReference? ReferenceImage(IDrawImage? image, ImageReference? previous) =>
        image is null ? null : ReferenceEquals(image, previous?.DirectImage) ? previous : new(image);

    private void Notify(string? propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Notify(propertyName);
        return true;
    }
}

public sealed class TetrisSpriteModel : INotifyPropertyChanged
{
    public TetrisSpriteModel(
        IDrawImage source,
        DrawRect? sourceRect,
        DrawRect destination,
        Color tint)
    {
        Image = new(source);
        SourceRect = sourceRect;
        Destination = destination;
        Tint = tint;
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add { }
        remove { }
    }

    public ImageReference Image { get; }

    public DrawRect? SourceRect { get; }

    public float SourceX => SourceRect?.X ?? 0;
    public float SourceY => SourceRect?.Y ?? 0;
    public float SourceWidth => SourceRect?.Width ?? float.NaN;
    public float SourceHeight => SourceRect?.Height ?? float.NaN;

    public DrawRect Destination { get; }

    public float X => Destination.X;
    public float Y => Destination.Y;
    public float Width => Destination.Width;
    public float Height => Destination.Height;

    public Color Tint { get; }
}
