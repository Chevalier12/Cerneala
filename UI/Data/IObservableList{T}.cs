using System.Collections;

namespace Cerneala.UI.Data;

public interface IObservableList : IEnumerable
{
    event EventHandler<ObservableListChangedEventArgs>? Changed;

    int Count { get; }

    object? this[int index] { get; }
}

public interface IObservableList<T> : IReadOnlyList<T>
{
    event EventHandler<ObservableListChangedEventArgs<T>>? Changed;
}
