using Content.Server.NPC;
using Robust.Shared.Map;

namespace Content.Server.NPC.HTN.Preconditions;

/// <summary>
/// Precondition that checks if the NPC can perform actions that require gravity.
/// </summary>
public sealed class PreconditionCanActInGravity : HTNPrecondition
{
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// If true, the NPC must have gravity to pass this precondition.
    /// If false, the NPC must be weightless to pass this precondition.
    /// </summary>
    [DataField("requiresGravity")]
    public bool RequiresGravity = true;

    public override bool IsMet(NPCBlackboard blackboard)
    {
        if (!blackboard.TryGetValue<EntityUid>(NPCBlackboard.Owner, out var owner, EntityManager))
            return false;

        var isWeightless = _gravity.IsWeightless(owner);
        
        // If we require gravity, the NPC must NOT be weightless
        // If we don't require gravity, the NPC must be weightless
        return RequiresGravity ? !isWeightless : isWeightless;
    }
}
