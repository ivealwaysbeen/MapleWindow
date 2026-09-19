using System.Windows;

namespace MapleWindow.App.Views;

public partial class ConfirmUninstallWindow : Window
{
    public ConfirmUninstallWindow()
    {
        InitializeComponent();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnUninstallClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
