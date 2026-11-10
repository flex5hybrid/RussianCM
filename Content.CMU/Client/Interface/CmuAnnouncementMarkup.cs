using Robust.Shared.Utility;

namespace Content.Client.CMU14.Interface;

/// <summary>Each announcement line must be independently balanced before a font wrapper is added.</summary>
public static class CmuAnnouncementMarkup
{
    public static FormattedMessage Balance(FormattedMessage original)
    {
        var output = new FormattedMessage(original.Count);
        var open = new List<string>();
        foreach (var node in original)
        {
            if (node.Name == null)
            {
                output.PushTag(node);
                continue;
            }
            if (!node.Closing)
            {
                output.PushTag(node);
                open.Add(node.Name);
                continue;
            }
            var matching = open.LastIndexOf(node.Name);
            if (matching < 0)
                continue; // A tag opened on another line cannot close this line's outer font.
            while (open.Count > matching)
            {
                output.Pop();
                open.RemoveAt(open.Count - 1);
            }
        }
        while (open.Count > 0)
        {
            output.Pop();
            open.RemoveAt(open.Count - 1);
        }
        return output;
    }
}
