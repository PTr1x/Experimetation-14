using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components;

/// <summary>
/// Component for data disks that store research points for custom research console
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchDataDiskComponent : Component
{
    /// <summary>
    /// The amount of research points stored on this disk
    /// </summary>
    [AutoNetworkedField]
    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points;

    /// <summary>
    /// The tier of this data disk (1-3)
    /// </summary>
    [AutoNetworkedField]
    [DataField("tier"), ViewVariables(VVAccess.ReadWrite)]
    public int Tier = 1;

    /// <summary>
    /// The discipline this disk is associated with
    /// </summary>
    [AutoNetworkedField]
    [DataField("discipline")]
    public string? Discipline;

    /// <summary>
    /// Whether this disk has been used
    /// </summary>
    [AutoNetworkedField]
    [DataField("used"), ViewVariables(VVAccess.ReadWrite)]
    public bool Used = false;
}

/// <summary>
/// Event raised when a data disk is inserted into a research console
/// </summary>
[ByRefEvent]
public readonly record struct ResearchDataDiskInsertedEvent(
    EntityUid Disk,
    EntityUid Console,
    int PointsAdded);
