using System.Collections.Immutable;
using System.Linq;
using Content.Server.Database;
using Content.Shared._RuMC14.DeviceKey;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Localization;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RuMC14.DeviceKey;

public sealed partial class DeviceKeyVerificationSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private ServerDbEntryManager _serverDbEntry = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;

    private readonly Dictionary<ICommonSession, PendingChallenge> _pending = new();
    private readonly Dictionary<ICommonSession, ImmutableArray<byte>> _verifiedDeviceIds = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DeviceKeyProofEvent>(OnDeviceKeyProof);
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        foreach (var (session, pending) in _pending.ToArray())
        {
            if (now < pending.ExpiresAt)
                continue;

            _pending.Remove(session);
            session.Channel.Disconnect("Device key verification timed out.");
        }
    }

    public bool TryGetDeviceId(ICommonSession session, out ImmutableArray<byte> deviceId)
    {
        return _verifiedDeviceIds.TryGetValue(session, out deviceId);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus == SessionStatus.Disconnected)
        {
            _pending.Remove(args.Session);
            _verifiedDeviceIds.Remove(args.Session);
            return;
        }

        if (args.NewStatus != SessionStatus.Connected || args.OldStatus != SessionStatus.Connecting)
            return;

        var challenge = new byte[DeviceKeyProofHelpers.ChallengeLength];
        _random.NextBytes(challenge);
        _pending[args.Session] = new PendingChallenge(
            challenge.ToImmutableArray(),
            _timing.CurTime + TimeSpan.FromSeconds(DeviceKeyProofHelpers.ChallengeTimeoutSeconds));

        RaiseNetworkEvent(new DeviceKeyChallengeEvent(challenge), args.Session);
    }

    private async void OnDeviceKeyProof(DeviceKeyProofEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_pending.TryGetValue(session, out var pending))
        {
            session.Channel.Disconnect("Unexpected device key proof.");
            return;
        }

        if (!BytesEqual(pending.Challenge, msg.Challenge) ||
            !VerifyDeviceKeyProof(session.UserId.UserId, msg.Challenge, msg.DeviceId, msg.Secret, msg.Proof))
        {
            _pending.Remove(session);
            session.Channel.Disconnect("Device key proof is invalid.");
            return;
        }

        _pending.Remove(session);

        var deviceId = msg.DeviceId.ToImmutableArray();
        _verifiedDeviceIds[session] = deviceId;

        var modernDeviceHwid = new ImmutableTypedHwid(deviceId, HwidType.Modern);
        var address = session.Channel.RemoteEndPoint.Address;
        var serverId = (await _serverDbEntry.ServerEntity).Id;
        var bans = await _db.GetServerBansAsync(address, session.UserId, null, [deviceId], includeUnbanned: false);

        if (bans.Count > 0)
        {
            var firstBan = bans[0];
            var connectionId = await _db.AddConnectionLogAsync(
                session.UserId,
                session.Name,
                address,
                modernDeviceHwid,
                session.Channel.UserData.Trust,
                ConnectionDenyReason.Ban,
                serverId);

            await _db.AddServerBanHitsAsync(connectionId, bans);
            session.Channel.Disconnect(firstBan.FormatBanMessage(_cfg, _loc));
            return;
        }

        await _db.AddConnectionLogAsync(
            session.UserId,
            session.Name,
            address,
            modernDeviceHwid,
            session.Channel.UserData.Trust,
            null,
            serverId);

        await _db.UpdatePlayerRecordAsync(session.UserId, session.Name, address, modernDeviceHwid);
    }

    private static bool VerifyDeviceKeyProof(
        Guid userId,
        byte[] challenge,
        byte[] deviceId,
        byte[] secret,
        byte[] proof)
    {
        if (deviceId.Length != DeviceKeyProofHelpers.DeviceIdLength ||
            secret.Length != DeviceKeyProofHelpers.SecretLength ||
            proof.Length != DeviceKeyProofHelpers.ProofLength)
        {
            return false;
        }

        var expectedDeviceId = DeviceKeyProofHelpers.GetDeviceId(secret);
        if (!BytesEqual(deviceId, expectedDeviceId))
            return false;

        var expectedProof = DeviceKeyProofHelpers.MakeProof(secret, challenge, userId);
        return BytesEqual(proof, expectedProof);
    }

    private static bool BytesEqual(ImmutableArray<byte> left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private static bool BytesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
            return false;

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i] != right[i])
                return false;
        }

        return true;
    }

    private readonly record struct PendingChallenge(ImmutableArray<byte> Challenge, TimeSpan ExpiresAt);
}
