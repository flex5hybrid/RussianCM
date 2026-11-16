using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.CMU14.Cassette;

public static class CMUDynamicCassetteLimits
{
    public const int MaxTracksPerCassette = 3;
    public const int MaxTrackBytes = 3 * 1024 * 1024;
    public const int MaxUploadChunkBytes = 64 * 1024;
    public const int MaxTitleBytes = 128;
    public const int MaxRoundBytes = 128 * 1024 * 1024;
    public const int MaxUserBytesPerRound = 24 * 1024 * 1024;
    public const int MaxTracksPerRound = 64;
    public const int MaxDurationSeconds = 10 * 60;
    public const uint MaxSampleRate = 48000;
    public static readonly TimeSpan UploadPermitLifetime = TimeSpan.FromSeconds(30);
    public static readonly ResPath ContentPrefix = new("/Audio/CMU14/DynamicCassettes");

    public static bool HasOggContainerHeader(ReadOnlySpan<byte> data) =>
        data.Length >= 4 && data[0] == (byte) 'O' && data[1] == (byte) 'g' &&
        data[2] == (byte) 'g' && data[3] == (byte) 'S';

    public static bool IsTitleValid(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return false;

        var utf8Length = 0;
        foreach (var character in title)
        {
            if (char.GetUnicodeCategory(character) is
                UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.Surrogate)
                return false;

            utf8Length += character switch
            {
                <= '\u007F' => 1,
                <= '\u07FF' => 2,
                _ => 3,
            };

            if (utf8Length > MaxTitleBytes)
                return false;
        }

        return true;
    }

    internal static bool IsValidUtf8(ReadOnlySpan<byte> data)
    {
        var index = 0;
        while (index < data.Length)
        {
            var first = data[index++];
            if (first <= 0x7F)
                continue;

            if (first is >= 0xC2 and <= 0xDF)
            {
                if (index >= data.Length || !IsUtf8Continuation(data[index++]))
                    return false;
                continue;
            }

            if (first is >= 0xE0 and <= 0xEF)
            {
                if (index + 1 >= data.Length)
                    return false;

                var second = data[index++];
                var third = data[index++];
                if (!IsUtf8Continuation(third) ||
                    first == 0xE0 && second is not (>= 0xA0 and <= 0xBF) ||
                    first == 0xED && second is not (>= 0x80 and <= 0x9F) ||
                    first is not (0xE0 or 0xED) && !IsUtf8Continuation(second))
                {
                    return false;
                }
                continue;
            }

            if (first is >= 0xF0 and <= 0xF4)
            {
                if (index + 2 >= data.Length)
                    return false;

                var second = data[index++];
                var third = data[index++];
                var fourth = data[index++];
                if (!IsUtf8Continuation(third) ||
                    !IsUtf8Continuation(fourth) ||
                    first == 0xF0 && second is not (>= 0x90 and <= 0xBF) ||
                    first == 0xF4 && second is not (>= 0x80 and <= 0x8F) ||
                    first is not (0xF0 or 0xF4) && !IsUtf8Continuation(second))
                {
                    return false;
                }
                continue;
            }

            return false;
        }

        return true;
    }

    private static bool IsUtf8Continuation(byte value) => value is >= 0x80 and <= 0xBF;

