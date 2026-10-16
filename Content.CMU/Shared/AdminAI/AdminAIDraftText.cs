using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Content.Shared.Administration.AdminAI;

/// <summary>Plain editor text only; untrusted output never supplies clickable links, commands or rich markup.</summary>
public static class AdminAIDraftText
{
    private static readonly Regex Links = new(@"(?:https?://|www\.)\S+", RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
    public const int MaxDraftChars = 4096;
    public static string Sanitize(string text)
    {
        if (text.Length > MaxDraftChars) return "";
        try { text = Links.Replace(text, "(ссылка удалена)"); }
        catch (RegexMatchTimeoutException) { return ""; }
        var plain = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Format or UnicodeCategory.PrivateUse
                || category == UnicodeCategory.Control && rune.Value is not (10 or 9)) continue;
            if (rune.Value == '[') plain.Append('(');
            else if (rune.Value == ']') plain.Append(')');
            else plain.Append(rune.ToString());
        }
        return plain.ToString().Trim();
    }
}
