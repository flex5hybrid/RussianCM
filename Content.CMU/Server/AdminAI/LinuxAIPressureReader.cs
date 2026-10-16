using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Read-only global and optional cgroup v2 PSI. Always called on a background task.</summary>
public sealed class LinuxAIPressureReader
{
    public static bool TryParseAvg10(string text, string category, out double value)
    {
        value = 0;
        foreach (var line in text.Split('\n'))
        {
            var fields = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length == 0 || fields[0] != category) continue;
            foreach (var field in fields)
            {
                if (!field.StartsWith("avg10=", StringComparison.Ordinal)) continue;
                return double.TryParse(field.AsSpan(6), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value)
                    && double.IsFinite(value) && value is >= 0 and <= 100;
            }
        }
        return false;
    }

    public static bool IsValidCgroupPath(string path) => path.StartsWith("/sys/fs/cgroup/", StringComparison.Ordinal)
        && !path.Contains("..", StringComparison.Ordinal) && !path.Contains('\0') && !path.Contains('\n')
        && !path.Contains('\r') && !path.Contains('\\') && path.Length is > 15 and <= 512
        && !Array.Exists(path.ToCharArray(), char.IsControl);

    public async Task<AILoadSample?> ReadAsync(string[] cgroups, TimeSpan now, double? simulationRatio, CancellationToken token)
    {
        if (!OperatingSystem.IsLinux() || simulationRatio == null || cgroups.Length > 4) return null;
        try
        {
            var cpu = await ReadValue("/proc/pressure/cpu", "some", token).ConfigureAwait(false);
            var memory = await ReadValue("/proc/pressure/memory", "full", token).ConfigureAwait(false);
            var io = await ReadValue("/proc/pressure/io", "full", token).ConfigureAwait(false);
            foreach (var group in cgroups)
            {
                if (!IsValidCgroupPath(group)) return null;
                cpu = Math.Max(cpu, await ReadValue(group + "/cpu.pressure", "some", token).ConfigureAwait(false));
                memory = Math.Max(memory, await ReadValue(group + "/memory.pressure", "full", token).ConfigureAwait(false));
                io = Math.Max(io, await ReadValue(group + "/io.pressure", "full", token).ConfigureAwait(false));
            }
            return new(now, cpu, memory, io, simulationRatio.Value);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or FormatException)
        { return null; }
    }

    private static async Task<double> ReadValue(string path, string category, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 512, true);
        var buffer = new byte[4097];
        var count = 0;
        while (count < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(count), token).ConfigureAwait(false);
            if (read == 0) break;
            count += read;
        }
        if (count > 4096 || !TryParseAvg10(Encoding.ASCII.GetString(buffer, 0, count), category, out var value))
            throw new FormatException("Invalid or oversized PSI sample.");
        return value;
    }
}
