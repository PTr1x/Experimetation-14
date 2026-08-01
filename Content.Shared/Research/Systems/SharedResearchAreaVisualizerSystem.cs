using System.Linq;
using Content.Shared.Research.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Random;

namespace Content.Shared.Research.Systems;

public abstract class SharedResearchAreaVisualizerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLog _adminLog = default!;
    [Dependency] protected readonly IRobustRandom _random = default!;

    /// <summary>
    /// Default constants for polar plot calculation.
    /// </summary>
    private const float DefaultRadius = 100f;
    private const float Eccentricity = 0.2f;

    /// <summary>
    /// Try to get the visualizer and validate the player's access.
    /// </summary>
    protected bool TryGetVisualizerAndValidate(
        VisualizerMessage message, 
        EntitySessionEventArgs args, 
        out EntityUid visualizerEntity, 
        out ResearchAreaVisualizerComponent visualizer)
    {
        visualizerEntity = EntityUid.Invalid;
        visualizer = null!;

        var playerEntity = args.SenderSession.AttachedEntity;
        if (playerEntity == null)
        {
            return false;
        }

        if (!TryGetEntity(message.VisualizerUid, out visualizerEntity) ||
            !TryComp<ResearchAreaVisualizerComponent>(visualizerEntity, out visualizer))
        {
            return false;
        }

        if (!HasComp<IInteractionSystem>(playerEntity) ||
            !HasComp<IInteractionSystem>(visualizerEntity))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get random technologies for a given tier.
    /// </summary>
    protected List<string> GetRandomTechsForTier(ResearchAreaVisualizerComponent visualizer, int tier)
    {
        if (tier < 0)
        {
            _adminLog.Add(LogType.Error, LogImpact.Low, $"Invalid tier {tier} requested");
            return GetPlaceholderTechnologies(0);
        }

        if (visualizer.InsertedDisk != null && TryComp<ResearchDataDiskComponent>(visualizer.InsertedDisk.Value, out var disk))
        {
            var validTechs = disk.Technologies?.Where(t => !string.IsNullOrEmpty(t)).ToList();
            
            if (validTechs != null && validTechs.Count > 0)
            {
                int count = Math.Min(3, validTechs.Count);
                return PickRandomElements(validTechs, count);
            }
        }

        if (visualizer.TechnologiesByTier != null &&
            visualizer.TechnologiesByTier.TryGetValue(tier, out var tierTechs) &&
            tierTechs != null &&
            tierTechs.Count > 0)
        {
            int count = Math.Min(3, tierTechs.Count);
            return PickRandomElements(tierTechs, count);
        }

        return GetPlaceholderTechnologies(tier);
    }

    /// <summary>
    /// Pick random elements from a list - manual implementation since PickRandom does not exist
    /// </summary>
    protected List<string> PickRandomElements(List<string> list, int count)
    {
        var result = new List<string>();
        var tempList = new List<string>(list);
        
        for (int i = 0; i < count && tempList.Count > 0; i++)
        {
            int index = _random.Next(tempList.Count);
            result.Add(tempList[index]);
            tempList.RemoveAt(index);
        }
        
        return result;
    }

    /// <summary>
    /// Get placeholder technologies - can be easily replaced later
    /// </summary>
    protected List<string> GetPlaceholderTechnologies(int tier)
    {
        return new List<string> { $"Tier {tier} Technology 1", $"Tier {tier} Technology 2" };
    }

    /// <summary>
    /// Add technologies to visualizer's collected technologies
    /// </summary>
    protected void AddTechnologies(IEnumerable<string> techs, ResearchAreaVisualizerComponent visualizer)
    {
        if (techs == null || visualizer.CollectedTechnologies == null)
            return;

        var validTechs = techs.Where(t => !string.IsNullOrEmpty(t));
        
        foreach (var tech in validTechs)
        {
            visualizer.CollectedTechnologies.Add(tech);
        }
    }

    /// <summary>
    /// Calculate polar plot points based on current points.
    /// </summary>
    protected Dictionary<float, float> CalculatePolarPlotPoints(long currentPoints)
    {
        var points = new Dictionary<float, float>();
        int pointCount = 36;

        for (int i = 0; i < pointCount; i++)
        {
            var theta = (float)(i * (2.0 * Math.PI / (float)pointCount));
            var r = DefaultRadius * (1 + 1.2f * Eccentricity * Math.Pow(Math.Cos(1.5f * theta), 2));
            var scaledR = r * (1 + currentPoints / 100000f);
            points[(float)i] = scaledR;
        }
        
        return points;
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeAllEvents();
    }

    protected virtual void SubscribeAllEvents() { }
}

/// <summary>
/// Base message for visualizer actions.
/// </summary>
public abstract class VisualizerMessage : EntityEventArgs
{
    public NetEntity VisualizerUid { get; }

    protected VisualizerMessage(NetEntity visualizerUid)
    {
        VisualizerUid = visualizerUid;
    }
}

/// <summary>
/// Message for changing visualization mode.
/// </summary>
public sealed class VisualizerModeChangeMessage : VisualizerMessage
{
    public VisualizationMode NewMode { get; }

    public VisualizerModeChangeMessage(VisualizationMode newMode, NetEntity visualizerUid) 
        : base(visualizerUid)
    {
        NewMode = newMode;
    }
}

/// <summary>
/// Message for inserting a disk.
/// </summary>
public sealed class VisualizerDiskInsertMessage : VisualizerMessage
{
    public EntityUid DiskUid { get; }

    public VisualizerDiskInsertMessage(EntityUid diskUid, NetEntity visualizerUid) 
        : base(visualizerUid)
    {
        DiskUid = diskUid;
    }
}

/// <summary>
/// Message for ejecting a disk.
/// </summary>
public sealed class VisualizerDiskEjectMessage : VisualizerMessage
{
    public VisualizerDiskEjectMessage(NetEntity visualizerUid) : base(visualizerUid) { }
}

/// <summary>
/// UI key for the research area visualizer.
/// </summary>
public static class ResearchAreaVisualizerUiKey
{
    public static readonly string Key = "ResearchAreaVisualizer";
}

/// <summary>
/// Bound user interface state for the research area visualizer.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ResearchAreaVisualizerBoundInterfaceState : BoundUserInterfaceState
{
    public VisualizationMode CurrentMode;
    public long CurrentPoints;
    public List<string> CollectedTechnologies;
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
        CollectedTechnologies = collectedTechs?.ToList() ?? new List<string>();
        TierWeights = weights ?? new Dictionary<int, float>();
        InsertedDiskName = diskName;
    }
}