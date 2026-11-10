using Robust.Shared.Input;

namespace Content.Client.CMU14.Chat;

/// <summary>Local console bindings. Deliberately excluded from the network key-function map.</summary>
public static class CMUChatInputCommands
{
    public const string SearchCommand = "cmu_chat_search";
    public static readonly BoundKeyFunction Search = SearchCommand;
}
