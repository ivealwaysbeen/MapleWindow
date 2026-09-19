using System.Windows;

namespace MapleWindow.App.Views;

public partial class ConfirmUninstallWindow : Window
{
    public ConfirmUninstallWindow(string installDir)
    {
        InitializeComponent();
        MessageText.Text = $"정말 MapleWindow를 삭제하시겠습니까?\n\n다음 폴더가 완전히 삭제됩니다:\n{installDir}\n\n설정과 캐릭터 데이터도 함께 삭제되며 되돌릴 수 없습니다.";
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OnUninstallClick(object sender, RoutedEventArgs e) => DialogResult = true;
}
