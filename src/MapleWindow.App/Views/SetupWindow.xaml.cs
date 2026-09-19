using System.Windows;
using MapleWindow.App.ViewModels;

namespace MapleWindow.App.Views;

public partial class SetupWindow : Window
{
    public SetupViewModel ViewModel { get; }

    public SetupWindow(SetupViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
        DataContext = ViewModel;
        ViewModel.RequestClose += OnRequestClose;
    }

    private void OnRequestClose(object? sender, bool success)
    {
        DialogResult = success;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        ViewModel.RequestClose -= OnRequestClose;
        base.OnClosed(e);
    }
}
