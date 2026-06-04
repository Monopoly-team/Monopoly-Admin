using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using System;

namespace MonopolyAdminPanel.Views.Controls;

public partial class ChatPanel : UserControl
{
    private StackPanel? _messagesPanel;
    private ScrollViewer? _chatScrollViewer;
    private TextBox? _chatMessageTextBox;

    public event Action? SendRequested;

    public string MessageText => _chatMessageTextBox?.Text?.Trim() ?? string.Empty;

    public ChatPanel()
    {
        InitializeComponent();

        _messagesPanel = this.FindControl<StackPanel>("MessagesPanel");
        _chatScrollViewer = this.FindControl<ScrollViewer>("ChatScrollViewer");
        _chatMessageTextBox = this.FindControl<TextBox>("ChatMessageTextBox");
    }

    public void AddMessage(string text)
    {
        if (_messagesPanel == null)
            return;

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.LightGray,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap
        };

        bool shouldAutoScroll = true;

        if (_chatScrollViewer != null)
        {
            double offset = _chatScrollViewer.Offset.Y;
            double viewportHeight = _chatScrollViewer.Viewport.Height;
            double extentHeight = _chatScrollViewer.Extent.Height;

            shouldAutoScroll = offset + viewportHeight >= extentHeight - 40;
        }

        _messagesPanel.Children.Add(textBlock);

        if (shouldAutoScroll)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _chatScrollViewer?.ScrollToEnd();
            });
        }
    }

    public void ClearInput()
    {
        if (_chatMessageTextBox != null)
            _chatMessageTextBox.Text = string.Empty;
    }

    private void ChatMessageTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        e.Handled = true;

        SendRequested?.Invoke();
    }
}