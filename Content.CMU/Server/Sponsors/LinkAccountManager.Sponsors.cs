using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Content.Shared.CMU14.Sponsors;
using Robust.Shared.Network;

// ReSharper disable CheckNamespace
namespace Content.Server._RMC14.LinkAccount;

public sealed partial class LinkAccountManager
{
    [Dependency] private IEntityManager _sponsorEntities = default!;
    private readonly HashSet<NetUserId> _publicPatrons = [];
    private readonly Dictionary<NetUserId, TimeSpan> _sponsorLastSave = [];
    private readonly SemaphoreSlim[] _sponsorWriteLocks = Enumerable.Range(0, 64).Select(_ => new SemaphoreSlim(1, 1)).ToArray();

    public static CMUSponsorSettings DeserializeSponsorSettings(string? json, int? legacyColor = null)
    {
        if (json == null)
            return new CMUSponsorSettings(GhostColor: legacyColor);
        try
        {
            return JsonSerializer.Deserialize<CMUSponsorSettings>(json) ?? new CMUSponsorSettings();
        }
        catch (JsonException)
        {
            return new CMUSponsorSettings();
        }
    }

    private List<Content.Shared._RMC14.LinkAccount.SharedRMCPatron> PublicPatrons() =>
        _allPatrons.Where(p => _publicPatrons.Contains(p.Key)).Select(p => p.Value).ToList();

    private string? GetSponsorFigurine(NetUserId user) =>
        _sponsorEntities.System<Content.Server._RMC14.Figurines.FigurineSystem>().GetSponsorFigurine(user.UserId);

    public async Task<bool> SaveSponsorSettings(NetUserId user, CMUSponsorSettings requested, bool ghostColorOnly = false)
    {
        var now = _timing.RealTime;
        if (_sponsorLastSave.TryGetValue(user, out var last) && now - last < TimeSpan.FromSeconds(0.5))
            return false;
        _sponsorLastSave[user] = now;
        var guard = _sponsorWriteLocks[(uint)user.GetHashCode() % (uint)_sponsorWriteLocks.Length];
        await guard.WaitAsync();
        try
        {
            if (GetConnectedPatron(user) is not { } patron)
                return false;
            var current = patron.SponsorSettings ?? new CMUSponsorSettings();
            if (ghostColorOnly)
                requested = current with { GhostColor = requested.GhostColor };
            if (!CMUSponsorCatalog.TryValidate(patron.Tier, current, requested, out var settings))
                return false;

            await _db.SetCMUSponsorSettings(user.UserId, JsonSerializer.Serialize(settings));
            // Persistence completed before acknowledging or applying the choice.
            Robust.Shared.Maths.Color? color = settings.GhostColor is { } argb
                ? new Robust.Shared.Maths.Color((byte)(argb >> 16), (byte)(argb >> 8), (byte)argb, (byte)(argb >> 24)) : null;
            OnPatronUpdated(user, p => p with { SponsorSettings = settings, GhostColor = color });
            if (settings.PublicRecognition && patron.Tier is { ShowOnCredits: true })
                _publicPatrons.Add(user);
            else
                _publicPatrons.Remove(user);
            SendPatronsToAll();
            return true;
        }
        finally
        {
            guard.Release();
        }
    }

    private async void SaveSponsorGhostColor(NetUserId user, Robust.Shared.Maths.Color? color)
    {
        try
        {
            if (GetConnectedPatron(user) is not { Tier.GhostColor: true } patron)
                return;
            await SaveSponsorSettings(user, (patron.SponsorSettings ?? new CMUSponsorSettings()) with
            {
                GhostColor = color?.WithAlpha(0x88 / 255f).ToArgb(),
            }, ghostColorOnly: true);
        }
        catch (Exception e)
        {
            SendPatronStatus(user);
            IoCManager.Resolve<Robust.Shared.Log.ILogManager>().GetSawmill("cmu.sponsors").Error($"Could not save ghost color: {e}");
        }
    }
}
