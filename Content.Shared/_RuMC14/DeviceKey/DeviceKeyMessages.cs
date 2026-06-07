using Robust.Shared.Serialization;

namespace Content.Shared._RuMC14.DeviceKey;

[Serializable, NetSerializable]
public sealed class DeviceKeyChallengeEvent : EntityEventArgs
{
    public byte[] Challenge { get; }

    public DeviceKeyChallengeEvent(byte[] challenge)
    {
        Challenge = challenge;
    }
}

[Serializable, NetSerializable]
public sealed class DeviceKeyProofEvent : EntityEventArgs
{
    public byte[] Challenge { get; }
    public byte[] DeviceId { get; }
    public byte[] Secret { get; }
    public byte[] Proof { get; }

    public DeviceKeyProofEvent(byte[] challenge, byte[] deviceId, byte[] secret, byte[] proof)
    {
        Challenge = challenge;
        DeviceId = deviceId;
        Secret = secret;
        Proof = proof;
    }
}

public static class DeviceKeyProofHelpers
{
    public const int ChallengeLength = 32;
    public const int SecretLength = 32;
    public const int DeviceIdLength = 32;
    public const int ProofLength = 32;
    public const int ChallengeTimeoutSeconds = 15;

    public static byte[] GetDeviceId(byte[] secret)
    {
        var state = CreateHashState(0x44564944u);
        UpdateAscii(state, "RUSSIANCM_DEVICE_ID_V1");
        UpdateBytes(state, secret);
        return FinishHash(state);
    }

    public static byte[] MakeProof(byte[] secret, byte[] challenge, Guid userId)
    {
        var state = CreateHashState(0x50524f46u);
        UpdateAscii(state, "RUSSIANCM_DEVICE_PROOF_V1");
        UpdateBytes(state, secret);
        UpdateBytes(state, challenge);
        UpdateBytes(state, userId.ToByteArray());
        return FinishHash(state);
    }

    private static uint[] CreateHashState(uint seed)
    {
        var state = new uint[8];
        state[0] = 0x811c9dc5u ^ seed;
        state[1] = 0x01000193u + seed;
        state[2] = 0x9e3779b9u ^ seed;
        state[3] = 0x85ebca6bu + seed;
        state[4] = 0xc2b2ae35u ^ seed;
        state[5] = 0x27d4eb2fu + seed;
        state[6] = 0x165667b1u ^ seed;
        state[7] = 0xd3a2646cu + seed;
        return state;
    }

    private static void UpdateAscii(uint[] state, string value)
    {
        for (var i = 0; i < value.Length; i++)
            UpdateByte(state, (byte) value[i]);
    }

    private static void UpdateBytes(uint[] state, byte[] value)
    {
        UpdateByte(state, (byte) value.Length);
        UpdateByte(state, (byte) (value.Length >> 8));

        for (var i = 0; i < value.Length; i++)
            UpdateByte(state, value[i]);
    }

    private static void UpdateByte(uint[] state, byte value)
    {
        for (var i = 0; i < state.Length; i++)
        {
            var mixed = state[i] ^ (uint) (value + i * 17);
            mixed *= 16777619u;
            mixed ^= mixed >> 13;
            mixed = (mixed << 7) | (mixed >> 25);
            state[i] = mixed + 0x9e3779b9u + (uint) i;
        }
    }

    private static byte[] FinishHash(uint[] state)
    {
        var result = new byte[32];

        for (var round = 0; round < 16; round++)
        {
            for (var i = 0; i < state.Length; i++)
                UpdateByte(state, (byte) (round * 31 + i));
        }

        for (var i = 0; i < state.Length; i++)
            WriteUInt32(result, i * 4, state[i]);

        return result;
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte) value;
        data[offset + 1] = (byte) (value >> 8);
        data[offset + 2] = (byte) (value >> 16);
        data[offset + 3] = (byte) (value >> 24);
    }
}
