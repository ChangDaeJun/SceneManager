using System.Windows;

namespace SceneManager.Views;

/// <summary>
/// 한국어/영어 두 버전의 프롬프트를 토글하며 보여주고 클립보드로 복사하게 하는 재사용 다이얼로그.
/// </summary>
public partial class PromptDialog : Window
{
    private readonly string _korean;
    private readonly string _english;

    public PromptDialog(string title, string korean, string english)
    {
        InitializeComponent();
        Title = title;
        _korean = korean;
        _english = english;
        PromptBox.Text = _korean;
    }

    private void OnLangChanged(object sender, RoutedEventArgs e)
    {
        // Checked는 InitializeComponent 중에도 발생할 수 있어 null 가드.
        if (PromptBox is null)
            return;

        PromptBox.Text = KorRadio.IsChecked == true ? _korean : _english;
        CopiedNote.Visibility = Visibility.Collapsed;
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(PromptBox.Text);
            CopiedNote.Visibility = Visibility.Visible;
        }
        catch
        {
            // 다른 앱이 클립보드를 점유 중이면 실패할 수 있다. 텍스트는 선택해 수동 복사 가능.
            PromptBox.SelectAll();
            PromptBox.Focus();
        }
    }
}
