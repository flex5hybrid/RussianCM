using Content.Client.Chat.Managers;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Chat;
using Content.Shared.CMU14.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;
using Robust.Shared.Input.Binding;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.PhraseBinder;

[UsedImplicitly]
public sealed class PhraseBinderUIController : UIController, IOnStateChanged<GameplayState>
{
    [Dependency] private IResourceManager _resources = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IPlayerManager _player = default!;

    private static readonly ResPath SavePath = new("/cmu_phrase_binder.yml");
    private PhraseWheelMenu? _wheel;
    private PhraseBinderWindow? _editor;

    public void OnStateEntered(GameplayState state)
    {
        CommandBinds.Builder
            .Bind(CMUKeyFunctions.CMUOpenPhraseWheel, InputCmdHandler.FromDelegate(_ => ToggleWheel()))
            .Register<PhraseBinderUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        CommandBinds.Unregister<PhraseBinderUIController>();
        _wheel?.Close();
        _editor?.Close();
    }

    public void OpenEditor()
    {
        _wheel?.Close();
        if (_editor != null)
        {
            _editor.MoveToFront();
            return;
        }

        var editor = new PhraseBinderWindow(Load(), UIManager.GetUIController<ChatUIController>().MaxMessageLength);
        editor.SaveRequested += Save;
        editor.OnClose += () =>
        {
            editor.SaveRequested -= Save;
            _editor = null;
        };
        _editor = editor;
        editor.OpenCentered();
    }

    private void ToggleWheel()
    {
        if (_wheel != null)
        {
            _wheel.Close();
            return;
        }

        var wheel = new PhraseWheelMenu(Load(), Send, OpenEditor);
        wheel.OnClose += () =>
        {
            _wheel = null;
        };
        _wheel = wheel;
        wheel.OpenCentered();
    }

    private void Send(BoundPhrase phrase)
    {
        var chatUi = UIManager.GetUIController<ChatUIController>();
        if (_player.LocalEntity == null || !phrase.CanSend(chatUi.CanSendChannels, chatUi.MaxMessageLength))
            return;

        // The normal chat manager escapes the text and keeps server speech/rate-limit checks intact.
        _chat.SendMessage(phrase.MessageText, phrase.ChatChannel);
    }

    private BoundPhrase[] Load()
    {
        try
        {
            return _resources.UserData.TryReadAllText(SavePath, out var text)
                ? PhraseBinderData.Deserialize(text)
                : PhraseBinderData.EmptySlots();
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load phrase binder: {e}");
            return PhraseBinderData.EmptySlots();
        }
    }

    private bool Save(BoundPhrase[] slots)
    {
        try
        {
            _resources.UserData.WriteAllText(SavePath, PhraseBinderData.Serialize(slots));
            return true;
        }
        catch (Exception e)
        {
            Log.Error($"Failed to save phrase binder: {e}");
            return false;
        }
    }
}