    public static bool IsValidOggVorbis(ReadOnlySpan<byte> data)
    {
        if (data.Length < 58 || data.Length > MaxTrackBytes)
            return false;

        var offset = 0;
        var pageCount = 0;
        var sampleRate = 0u;
        ulong finalGranule = 0;
        var foundEnd = false;
        var packetIndex = 0;
        var packetContinues = false;
        var foundAudioPacket = false;
        uint? streamSerial = null;

        while (offset < data.Length)
        {
            if (foundEnd || data.Length - offset < 27 || !HasOggContainerHeader(data[offset..]) || data[offset + 4] != 0)
                return false;

            var headerType = data[offset + 5];
            if ((headerType & ~0x07) != 0 || ((headerType & 0x02) != 0) != (pageCount == 0))
                return false;

            if (((headerType & 0x01) != 0) != packetContinues)
                return false;

            var serial = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 14, 4));
            streamSerial ??= serial;
            if (streamSerial != serial || BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 18, 4)) != (uint) pageCount)
                return false;

            var segments = data[offset + 26];
            var payloadOffset = offset + 27 + segments;
            if (payloadOffset > data.Length)
                return false;

            var payloadLength = 0;
            for (var i = 0; i < segments; i++)
                payloadLength += data[offset + 27 + i];

            if (payloadLength > data.Length - payloadOffset)
                return false;

            var pageLength = 27 + segments + payloadLength;
            if (!HasValidOggChecksum(data.Slice(offset, pageLength)))
                return false;

            var payloadPosition = payloadOffset;
            for (var i = 0; i < segments; i++)
            {
                var segmentLength = data[offset + 27 + i];
                if (!packetContinues)
                {
                    var segment = data.Slice(payloadPosition, segmentLength);
                    if (packetIndex < 3)
                    {
                        var expectedType = (byte) (packetIndex * 2 + 1);
                        if (segment.Length < 7 || segment[0] != expectedType ||
                            !HasVorbisSignature(segment))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (segment.Length == 0)
                            return false;
                        foundAudioPacket = true;
                    }

                    if (packetIndex == 0)
                    {
                        if (segment.Length < 30 || BinaryPrimitives.ReadUInt32LittleEndian(segment[7..]) != 0)
                            return false;

                        var channels = segment[11];
                        sampleRate = BinaryPrimitives.ReadUInt32LittleEndian(segment[12..]);
                        if (channels != 1 || sampleRate is < 8000 or > MaxSampleRate)
                            return false;
                    }
                }

                payloadPosition += segmentLength;
                packetContinues = segmentLength == 255;
                if (!packetContinues)
                    packetIndex++;
            }

            var granule = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(offset + 6, 8));
            if (granule != ulong.MaxValue)
                finalGranule = granule;

            foundEnd = (headerType & 0x04) != 0;
            offset = payloadOffset + payloadLength;
            pageCount++;
        }

        return pageCount > 0 && foundEnd && foundAudioPacket && !packetContinues &&
               sampleRate != 0 && finalGranule > 0 &&
               finalGranule <= (ulong) sampleRate * MaxDurationSeconds &&
               finalGranule <= (ulong) MaxSampleRate * MaxDurationSeconds;
    }

    public static ResPath RelativePath(Guid id) => new($"{id:N}.ogg");

    public static ResPath FullPath(Guid id) => ContentPrefix / RelativePath(id);

    private static bool HasVorbisSignature(ReadOnlySpan<byte> packet) =>
        packet[1] == (byte) 'v' &&
        packet[2] == (byte) 'o' &&
        packet[3] == (byte) 'r' &&
        packet[4] == (byte) 'b' &&
        packet[5] == (byte) 'i' &&
        packet[6] == (byte) 's';

    private static bool HasValidOggChecksum(ReadOnlySpan<byte> page)
    {
        var expected = BinaryPrimitives.ReadUInt32LittleEndian(page.Slice(22, 4));
        uint checksum = 0;
        for (var i = 0; i < page.Length; i++)
        {
            var value = i is >= 22 and < 26 ? (byte) 0 : page[i];
            checksum = checksum << 8 ^ OggCrcLookup[(byte) (checksum >> 24) ^ value];
        }

        return checksum == expected;
    }

    private static readonly uint[] OggCrcLookup = CreateOggCrcLookup();

    private static uint[] CreateOggCrcLookup()
    {
        var lookup = new uint[256];
        for (var i = 0; i < lookup.Length; i++)
        {
            var value = (uint) i << 24;
            for (var bit = 0; bit < 8; bit++)
                value = (value & 0x80000000) != 0 ? value << 1 ^ 0x04C11DB7 : value << 1;
            lookup[i] = value;
        }

        return lookup;
    }
}

public enum CMUCassetteUploadStatus : byte
{
    Success,
    InvalidEntity,
    OutOfRange,
    CassetteFull,
    InvalidFile,
    RoundStorageFull,
    UserStorageFull,
    InvalidPermit,
    RateLimited,
}

public sealed class CMUCassetteUploadPermitRequestMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public NetEntity Cassette;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Cassette = buffer.ReadNetEntity();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Cassette);
    }
}

public sealed class CMUCassetteUploadPermitMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid Permit;
    public NetEntity Cassette;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Permit = buffer.ReadGuid();
        Cassette = buffer.ReadNetEntity();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Permit);
        buffer.Write(Cassette);
    }
}

public sealed class CMUCassetteUploadStartMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public Guid Permit;
    public string Title = string.Empty;
    public int TotalBytes;

    public override int EstimateBufferSize() => 24 + Encoding.UTF8.GetByteCount(Title);

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Permit = buffer.ReadGuid();
        Title = ReadBoundedString(buffer, CMUDynamicCassetteLimits.MaxTitleBytes);
        TotalBytes = buffer.ReadVariableInt32();
        if (TotalBytes <= 0 || TotalBytes > CMUDynamicCassetteLimits.MaxTrackBytes)
            throw new InvalidOperationException($"Cassette payload length {TotalBytes} exceeds the limit {CMUDynamicCassetteLimits.MaxTrackBytes}.");
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Permit);
        WriteBoundedString(buffer, Title, CMUDynamicCassetteLimits.MaxTitleBytes);
        if (TotalBytes <= 0 || TotalBytes > CMUDynamicCassetteLimits.MaxTrackBytes)
            throw new InvalidOperationException($"Cassette payload length {TotalBytes} exceeds the limit {CMUDynamicCassetteLimits.MaxTrackBytes}.");
        buffer.WriteVariableInt32(TotalBytes);
    }

    internal static byte[] ReadBoundedBytes(NetIncomingMessage buffer, int maximum)
    {
        var length = buffer.ReadVariableInt32();
        if (length < 0 || length > maximum)
            throw new InvalidOperationException($"Cassette payload length {length} exceeds the limit {maximum}.");

        buffer.AssertAtLeastSize(length);
        return buffer.ReadBytes(length);
    }

    internal static void WriteBoundedBytes(NetOutgoingMessage buffer, ReadOnlySpan<byte> data, int maximum)
    {
        if (data.Length > maximum)
            throw new InvalidOperationException($"Cassette payload length {data.Length} exceeds the limit {maximum}.");

        buffer.WriteVariableInt32(data.Length);
        buffer.Write(data);
    }

    internal static string ReadBoundedString(NetIncomingMessage buffer, int maximumBytes)
    {
        var bytes = ReadBoundedBytes(buffer, maximumBytes);
        if (!CMUDynamicCassetteLimits.IsValidUtf8(bytes))
            throw new InvalidOperationException("Cassette title contains invalid UTF-8.");

        return Encoding.UTF8.GetString(bytes);
    }

    internal static void WriteBoundedString(NetOutgoingMessage buffer, string value, int maximumBytes)
    {
        if (!CMUDynamicCassetteLimits.IsTitleValid(value))
            throw new InvalidOperationException("Cassette title is invalid.");

        var bytes = Encoding.UTF8.GetBytes(value);
        WriteBoundedBytes(buffer, bytes, maximumBytes);
    }
}

public sealed class CMUCassetteUploadChunkMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public Guid Permit;
    public int Offset;
    public byte[] Data = Array.Empty<byte>();

    public override int EstimateBufferSize() => 26 + Data.Length;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Permit = buffer.ReadGuid();
        Offset = buffer.ReadVariableInt32();
        Data = CMUCassetteUploadStartMsg.ReadBoundedBytes(buffer, CMUDynamicCassetteLimits.MaxUploadChunkBytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Permit);
        buffer.WriteVariableInt32(Offset);
        CMUCassetteUploadStartMsg.WriteBoundedBytes(buffer, Data, CMUDynamicCassetteLimits.MaxUploadChunkBytes);
    }
}

public sealed class CMUCassetteTrackDataMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public Guid TrackId;
    public byte[] Data = Array.Empty<byte>();

    public override int EstimateBufferSize() => 21 + Data.Length;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        TrackId = buffer.ReadGuid();
        Data = CMUCassetteUploadStartMsg.ReadBoundedBytes(buffer, CMUDynamicCassetteLimits.MaxTrackBytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(TrackId);
        CMUCassetteUploadStartMsg.WriteBoundedBytes(buffer, Data, CMUDynamicCassetteLimits.MaxTrackBytes);
    }
}

public sealed class CMUCassetteTrackCacheRequestMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid RequestId;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        RequestId = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(RequestId);
    }
}

public sealed class CMUCassetteTrackCacheMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public Guid RequestId;
    public Guid[] TrackIds = Array.Empty<Guid>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        RequestId = buffer.ReadGuid();
        var count = buffer.ReadByte();
        if (count > CMUDynamicCassetteLimits.MaxTracksPerRound)
            throw new InvalidOperationException($"Cassette track cache count {count} exceeds the limit {CMUDynamicCassetteLimits.MaxTracksPerRound}.");

        buffer.AssertAtLeastSize(count * 16);
        TrackIds = new Guid[count];
        for (var i = 0; i < count; i++)
            TrackIds[i] = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        if (TrackIds.Length > CMUDynamicCassetteLimits.MaxTracksPerRound)
            throw new InvalidOperationException($"Cassette track cache count {TrackIds.Length} exceeds the limit {CMUDynamicCassetteLimits.MaxTracksPerRound}.");

        buffer.Write(RequestId);
        buffer.Write((byte) TrackIds.Length);
        foreach (var trackId in TrackIds)
            buffer.Write(trackId);
    }
}

public sealed class CMUCassetteUploadResultMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public CMUCassetteUploadStatus Status;
    public Guid TrackId;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Status = (CMUCassetteUploadStatus) buffer.ReadByte();
        TrackId = buffer.ReadGuid();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write((byte) Status);
        buffer.Write(TrackId);
    }
}
