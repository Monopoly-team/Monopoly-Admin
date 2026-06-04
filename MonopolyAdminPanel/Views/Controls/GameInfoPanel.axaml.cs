using Avalonia.Controls;

namespace MonopolyAdminPanel.Views.Controls;

public partial class GameInfoPanel : UserControl
{
    private TextBlock? _totalPlayersText;
    private TextBlock? _bankBalanceText;
    private TextBlock? _totalPurchasesText;
    private TextBlock? _totalFinesText;
    private TextBlock? _totalBonusesText;
    private TextBlock? _totalEventsText;
    private TextBlock? _totalTurnsText;

    public GameInfoPanel()
    {
        InitializeComponent();

        _totalPlayersText = this.FindControl<TextBlock>("TotalPlayersText");
        _bankBalanceText = this.FindControl<TextBlock>("BankBalanceText");
        _totalPurchasesText = this.FindControl<TextBlock>("TotalPurchasesText");
        _totalFinesText = this.FindControl<TextBlock>("TotalFinesText");
        _totalBonusesText = this.FindControl<TextBlock>("TotalBonusesText");
        _totalEventsText = this.FindControl<TextBlock>("TotalEventsText");
        _totalTurnsText = this.FindControl<TextBlock>("TotalTurnsText");
    }

    public void UpdateInfo(
        int totalPlayers,
        int totalPurchases,
        int totalFines,
        int totalBonuses,
        int totalEvents,
        int totalTurns)
    {
        if (_totalPlayersText != null)
            _totalPlayersText.Text = totalPlayers.ToString();

        if (_bankBalanceText != null)
            _bankBalanceText.Text = "∞";

        if (_totalPurchasesText != null)
            _totalPurchasesText.Text = totalPurchases.ToString();

        if (_totalFinesText != null)
            _totalFinesText.Text = totalFines.ToString();

        if (_totalBonusesText != null)
            _totalBonusesText.Text = totalBonuses.ToString();

        if (_totalEventsText != null)
            _totalEventsText.Text = totalEvents.ToString();

        if (_totalTurnsText != null)
            _totalTurnsText.Text = totalTurns.ToString();
    }
}