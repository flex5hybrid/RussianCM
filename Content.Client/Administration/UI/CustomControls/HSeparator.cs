using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class HSeparator : Control
{
    public const string StylePropertyColor = "separator-color";
    private static readonly Color SeparatorColor = Color.FromHex("#3D4059");

    private readonly StyleBoxFlat _styleBox;
    private Color _fallbackColor;

    public Color Color
    {
        get => _fallbackColor;
        set
        {
            _fallbackColor = value;
            RefreshColor();
        }
    }

    public HSeparator(Color color)
    {
        _fallbackColor = color;
        _styleBox = new StyleBoxFlat
        {
            BackgroundColor = color,
            ContentMarginBottomOverride = 2, ContentMarginLeftOverride = 2
        };

        AddChild(new PanelContainer { PanelOverride = _styleBox });
    }

    public HSeparator() : this(SeparatorColor) { }

    protected override void StylePropertiesChanged()
    {
        base.StylePropertiesChanged();
        RefreshColor();
    }

    private void RefreshColor()
    {
        if (_styleBox == null)
            return;
        // Preserve authored colours off-theme, while palette previews restyle existing dividers.
        _styleBox.BackgroundColor = TryGetStyleProperty<Color>(StylePropertyColor, out var color)
            ? color
            : _fallbackColor;
    }
}
