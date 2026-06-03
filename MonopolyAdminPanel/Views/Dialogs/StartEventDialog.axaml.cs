using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MonopolyAdminPanel.Views.Dialogs;

public partial class StartEventDialog : Window
{
    private ComboBox? _eventsComboBox;
    private TextBox? _turnsTextBox;
    private TextBlock? _eventTitleText;
    private TextBlock? _eventDescriptionText;

    private readonly List<GameEventItem> _events = new()
    {
        new(1, "Неделя скидок", "Все клетки дешевле на 5%"),
        new(2, "Инвестиционный бум", "Все игроки получают повышенный доход от владений"),
        new(3, "Налоговая проверка", "Игроки с большим количеством клеток платят дополнительный налог"),
        new(4, "Государственная субсидия", "Игроки с низким балансом получают помощь от банка"),
        new(5, "Благотворительный фонд", "Часть средств перераспределяется между игроками")
    };

    public int EventId => SelectedEvent?.Id ?? 0;

    public string EventTitle => SelectedEvent?.Title ?? string.Empty;

    public string EventDescription => SelectedEvent?.Description ?? string.Empty;

    public int? Turns
    {
        get
        {
            string text = _turnsTextBox?.Text?.Trim() ?? string.Empty;

            if (int.TryParse(text, out int turns))
                return turns;

            return null;
        }
    }

    private GameEventItem? SelectedEvent =>
        _eventsComboBox?.SelectedItem as GameEventItem;

    public StartEventDialog()
    {
        InitializeComponent();

        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _eventsComboBox = this.FindControl<ComboBox>("EventsComboBox");
        _turnsTextBox = this.FindControl<TextBox>("TurnsTextBox");
        _eventTitleText = this.FindControl<TextBlock>("EventTitleText");
        _eventDescriptionText = this.FindControl<TextBlock>("EventDescriptionText");

        if (_eventsComboBox != null)
        {
            _eventsComboBox.ItemsSource = _events;
            _eventsComboBox.SelectedIndex = 0;
        }

        UpdateEventDescription();
    }

    private void EventsComboBox_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateEventDescription();
    }

    private void UpdateEventDescription()
    {
        GameEventItem? selectedEvent = SelectedEvent;

        if (selectedEvent == null)
            return;

        if (_eventTitleText != null)
            _eventTitleText.Text = selectedEvent.Title;

        if (_eventDescriptionText != null)
            _eventDescriptionText.Text = selectedEvent.Description;
    }

    private void TitleBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void ConfirmButton_Click(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }

    private sealed class GameEventItem
    {
        public int Id { get; }

        public string Title { get; }

        public string Description { get; }

        public GameEventItem(int id, string title, string description)
        {
            Id = id;
            Title = title;
            Description = description;
        }

        public override string ToString()
        {
            return Title;
        }
    }
}