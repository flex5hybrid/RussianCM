using System.Numerics;
using Content.Shared.CMU14.Movement; // CMU14
using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Timing;

namespace Content.Shared.Movement.Components
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class InputMoverComponent : Component
    {
        // This class has to be able to handle server TPS being lower than client FPS.
        // While still having perfectly responsive movement client side.
        // We do this by keeping track of the exact sub-tick values that inputs are pressed on the client,
        // and then building a total movement vector based on those sub-tick steps.
        //
        // We keep track of the last sub-tick a movement input came in,
        // Then when a new input comes in, we calculate the fraction of the tick the LAST input was active for
        //   (new sub-tick - last sub-tick)
        // and then add to the total-this-tick movement vector
        // by multiplying that fraction by the movement direction for the last input.
        // This allows us to incrementally build the movement vector for the current tick,
        // without having to keep track of some kind of list of inputs and calculating it later.
        //
        // We have to keep track of a separate movement vector for walking and sprinting,
        // since we don't actually know our current movement speed while processing inputs.
        // We change which vector we write into based on whether we were sprinting after the previous input.
        //   (well maybe we do but the code is designed such that MoverSystem applies movement speed)
        //   (and I'm not changing that)

        public GameTick LastInputTick;
        public ushort LastInputSubTick;

        public Vector2 CurTickWalkMovement;
        public Vector2 CurTickSprintMovement;

        [ViewVariables]
        public MoveButtons HeldMoveButtons = MoveButtons.None;

        /// <summary>
        /// Does our input indicate actual movement, and not just modifiers?
        /// </summary>
        /// <remarks>
        /// This can be useful to filter out input from just pressing the walk button with no directions, for example.
        /// </remarks>
        [ViewVariables]
        public bool HasDirectionalMovement => (HeldMoveButtons & MoveButtons.AnyDirection) != MoveButtons.None;

        // I don't know if we even need this networked? It's mostly so conveyors can calculate properly.
        /// <summary>
        /// Direction to move this tick.
        /// </summary>
        [ViewVariables]
        public Vector2 WishDir;

        /// <summary>
        /// Entity our movement is relative to.
        /// </summary>
        public EntityUid? RelativeEntity;

        /// <summary>
        /// Although our movement might be relative to a particular entity we may have an additional relative rotation
        /// e.g. if we've snapped to a different cardinal direction
        /// </summary>
        [ViewVariables]
        public Angle TargetRelativeRotation = Angle.Zero;

        /// <summary>
        /// The current relative rotation. This will lerp towards the <see cref="TargetRelativeRotation"/>.
        /// </summary>
        [ViewVariables]
        public Angle RelativeRotation;

        /// <summary>
        /// If we traverse on / off a grid then set a timer to update our relative inputs.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
        public TimeSpan LerpTarget;

        public const float LerpTime = 1.0f;
        public const float SprintingSoundModifier = 3.5f;
        public const float WalkingSoundModifier = 1.5f;

        // CMU14 Human movement Begin
        public bool SprintOnModifier;
        public bool SprintBlocked;
        public bool QuietWalk;
        public CMUHumanMovementPace HumanWalkPace;

        public float HumanWalkSpeedMultiplier => HumanWalkPace switch
        {
            CMUHumanMovementPace.Quiet => 0.60f,
            CMUHumanMovementPace.Tactical => 0.82f,
            _ => 1f,
        };

        public float HumanFootstepVolumeOffset => Sprinting ? 0f : HumanWalkPace switch
        {
            CMUHumanMovementPace.Quiet => -8f,
            CMUHumanMovementPace.Tactical => -2f,
            _ => 0f,
        };

        public float GetFootstepDistance(MobMoverComponent mover)
        {
            if (Sprinting)
                return mover.StepSoundMoveDistanceRunning;

            var spacing = HumanWalkPace switch
            {
                CMUHumanMovementPace.Quiet => 0.82f,
                CMUHumanMovementPace.Tactical => 0.90f,
                _ => 1f,
            };
            return mover.StepSoundMoveDistanceWalking * spacing;
        }

        [ViewVariables]
        public bool Sprinting
            => !SprintBlocked && SprintOnModifier == ((HeldMoveButtons & MoveButtons.Walk) != 0);
        // CMU14 End

        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanMove = true;
    }

    [Serializable, NetSerializable]
    public sealed class InputMoverComponentState : ComponentState
    {
        public MoveButtons HeldMoveButtons;
        public NetEntity? RelativeEntity;
        public Angle TargetRelativeRotation;
        public Angle RelativeRotation;
        public TimeSpan LerpTarget;
        public bool CanMove;
        // CMU14 Human movement Begin
        public bool SprintOnModifier;
        public bool SprintBlocked;
        public bool QuietWalk;
        public CMUHumanMovementPace HumanWalkPace;
        // CMU14 End
    }
}
