using Content.Server.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.CMU14.AdminAI;

/// <summary>Read-only public round data. No round-control or game-action API.</summary>
public interface IAdminAIRoundContext
{
    int CurrentRoundId { get; }
    string? PublicMapDescription => null;
}
public sealed partial class AdminAIRoundContext : IAdminAIRoundContext
{
    [Dependency] private IEntitySystemManager _systems = default!;
    public int CurrentRoundId => GameTicker.GetRoundId(_systems);
    public string? PublicMapDescription => _systems.GetEntitySystem<GameTicker>().GetPublicAIMapDescription();
}
