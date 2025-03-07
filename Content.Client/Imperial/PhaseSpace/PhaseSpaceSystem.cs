using Content.Shared.Imperial.PhaseSpace;

namespace Content.Client.Imperial.PhaseSpace;

/// <summary>
/// Responsible for shadow spawning on the client.
/// Don't move this to Shared. Rendering logic should be client-side only.
/// </summary>
public sealed partial class PhaseSpaceSystem : SharedPhaseSpaceSystem;
