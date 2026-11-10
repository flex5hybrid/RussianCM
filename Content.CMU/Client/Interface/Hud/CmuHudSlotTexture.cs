using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Actions.Controls;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>
/// Retains TextureRect measurement and input routing while separating native slot glyphs from HUD chrome.
/// Other user-selected HUD themes and action-specific toggle backgrounds keep their original rendering.
/// </summary>
public sealed class CmuHudSlotTexture : TextureRect
{
    private static readonly CmuHudStyleBox Normal = new() { OutlineOnly = true };
    private static readonly CmuHudStyleBox Hovered = new() { Tone = CmuHudTone.Hover, Opacity = 0.15f };
    private static readonly CmuHudStyleBox Selected = new() { Selected = true, Opacity = 0.22f };
    private static readonly CmuHudStyleBox Highlight = new() { Selected = true, OutlineOnly = true };
    private readonly SlotControl? _slot;
    private readonly ActionButton? _action;
    private readonly bool _highlight;
    private Texture? _glyph;
    private bool _knownSlot;
    private string? _glyphPath;
    private bool _glyphResolved;

    public bool UseHudChrome { get; set; } = true;

    public CmuHudSlotTexture(SlotControl slot, bool highlight = false)
    {
        _slot = slot;
        _highlight = highlight;
    }

    public CmuHudSlotTexture(ActionButton action) => _action = action;

    public void SetGlyph(string? path)
    {
        if (_glyphResolved && _glyphPath == path)
            return;
        _glyphResolved = true;
        _glyphPath = path;
        _glyph = null;
        _knownSlot = path == null || !path.StartsWith("Slots/", StringComparison.Ordinal);
        if (path == null)
            return;
        var cache = IoCManager.Resolve<IResourceCache>();
        if (!cache.TryGetResource<TextureResource>("/Textures/CMU14/Interface/Hud/" + path + ".png", out var resource))
            return;
        _glyph = resource.Texture;
        _knownSlot = true;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        if (!Content.Client.Stylesheets.StyleNano.CrtUiEnabled || Theme.ID != "SS14DefaultTheme" || !UseHudChrome || _slot != null && !_highlight && !_knownSlot)
        {
            base.Draw(handle);
            return;
        }

        var box = UIBox2.FromDimensions(Vector2.Zero, PixelSize);
        if (_highlight)
        {
            Highlight.Draw(handle, box, UIScale);
            return;
        }
        if (_action != null && Texture == null)
            return;

        var selected = _slot?.Highlight == true || _action?.Action?.Comp.Toggled == true;
        var hovered = _slot?.MouseIsHovering == true || _action?.HighlightRect.Visible == true;
        (selected ? Selected : hovered ? Hovered : Normal).Draw(handle, box, UIScale);
        if (_slot?.Entity == null && _glyph != null)
            handle.DrawTextureRect(_glyph, box, CmuHudStyle.TextColor);
    }
}
