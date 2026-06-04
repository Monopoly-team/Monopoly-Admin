using Avalonia.Controls;

namespace MonopolyAdminPanel.Views.Controls;

public partial class DicePanel : UserControl
{
    private Control? _firstDiceSvg;
    private Control? _secondDiceSvg;
    private TextBlock? _lastDiceRollText;

    public DicePanel()
    {
        InitializeComponent();

        _firstDiceSvg = this.FindControl<Control>("FirstDiceSvg");
        _secondDiceSvg = this.FindControl<Control>("SecondDiceSvg");
        _lastDiceRollText = this.FindControl<TextBlock>("LastDiceRollText");
    }

    public void SetDice(int first, int second)
    {
        SetSvgPath(_firstDiceSvg, $"/Assets/dice{first}.svg");
        SetSvgPath(_secondDiceSvg, $"/Assets/dice{second}.svg");

        if (_lastDiceRollText != null)
            _lastDiceRollText.Text = $"Последний бросок: {first + second} ({first} + {second})";
    }

    private static void SetSvgPath(Control? svg, string path)
    {
        if (svg == null)
            return;

        var property = svg.GetType().GetProperty("Path");

        property?.SetValue(svg, path);
    }
}