// Bluedge
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._Bluedge.BloomLight;

[RegisterComponent, AutoGenerateComponentState]
public sealed partial class BloomLightMaskComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public List<BloomMaskSpecifier> LightMasks = new()
    {
        new() {
            UseShader = true,
            Modulate = Color.White,
            Sprite = new SpriteSpecifier.Texture(new("_Bluedge/BloomLight/Masks/lightmask_lamp_soft.png"))
        }
    };

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool Enabled = true;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool UseLightColor = false;

    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public bool UseShader = true;
}

[DataDefinition, Serializable, NetSerializable]
public partial struct BloomMaskSpecifier
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool UseShader = true;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Unshaded = false;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Modulate = Color.White;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier Sprite;
}
