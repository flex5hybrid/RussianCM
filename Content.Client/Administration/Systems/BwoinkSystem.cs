#nullable enable
using Content.Shared.Administration;
using Content.Shared.Administration.AdminAI; // CMU14
using JetBrains.Annotations;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Client.Administration.Systems
{
    [UsedImplicitly]
    public sealed partial class BwoinkSystem : SharedBwoinkSystem
    {
        [Dependency] private IGameTiming _timing = default!;

        public event EventHandler<BwoinkTextMessage>? OnBwoinkTextMessageRecieved;
        public event Action<AdminAIDraftState>? OnAdminAIDraftState; // CMU14
        public event Action<AdminAICaseState>? OnAdminAICaseState; // CMU14
        public event Action? OnAdminAIDraftReset; // CMU14
        private (TimeSpan Timestamp, bool Typing) _lastTypingUpdateSent;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeNetworkEvent<AdminAIDraftState>((state, _) => OnAdminAIDraftState?.Invoke(state)); // CMU14
            SubscribeNetworkEvent<AdminAICaseState>((state, _) => OnAdminAICaseState?.Invoke(state)); // CMU14
            SubscribeNetworkEvent<AdminAIDraftReset>((_, _) => OnAdminAIDraftReset?.Invoke()); // CMU14
        }

        public void RequestAIDraft(AdminAIDraftRequest request) => RaiseNetworkEvent(request); // CMU14
        public void RequestAIContext(AdminAIContextRequest request) => RaiseNetworkEvent(request); // CMU14

        protected override void OnBwoinkTextMessage(BwoinkTextMessage message, EntitySessionEventArgs eventArgs)
        {
            OnBwoinkTextMessageRecieved?.Invoke(this, message);
        }

        public void Send(NetUserId channelId, string text, bool playSound, bool adminOnly)
        {
            // Reuse the channel ID as the 'true sender'.
            // Server will ignore this and if someone makes it not ignore this (which is bad, allows impersonation!!!), that will help.
            RaiseNetworkEvent(new BwoinkTextMessage(channelId, channelId, text, playSound: playSound, adminOnly: adminOnly));
            SendInputTextUpdated(channelId, false);
        }

        public void SendInputTextUpdated(NetUserId channel, bool typing)
        {
            if (_lastTypingUpdateSent.Typing == typing &&
                _lastTypingUpdateSent.Timestamp + TimeSpan.FromSeconds(1) > _timing.RealTime)
            {
                return;
            }

            _lastTypingUpdateSent = (_timing.RealTime, typing);
            RaiseNetworkEvent(new BwoinkClientTypingUpdated(channel, typing));
        }
    }
}
