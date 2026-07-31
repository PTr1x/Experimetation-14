using System.Linq; // FIXED: Added for LINQ methods
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
    /// Current visualization mode - simplified to only PolarPlot
    /// </summary>
    [AutoNetworkedField]
    [DataField("mode")]
    public VisualizationMode Mode = VisualizationMode.PolarPlot;

    /// <summary>
    /// The entity currently inserted in the disk slot
    /// </summary>
    [AutoNetworkedField] // FIXED: Added for automatic synchronization
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? InsertedDisk;

    /// <summary>
    /// Current research points available for visualization
    /// </summary>
    [AutoNetworkedField]
    [DataField("points")]
    public long Points; // Changed to long for safety

    /// <summary>
    /// Maximum points capacity - reasonable limit
    /// </summary>
    [DataField("maxPoints")]
    public long MaxPoints = 1000000; // Reasonable limit instead of int.MaxValue

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
    /// Technologies by tier - configurable via prototype
    /// </summary>
    [DataField("technologiesByTier")]
    public Dictionary<int, List<string>> TechnologiesByTier = new();

    /// <summary>
    /// Collected technologies from inserted disks - FIXED: Changed to HashSet for O(1) lookups
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<string> CollectedTechnologies = new();
}

/// <summary>
/// Visualization modes - simplified to only PolarPlot
/// </summary>
[Serializable, NetSerializable]
public enum VisualizationMode : byte
{
    PolarPlot
    // TODO: Add other modes (RadialChart, ScatterPlot, BarChart) when implemented
}

/// <summary>
/// UI keys for research area visualizer
/// </summary>
[Serializable, NetSerializable]
public enum ResearchAreaVisualizerUiKey : byte
{
    Key
}

/// <summary>
/// Message for changing visualization mode
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualizerModeChangeMessage : BoundUserInterfaceMessage
{
    public VisualizationMode NewMode;
    public NetEntity VisualizerUid; // Added to fix identification issue

    public VisualizerModeChangeMessage() { }
    
    public VisualizerModeChangeMessage(VisualizationMode newMode, NetEntity visualizerUid)
    {
        NewMode = newMode;
        VisualizerUid = visualizerUid;
    }
}

/// <summary>
/// Message for inserting a data disk
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualizerDiskInsertMessage : BoundUserInterfaceMessage
{
    public EntityUid DiskUid;
    public NetEntity VisualizerUid; // Added to fix identification issue

    public VisualizerDiskInsertMessage() { }
    
    public VisualizerDiskInsertMessage(EntityUid diskUid, NetEntity visualizerUid)
    {
        DiskUid = diskUid;
        VisualizerUid = visualizerUid;
    }
}

/// <summary>
/// Message for ejecting a data disk
/// </summary>
[Serializable, NetSerializable]
public sealed class VisualizerDiskEjectMessage : BoundUserInterfaceMessage
{
    public NetEntity VisualizerUid;

    public VisualizerDiskEjectMessage() { }
    
    public VisualizerDiskEjectMessage(NetEntity visualizerUid)
    {
        VisualizerUid = visualizerUid;
    }
}

/// <summary>
/// UI state for research area visualizer
/// </summary>
[Serializable, NetSerializable]
public sealed class ResearchAreaVisualizerBoundInterfaceState : BoundUserInterfaceState
{
    public VisualizationMode CurrentMode;
    public long CurrentPoints;
    public List<string> CollectedTechnologies; // Convert HashSet to List for serialization
    public Dictionary<int, float> TierWeights;
    public string? InsertedDiskName;

    public ResearchAreaVisualizerBoundInterfaceState(
        VisualizationMode mode,
        long points,
        HashSet<string> collectedTechs,
        Dictionary<int, float> weights,
        string? diskName)
    {
        CurrentMode = mode;
        CurrentPoints = points;
        // FIXED: Convert HashSet to List for serialization using LINQ
        CollectedTechnologies = collectedTechs.ToList();
        TierWeights = weights;
        InsertedDiskName = diskName;
    }
}