namespace VisualStudioConsumer;

using System.Collections;
using System.ComponentModel;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    public string Title { get; set; } = "Community integration";

    public IEnumerable? Rows { get; } = new DashboardRow[]
    {
        new() { Label = "Language server", IsReady = true },
        new() { Label = "Source generator", IsReady = true }
    };

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }
}

public sealed class DashboardRow : INotifyPropertyChanged
{
    public string Label { get; set; } = string.Empty;

    public bool IsReady { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged
    {
        add { }
        remove { }
    }
}
