using System.Globalization;
using System.Numerics;

namespace Content.Client.CMU14.Interface.Hud;

/// <summary>Client-only normalized placement, separately archived for each native HUD layout.</summary>
public sealed class CmuHudLayoutSettings
{
    public readonly Dictionary<string, Placement> Items = new();
    public readonly record struct Placement(float X, float Y, float Width, float Height);

    public static CmuHudLayoutSettings Parse(string text)
    {
        var result = new CmuHudLayoutSettings();
        foreach (var row in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var cells = row.Split(':');
            if (cells.Length != 5 || cells[0].Length > 180)
                continue;
            var numbers = new float[4];
            var valid = true;
            for (var i = 0; i < 4; i++)
                valid &= float.TryParse(cells[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out numbers[i])
                    && float.IsFinite(numbers[i]);
            if (valid)
                result.Items[cells[0]] = new Placement(Math.Clamp(numbers[0], 0, 1), Math.Clamp(numbers[1], 0, 1),
                    Math.Clamp(numbers[2], 0, 4096), Math.Clamp(numbers[3], 0, 4096));
        }
        return result;
    }

    public string Serialize()
    {
        var rows = new List<string>();
        foreach (var (key, value) in Items)
            rows.Add(string.Join(":", new string[] { key, value.X.ToString("R", CultureInfo.InvariantCulture),
                value.Y.ToString("R", CultureInfo.InvariantCulture), value.Width.ToString("R", CultureInfo.InvariantCulture),
                value.Height.ToString("R", CultureInfo.InvariantCulture) }));
        return string.Join(";", rows);
    }

    public static UIBox2 Resolve(Placement placement, Vector2 available, Vector2 natural)
    {
        available = Vector2.Max(Vector2.One, available);
        var size = Vector2.Clamp(new Vector2(placement.Width > 0 ? placement.Width : natural.X,
            placement.Height > 0 ? placement.Height : natural.Y), Vector2.One, available);
        var position = Vector2.Max(Vector2.Zero, available - size) * new Vector2(placement.X, placement.Y);
        return UIBox2.FromDimensions(position, size);
    }
}
