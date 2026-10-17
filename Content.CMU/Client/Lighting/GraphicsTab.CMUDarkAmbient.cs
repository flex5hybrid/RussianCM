using Content.Shared.CCVar;

namespace Content.Client.Options.UI.Tabs;

public sealed partial class GraphicsTab
{
    private void InitializeCMUDarkAmbientOptions()
    {
        Control.AddOptionCheckBox(CCVars.CMUDarkAmbientEnabled, CMUDarkAmbientCheckBox);
        Control.AddOptionSlider(
            CCVars.CMUDarkAmbientIntensity,
            CMUDarkAmbientIntensitySlider,
            0,
            100,
            (_, value) => Loc.GetString("cmu-options-dark-ambient-intensity-value", ("value", value)));
    }
}
