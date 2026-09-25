using System.Collections;
using System.Collections.ObjectModel;
using Cerneala.Tetris;
using Cerneala.UI.Controls;

public static class SceneItems2DCollectionCompileRed
{
    public static void Assign(
        SceneItems2D target,
        IEnumerable sequence,
        ObservableCollection<TetrisSpriteModel> sprites)
    {
        target.ItemsSource = sequence;
        target.ItemsSource = sprites;
    }
}
