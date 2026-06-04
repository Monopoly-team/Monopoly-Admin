using Avalonia.Controls;
using Avalonia.Media;

namespace MonopolyAdminPanel.Views.Controls;

public partial class GameStatePanel : UserControl
{
    private TextBlock? _gameStatusText;
    private TextBlock? _currentPlayerText;
    private TextBlock? _gameTimeText;

    public GameStatePanel()
    {
        InitializeComponent();

        _gameStatusText = this.FindControl<TextBlock>("GameStatusText");
        _currentPlayerText = this.FindControl<TextBlock>("CurrentPlayerText");
        _gameTimeText = this.FindControl<TextBlock>("GameTimeText");
    }

    public void SetStatus(string text, IBrush foreground)
    {
        if (_gameStatusText == null)
            return;

        _gameStatusText.Text = text;
        _gameStatusText.Foreground = foreground;
    }

    public void SetCurrentPlayer(string text)
    {
        if (_currentPlayerText != null)
            _currentPlayerText.Text = text;
    }

    public void SetTime(string text)
    {
        if (_gameTimeText != null)
            _gameTimeText.Text = text;
    }
}