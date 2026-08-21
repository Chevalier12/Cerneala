using System.ComponentModel;
using System.Runtime.CompilerServices;
using Cerneala.UI.Input;

namespace Cerneala.MenuLab;

public sealed class MenuLabViewModel : INotifyPropertyChanged
{
    private readonly Queue<string> activity = new();
    private readonly ActionCommand saveCommand;
    private bool documentDirty;
    private int activitySequence;
    private string sessionState = "SESSION IDLE";
    private string saveState = "document=clean / item=disabled";
    private string actionTitle = "Ready";
    private string actionBody = "No command has run yet.";
    private string actionDetail = "Workspace ready";
    private string commandState = "Save waits for a change";
    private string lifecycle = "No submenu transition";
    private string activityText = "01  Window initialized";

    public MenuLabViewModel()
    {
        NewWorkspaceCommand = new ActionCommand(_ =>
        {
            SetDocumentDirty(true);
            RecordAction("New workspace", "The document changed and the shared Save command became available.");
        });
        MarkDirtyCommand = new ActionCommand(_ =>
        {
            SetDocumentDirty(true);
            RecordAction("Document changed", "CanExecuteChanged refreshed both Save entry points.");
        });
        saveCommand = new ActionCommand(_ => SaveWorkspace(), _ => documentDirty);
        SaveCommand = saveCommand;
        ResetCommand = new ActionCommand(_ =>
        {
            SetDocumentDirty(false);
            RecordAction("Reset", "The document returned to a clean state.");
        });
        ExportCommand = new ActionCommand(parameter => RecordParameterizedAction("Exported", parameter));
        OpenRecentCommand = new ActionCommand(parameter => RecordParameterizedAction("Opened recent", parameter));
        NavigateCommand = new ActionCommand(parameter => RecordParameterizedAction("Navigation", parameter));
        SelectViewCommand = new ActionCommand(parameter => RecordParameterizedAction("View selected", parameter));
        AboutCommand = new ActionCommand(_ =>
            RecordAction("Cerneala Menu Lab", "MenuBar, nested MenuItem overlays and commands in one native window."));
        ClearActivityCommand = new ActionCommand(_ => ClearActivity());
        ExitCommand = new ActionCommand(_ => CloseRequested?.Invoke(this, EventArgs.Empty));

        RecordAction("Ready", "MenuBar, vertical Menu and command routing are active.");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? CloseRequested;

    public ICommand NewWorkspaceCommand { get; }

    public ICommand MarkDirtyCommand { get; }

    public ICommand SaveCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand ExportCommand { get; }

    public ICommand OpenRecentCommand { get; }

    public ICommand NavigateCommand { get; }

    public ICommand SelectViewCommand { get; }

    public ICommand AboutCommand { get; }

    public ICommand ClearActivityCommand { get; }

    public ICommand ExitCommand { get; }

    public string SessionState
    {
        get => sessionState;
        private set => SetField(ref sessionState, value);
    }

    public string SaveState
    {
        get => saveState;
        private set => SetField(ref saveState, value);
    }

    public string ActionTitle
    {
        get => actionTitle;
        private set => SetField(ref actionTitle, value);
    }

    public string ActionBody
    {
        get => actionBody;
        private set => SetField(ref actionBody, value);
    }

    public string ActionDetail
    {
        get => actionDetail;
        private set => SetField(ref actionDetail, value);
    }

    public string CommandState
    {
        get => commandState;
        private set => SetField(ref commandState, value);
    }

    public string Lifecycle
    {
        get => lifecycle;
        private set => SetField(ref lifecycle, value);
    }

    public string ActivityText
    {
        get => activityText;
        private set => SetField(ref activityText, value);
    }

    public void UpdateMenuSession(bool isOpen)
    {
        SessionState = isOpen ? "SESSION OPEN" : "SESSION IDLE";
    }

    public void RecordSubmenuOpened(string header)
    {
        Lifecycle = $"opened: {header}";
        AppendActivity($"Opened submenu: {header}");
    }

    public void RecordSubmenuClosed(string header)
    {
        Lifecycle = $"closed: {header}";
        AppendActivity($"Closed submenu: {header}");
    }

    private void SaveWorkspace()
    {
        SetDocumentDirty(false);
        RecordAction("Saved", "The shared command ran once and disabled itself again.");
    }

    private void SetDocumentDirty(bool value)
    {
        documentDirty = value;
        saveCommand.RaiseCanExecuteChanged();
        SaveState = $"document={(documentDirty ? "modified" : "clean")} / item={(documentDirty ? "enabled" : "disabled")}";
        CommandState = documentDirty ? "Save is available" : "Save waits for a change";
    }

    private void RecordParameterizedAction(string action, object? parameter)
    {
        RecordAction(action, parameter?.ToString() ?? "No parameter");
    }

    private void RecordAction(string title, string detail)
    {
        ActionTitle = title;
        ActionBody = detail;
        ActionDetail = detail;
        AppendActivity($"{title}: {detail}");
    }

    private void ClearActivity()
    {
        activity.Clear();
        activitySequence = 0;
        AppendActivity("Activity cleared");
        ActionTitle = "Activity cleared";
        ActionBody = "The menu session remains active.";
        ActionDetail = "Activity cleared";
    }

    private void AppendActivity(string message)
    {
        activity.Enqueue($"{++activitySequence:00}  {message}");
        while (activity.Count > 5)
        {
            activity.Dequeue();
        }

        ActivityText = string.Join(Environment.NewLine, activity);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
