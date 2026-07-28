using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components;

/// <summary>
/// Component for data disks that store research points for visualizer
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchDataDiskComponent : Component
{
    /// <summary>
    /// The amount of research points stored on this disk
    /// </summary>
    [AutoNetworkedField]
    [DataField("points")]
    public long Points; // Changed to long for safety

    /// <summary>
    /// The tier of this data disk (1-3)
    /// </summary>
    [AutoNetworkedField]
    [DataField("tier")]
    public int Tier = 1;

    /// <summary>
    /// Technologies available on this disk
    /// </summary>
    [DataField("technologies")]
    public List<string> Technologies = new();

    /// <summary>
    /// Whether this disk has been used
    /// </summary>
    [AutoNetworkedField]
    [DataField("used")]
    public bool Used;
}