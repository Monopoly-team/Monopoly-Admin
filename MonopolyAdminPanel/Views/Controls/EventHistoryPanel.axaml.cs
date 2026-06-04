using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace MonopolyAdminPanel.Views.Controls;

public partial class EventHistoryPanel : UserControl
{
    private StackPanel? _eventsPanel;
    private ScrollViewer? _eventsScrollViewer;

    public EventHistoryPanel()
    {
        InitializeComponent();

        _eventsPanel = this.FindControl<StackPanel>("EventsPanel");
        _eventsScrollViewer = this.FindControl<ScrollViewer>("EventsScrollViewer");
    }

    public void AddEvent(string text)
    {
        if (_eventsPanel == null)
            return;

        var textBlock = new TextBlock
        {
            Text = text,
            Foreground = Brushes.LightGray,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap
        };

        bool shouldAutoScroll = true;

        if (_eventsScrollViewer != null)
        {
            double offset = _eventsScrollViewer.Offset.Y;
            double viewportHeight = _eventsScrollViewer.Viewport.Height;
            double extentHeight = _eventsScrollViewer.Extent.Height;

            shouldAutoScroll = offset + viewportHeight >= extentHeight - 40;
        }

        _eventsPanel.Children.Add(textBlock);

        if (shouldAutoScroll)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _eventsScrollViewer?.ScrollToEnd();
            });
        }
    }
}