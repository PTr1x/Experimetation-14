using Robust.Shared.GameStates;

namespace Content.Server.NPC.Components;

/// <summary>
/// Added to NPCs that can move in weightless (zero gravity) environments.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class CanMoveInWeightlessComponent : Component
{
}