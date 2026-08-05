using System.Windows;
using System.Windows.Input;
using Senice.App.ViewModels;

namespace Senice.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            e.Effects = DragDropEffects.Copy;
            DropOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        DropOverlay.Visibility = Visibility.Collapsed;

        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        var paths = (string[]?)e.Data.GetData(DataFormats.FileDrop);
        if (paths is { Length: > 0 })
            _viewModel.SetTarget(paths[0]);

        e.Handled = true;
    }

    protected override void OnDragLeave(DragEventArgs e)
    {
        base.OnDragLeave(e);
        DropOverlay.Visibility = Visibility.Collapsed;
    }
}
