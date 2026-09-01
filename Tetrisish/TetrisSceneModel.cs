using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.Drawing;

namespace Cerneala.Tetris;

public sealed class TetrisSceneModel : INotifyPropertyChanged
{
    private IEnumerable lockedPieces = Array.Empty<TetrisSpriteModel>();
    private IDrawImage? currentImage;
    private DrawRect? currentSource;
    private DrawRect currentDestination;
    private Color currentTint = Color.White;
    private bool currentVisible;
    private IDrawImage? ghostImage;
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

    public IDrawImage? CurrentImage
    {
        get => currentImage;
        private set => Set(ref currentImage, value);
    }

    public DrawRect? CurrentSource
    {
        get => currentSource;
        private set => Set(ref currentSource, value);
    }

    public DrawRect CurrentDestination
    {
        get => currentDestination;
        private set => Set(ref currentDestination, value);
    }

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

    public IDrawImage? GhostImage
    {
        get => ghostImage;
        private set => Set(ref ghostImage, value);
    }

    public DrawRect? GhostSource
    {
        get => ghostSource;
        private set => Set(ref ghostSource, value);
    }

    public DrawRect GhostDestination
    {
        get => ghostDestination;
        private set => Set(ref ghostDestination, value);
    }

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
        CurrentImage = currentImageValue;
        CurrentSource = source;
        CurrentDestination = currentDestinationValue;
        CurrentTint = tint;
        CurrentVisible = visible && currentImageValue is not null;

        GhostImage = ghostImageValue;
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

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
        Source = source;
        SourceRect = sourceRect;
        Destination = destination;
        Tint = tint;
    }

    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add { }
        remove { }
    }

    public IDrawImage Source { get; }

    public DrawRect? SourceRect { get; }

    public DrawRect Destination { get; }

    public Color Tint { get; }
}
