using Content.Shared._RMC14.GhostColor;
using Content.Shared.CMU14.Ghost;
using Content.Shared.Ghost.Components;

namespace Content.Server.CMU14.Ghost;

public sealed class CMUGhostColorSystem : EntitySystem
{
    [Dependency] private Content.Server._RMC14.LinkAccount.LinkAccountManager _link = default!;
    private const float GhostAlpha = 0x88 / 255f;

    public override void Initialize()
    {
        SubscribeNetworkEvent<CMUSetGhostColorEvent>(OnSetGhostColor);
    }

    private async void OnSetGhostColor(CMUSetGhostColorEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } ghost || !HasComp<GhostComponent>(ghost))
            return;
        if (_link.GetConnectedPatron(args.SenderSession.UserId) is not { Tier.GhostColor: true } patron)
            return;
        try
        {
            await _link.SaveSponsorSettings(args.SenderSession.UserId,
                (patron.SponsorSettings ?? new Content.Shared.CMU14.Sponsors.CMUSponsorSettings()) with
                {
                    GhostColor = ev.Color?.WithAlpha(GhostAlpha).ToArgb(),
                }, ghostColorOnly: true);
        }
        catch (Exception e)
        {
            Log.Error($"Could not save sponsor ghost color: {e}");
        }
    }
}
