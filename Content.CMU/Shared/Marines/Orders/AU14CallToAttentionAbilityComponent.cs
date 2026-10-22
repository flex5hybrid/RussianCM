using Content.Shared.Chat.Prototypes;
using Content.Shared.Examine;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CMU14.Marines.Orders;

/// <summary>
/// Grants the Call to Attention order ability.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AU14CallToAttentionAbilityComponent : Component
{
    /// <summary>
    /// Whether this entity can open the military-order selector.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanCall = true;

    [DataField, AutoNetworkedField]
    public EntProtoId Action = "ActionMarineCallToAttention";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> SaluteEmote = "Salute";

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> AttentionEmote = "AU14SnapToAttention";

    [DataField]
    public List<LocId> SaluteCallouts = new()
    {
        "au14-salute-order-callout-salute",
        "au14-salute-order-callout-salute-sharp",
        "au14-salute-order-callout-present-arms",
    };

    [DataField]
    public List<LocId> AttentionCallouts = new()
    {
        "au14-call-to-attention-callout-room-attention",
        "au14-call-to-attention-callout-room-tenchut",
    };

    [DataField]
    public List<LocId> SilenceCallouts = new()
    {
        "au14-silence-order-callout-silence",
        "au14-silence-order-callout-shhhh",
        "au14-silence-order-callout-ahem",
        "au14-silence-order-callout-lock-it-up",
    };

    /// <summary>
    /// Cooldown in seconds between uses.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    [DataField, AutoNetworkedField]
    public TimeSpan SaluteReadyAt;

    [DataField, AutoNetworkedField]
    public TimeSpan AttentionReadyAt;

    [DataField, AutoNetworkedField]
    public TimeSpan SilenceReadyAt;

    /// <summary>
    /// Maximum sight range of the effect in tiles.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Range = 30f;

    /// <summary>
    /// Higher values are chosen as the officer everyone faces when the order is called.
    /// Zero means this role can call the order but is not considered the officer focus.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int AttentionFocusPriority;

    /// <summary>
    /// Maximum random delay before each nearby humanoid responds.
    /// </summary>
    [DataField]
    public TimeSpan ResponseStagger = TimeSpan.FromSeconds(2.5);

    [DataField]
    public float OfficerNearbyRange = 7f;
}
