using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.Medieval.Magic;


/// <summary>
/// DoAfter parameters that are required to cast the spell
/// </summary>
[ImplicitDataDefinitionForInheritors, Serializable, NetSerializable]
public sealed partial class MedievalSpellDoAfterArgs
{
    /// <summary>
    ///     How long does the do_after require to complete
    /// </summary>
    [DataField(required: true)]
    public TimeSpan Delay;

    /// <summary>
    /// Whether the progress bar for this DoAfter should be hidden from other players.
    /// </summary>
    [DataField]
    public bool Hidden = false;

    // Break the chains
    /// <summary>
    ///     Whether or not this do after requires the user to have hands.
    /// </summary>
    [DataField]
    public bool NeedHand = false;

    /// <summary>
    ///     Whether we need to keep our active hand as is (i.e. can't change hand or change item). This also covers
    ///     requiring the hand to be free (if applicable). This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    ///     Whether the do-after should get interrupted if we drop the
    ///     active item we started the do-after with
    ///     This does nothing if <see cref="NeedHand"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnDropItem = true;

    /// <summary>
    ///     If do_after stops when the user or target moves
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    ///     Whether to break on movement when the user is weightless.
    ///     This does nothing if <see cref="BreakOnMove"/> is false.
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = false;

    /// <summary>
    ///     Threshold for user and target movement
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    ///     Threshold for distance user from the used OR target entities.
    /// </summary>
    [DataField]
    public float? DistanceThreshold = null;

    /// <summary>
    ///     Whether damage will cancel the DoAfter. See also <see cref="DamageThreshold"/>.
    /// </summary>
    [DataField]
    public bool BreakOnDamage = false;

    /// <summary>
    ///     Threshold for user damage. This damage has to be dealt in a single event, not over time.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;

    /// <summary>
    ///     If true, this DoAfter will be canceled if the user can no longer interact with the target.
    /// </summary>
    [DataField]
    public bool RequireCanInteract = true;

    /// <summary>
    /// Speed modifier when cast spell
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.0f;

    /// <summary>
    ///     If true, this will prevent duplicate DoAfters from being started See also <see cref="DuplicateConditions"/>.
    /// </summary>
    /// <remarks>
    ///     Note that this will block even if the duplicate is cancelled because either DoAfter had
    ///     <see cref="CancelDuplicate"/> enabled.
    /// </remarks>
    [DataField]
    public bool BlockDuplicate = false;

    //TODO: User pref to not cancel on second use on specific doafters
    /// <summary>
    ///     If true, this will cancel any duplicate DoAfters when attempting to add a new DoAfter. See also
    ///     <see cref="DuplicateConditions"/>.
    /// </summary>
    [DataField]
    public bool CancelDuplicate = false;

    /// <summary>
    ///     These flags determine what DoAfter properties are used to determine whether one DoAfter is a duplicate of
    ///     another.
    /// </summary>
    /// <remarks>
    ///     Note that both DoAfters may have their own conditions, and they will be considered duplicated if either set
    ///     of conditions is satisfied.
    /// </remarks>
    [DataField]
    public DuplicateConditions DuplicateCondition = DuplicateConditions.All;

    public void CopyToDoAfter(ref DoAfterArgs doAfter)
    {
        doAfter.Delay = Delay;
        doAfter.Hidden = Hidden;
        doAfter.NeedHand = NeedHand;
        doAfter.BreakOnHandChange = BreakOnHandChange;
        doAfter.BreakOnDropItem = BreakOnDropItem;
        doAfter.BreakOnMove = BreakOnMove;
        doAfter.BreakOnWeightlessMove = BreakOnWeightlessMove;
        doAfter.MovementThreshold = MovementThreshold;
        doAfter.DistanceThreshold = DistanceThreshold;
        doAfter.BreakOnDamage = BreakOnDamage;
        doAfter.DamageThreshold = DamageThreshold;
        doAfter.RequireCanInteract = RequireCanInteract;
        doAfter.BlockDuplicate = BlockDuplicate;
        doAfter.CancelDuplicate = CancelDuplicate;
        doAfter.DuplicateCondition = DuplicateCondition;
    }
}
