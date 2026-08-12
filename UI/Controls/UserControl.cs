using Cerneala.UI.Rendering;

namespace Cerneala.UI.Controls;

public class UserControl : Control
{
    protected override void OnRender(RenderContext context)
    {
        Border.RenderBox(this, context);
    }
}

public class UserControl<TViewModel> : UserControl
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
