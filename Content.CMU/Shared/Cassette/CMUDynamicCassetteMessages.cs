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
    public const int MaxTitleBytes = 128;
    public const int MaxRoundBytes = 128 * 1024 * 1024;
    public const int MaxUserBytesPerRound = 24 * 1024 * 1024;
    public const int MaxTracksPerRound = 64;
    public const int MaxDurationSeconds = 15 * 60;
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

    /// <summary>
    /// Выполняет структурную проверку без выделения памяти до передачи недоверенных данных аудиодекодеру. /
    /// Performs an allocation-free structural check before untrusted data reaches the audio decoder.
    /// Принимается только Vorbis в OGG, а длительность ограничена для защиты от декомпрессионных бомб. /
    /// Only Vorbis-in-Ogg is accepted, with bounded duration to mitigate decompression bombs.
    /// </summary>
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
                        if (channels is < 1 or > 2 || sampleRate is < 8000 or > 192000)
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
               finalGranule <= (ulong) sampleRate * MaxDurationSeconds;
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
            checksum ^= (uint) value << 24;
            for (var bit = 0; bit < 8; bit++)
                checksum = (checksum & 0x80000000) != 0
                    ? (checksum << 1) ^ 0x04C11DB7
                    : checksum << 1;
        }

        return checksum == expected;
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

/// <summary>
/// Запрос одноразового разрешения на загрузку для кассеты в руках игрока. /
/// Requests a one-use upload permit for a cassette held by the player.
/// </summary>
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

/// <summary>
/// Короткоживущее одноразовое разрешение от серверной системы записывающей машинки. /
/// Short-lived one-use authorization issued by the server-side recorder system.
/// </summary>
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

/// <summary>
/// Запрос клиента на добавление одного OGG-трека в записываемую кассету. /
/// Client request to append one OGG track to a recordable cassette.
/// </summary>
public sealed class CMUCassetteUploadMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public Guid Permit;
    public string Title = string.Empty;
    public byte[] Data = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        Permit = buffer.ReadGuid();
        Title = ReadBoundedString(buffer, CMUDynamicCassetteLimits.MaxTitleBytes);
        Data = ReadBoundedBytes(buffer, CMUDynamicCassetteLimits.MaxTrackBytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Permit);
        WriteBoundedString(buffer, Title, CMUDynamicCassetteLimits.MaxTitleBytes);
        WriteBoundedBytes(buffer, Data, CMUDynamicCassetteLimits.MaxTrackBytes);
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

    private static string ReadBoundedString(NetIncomingMessage buffer, int maximumBytes)
    {
        var bytes = ReadBoundedBytes(buffer, maximumBytes);
        if (!CMUDynamicCassetteLimits.IsValidUtf8(bytes))
            throw new InvalidOperationException("Cassette title contains invalid UTF-8.");

        return Encoding.UTF8.GetString(bytes);
    }

    private static void WriteBoundedString(NetOutgoingMessage buffer, string value, int maximumBytes)
    {
        if (!CMUDynamicCassetteLimits.IsTitleValid(value))
            throw new InvalidOperationException("Cassette title is invalid.");

        var bytes = Encoding.UTF8.GetBytes(value);
        WriteBoundedBytes(buffer, bytes, maximumBytes);
    }
}

/// <summary>
/// Передача клиенту проверенного трека, существующего только в текущем раунде. /
/// Delivery of a validated, round-scoped track to a client.
/// </summary>
public sealed class CMUCassetteTrackDataMsg : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableOrdered;

    public Guid TrackId;
    public byte[] Data = Array.Empty<byte>();

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        TrackId = buffer.ReadGuid();
        Data = CMUCassetteUploadMsg.ReadBoundedBytes(buffer, CMUDynamicCassetteLimits.MaxTrackBytes);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(TrackId);
        CMUCassetteUploadMsg.WriteBoundedBytes(buffer, Data, CMUDynamicCassetteLimits.MaxTrackBytes);
    }
}

/// <summary>
/// Ответ сервера существующему интерфейсу записи кассеты. /
/// Server acknowledgement for the existing cassette recording UI.
/// </summary>
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
