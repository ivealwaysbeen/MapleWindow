using System.Windows;
using MapleWindow.App.ViewModels;

namespace MapleWindow.App.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closed += (_, _) => viewModel.Dispose();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
