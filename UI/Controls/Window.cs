using Cerneala.UI.Core;
using Cerneala.UI.Hosting;
using Cerneala.UI.Hosting.Windows;
using Cerneala.UI.Markup;

namespace Cerneala.UI.Controls;

public class Window : ContentControl
{
    public static readonly UiProperty<string> TitleProperty = UiProperty<string>.Register(
        nameof(Title),
        typeof(Window),
        new UiPropertyMetadata<string>(string.Empty, UiPropertyOptions.None, validateValue: value => value is not null));

    public static readonly UiProperty<float> WidthProperty = UiProperty<float>.Register(
        nameof(Width),
        typeof(Window),
        new UiPropertyMetadata<float>(800, UiPropertyOptions.AffectsMeasure, validateValue: IsValidPositiveDimension));

    public static readonly UiProperty<float> HeightProperty = UiProperty<float>.Register(
        nameof(Height),
        typeof(Window),
        new UiPropertyMetadata<float>(600, UiPropertyOptions.AffectsMeasure, validateValue: IsValidPositiveDimension));

    public static readonly UiProperty<float> MinWidthProperty = UiProperty<float>.Register(
        nameof(MinWidth),
        typeof(Window),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsMeasure, validateValue: IsValidMinimumDimension));

    public static readonly UiProperty<float> MinHeightProperty = UiProperty<float>.Register(
        nameof(MinHeight),
        typeof(Window),
        new UiPropertyMetadata<float>(0, UiPropertyOptions.AffectsMeasure, validateValue: IsValidMinimumDimension));

    public static readonly UiProperty<float> MaxWidthProperty = UiProperty<float>.Register(
        nameof(MaxWidth),
        typeof(Window),
        new UiPropertyMetadata<float>(float.PositiveInfinity, UiPropertyOptions.AffectsMeasure, validateValue: IsValidMaximumDimension));

    public static readonly UiProperty<float> MaxHeightProperty = UiProperty<float>.Register(
        nameof(MaxHeight),
        typeof(Window),
        new UiPropertyMetadata<float>(float.PositiveInfinity, UiPropertyOptions.AffectsMeasure, validateValue: IsValidMaximumDimension));

    public static readonly UiProperty<float> LeftProperty = UiProperty<float>.Register(
        nameof(Left),
        typeof(Window),
        new UiPropertyMetadata<float>(float.NaN, UiPropertyOptions.None, validateValue: IsValidPosition));

    public static readonly UiProperty<float> TopProperty = UiProperty<float>.Register(
        nameof(Top),
        typeof(Window),
        new UiPropertyMetadata<float>(float.NaN, UiPropertyOptions.None, validateValue: IsValidPosition));

    public static readonly UiProperty<WindowState> WindowStateProperty = UiProperty<WindowState>.Register(
        nameof(WindowState),
        typeof(Window),
        new UiPropertyMetadata<WindowState>(WindowState.Normal, UiPropertyOptions.None, validateValue: Enum.IsDefined));

    public static readonly UiProperty<ResizeMode> ResizeModeProperty = UiProperty<ResizeMode>.Register(
        nameof(ResizeMode),
        typeof(Window),
        new UiPropertyMetadata<ResizeMode>(ResizeMode.CanResize, UiPropertyOptions.None, validateValue: Enum.IsDefined));

    public static readonly UiProperty<WindowStartupLocation> WindowStartupLocationProperty = UiProperty<WindowStartupLocation>.Register(
        nameof(WindowStartupLocation),
        typeof(Window),
        new UiPropertyMetadata<WindowStartupLocation>(WindowStartupLocation.CenterScreen, UiPropertyOptions.None, validateValue: Enum.IsDefined));

    public static readonly UiProperty<bool> TopmostProperty = UiProperty<bool>.Register(
        nameof(Topmost),
        typeof(Window),
        new UiPropertyMetadata<bool>(false, UiPropertyOptions.None));

    public static readonly UiProperty<bool> ShowInTaskbarProperty = UiProperty<bool>.Register(
        nameof(ShowInTaskbar),
        typeof(Window),
        new UiPropertyMetadata<bool>(true, UiPropertyOptions.None));

    private readonly List<Window> ownedWindows = [];
    private Window? owner;
    private bool? dialogResult;
    private TaskCompletionSource<bool?>? dialogCompletion;
    private WindowApplicationRuntime? runtimeOwner;
    private UiProperty? platformOriginatedProperty;

    public event EventHandler? SourceInitialized;

    public event EventHandler? Activated;

    public event EventHandler? Deactivated;

    public event EventHandler<WindowClosingEventArgs>? Closing;

    public event EventHandler? Closed;

    public event EventHandler? StateChanged;

    public event EventHandler? LocationChanged;

    public event EventHandler? ContentRendered;

    public event EventHandler? FrameRendered;

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public float Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public float Height
    {
        get => GetValue(HeightProperty);
        set => SetValue(HeightProperty, value);
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MinWidth
    {
        get => GetValue(MinWidthProperty);
        set
        {
            if (value > MaxWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MinWidth cannot exceed MaxWidth.");
            }

            SetValue(MinWidthProperty, value);
        }
    }

    [MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
    public float MinHeight
    {
        get => GetValue(MinHeightProperty);
        set
        {
            if (value > MaxHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MinHeight cannot exceed MaxHeight.");
            }

            SetValue(MinHeightProperty, value);
        }
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public float MaxWidth
    {
        get => GetValue(MaxWidthProperty);
        set
        {
            if (value < MinWidth)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxWidth cannot be below MinWidth.");
            }

            SetValue(MaxWidthProperty, value);
        }
    }

    [MarkupValueConstraint(MarkupValueConstraint.Positive)]
    public float MaxHeight
    {
        get => GetValue(MaxHeightProperty);
        set
        {
            if (value < MinHeight)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "MaxHeight cannot be below MinHeight.");
            }

            SetValue(MaxHeightProperty, value);
        }
    }

    public float Left
    {
        get => GetValue(LeftProperty);
        set => SetValue(LeftProperty, value);
    }

    public float Top
    {
        get => GetValue(TopProperty);
        set => SetValue(TopProperty, value);
    }

    public WindowState WindowState
    {
        get => GetValue(WindowStateProperty);
        set => SetValue(WindowStateProperty, value);
    }

    public ResizeMode ResizeMode
    {
        get => GetValue(ResizeModeProperty);
        set => SetValue(ResizeModeProperty, value);
    }

    public WindowStartupLocation WindowStartupLocation
    {
        get => GetValue(WindowStartupLocationProperty);
        set => SetValue(WindowStartupLocationProperty, value);
    }

    public bool Topmost
    {
        get => GetValue(TopmostProperty);
        set => SetValue(TopmostProperty, value);
    }

    public bool ShowInTaskbar
    {
        get => GetValue(ShowInTaskbarProperty);
        set => SetValue(ShowInTaskbarProperty, value);
    }

    public Window? Owner
    {
        get => owner;
        set
        {
            if (runtimeOwner is WindowApplicationRuntime runtime)
            {
                runtime.SetOwner(this, value);
                return;
            }

            ValidateOwner(value);
            SetOwnerCore(value);
        }
    }

    public IReadOnlyList<Window> OwnedWindows => ownedWindows;

    public bool? DialogResult
    {
        get => dialogResult;
        set
        {
            if (dialogCompletion is null)
            {
                throw new InvalidOperationException("DialogResult can be set only while the window is shown with ShowDialogAsync().");
            }

            bool? previous = dialogResult;
            dialogResult = value;
            if (!(runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Close(this, force: false))
            {
                dialogResult = previous;
            }
        }
    }

    public bool IsActive { get; private set; }

    public bool IsShown { get; private set; }

    public bool IsClosed { get; private set; }

    public UiFrame? LastFrame { get; private set; }

    public void Show()
    {
        (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Show(this, modal: false);
    }

    public void Hide()
    {
        (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Hide(this);
    }

    public void Activate()
    {
        (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Activate(this);
    }

    public void SaveScreenshot(string path)
    {
        (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).SaveScreenshot(this, path);
    }

    public void Close()
    {
        (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Close(this, force: false);
    }

    public Task<bool?> ShowDialogAsync()
    {
        if (dialogCompletion is not null)
        {
            throw new InvalidOperationException("The window is already being shown as a dialog.");
        }

        dialogResult = null;
        dialogCompletion = new TaskCompletionSource<bool?>(TaskCreationOptions.RunContinuationsAsynchronously);
        try
        {
            (runtimeOwner ?? WindowApplicationRuntime.CurrentOrDefault).Show(this, modal: true);
            return dialogCompletion.Task;
        }
        catch
        {
            dialogCompletion = null;
            throw;
        }
    }

    protected override void OnPropertyChanged(UiPropertyChangedEventArgs args)
    {
        bool originatedFromPlatform = ReferenceEquals(platformOriginatedProperty, args.Property);
        if (originatedFromPlatform)
        {
            platformOriginatedProperty = null;
        }

        base.OnPropertyChanged(args);
        if (ReferenceEquals(args.Property, WindowStateProperty))
        {
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (ReferenceEquals(args.Property, LeftProperty) || ReferenceEquals(args.Property, TopProperty))
        {
            LocationChanged?.Invoke(this, EventArgs.Empty);
        }

        if (!originatedFromPlatform && IsNativeWindowProperty(args.Property))
        {
            runtimeOwner?.ApplyProperties(this);
        }
    }

    internal bool RaiseClosing()
    {
        WindowClosingEventArgs args = new();
        Closing?.Invoke(this, args);
        return !args.Cancel;
    }

    internal void SetOwnerCore(Window? value)
    {
        if (ReferenceEquals(owner, value))
        {
            return;
        }

        owner?.ownedWindows.Remove(this);
        owner = value;
        owner?.ownedWindows.Add(this);
    }

    internal void SetRuntimeOwner(WindowApplicationRuntime? value)
    {
        runtimeOwner = value;
    }

    internal void SetShown(bool value)
    {
        IsShown = value;
    }

    internal void SetActive(bool value)
    {
        if (IsActive == value)
        {
            return;
        }

        IsActive = value;
        if (value)
        {
            Activated?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Deactivated?.Invoke(this, EventArgs.Empty);
        }
    }

    internal void SetPlatformBounds(float left, float top, WindowState state)
    {
        SetPlatformValue(LeftProperty, left);
        SetPlatformValue(TopProperty, top);
        SetPlatformValue(WindowStateProperty, state);
    }

    internal void MarkSourceInitialized()
    {
        SourceInitialized?.Invoke(this, EventArgs.Empty);
    }

    internal void MarkContentRendered()
    {
        ContentRendered?.Invoke(this, EventArgs.Empty);
    }

    internal void MarkFrameRendered(UiFrame frame)
    {
        LastFrame = frame ?? throw new ArgumentNullException(nameof(frame));
        FrameRendered?.Invoke(this, EventArgs.Empty);
    }

    internal void MarkClosed()
    {
        IsShown = false;
        IsActive = false;
        IsClosed = true;
        Closed?.Invoke(this, EventArgs.Empty);
        TaskCompletionSource<bool?>? completion = dialogCompletion;
        dialogCompletion = null;
        completion?.TrySetResult(dialogResult);
    }

    private static bool IsValidPositiveDimension(float value)
    {
        return float.IsFinite(value) && value > 0;
    }

    private static bool IsValidMinimumDimension(float value)
    {
        return float.IsFinite(value) && value >= 0;
    }

    private static bool IsValidMaximumDimension(float value)
    {
        return (float.IsFinite(value) && value > 0) || float.IsPositiveInfinity(value);
    }

    private static bool IsValidPosition(float value)
    {
        return float.IsFinite(value) || float.IsNaN(value);
    }

    private static bool IsNativeWindowProperty(UiProperty property)
    {
        return ReferenceEquals(property, TitleProperty) ||
            ReferenceEquals(property, WidthProperty) ||
            ReferenceEquals(property, HeightProperty) ||
            ReferenceEquals(property, MinWidthProperty) ||
            ReferenceEquals(property, MinHeightProperty) ||
            ReferenceEquals(property, MaxWidthProperty) ||
            ReferenceEquals(property, MaxHeightProperty) ||
            ReferenceEquals(property, LeftProperty) ||
            ReferenceEquals(property, TopProperty) ||
            ReferenceEquals(property, WindowStateProperty) ||
            ReferenceEquals(property, ResizeModeProperty) ||
            ReferenceEquals(property, WindowStartupLocationProperty) ||
            ReferenceEquals(property, TopmostProperty) ||
            ReferenceEquals(property, ShowInTaskbarProperty);
    }

    private void SetPlatformValue<T>(UiProperty<T> property, T value)
    {
        UiProperty? previousOrigin = platformOriginatedProperty;
        platformOriginatedProperty = property;
        try
        {
            SetValue(property, value);
        }
        finally
        {
            platformOriginatedProperty = previousOrigin;
        }
    }

    private void ValidateOwner(Window? value)
    {
        if (ReferenceEquals(this, value))
        {
            throw new InvalidOperationException("A Window cannot own itself.");
        }

        for (Window? current = value; current is not null; current = current.Owner)
        {
            if (ReferenceEquals(current, this))
            {
                throw new InvalidOperationException("Window ownership cannot contain a cycle.");
            }
        }

        if (value?.IsClosed == true)
        {
            throw new InvalidOperationException("A closed Window cannot own another Window.");
        }
    }
}

public class Window<TViewModel> : Window
    where TViewModel : class
{
    protected TViewModel ViewModel
    {
        get
        {
            if (DataContext is TViewModel viewModel)
            {
                return viewModel;
            }

            string actualType = DataContext?.GetType().FullName ?? "null";
            throw new InvalidOperationException(
                $"{GetType().FullName} requires a DataContext assignable to {typeof(TViewModel).FullName}, but the current value is {actualType}.");
        }
    }
}
