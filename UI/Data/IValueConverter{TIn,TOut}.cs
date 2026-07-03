namespace Cerneala.UI.Data;

public interface IValueConverter<TIn, TOut>
{
    TOut Convert(TIn value);

    TIn ConvertBack(TOut value);
}
