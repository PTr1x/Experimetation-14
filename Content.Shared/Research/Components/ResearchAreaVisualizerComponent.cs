using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components;

/// <summary>
/// Component for the research area visualizer machine
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchAreaVisualizerComponent : Component
{
    /// <summary>
    /// Current visualization mode
    /// </summary>
    [AutoNetworkedField]
    [DataField("visualizationMode"), ViewVariables(VVAccess.ReadWrite)]
    public VisualizationMode Mode = VisualizationMode.PolarPlot;

    /// <summary>
    /// The entity currently inserted in the disk slot
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? InsertedDisk;

    /// <summary>
    /// Current research points available for visualization
    /// </summary>
    [AutoNetworkedField]
    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points;

    /// <summary>
    /// Tier distribution weights (Tier1: 25%, Tier2: 50%, Tier3: 25%)
    /// </summary>
    [DataField("tierWeights")]
    public Dictionary<int, float> TierWeights = new()
    {
        {1, 0.25f},
        {2, 0.50f},
        {3, 0.25f}
    };

    /// <summary>
    /// Randomized tech placements by tier
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<int, List<string>> TechPlacementsByTier = new();
}

/// <summary>
/// Visualization modes for the research area visualizer
/// </summary>
[Serializable, NetSerializable]
public enum VisualizationMode : byte
{
    PolarPlot,
    RadialChart,
    ScatterPlot,
    BarChart
}

/// <summary>
/// UI keys for research area visualizer
/// </summary>
[Serializable, NetSerializable]
public enum ResearchAreaVisualizerUiKey : byte
{
    Key,
    ModeSelection,
    DataDisplay
}

/// <summary>
/// Message for changing visualization mode
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualizerModeChangeMessage : BoundUserInterfaceMessage
{
    public VisualizationMode NewMode;

    public VisualizerModeChangeMessage(VisualizationMode newMode)
    {
        NewMode = newMode;
    }
}

/// <summary>
/// Message for inserting a data disk
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualizerDiskInsertMessage : BoundUserInterfaceMessage
{
    public EntityUid DiskUid;

    public VisualizerDiskInsertMessage(EntityUid diskUid)
    {
        DiskUid = diskUid;
    }
}

/// <summary>
/// UI state for research area visualizer
/// </summary>
[Serializable, NetSerializable]
public sealed class ResearchAreaVisualizerBoundInterfaceState : BoundUserInterfaceState
{
    public VisualizationMode CurrentMode;
    public int CurrentPoints;
    public Dictionary<int, List<string>> TechPlacements;
    public Dictionary<int, float> TierWeights;
    public string? InsertedDiskName;

    public ResearchAreaVisualizerBoundInterfaceState(
        VisualizationMode mode,
        int points,
        Dictionary<int, List<string>> placements,
        Dictionary<int, float> weights,
        string? diskName)
    {
        CurrentMode = mode;
        CurrentPoints = points;
        TechPlacements = placements;
        TierWeights = weights;
        InsertedDiskName = diskName;
    }
}
