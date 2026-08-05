using System.Windows;
using Senice.App.ViewModels;

namespace Senice.App.Views;

public partial class DiffWindow : Window
{
    public DiffWindow(DiffWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
