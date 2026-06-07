using System.IO;
using Content.Shared._RuMC14.DeviceKey;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Client._RuMC14.DeviceKey;

public sealed partial class DeviceKeySystem : EntitySystem
{
    [Dependency] private IResourceManager _resource = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IRobustRandom _random = default!;

    private static readonly ResPath DeviceKeyPath = new("/device_key");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DeviceKeyChallengeEvent>(OnDeviceKeyChallenge);
    }

    private void OnDeviceKeyChallenge(DeviceKeyChallengeEvent msg)
    {
        var secret = LoadOrCreateSecret();
        var deviceId = DeviceKeyProofHelpers.GetDeviceId(secret);
        var proof = DeviceKeyProofHelpers.MakeProof(secret, msg.Challenge, _player.LocalUser!.Value.UserId);

        RaiseNetworkEvent(new DeviceKeyProofEvent(msg.Challenge, deviceId, secret, proof));
    }

    private byte[] LoadOrCreateSecret()
    {
        var secret = TryReadSecret();
        if (secret != null)
            return secret;

        secret = new byte[DeviceKeyProofHelpers.SecretLength];
        _random.NextBytes(secret);
        using (var stream = _resource.UserData.Open(DeviceKeyPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            stream.Write(secret);
        }

        return secret;
    }

    private byte[]? TryReadSecret()
    {
        try
        {
            using var stream = _resource.UserData.Open(DeviceKeyPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var secret = memory.ToArray();
            return secret.Length == DeviceKeyProofHelpers.SecretLength ? secret : null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
