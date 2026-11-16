using System;
using System.Buffers.Binary;
using System.Linq;
using Content.Shared.CMU14.Cassette;
using NUnit.Framework;

namespace Content.Tests.CMU14.Cassette;

[TestFixture]
public sealed class CMUDynamicCassetteValidationTest
{
    [Test]
    public void AcceptsBoundedVorbisStream()
    {
        Assert.That(CMUDynamicCassetteLimits.IsValidOggVorbis(MakeStream(60)), Is.True);
    }

    [Test]
    public void RejectsMalformedOrIncompleteStreams()
    {
        var valid = MakeStream(60);
        var invalidMagic = (byte[]) valid.Clone();
        invalidMagic[0] = (byte) 'X';
        var missingSetupHeader = (byte[]) valid.Clone();
        missingSetupHeader[31 + 30 + 7] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(missingSetupHeader.AsSpan(22), OggChecksum(missingSetupHeader));

        Assert.Multiple(() =>
        {
            Assert.That(CMUDynamicCassetteLimits.IsValidOggVorbis(invalidMagic), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsValidOggVorbis(missingSetupHeader), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsValidOggVorbis(valid[..^1]), Is.False);
        });
    }

    [Test]
    public void RejectsExcessiveDecodedDuration()
    {
        Assert.That(CMUDynamicCassetteLimits.IsValidOggVorbis(
            MakeStream(CMUDynamicCassetteLimits.MaxDurationSeconds + 1)), Is.False);
    }

    [Test]
    public void TitleLimitUsesUtf8Bytes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid("track"), Is.True);
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid(new string('a', 128)), Is.True);
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid(new string('я', 64)), Is.True);
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid(string.Empty), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid(new string('я', 65)), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsTitleValid("track\uD800"), Is.False);
        });
    }

    [Test]
    public void StrictUtf8ValidationRejectsInvalidSequences()
    {
        Assert.Multiple(() =>
        {
            Assert.That(CMUDynamicCassetteLimits.IsValidUtf8("кассета"u8), Is.True);
            Assert.That(CMUDynamicCassetteLimits.IsValidUtf8([0xC0, 0xAF]), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsValidUtf8([0xED, 0xA0, 0x80]), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsValidUtf8([0xF4, 0x90, 0x80, 0x80]), Is.False);
            Assert.That(CMUDynamicCassetteLimits.IsValidUtf8([0xE2, 0x82]), Is.False);
        });
    }

    private static byte[] MakeStream(int seconds)
    {
        const uint sampleRate = 48000;
        int[] packetLengths = [30, 7, 7, 1];
        var data = new byte[27 + packetLengths.Length + packetLengths.Sum()];
        "OggS"u8.CopyTo(data);
        data[5] = 0x06; // Начало и конец потока. / Beginning and end of stream.
        BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(6), (ulong) sampleRate * (uint) seconds);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(14), 0xC455E77E);
        data[26] = (byte) packetLengths.Length;

        for (var i = 0; i < packetLengths.Length; i++)
            data[27 + i] = (byte) packetLengths[i];

        var payload = 27 + packetLengths.Length;
        data[payload] = 1;
        "vorbis"u8.CopyTo(data.AsSpan(payload + 1));
        data[payload + 11] = 2;
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(payload + 12), sampleRate);

        payload += 30;
        data[payload] = 3;
        "vorbis"u8.CopyTo(data.AsSpan(payload + 1));
        payload += 7;
        data[payload] = 5;
        "vorbis"u8.CopyTo(data.AsSpan(payload + 1));
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(22), OggChecksum(data));
        return data;
    }

    private static uint OggChecksum(byte[] page)
    {
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

        return checksum;
    }
}
