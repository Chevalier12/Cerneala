using System.ComponentModel;
using Cerneala.UI.Input;

namespace Cerneala.InvestorReel;

/// <summary>The four versions of the film. Each tells the same twelve chapters in its own visual language.</summary>
public enum ReelTheme
{
    Ink,
    Cyberpunk,
    Brutalist,
    Sakura,
}

/// <summary>
/// The shell's commands. The theme picker and the last chapter of every theme bind to them; the
/// window owns navigation and reacts to the requests.
/// </summary>
public sealed class ShellViewModel : INotifyPropertyChanged
{
    public ShellViewModel()
    {
        PlayCommand = new ActionCommand(parameter =>
        {
            if (parameter is string name && Enum.TryParse(name, ignoreCase: true, out ReelTheme theme))
            {
                PlayRequested?.Invoke(this, theme);
            }
        });
        HomeCommand = new ActionCommand(_ => HomeRequested?.Invoke(this, EventArgs.Empty));
        ExitCommand = new ActionCommand(_ => ExitRequested?.Invoke(this, EventArgs.Empty));
    }

    // Nothing here changes after construction; the interface makes the command paths observable bindings.
    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }

    public event EventHandler<ReelTheme>? PlayRequested;

    public event EventHandler? HomeRequested;

    public event EventHandler? ExitRequested;

    /// <summary>Starts the film in the theme named by the command parameter.</summary>
    public ICommand PlayCommand { get; }

    /// <summary>Returns to the theme picker.</summary>
    public ICommand HomeCommand { get; }

    /// <summary>Closes the application.</summary>
    public ICommand ExitCommand { get; }
}
