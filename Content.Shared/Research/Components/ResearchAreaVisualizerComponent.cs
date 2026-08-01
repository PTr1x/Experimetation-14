using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Primitive;
using Robust.Shared.Utility;

namespace Content.Shared.Research.Components;

/// <summary>
/// Component for research area visualization.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchAreaVisualizerComponent : Component
{
    /// <summary>
    /// Default maximum points before overflow.
    /// </summary>
    public const long DefaultMaxPoints = 1000000;

    /// <summary>
    /// Current visualization mode.
    /// </summary>
    [DataField, AutoNetworkedField]
    public VisualizationMode Mode = VisualizationMode.PolarPlot;

    /// <summary>
    /// Current points accumulated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public long Points = 0;

    /// <summary>
    /// Maximum points before overflow.
    /// </summary>
    [DataField]
    public long MaxPoints = DefaultMaxPoints;

    /// <summary>
    /// Collected technologies (stored as HashSet for O(1) lookups).
    /// </summary>
    [DataField]
    public HashSet<string> CollectedTechnologies = new();

    /// <summary>
    /// Weights for each tier (tier -> weight).
    /// </summary>
    [DataField]
    public Dictionary<int, float> TierWeights = new();

    /// <summary>
    /// Technologies available for each tier.
    /// </summary>
    [DataField]
    public Dictionary<int, List<string>> TechnologiesByTier = new();

    /// <summary>
    /// Currently inserted research disk.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? InsertedDisk;

    /// <summary>
    /// Name of the inserted disk (cached for UI).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? InsertedDiskName;
}

/// <summary>
/// Visualization modes for the research area visualizer.
/// </summary>
public enum VisualizationMode
{
    PolarPlot,
    CartesianPlot,
    ScatterPlot
}