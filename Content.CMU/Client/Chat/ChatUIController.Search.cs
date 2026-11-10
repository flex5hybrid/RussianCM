using Content.Client.CMU14.Chat;
using Robust.Shared.Console;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    [Dependency] private IConsoleHost _cmuChatConsole = default!;

    private void CMUInitializeChatSearch()
    {
        _cmuChatConsole.RegisterCommand(CMUChatInputCommands.SearchCommand,
            Loc.GetString("cmu-chat-search-command-description"), CMUChatInputCommands.SearchCommand,
            (_, _, _) => CMUFocusChatSearch());
    }

    private void CMUFocusChatSearch()
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main || !chat.VisibleInTree)
                continue;
            chat.CMUOpenSearch();
            return;
        }
    }
}
